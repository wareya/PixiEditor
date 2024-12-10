﻿using PixiEditor.ChangeableDocument.Changeables.Graph.Nodes;
using PixiEditor.ChangeableDocument.Changeables.Interfaces;
using PixiEditor.ChangeableDocument.Changes.Structure;
using PixiEditor.ChangeableDocument.Rendering;
using Drawie.Backend.Core;
using Drawie.Backend.Core.Bridge;
using Drawie.Backend.Core.Numerics;
using Drawie.Backend.Core.Vector;
using Drawie.Numerics;
using PixiEditor.ChangeableDocument.Changeables.Animations;
using PixiEditor.ChangeableDocument.Changeables.Graph.Nodes.Shapes.Data;
using PixiEditor.ChangeableDocument.ChangeInfos.Animation;
using PixiEditor.ChangeableDocument.ChangeInfos.Vectors;

namespace PixiEditor.ChangeableDocument.Changes.Drawing;

internal class CombineStructureMembersOnto_Change : Change
{
    private HashSet<Guid> membersToMerge;

    private HashSet<Guid> layersToCombine = new();

    private Guid targetLayerGuid;
    private Dictionary<int, CommittedChunkStorage> originalChunks = new();
    
    private Dictionary<int, VectorPath> originalPaths = new();


    [GenerateMakeChangeAction]
    public CombineStructureMembersOnto_Change(HashSet<Guid> membersToMerge, Guid targetLayer)
    {
        this.membersToMerge = new HashSet<Guid>(membersToMerge);
        this.targetLayerGuid = targetLayer;
    }

    public override bool InitializeAndValidate(Document target)
    {
        if (!target.HasMember(targetLayerGuid) || membersToMerge.Count == 0)
            return false;
        foreach (Guid guid in membersToMerge)
        {
            if (!target.TryFindMember(guid, out var member))
                return false;

            if (member is LayerNode layer)
                layersToCombine.Add(layer.Id);
            else if (member is FolderNode innerFolder)
                AddChildren(innerFolder, layersToCombine);
        }

        return true;
    }

    private void AddChildren(FolderNode folder, HashSet<Guid> collection)
    {
        if (folder.Content.Connection != null)
        {
            folder.Content.Connection.Node.TraverseBackwards(node =>
            {
                if (node is LayerNode layer)
                {
                    collection.Add(layer.Id);
                    return true;
                }

                return true;
            });
        }
    }

    public override OneOf<None, IChangeInfo, List<IChangeInfo>> Apply(Document target, bool firstApply,
        out bool ignoreInUndo)
    {
        List<IChangeInfo> changes = new();
        var targetLayer = target.FindMemberOrThrow<LayerNode>(targetLayerGuid);

        int maxFrame = GetMaxFrame(target, targetLayer);

        for (int frame = 0; frame < maxFrame || frame == 0; frame++)
        {
            changes.AddRange(ApplyToFrame(target, targetLayer, frame));
        }

        ignoreInUndo = false;

        return changes;
    }

    public override OneOf<None, IChangeInfo, List<IChangeInfo>> Revert(Document target)
    {
        var toDrawOn = target.FindMemberOrThrow<LayerNode>(targetLayerGuid);

        List<IChangeInfo> changes = new();

        int maxFrame = GetMaxFrame(target, toDrawOn);

        for (int frame = 0; frame < maxFrame || frame == 0; frame++)
        {
            changes.Add(RevertFrame(toDrawOn, frame));
        }

        target.AnimationData.RemoveKeyFrame(targetLayerGuid);
        originalChunks.Clear();
        changes.Add(new DeleteKeyFrame_ChangeInfo(targetLayerGuid));

        return changes;
    }

    private List<IChangeInfo> ApplyToFrame(Document target, LayerNode targetLayer, int frame)
    {
        var chunksToCombine = new HashSet<VecI>();
        List<IChangeInfo> changes = new();

        var ordererd = OrderLayers(layersToCombine, target);

        foreach (var guid in ordererd)
        {
            var layer = target.FindMemberOrThrow<LayerNode>(guid);

            AddMissingKeyFrame(targetLayer, frame, layer, changes, target);

            if (layer is not IRasterizable or ImageLayerNode)
                continue;

            if (layer is ImageLayerNode imageLayerNode)
            {
                var layerImage = imageLayerNode.GetLayerImageAtFrame(frame);
                chunksToCombine.UnionWith(layerImage.FindAllChunks());
            }
            else
            {
                AddChunksByTightBounds(layer, chunksToCombine, frame);
            }
        }

        bool allVector = layersToCombine.All(x => target.FindMember(x) is VectorLayerNode);

        AffectedArea affArea = new();

        // TODO: add custom layer merge
        if (!allVector)
        {
            affArea = RasterMerge(target, targetLayer, frame);
        }
        else
        {
            affArea = VectorMerge(target, targetLayer, frame, layersToCombine);
        }

        changes.Add(new LayerImageArea_ChangeInfo(targetLayerGuid, affArea));
        return changes;
    }

    private AffectedArea VectorMerge(Document target, LayerNode targetLayer, int frame, HashSet<Guid> toCombine)
    {
        if (targetLayer is not VectorLayerNode vectorLayer)
            throw new InvalidOperationException("Target layer is not a vector layer");

        ShapeVectorData targetData = vectorLayer.ShapeData ?? null;
        VectorPath? targetPath = targetData?.ToPath();

        var reversed = toCombine.Reverse().ToHashSet();
        
        foreach (var guid in reversed)
        {
            if (target.FindMember(guid) is not VectorLayerNode vectorNode)
                continue;

            if (vectorNode.ShapeData == null)
                continue;

            VectorPath path = vectorNode.ShapeData.ToPath();

            if (targetData == null)
            {
                targetData = vectorNode.ShapeData;
                targetPath = path;
                
                if(originalPaths.ContainsKey(frame))
                    originalPaths[frame].Dispose();
                
                originalPaths[frame] = new VectorPath(path);
            }
            else
            {
                targetPath.AddPath(path, AddPathMode.Append);
                path.Dispose();
            }
        }

        var pathData = new PathVectorData(targetPath)
        {
            StrokeWidth = targetData.StrokeWidth,
            StrokeColor = targetData.StrokeColor,
            FillColor = targetData.FillColor
        };

        vectorLayer.ShapeData = pathData;

        return new AffectedArea(new HashSet<VecI>());
    }

    private AffectedArea RasterMerge(Document target, LayerNode targetLayer, int frame)
    {
        var toDrawOnImage = ((ImageLayerNode)targetLayer).GetLayerImageAtFrame(frame);
        toDrawOnImage.EnqueueClear();

        Texture tempTexture = new Texture(target.Size);

        DocumentRenderer renderer = new(target);

        AffectedArea affArea = new();
        DrawingBackendApi.Current.RenderingDispatcher.Invoke(() =>
        {
            if (frame == 0)
            {
                renderer.RenderLayers(tempTexture.DrawingSurface, layersToCombine, frame, ChunkResolution.Full);
            }
            else
            {
                HashSet<Guid> layersToRender = new();
                foreach (var layer in layersToCombine)
                {
                    if (target.FindMember(layer) is LayerNode node)
                    {
                        if (node.KeyFrames.Any(x => x.IsInFrame(frame)))
                        {
                            layersToRender.Add(layer);
                        }
                    }
                }

                renderer.RenderLayers(tempTexture.DrawingSurface, layersToRender, frame, ChunkResolution.Full);
            }

            toDrawOnImage.EnqueueDrawTexture(VecI.Zero, tempTexture);

            affArea = toDrawOnImage.FindAffectedArea();
            originalChunks.Add(frame, new CommittedChunkStorage(toDrawOnImage, affArea.Chunks));
            toDrawOnImage.CommitChanges();

            tempTexture.Dispose();
        });
        return affArea;
    }

    private HashSet<Guid> OrderLayers(HashSet<Guid> layersToCombine, Document document)
    {
        HashSet<Guid> ordered = new();
        document.NodeGraph.TryTraverse(node =>
        {
            if (node is LayerNode layer && layersToCombine.Contains(layer.Id))
            {
                ordered.Add(layer.Id);
            }
        });

        return ordered.Reverse().ToHashSet();
    }

    private void AddMissingKeyFrame(LayerNode targetLayer, int frame, LayerNode layer, List<IChangeInfo> changes,
        Document target)
    {
        bool hasKeyframe = targetLayer.KeyFrames.Any(x => x.IsInFrame(frame));
        if (hasKeyframe)
            return;

        if (layer is not ImageLayerNode)
            return;

        var keyFrameData = layer.KeyFrames.FirstOrDefault(x => x.IsInFrame(frame));
        if (keyFrameData is null)
            return;

        var clonedData = keyFrameData.Clone(true);

        targetLayer.AddFrame(keyFrameData.KeyFrameGuid, clonedData);

        changes.Add(new CreateRasterKeyFrame_ChangeInfo(targetLayerGuid, frame, clonedData.KeyFrameGuid, true));
        changes.Add(new KeyFrameLength_ChangeInfo(targetLayerGuid, clonedData.StartFrame, clonedData.Duration));

        target.AnimationData.AddKeyFrame(new RasterKeyFrame(clonedData.KeyFrameGuid, targetLayerGuid, frame, target));
    }

    private int GetMaxFrame(Document target, LayerNode targetLayer)
    {
        if (targetLayer.KeyFrames.Count == 0)
            return 0;

        int maxFrame = targetLayer.KeyFrames.Max(x => x.StartFrame + x.Duration);
        foreach (var toMerge in membersToMerge)
        {
            var member = target.FindMemberOrThrow<LayerNode>(toMerge);
            if (member.KeyFrames.Count > 0)
            {
                maxFrame = Math.Max(maxFrame, member.KeyFrames.Max(x => x.StartFrame + x.Duration));
            }
        }

        return maxFrame;
    }

    private void AddChunksByTightBounds(LayerNode layer, HashSet<VecI> chunksToCombine, int frame)
    {
        var tightBounds = layer.GetTightBounds(frame);
        if (tightBounds.HasValue)
        {
            VecI chunk = (VecI)tightBounds.Value.TopLeft / ChunkyImage.FullChunkSize;
            VecI sizeInChunks = ((VecI)tightBounds.Value.Size / ChunkyImage.FullChunkSize);
            sizeInChunks = new VecI(Math.Max(1, sizeInChunks.X), Math.Max(1, sizeInChunks.Y));
            for (int x = 0; x < sizeInChunks.X; x++)
            {
                for (int y = 0; y < sizeInChunks.Y; y++)
                {
                    chunksToCombine.Add(chunk + new VecI(x, y));
                }
            }
        }
    }

    private IChangeInfo RevertFrame(LayerNode targetLayer, int frame)
    {
        if (targetLayer is ImageLayerNode imageLayerNode)
        {
            return RasterRevert(imageLayerNode, frame);
        }
        else if (targetLayer is VectorLayerNode vectorLayerNode)
        {
            return VectorRevert(vectorLayerNode, frame);
        }
        
        throw new InvalidOperationException("Layer type not supported");
    }

    private IChangeInfo RasterRevert(ImageLayerNode targetLayer, int frame)
    {
        var toDrawOnImage = targetLayer.GetLayerImageAtFrame(frame);
        toDrawOnImage.EnqueueClear();

        CommittedChunkStorage? storedChunks = originalChunks[frame];

        var affectedArea =
            DrawingChangeHelper.ApplyStoredChunksDisposeAndSetToNull(
                targetLayer.GetLayerImageAtFrame(frame),
                ref storedChunks);

        toDrawOnImage.CommitChanges();
        return new LayerImageArea_ChangeInfo(targetLayerGuid, affectedArea);
    }
    
    private IChangeInfo VectorRevert(VectorLayerNode targetLayer, int frame)
    {
        if (!originalPaths.TryGetValue(frame, out var path))
            throw new InvalidOperationException("Original path not found");

        targetLayer.ShapeData = new PathVectorData(path);
        return new VectorShape_ChangeInfo(targetLayer.Id, new AffectedArea(new HashSet<VecI>()));
    }

    public override void Dispose()
    {
        foreach (var originalChunk in originalChunks)
        {
            originalChunk.Value.Dispose();
        }
        
        originalChunks.Clear();
        
        foreach (var originalPath in originalPaths)
        {
            originalPath.Value.Dispose();
        }
        
        originalPaths.Clear();
    }
}

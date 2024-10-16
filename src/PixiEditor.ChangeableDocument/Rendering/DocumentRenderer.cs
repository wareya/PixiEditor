﻿using PixiEditor.ChangeableDocument.Changeables.Animations;
using PixiEditor.ChangeableDocument.Changeables.Graph;
using PixiEditor.ChangeableDocument.Changeables.Graph.Interfaces;
using PixiEditor.ChangeableDocument.Changeables.Graph.Nodes;
using PixiEditor.ChangeableDocument.Changeables.Interfaces;
using PixiEditor.DrawingApi.Core;
using PixiEditor.DrawingApi.Core.Surfaces;
using PixiEditor.DrawingApi.Core.Surfaces.ImageData;
using PixiEditor.DrawingApi.Core.Surfaces.PaintImpl;
using PixiEditor.Numerics;

namespace PixiEditor.ChangeableDocument.Rendering;

public class DocumentRenderer : IPreviewRenderable
{
    
    private Paint ClearPaint { get; } = new Paint()
    {
        BlendMode = BlendMode.Src, Color = PixiEditor.DrawingApi.Core.ColorsImpl.Colors.Transparent
    };

    public DocumentRenderer(IReadOnlyDocument document)
    {
        Document = document;
    }

    private IReadOnlyDocument Document { get; }

    public void RenderChunk(VecI chunkPos, ChunkResolution resolution, KeyFrameTime frameTime)
    {
        try
        {
            Document.NodeGraph.TryTraverse((node =>
            {
                if (node is IChunkRenderable imageNode)
                {
                    imageNode.RenderChunk(chunkPos, resolution, frameTime);
                }
            }));
        }
        catch (ObjectDisposedException)
        {
        }
    }

    private static RectI? TransformClipRect(RectI? globalClippingRect, ChunkResolution resolution, VecI chunkPos)
    {
        if (globalClippingRect is not RectI rect)
            return null;

        double multiplier = resolution.Multiplier();
        VecI pixelChunkPos = chunkPos * (int)(ChunkyImage.FullChunkSize * multiplier);
        return (RectI?)rect.Scale(multiplier).Translate(-pixelChunkPos).RoundOutwards();
    }

    public OneOf<Chunk, EmptyChunk> RenderLayersChunk(VecI chunkPos, ChunkResolution resolution, KeyFrameTime frame,
        HashSet<Guid> layersToCombine, RectI? globalClippingRect)
    {
        //using RenderingContext context = new(frame, chunkPos, resolution, Document.Size);
        IReadOnlyNodeGraph membersOnlyGraph = ConstructMembersOnlyGraph(layersToCombine, Document.NodeGraph);
        try
        {
            //return RenderChunkOnGraph(chunkPos, resolution, globalClippingRect, membersOnlyGraph, context);
            return new EmptyChunk();
        }
        catch (ObjectDisposedException)
        {
            return new EmptyChunk();
        }
    }
    
    
    public void RenderLayer(DrawingSurface renderOn, Guid nodeId, ChunkResolution resolution, KeyFrameTime frameTime)
    {
        var node = Document.FindNode(nodeId);
        
        if (node is null)
        {
            return;
        }
        
        using RenderContext context = new(renderOn, frameTime, resolution, Document.Size);
        context.IsExportRender = true;
        
        node.Execute(context);
    }

    public static IReadOnlyNodeGraph ConstructMembersOnlyGraph(IReadOnlyNodeGraph fullGraph)
    {
        return ConstructMembersOnlyGraph(null, fullGraph); 
    }

    public static IReadOnlyNodeGraph ConstructMembersOnlyGraph(HashSet<Guid>? layersToCombine,
        IReadOnlyNodeGraph fullGraph)
    {
        NodeGraph membersOnlyGraph = new();

        OutputNode outputNode = new();

        membersOnlyGraph.AddNode(outputNode);

        List<LayerNode> layersInOrder = new();

        fullGraph.TryTraverse(node =>
        {
            if (node is LayerNode layer && (layersToCombine == null || layersToCombine.Contains(layer.Id)))
            {
                layersInOrder.Insert(0, layer);
            }
        });

        IInputProperty<DrawingSurface> lastInput = outputNode.Input;

        foreach (var layer in layersInOrder)
        {
            var clone = (LayerNode)layer.Clone();
            membersOnlyGraph.AddNode(clone);

            clone.Output.ConnectTo(lastInput);
            lastInput = clone.RenderTarget;
        }

        return membersOnlyGraph;
    }

    public RectD? GetPreviewBounds(int frame, string elementNameToRender = "") => 
        new(0, 0, Document.Size.X, Document.Size.Y); 

    public bool RenderPreview(DrawingSurface renderOn, ChunkResolution resolution, int frame, string elementToRenderName)
    {
        using RenderContext context = new(renderOn, frame, resolution, Document.Size);
        Document.NodeGraph.Execute(context);
        
        return true;
    }

    public void RenderDocument(DrawingSurface toRenderOn, KeyFrameTime frameTime)
    {
        using RenderContext context = new(toRenderOn, frameTime, ChunkResolution.Full, Document.Size) { IsExportRender = true };
        Document.NodeGraph.Execute(context);
    }
}

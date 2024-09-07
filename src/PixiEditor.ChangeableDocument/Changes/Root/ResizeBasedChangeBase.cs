﻿using PixiEditor.ChangeableDocument.Changeables.Graph.Nodes;
using PixiEditor.ChangeableDocument.ChangeInfos.Root;
using PixiEditor.DrawingApi.Core.Numerics;
using PixiEditor.Numerics;

namespace PixiEditor.ChangeableDocument.Changes.Root;

internal abstract class ResizeBasedChangeBase : Change
{
    protected VecI _originalSize;
    protected double _originalHorAxisY;
    protected double _originalVerAxisX;
    protected Dictionary<Guid, List<CommittedChunkStorage>> deletedChunks = new();
    protected Dictionary<Guid, List<CommittedChunkStorage>> deletedMaskChunks = new();

    public ResizeBasedChangeBase()
    {
    }

    public override bool InitializeAndValidate(Document target)
    {
        _originalSize = target.Size;
        _originalHorAxisY = target.HorizontalSymmetryAxisY;
        _originalVerAxisX = target.VerticalSymmetryAxisX;
        return true;
    }

    /// <summary>
    /// Notice: this commits image changes, you won't have a chance to revert or set ignoreInUndo to true
    /// </summary>
    protected virtual void Resize(ChunkyImage img, Guid memberGuid, VecI size, VecI offset,
        Dictionary<Guid, List<CommittedChunkStorage>> deletedChunksDict)
    {
        img.EnqueueResize(size);
        img.EnqueueClear();
        img.EnqueueDrawCommitedChunkyImage(offset, img);
        
        if (!deletedChunksDict.ContainsKey(memberGuid))
            deletedChunksDict.Add(memberGuid, new());

        deletedChunksDict[memberGuid].Add(new CommittedChunkStorage(img, img.FindAffectedArea().Chunks));
        img.CommitChanges();
    }

    public override OneOf<None, IChangeInfo, List<IChangeInfo>> Revert(Document target)
    {
        target.Size = _originalSize;
        target.ForEveryMember((member) =>
        {
            if (member is ImageLayerNode layer)
            {
                layer.ForEveryFrame(img =>
                {
                    img.EnqueueResize(_originalSize);
                    foreach (var stored in deletedChunks[layer.Id])
                        stored.ApplyChunksToImage(img);
                    img.CommitChanges();
                });
            }

            // TODO: Add support for different Layer types?

            if (member.EmbeddedMask is null)
                return;
            member.EmbeddedMask.EnqueueResize(_originalSize);
            deletedMaskChunks[member.Id][0].ApplyChunksToImage(member.EmbeddedMask);
            member.EmbeddedMask.CommitChanges();
        });

        target.HorizontalSymmetryAxisY = _originalHorAxisY;
        target.VerticalSymmetryAxisX = _originalVerAxisX;

        DisposeDeletedChunks();

        return new Size_ChangeInfo(_originalSize, _originalVerAxisX, _originalHorAxisY);
    }

    private void DisposeDeletedChunks()
    {
        foreach (var stored in deletedChunks)
        {
            foreach (var storage in stored.Value)
            {
                storage.Dispose();
            }

        }
        deletedChunks = new();

        foreach (var stored in deletedMaskChunks)
        {
            foreach (var storage in stored.Value)
            {
                storage.Dispose();
            }
        }
        deletedMaskChunks = new();
    }

    public override void Dispose()
    {
        DisposeDeletedChunks();
    }
}

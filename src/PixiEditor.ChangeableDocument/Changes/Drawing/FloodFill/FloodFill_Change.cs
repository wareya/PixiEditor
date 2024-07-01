﻿using PixiEditor.DrawingApi.Core.ColorsImpl;
using PixiEditor.DrawingApi.Core.Numerics;
using PixiEditor.DrawingApi.Core.Surface.Vector;
using PixiEditor.Numerics;

namespace PixiEditor.ChangeableDocument.Changes.Drawing.FloodFill;

internal class FloodFill_Change : Change
{
    private readonly Guid memberGuid;
    private readonly VecI pos;
    private readonly Color color;
    private readonly bool referenceAll;
    private readonly bool drawOnMask;
    private CommittedChunkStorage? chunkStorage = null;
    private int frame;

    [GenerateMakeChangeAction]
    public FloodFill_Change(Guid memberGuid, VecI pos, Color color, bool referenceAll, bool drawOnMask, int frame)
    {
        this.memberGuid = memberGuid;
        this.pos = pos;
        this.color = color;
        this.referenceAll = referenceAll;
        this.drawOnMask = drawOnMask;
        this.frame = frame;
    }

    public override bool InitializeAndValidate(Document target)
    {
        if (pos.X < 0 || pos.Y < 0 || pos.X >= target.Size.X || pos.Y >= target.Size.Y)
            return false;
        
        return DrawingChangeHelper.IsValidForDrawing(target, memberGuid, drawOnMask);
    }

    public override OneOf<None, IChangeInfo, List<IChangeInfo>> Apply(Document target, bool firstApply, out bool ignoreInUndo)
    {
        var image = DrawingChangeHelper.GetTargetImageOrThrow(target, memberGuid, drawOnMask, frame);

        VectorPath? selection = target.Selection.SelectionPath.IsEmpty ? null : target.Selection.SelectionPath;
        HashSet<Guid> membersToReference = new();
        if (referenceAll)
            target.ForEveryReadonlyMember(member => membersToReference.Add(member.GuidValue));
        else
            membersToReference.Add(memberGuid);
        var floodFilledChunks = FloodFillHelper.FloodFill(membersToReference, target, selection, pos, color, frame);
        if (floodFilledChunks.Count == 0)
        {
            ignoreInUndo = true;
            return new None();
        }

        foreach (var (chunkPos, chunk) in floodFilledChunks)
        {
            image.EnqueueDrawImage(chunkPos * ChunkyImage.FullChunkSize, chunk.Surface, null, false);
        }
        var affArea = image.FindAffectedArea();
        chunkStorage = new CommittedChunkStorage(image, affArea.Chunks);
        image.CommitChanges();
        foreach (var chunk in floodFilledChunks.Values)
            chunk.Dispose();

        ignoreInUndo = false;
        return DrawingChangeHelper.CreateAreaChangeInfo(memberGuid, affArea, drawOnMask);
    }

    public override OneOf<None, IChangeInfo, List<IChangeInfo>> Revert(Document target)
    {
        var affArea = DrawingChangeHelper.ApplyStoredChunksDisposeAndSetToNull(target, memberGuid, drawOnMask, frame, ref chunkStorage);
        return DrawingChangeHelper.CreateAreaChangeInfo(memberGuid, affArea, drawOnMask);
    }

    public override void Dispose()
    {
        chunkStorage?.Dispose();
    }
}

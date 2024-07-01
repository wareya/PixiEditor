﻿using PixiEditor.ChangeableDocument.Changeables.Animations;
using PixiEditor.ChangeableDocument.ChangeInfos.Animation;

namespace PixiEditor.ChangeableDocument.Changes.Animation;

internal class CreateRasterKeyFrame_Change : Change
{
    private readonly Guid _targetLayerGuid;
    private readonly int _frame;
    private readonly Guid? cloneFrom;
    private int? cloneFromFrame;
    private RasterLayer? _layer;
    private Guid createdKeyFrameId;

    [GenerateMakeChangeAction]
    public CreateRasterKeyFrame_Change(Guid targetLayerGuid, Guid newKeyFrameGuid, int frame,
        int? cloneFromFrame = null,
        Guid? cloneFromExisting = null)
    {
        _targetLayerGuid = targetLayerGuid;
        _frame = frame;
        cloneFrom = cloneFromExisting;
        createdKeyFrameId = newKeyFrameGuid;
        this.cloneFromFrame = cloneFromFrame;
    }

    public override bool InitializeAndValidate(Document target)
    {
        return target.TryFindMember(_targetLayerGuid, out _layer);
    }

    public override OneOf<None, IChangeInfo, List<IChangeInfo>> Apply(Document target, bool firstApply,
        out bool ignoreInUndo)
    {
        var cloneFromImage = cloneFrom.HasValue
            ? target.FindMemberOrThrow<RasterLayer>(cloneFrom.Value).GetLayerImageAtFrame(cloneFromFrame ?? 0)
            : null;
        var keyFrame =
            new RasterKeyFrame(_targetLayerGuid, _frame, target, cloneFromImage);
        keyFrame.Id = createdKeyFrameId;
        target.AnimationData.AddKeyFrame(keyFrame);
        ignoreInUndo = false;
        return new CreateRasterKeyFrame_ChangeInfo(_targetLayerGuid, _frame, createdKeyFrameId, cloneFrom.HasValue);
    }

    public override OneOf<None, IChangeInfo, List<IChangeInfo>> Revert(Document target)
    {
        target.AnimationData.RemoveKeyFrame(createdKeyFrameId);
        return new DeleteKeyFrame_ChangeInfo(createdKeyFrameId);
    }
}

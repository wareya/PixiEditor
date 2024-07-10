﻿using PixiEditor.ChangeableDocument.ChangeInfos.Properties;

namespace PixiEditor.ChangeableDocument.Changes.Properties.LayerStructure;

internal class DeleteStructureMemberMask_Change : Change
{
    private readonly Guid memberGuid;
    private ChunkyImage? storedMask;

    [GenerateMakeChangeAction]
    public DeleteStructureMemberMask_Change(Guid memberGuid)
    {
        this.memberGuid = memberGuid;
    }

    public override bool InitializeAndValidate(Document target)
    {
        if (!target.TryFindMember(memberGuid, out var member) || member.Mask is null)
            return false;
        
        storedMask = member.Mask.Value.CloneFromCommitted();
        return true;
    }

    public override OneOf<None, IChangeInfo, List<IChangeInfo>> Apply(Document target, bool firstApply, out bool ignoreInUndo)
    {
        var member = target.FindMemberOrThrow(memberGuid);
        if (member.Mask is null)
            throw new InvalidOperationException("Cannot delete the mask; Target member has no mask");
        member.Mask.NonOverridenValue.Dispose();
        member.Mask.NonOverridenValue = null;

        ignoreInUndo = false;
        return new StructureMemberMask_ChangeInfo(memberGuid, false);
    }

    public override OneOf<None, IChangeInfo, List<IChangeInfo>> Revert(Document target)
    {
        var member = target.FindMemberOrThrow(memberGuid);
        if (member.Mask is not null)
            throw new InvalidOperationException("Cannot revert mask deletion; The target member already has a mask");
        member.Mask.NonOverridenValue = storedMask!.CloneFromCommitted();

        return new StructureMemberMask_ChangeInfo(memberGuid, true);
    }

    public override void Dispose()
    {
        storedMask?.Dispose();
    }
}

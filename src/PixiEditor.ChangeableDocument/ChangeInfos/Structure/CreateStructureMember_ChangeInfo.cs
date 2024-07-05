﻿using System.Collections.Immutable;
using System.Reflection;
using PixiEditor.ChangeableDocument.ChangeInfos.NodeGraph;
using PixiEditor.ChangeableDocument.Enums;
using PixiEditor.Numerics;

namespace PixiEditor.ChangeableDocument.ChangeInfos.Structure;

public abstract record class CreateStructureMember_ChangeInfo(
    Guid ParentGuid,
    int Index,
    float Opacity,
    bool IsVisible,
    bool ClipToMemberBelow,
    string Name,
    BlendMode BlendMode,
    Guid Id,
    bool HasMask,
    bool MaskIsVisible,
    ImmutableArray<NodePropertyInfo> InputProperties,
    ImmutableArray<NodePropertyInfo> OutputProperties
) : CreateNode_ChangeInfo(Name, VecD.Zero, Id, InputProperties, OutputProperties);

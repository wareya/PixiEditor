﻿using PixiEditor.ChangeableDocument.Actions;
using PixiEditor.ChangeableDocument.ChangeInfos;

namespace PixiEditor.AvaloniaUI.Models.DocumentPassthroughActions;

internal record AddSelectedKeyFrame_PassthroughAction(Guid KeyFrameGuid) : IChangeInfo, IAction;

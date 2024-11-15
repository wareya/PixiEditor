﻿using ChunkyImageLib.DataHolders;
using Drawie.Backend.Core.Numerics;
using PixiEditor.Models.DocumentModels;
using Drawie.Numerics;
using PixiEditor.Models.Controllers.InputDevice;

namespace PixiEditor.Models.Handlers;

internal interface ITransformHandler : IHandler
{
    public void KeyModifiersInlet(bool argsIsShiftDown, bool argsIsCtrlDown, bool argsIsAltDown);
    public void ShowTransform(DocumentTransformMode transformMode, bool coverWholeScreen, ShapeCorners shapeCorners, bool showApplyButton);
    public void HideTransform();
    public bool Undo();
    public bool Redo();
    public bool Nudge(VecD distance);
    public bool HasUndo { get; }
    public bool HasRedo { get; }
    public bool ShowTransformControls { get; set; }
    public event Action<MouseOnCanvasEventArgs> PassthroughPointerPressed;
}

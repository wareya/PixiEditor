﻿using Avalonia.Input;
using PixiEditor.Models.Handlers;
using PixiEditor.Models.Handlers.Tools;
using PixiEditor.ViewModels.Tools.ToolSettings.Toolbars;
using PixiEditor.Views.Overlays.BrushShapeOverlay;

namespace PixiEditor.ViewModels.Tools;

internal abstract class ShapeTool : ToolViewModel, IShapeToolHandler
{
    public override BrushShape BrushShape => BrushShape.Hidden;

    public override bool UsesColor => true;

    public override bool IsErasable => true;
    public bool DrawEven { get; protected set; }
    public bool DrawFromCenter { get; protected set; }
    
    protected bool isActivated;

    public ShapeTool()
    {
        Cursor = new Cursor(StandardCursorType.Cross);
        Toolbar = new FillableShapeToolbar();
    }

    public override void OnSelected(bool restoring)
    {
        base.OnSelected(restoring);
        if (!restoring)
        {
            isActivated = true;
        }
    }

    public override void OnDeselecting(bool transient)
    {
        if (!transient)
        {
            ViewModelMain.Current.DocumentManagerSubViewModel.ActiveDocument?.Operations.TryStopToolLinkedExecutor();
            isActivated = false;
        }
    }
}

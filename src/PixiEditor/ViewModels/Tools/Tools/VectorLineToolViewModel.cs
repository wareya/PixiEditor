﻿using Avalonia.Input;
using ChunkyImageLib.DataHolders;
using PixiEditor.ChangeableDocument.Changeables.Graph.Interfaces.Shapes;
using PixiEditor.ChangeableDocument.Changeables.Graph.Nodes;
using PixiEditor.ChangeableDocument.Changeables.Graph.Nodes.Shapes.Data;
using PixiEditor.Views.Overlays.BrushShapeOverlay;
using Drawie.Backend.Core.Numerics;
using PixiEditor.Extensions.Common.Localization;
using PixiEditor.Models.Commands.Attributes.Commands;
using PixiEditor.Models.DocumentModels;
using PixiEditor.Models.Handlers;
using PixiEditor.Models.Handlers.Tools;
using Drawie.Numerics;
using PixiEditor.UI.Common.Fonts;
using PixiEditor.ViewModels.Tools.ToolSettings.Toolbars;

namespace PixiEditor.ViewModels.Tools.Tools;

[Command.Tool(Key = Key.L)]
internal class VectorLineToolViewModel : ShapeTool, IVectorLineToolHandler
{
    public const string NewLayerKey = "NEW_LINE_LAYER_NAME";
    private string defaultActionDisplay = "LINE_TOOL_ACTION_DISPLAY_DEFAULT";

    public override bool IsErasable => false;
    private bool isActivated;

    public VectorLineToolViewModel()
    {
        ActionDisplay = defaultActionDisplay;
        Toolbar = ToolbarFactory.Create<VectorLineToolViewModel, ShapeToolbar>(this);
    }

    public override string ToolNameLocalizationKey => "LINE_TOOL";
    public override LocalizedString Tooltip => new LocalizedString("LINE_TOOL_TOOLTIP", Shortcut);

    public override string DefaultIcon => PixiPerfectIcons.Line;
    public override Type[]? SupportedLayerTypes { get; } = [];
    public string? DefaultNewLayerName { get; } = new LocalizedString(NewLayerKey);

    [Settings.Inherited] public double ToolSize => GetValue<double>();

    public bool Snap { get; private set; }

    public override Type LayerTypeToCreateOnEmptyUse { get; } = typeof(VectorLayerNode);

    public override void ModifierKeyChanged(bool ctrlIsDown, bool shiftIsDown, bool altIsDown)
    {
        DrawFromCenter = ctrlIsDown;

        if (shiftIsDown)
        {
            ActionDisplay = "LINE_TOOL_ACTION_DISPLAY_SHIFT";
            Snap = true;
        }
        else
        {
            ActionDisplay = defaultActionDisplay;
            Snap = false;
        }
    }

    public override void UseTool(VecD pos)
    {
        ViewModelMain.Current?.DocumentManagerSubViewModel.ActiveDocument?.Tools.UseVectorLineTool();
    }

    public override void OnSelected(bool restoring)
    {
        if (restoring) return;

        var document = ViewModelMain.Current?.DocumentManagerSubViewModel.ActiveDocument;
        document.Tools.UseVectorLineTool();
        isActivated = true;
    }

    public override void OnPostUndo()
    {
        if (isActivated)
        {
            OnSelected(false);
        }
    }

    public override void OnPostRedo()
    {
        if (isActivated)
        {
            OnSelected(false);
        }
    }

    protected override void OnSelectedLayersChanged(IStructureMemberHandler[] layers)
    {
        OnDeselecting(false);
        OnSelected(false);
    }

    public override void OnDeselecting(bool transient)
    {
        base.OnDeselecting(transient);
        isActivated = false;
    }
}

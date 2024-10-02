﻿using Avalonia.Input;
using PixiEditor.ChangeableDocument.Enums;
using PixiEditor.DrawingApi.Core.Numerics;
using PixiEditor.Extensions.Common.Localization;
using PixiEditor.Models.Commands.Attributes.Commands;
using PixiEditor.Models.Handlers.Tools;
using PixiEditor.Numerics;
using PixiEditor.UI.Common.Fonts;
using PixiEditor.ViewModels.Tools.ToolSettings.Toolbars;
using PixiEditor.Views.Overlays.BrushShapeOverlay;

namespace PixiEditor.ViewModels.Tools.Tools;

[Command.Tool(Key = Key.Q)]
internal class LassoToolViewModel : ToolViewModel, ILassoToolHandler
{
    private string defaultActionDisplay = "LASSO_TOOL_ACTION_DISPLAY_DEFAULT";

    public LassoToolViewModel()
    {
        Toolbar = ToolbarFactory.Create(this);
        ActionDisplay = defaultActionDisplay;
    }

    private SelectionMode KeyModifierselectionMode = SelectionMode.New;
    public SelectionMode? ResultingSelectionMode => KeyModifierselectionMode != SelectionMode.New ? KeyModifierselectionMode : SelectMode;

    public override Type LayerTypeToCreateOnEmptyUse { get; } = null;

    public override void ModifierKeyChanged(bool ctrlIsDown, bool shiftIsDown, bool altIsDown)
    {
        if (shiftIsDown)
        {
            ActionDisplay = "LASSO_TOOL_ACTION_DISPLAY_SHIFT";
            KeyModifierselectionMode = SelectionMode.Add;
        }
        else if (ctrlIsDown)
        {
            ActionDisplay = "LASSO_TOOL_ACTION_DISPLAY_CTRL";
            KeyModifierselectionMode = SelectionMode.Subtract;
        }
        else
        {
            ActionDisplay = defaultActionDisplay;
            KeyModifierselectionMode = SelectionMode.New;
        }
    }

    public override LocalizedString Tooltip => new LocalizedString("LASSO_TOOL_TOOLTIP", Shortcut);

    public override string ToolNameLocalizationKey => "LASSO_TOOL";
    public string Icon => PixiPerfectIcons.Lasso;
    public override BrushShape BrushShape => BrushShape.Pixel;
    
    public override Type[]? SupportedLayerTypes { get; } = null; // all layer types are supported

    [Settings.Enum("MODE_LABEL")]
    public SelectionMode SelectMode => GetValue<SelectionMode>();
    
    public override void UseTool(VecD pos)
    {
        ViewModelMain.Current?.DocumentManagerSubViewModel.ActiveDocument?.Tools.UseLassoTool();
    }
}

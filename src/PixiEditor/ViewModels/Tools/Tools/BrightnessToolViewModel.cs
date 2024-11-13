﻿using Avalonia.Input;
using PixiEditor.ChangeableDocument.Changeables.Graph.Nodes;
using Drawie.Backend.Core.Numerics;
using PixiEditor.Extensions.Common.Localization;
using PixiEditor.Models.Commands.Attributes.Commands;
using PixiEditor.Models.Handlers;
using PixiEditor.Models.Handlers.Tools;
using PixiEditor.Models.Tools;
using Drawie.Numerics;
using PixiEditor.UI.Common.Fonts;
using PixiEditor.ViewModels.Tools.ToolSettings.Toolbars;
using PixiEditor.Views.Overlays.BrushShapeOverlay;

namespace PixiEditor.ViewModels.Tools.Tools;

[Command.Tool(Key = Key.U)]
internal class BrightnessToolViewModel : ToolViewModel, IBrightnessToolHandler
{
    private readonly string defaultActionDisplay = "BRIGHTNESS_TOOL_ACTION_DISPLAY_DEFAULT";
    private int _correctionFactor;
    public override string ToolNameLocalizationKey => "BRIGHTNESS_TOOL";

    public BrightnessToolViewModel()
    {
        ActionDisplay = defaultActionDisplay;
        Toolbar = ToolbarFactory.Create<BrightnessToolViewModel, BasicToolbar>(this);
    }

    public override bool IsErasable => true;
    public override LocalizedString Tooltip => new LocalizedString("BRIGHTNESS_TOOL_TOOLTIP", Shortcut);

    public override BrushShape BrushShape => BrushShape.Circle;

    public override string DefaultIcon => PixiPerfectIcons.Sun;

    public override Type[]? SupportedLayerTypes { get; } =
    {
        typeof(IRasterLayerHandler)
    };

    BrightnessMode IBrightnessToolHandler.BrightnessMode
    {
        get => BrightnessMode;
    }

    [Settings.Inherited]
    public int ToolSize => GetValue<int>();
    
    [Settings.Float("STRENGTH_LABEL", 5, 0, 50)]
    public float CorrectionFactor => GetValue<float>();

    [Settings.Enum("MODE_LABEL")]
    public BrightnessMode BrightnessMode => GetValue<BrightnessMode>();
    
    public bool Darken { get; private set; } = false;

    float IBrightnessToolHandler.CorrectionFactor => CorrectionFactor;

    public override Type LayerTypeToCreateOnEmptyUse { get; } = typeof(ImageLayerNode);

    public override void ModifierKeyChanged(bool ctrlIsDown, bool shiftIsDown, bool altIsDown)
    {
        if (!ctrlIsDown)
        {
            ActionDisplay = defaultActionDisplay;
            Darken = false;
        }
        else
        {
            ActionDisplay = "BRIGHTNESS_TOOL_ACTION_DISPLAY_CTRL";
            Darken = true;
        }
    }

    public override void UseTool(VecD pos)
    {
        ViewModelMain.Current?.DocumentManagerSubViewModel.ActiveDocument?.Tools.UseBrightnessTool();
    }
}

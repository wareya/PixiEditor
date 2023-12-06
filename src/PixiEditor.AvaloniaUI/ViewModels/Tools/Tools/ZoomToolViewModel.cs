﻿using Avalonia.Input;
using PixiEditor.AvaloniaUI.Models.Commands.Attributes.Commands;
using PixiEditor.AvaloniaUI.Models.Handlers;
using PixiEditor.AvaloniaUI.Views.Overlays.BrushShapeOverlay;
using PixiEditor.Extensions.Common.Localization;

namespace PixiEditor.AvaloniaUI.ViewModels.Tools.Tools;

[Command.Tool(Key = Key.Z)]
internal class ZoomToolViewModel : ToolViewModel
{
    private bool zoomOutOnClick = false;
    public bool ZoomOutOnClick
    {
        get => zoomOutOnClick;
        set => SetProperty(ref zoomOutOnClick, value);
    }

    private string defaultActionDisplay = new LocalizedString("ZOOM_TOOL_ACTION_DISPLAY_DEFAULT");

    public override string ToolNameLocalizationKey => "ZOOM_TOOL";
    public override BrushShape BrushShape => BrushShape.Hidden;

    public override bool StopsLinkedToolOnUse => false;

    public ZoomToolViewModel()
    {
        ActionDisplay = defaultActionDisplay;
    }

    public override bool HideHighlight => true;

    public override LocalizedString Tooltip => new LocalizedString("ZOOM_TOOL_TOOLTIP", Shortcut);

    public override void ModifierKeyChanged(bool ctrlIsDown, bool shiftIsDown, bool altIsDown)
    {
        if (ctrlIsDown)
        {
            ActionDisplay = new LocalizedString("ZOOM_TOOL_ACTION_DISPLAY_CTRL");
            ZoomOutOnClick = true;
        }
        else
        {
            ActionDisplay = defaultActionDisplay;
            ZoomOutOnClick = false;
        }
    }
}

﻿using Avalonia.Input;
using PixiEditor.ChangeableDocument.Changeables.Graph.Nodes;
using Drawie.Backend.Core.Numerics;
using PixiEditor.Extensions.Common.Localization;
using PixiEditor.Models.Commands.Attributes.Commands;
using PixiEditor.Models.Handlers;
using PixiEditor.Models.Handlers.Tools;
using Drawie.Numerics;
using PixiEditor.UI.Common.Fonts;

namespace PixiEditor.ViewModels.Tools.Tools;

[Command.Tool(Key = Key.C)]
internal class RasterEllipseToolViewModel : ShapeTool, IRasterEllipseToolHandler
{
    private string defaultActionDisplay = "ELLIPSE_TOOL_ACTION_DISPLAY_DEFAULT";
    public override string ToolNameLocalizationKey => "ELLIPSE_TOOL";

    public RasterEllipseToolViewModel()
    {
        ActionDisplay = defaultActionDisplay;
    }

    public override Type[]? SupportedLayerTypes { get; } = { typeof(IRasterLayerHandler) };
    public override LocalizedString Tooltip => new LocalizedString("ELLIPSE_TOOL_TOOLTIP", Shortcut);
    public bool DrawCircle { get; private set; }

    public override string Icon => PixiPerfectIcons.Circle;

    public override Type LayerTypeToCreateOnEmptyUse { get; } = typeof(ImageLayerNode);

    public override void ModifierKeyChanged(bool ctrlIsDown, bool shiftIsDown, bool altIsDown)
    {
        if (shiftIsDown)
        {
            ActionDisplay = "ELLIPSE_TOOL_ACTION_DISPLAY_SHIFT";
            DrawCircle = true;
        }
        else
        {
            ActionDisplay = defaultActionDisplay;
            DrawCircle = false;
        }
    }

    public override void UseTool(VecD pos)
    {
        ViewModelMain.Current?.DocumentManagerSubViewModel.ActiveDocument?.Tools.UseRasterEllipseTool();
    }
    
    public override void OnSelected(bool restoring)
    {
        if(restoring) return;
        
        ViewModelMain.Current?.DocumentManagerSubViewModel.ActiveDocument?.Tools.UseRasterEllipseTool();
    }
}

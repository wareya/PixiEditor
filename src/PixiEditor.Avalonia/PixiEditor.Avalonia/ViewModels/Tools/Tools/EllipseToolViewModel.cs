﻿using System.Windows.Input;
using Avalonia.Input;
using ChunkyImageLib.DataHolders;
using PixiEditor.Avalonia.ViewModels;
using PixiEditor.DrawingApi.Core.Numerics;
using PixiEditor.Extensions.Common.Localization;
using PixiEditor.Models.Commands.Attributes.Commands;
using PixiEditor.Models.Containers.Tools;
using PixiEditor.Models.Localization;

namespace PixiEditor.ViewModels.SubViewModels.Tools.Tools;

[Command.Tool(Key = Key.C)]
internal class EllipseToolViewModel : ShapeTool, IEllipseToolHandler
{
    private string defaultActionDisplay = "ELLIPSE_TOOL_ACTION_DISPLAY_DEFAULT";
    public override string ToolNameLocalizationKey => "ELLIPSE_TOOL";

    public EllipseToolViewModel()
    {
        ActionDisplay = defaultActionDisplay;
    }

    public override LocalizedString Tooltip => new LocalizedString("ELLIPSE_TOOL_TOOLTIP", Shortcut);
    public bool DrawCircle { get; private set; }

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
        ViewModelMain.Current?.DocumentManagerSubViewModel.ActiveDocument?.Tools.UseEllipseTool();
    }
}

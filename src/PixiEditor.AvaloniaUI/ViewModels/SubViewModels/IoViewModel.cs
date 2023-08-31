﻿using System.Linq;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Input;
using CommunityToolkit.Mvvm.Input;
using PixiEditor.AvaloniaUI.Models.Commands;
using PixiEditor.AvaloniaUI.Models.Commands.Commands;
using PixiEditor.AvaloniaUI.Models.Controllers;
using PixiEditor.AvaloniaUI.Models.Controllers.InputDevice;
using PixiEditor.AvaloniaUI.Models.Events;
using PixiEditor.AvaloniaUI.Models.Handlers;
using PixiEditor.AvaloniaUI.Models.Input;
using PixiEditor.AvaloniaUI.Models.Preferences;
using PixiEditor.AvaloniaUI.ViewModels.Document;
using PixiEditor.AvaloniaUI.ViewModels.Tools.Tools;
using PixiEditor.DrawingApi.Core.Numerics;

namespace PixiEditor.AvaloniaUI.ViewModels.SubViewModels;
#nullable enable
internal class IoViewModel : SubViewModel<ViewModelMain>
{
    private bool hadSwapped;
    private int? previousEraseSize;
    private bool hadSharedToolbar;
    private bool? drawingWithRight;
    private bool startedWithEraser;

    public RelayCommand<MouseOnCanvasEventArgs> MouseMoveCommand { get; set; }
    public RelayCommand<MouseOnCanvasEventArgs> MouseDownCommand { get; set; }
    public RelayCommand PreviewMouseMiddleButtonCommand { get; set; }
    public RelayCommand<MouseOnCanvasEventArgs> MouseUpCommand { get; set; }

    private MouseInputFilter mouseFilter = new();
    private KeyboardInputFilter keyboardFilter = new();

    public IoViewModel(ViewModelMain owner)
        : base(owner)
    {
        MouseDownCommand = new RelayCommand<MouseOnCanvasEventArgs>(mouseFilter.MouseDownInlet);
        MouseMoveCommand = new RelayCommand<MouseOnCanvasEventArgs>(mouseFilter.MouseMoveInlet);
        MouseUpCommand = new RelayCommand<MouseOnCanvasEventArgs>(mouseFilter.MouseUpInlet);
        PreviewMouseMiddleButtonCommand = new RelayCommand(OnPreviewMiddleMouseButton);
        // TODO: Implement mouse capturing
        //GlobalMouseHook.Instance.OnMouseUp += mouseFilter.MouseUpInlet;

        if (Application.Current.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.MainWindow.KeyDown += MainWindowKeyDown;
            desktop.MainWindow.KeyUp += MainWindowKeyUp;

            desktop.MainWindow.Deactivated += keyboardFilter.DeactivatedInlet;
            desktop.MainWindow.Deactivated += mouseFilter.DeactivatedInlet;
        }

        mouseFilter.OnMouseDown += OnMouseDown;
        mouseFilter.OnMouseMove += OnMouseMove;
        mouseFilter.OnMouseUp += OnMouseUp;

        keyboardFilter.OnAnyKeyDown += OnKeyDown;
        keyboardFilter.OnAnyKeyUp += OnKeyUp;

        keyboardFilter.OnConvertedKeyDown += OnConvertedKeyDown;
        keyboardFilter.OnConvertedKeyUp += OnConvertedKeyDown;
    }

    private void OnConvertedKeyDown(object? sender, FilteredKeyEventArgs args)
    {
        Owner.DocumentManagerSubViewModel.ActiveDocument?.EventInlet.OnConvertedKeyDown(args);
        Owner.ToolsSubViewModel.ConvertedKeyDownInlet(args);
    }

    private void OnConvertedKeyUp(object? sender, FilteredKeyEventArgs args)
    {
        Owner.DocumentManagerSubViewModel.ActiveDocument?.EventInlet.OnConvertedKeyUp(args);
        Owner.ToolsSubViewModel.ConvertedKeyUpInlet(args);
    }

    private void MainWindowKeyDown(object? sender, KeyEventArgs e)
    {
        keyboardFilter.KeyDownInlet(e);
    }

    private void MainWindowKeyUp(object? sender, KeyEventArgs e)
    {
        keyboardFilter.KeyUpInlet(e);
    }

    private void OnKeyDown(object? sender, FilteredKeyEventArgs args)
    {
        ProcessShortcutDown(args.IsRepeat, args.Key, args.Modifiers);
        Owner.DocumentManagerSubViewModel.ActiveDocument?.EventInlet.OnKeyDown(args.Key);
    }

    private void HandleTransientKey(Key transientKey)
    {
        if (ShortcutController.ShortcutExecutionBlocked)
        {
            return;
        }

        var tool = GetTransientTool(transientKey);

        if (tool is not null)
        {
            Owner.ToolsSubViewModel.SetActiveTool(tool.ToolType, true);
        }
    }

    private static Command.ToolCommand? GetTransientTool(Key transientKey)
    {
        Command.ToolCommand? tool = CommandController.Current.Commands
            .OfType<Command.ToolCommand?>()
            .FirstOrDefault(x => x != null && x.TransientKey == transientKey);
        return tool;
    }

    private void ProcessShortcutDown(bool isRepeat, Key key, KeyModifiers argsModifiers)
    {
        HandleTransientKey(key);
        if (isRepeat && Owner.ShortcutController.LastCommands != null &&
            Owner.ShortcutController.LastCommands.Any(x => x is Command.ToolCommand))
        {
            Owner.ToolsSubViewModel.HandleToolRepeatShortcutDown();
        }

        Owner.ShortcutController.KeyPressed(key, argsModifiers);
    }

    private void OnKeyUp(object? sender, FilteredKeyEventArgs args)
    {
        ProcessShortcutUp(new(args.Key, args.Modifiers));

        Owner.DocumentManagerSubViewModel.ActiveDocument?.EventInlet.OnKeyUp(args.Key);
    }

    private void ProcessShortcutUp(KeyCombination shortcut)
    {
        var transientTool = GetTransientTool(shortcut.Key);

        if (Owner.ShortcutController.LastCommands != null &&
            Owner.ShortcutController.LastCommands.Any(x => x.Shortcut == shortcut) || transientTool is not null)
        {
            Owner.ToolsSubViewModel.HandleToolShortcutUp();
        }
    }

    private void OnMouseDown(object? sender, MouseOnCanvasEventArgs args)
    {
        if (drawingWithRight != null || args.Button is not (MouseButton.Left or MouseButton.Right))
            return;

        if (args.Button == MouseButton.Right && !HandleRightMouseDown())
            return;

        var docManager = Owner.DocumentManagerSubViewModel;
        var activeDocument = docManager.ActiveDocument;
        if (activeDocument == null)
            return;

        drawingWithRight = args.Button == MouseButton.Right;
        Owner.ToolsSubViewModel.UseToolEventInlet(args.PositionOnCanvas, args.Button);
        activeDocument.EventInlet.OnCanvasLeftMouseButtonDown(args.PositionOnCanvas);
    }

    private bool HandleRightMouseDown()
    {
        var tools = Owner.ToolsSubViewModel;

        startedWithEraser = tools.ActiveTool is EraserToolViewModel;

        switch (tools.RightClickMode)
        {
            case RightClickMode.SecondaryColor when tools.ActiveTool.UsesColor:
            case RightClickMode.Erase when tools.ActiveTool is ColorPickerToolViewModel:
                Owner.ColorsSubViewModel.SwapColors(null);
                hadSwapped = true;
                return true;
            case RightClickMode.Erase when tools.ActiveTool.IsErasable:
            {
                HandleRightMouseEraseDown(tools);
                return true;
            }
            case RightClickMode.SecondaryColor when tools.ActiveTool is BrightnessToolViewModel:
                return true;
            case RightClickMode.ContextMenu:
            default:
                return false;
        }
    }

    private void HandleRightMouseEraseDown(IToolsHandler tools)
    {
        var currentToolSize = tools.ActiveTool.Toolbar.Settings.FirstOrDefault(x => x.Name == "ToolSize");
        hadSharedToolbar = tools.EnableSharedToolbar;
        if (currentToolSize != null)
        {
            tools.EnableSharedToolbar = false;
            var toolSize = tools.GetTool<EraserToolViewModel>().Toolbar.Settings.First(x => x.Name == "ToolSize");
            previousEraseSize = (int)toolSize.Value;
            toolSize.Value = tools.ActiveTool is PenToolViewModel { PixelPerfectEnabled: true } ? 1 : currentToolSize.Value;
        }
        else
        {
            previousEraseSize = null;
        }

        tools.SetActiveTool<EraserToolViewModel>(true);
    }
    
    private void OnPreviewMiddleMouseButton()
    {
        Owner.ToolsSubViewModel.SetActiveTool<MoveViewportToolViewModel>(true);
    }

    private void OnMouseMove(object? sender, VecD pos)
    {
        DocumentViewModel? activeDocument = Owner.DocumentManagerSubViewModel.ActiveDocument;
        if (activeDocument is null)
            return;
        activeDocument.EventInlet.OnCanvasMouseMove(pos);
    }

    private void OnMouseUp(object? sender, MouseButton button)
    {
        bool toLeftRightClick = drawingWithRight == null ||
                                (button == MouseButton.Left && drawingWithRight.Value) ||
                                (button == MouseButton.Right && !drawingWithRight.Value);
        
        if (toLeftRightClick && button != MouseButton.Middle)
            return;

        if (Owner.DocumentManagerSubViewModel.ActiveDocument is null)
            return;
        var tools = Owner.ToolsSubViewModel;

        var rightCanUp = (button == MouseButton.Right && tools.RightClickMode == RightClickMode.Erase || tools.RightClickMode == RightClickMode.SecondaryColor);
        
        if (button == MouseButton.Left || rightCanUp)
        {
            Owner.DocumentManagerSubViewModel.ActiveDocument.EventInlet.OnCanvasLeftMouseButtonUp();
        }
        
        drawingWithRight = null;

        HandleRightMouseUp(button, tools);
        
        hadSwapped = false;
    }

    private void HandleRightMouseUp(MouseButton button, IToolsHandler tools)
    {
        switch (button)
        {
            case MouseButton.Middle:
                tools.RestorePreviousTool();
                break;
            case MouseButton.Right when hadSwapped && 
                                        (tools.RightClickMode == RightClickMode.SecondaryColor || 
                                         tools is { ActiveTool: ColorPickerToolViewModel, RightClickMode: RightClickMode.Erase }
                                        ):

                Owner.ColorsSubViewModel.SwapColors(null);
                break;
            case MouseButton.Right when tools.RightClickMode == RightClickMode.Erase:
                HandleRightMouseEraseUp(tools);
                break;
        }
    }

    private void HandleRightMouseEraseUp(IToolsHandler tools)
    {
        if (startedWithEraser)
        {
            return;
        }

        tools.EnableSharedToolbar = hadSharedToolbar;
        if (previousEraseSize != null)
        {
            tools.GetTool<EraserToolViewModel>().Toolbar.Settings.First(x => x.Name == "ToolSize").Value = previousEraseSize.Value;
        }
        tools.RestorePreviousTool();
    }
}

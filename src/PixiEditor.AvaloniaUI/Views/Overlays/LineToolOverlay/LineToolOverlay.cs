﻿using System.Windows.Input;
using Avalonia;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using ChunkyImageLib.DataHolders;
using PixiEditor.AvaloniaUI.Helpers;
using PixiEditor.AvaloniaUI.Models.Controllers.InputDevice;
using PixiEditor.AvaloniaUI.Views.Overlays.Handles;
using PixiEditor.AvaloniaUI.Views.Overlays.TransformOverlay;
using PixiEditor.DrawingApi.Core.Numerics;

namespace PixiEditor.AvaloniaUI.Views.Overlays.LineToolOverlay;
internal class LineToolOverlay : Overlay
{
    public static readonly StyledProperty<VecD> LineStartProperty =
        AvaloniaProperty.Register<LineToolOverlay, VecD>(nameof(LineStart), defaultValue: VecD.Zero);

    public VecD LineStart
    {
        get => GetValue(LineStartProperty);
        set => SetValue(LineStartProperty, value);
    }

    public static readonly StyledProperty<VecD> LineEndProperty =
        AvaloniaProperty.Register<LineToolOverlay, VecD>(nameof(LineEnd), defaultValue: VecD.Zero);

    public VecD LineEnd
    {
        get => GetValue(LineEndProperty);
        set => SetValue(LineEndProperty, value);
    }

    public static readonly StyledProperty<ICommand?> ActionCompletedProperty =
        AvaloniaProperty.Register<LineToolOverlay, ICommand?>(nameof(ActionCompleted));

    public ICommand? ActionCompleted
    {
        get => GetValue(ActionCompletedProperty);
        set => SetValue(ActionCompletedProperty, value);
    }

    static LineToolOverlay()
    {
        AffectsRender<LineToolOverlay>(ZoomScaleProperty, LineStartProperty, LineEndProperty);
    }

    private Pen blackPen = new Pen(Brushes.Black, 1);

    private VecD mouseDownPos = VecD.Zero;
    private VecD lineStartOnMouseDown = VecD.Zero;
    private VecD lineEndOnMouseDown = VecD.Zero;

    private bool movedWhileMouseDown = false;

    private MouseUpdateController mouseUpdateController;

    private RectangleHandle startHandle;
    private RectangleHandle endHandle;
    private TransformHandle moveHandle;

    public LineToolOverlay()
    {
        Cursor = new Cursor(StandardCursorType.Arrow);

        startHandle = new AnchorHandle(this);
        startHandle.HandlePen = blackPen;
        startHandle.OnDrag += StartHandleOnDrag;
        AddHandle(startHandle);

        endHandle = new AnchorHandle(this);
        startHandle.HandlePen = blackPen;
        endHandle.OnDrag += EndHandleOnDrag;
        AddHandle(endHandle);

        moveHandle = new TransformHandle(this);
        moveHandle.HandlePen = blackPen;
        moveHandle.OnDrag += MoveHandleOnDrag;
        AddHandle(moveHandle);

        Loaded += OnLoaded;
        Unloaded += OnUnloaded;
    }

    private void OnLoaded(object? sender, RoutedEventArgs e)
    {
        //TODO: Ensure this bug doesn't happen in Avalonia, currently Handle classes are taking care of dragging events
        //mouseUpdateController = new MouseUpdateController(this, MouseMoved);
    }
    
    private void OnUnloaded(object? sender, RoutedEventArgs e)
    {
        //mouseUpdateController?.Dispose();
    }

    protected override void ZoomChanged(double newZoom)
    {
        blackPen.Thickness = 1.0 / newZoom;
    }

    public override void Render(DrawingContext context)
    {
        startHandle.Position = LineStart;
        endHandle.Position = LineEnd;
        VecD center = (LineStart + LineEnd) / 2;
        VecD size = LineEnd - LineStart;
        moveHandle.Position = TransformHelper.GetHandlePos(new ShapeCorners(center, size), ZoomScale, moveHandle.Size);

        startHandle.Draw(context);
        endHandle.Draw(context);
        moveHandle.Draw(context);
    }

    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        base.OnPointerPressed(e);

        MouseButton changedButton = e.GetMouseButton(this);

        if (changedButton != MouseButton.Left)
            return;

        VecD pos = TransformHelper.ToVecD(e.GetPosition(this));

        movedWhileMouseDown = false;
        mouseDownPos = pos;
        lineStartOnMouseDown = LineStart;
        lineEndOnMouseDown = LineEnd;

        e.Pointer.Capture(this);
    }

    private void StartHandleOnDrag(Handle source, VecD position)
    {
        LineStart = position;
        movedWhileMouseDown = true;
    }

    private void EndHandleOnDrag(Handle source, VecD position)
    {
        LineEnd = position;
        movedWhileMouseDown = true;
    }

    private void MoveHandleOnDrag(Handle source, VecD position)
    {
        var delta = position - mouseDownPos;

        LineStart = lineStartOnMouseDown + delta;
        LineEnd = lineEndOnMouseDown + delta;

        movedWhileMouseDown = true;
    }

    protected override void OnPointerReleased(PointerReleasedEventArgs e)
    {
        base.OnPointerReleased(e);
        if (e.InitialPressMouseButton != MouseButton.Left)
            return;

        if (movedWhileMouseDown && ActionCompleted is not null && ActionCompleted.CanExecute(null))
            ActionCompleted.Execute(null);
    }
}

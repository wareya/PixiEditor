﻿using System.Windows.Input;
using Avalonia;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using ChunkyImageLib.DataHolders;
using PixiEditor.Helpers;
using PixiEditor.Models.Controllers.InputDevice;
using PixiEditor.DrawingApi.Core.Numerics;
using PixiEditor.Extensions.UI.Overlays;
using PixiEditor.Numerics;
using PixiEditor.Views.Overlays.Handles;
using PixiEditor.Views.Overlays.TransformOverlay;
using Point = Avalonia.Point;

namespace PixiEditor.Views.Overlays.LineToolOverlay;
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
        LineStartProperty.Changed.Subscribe(RenderAffectingPropertyChanged);
        LineEndProperty.Changed.Subscribe(RenderAffectingPropertyChanged);
    }

    private Pen blackPen = new Pen(Brushes.Black, 1);
    private Pen blackDashedPen = new Pen(Brushes.Black, 1) { DashStyle = new DashStyle(new double[] { 2, 4 }, 0) };
    private Pen whiteDashedPen = new Pen(Brushes.White, 1) { DashStyle = new DashStyle(new double[] { 2, 4 }, 2) };

    private VecD mouseDownPos = VecD.Zero;
    private VecD lineStartOnMouseDown = VecD.Zero;
    private VecD lineEndOnMouseDown = VecD.Zero;

    private bool movedWhileMouseDown = false;

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
        endHandle.HandlePen = blackPen;
        endHandle.OnDrag += EndHandleOnDrag;
        AddHandle(endHandle);

        moveHandle = new TransformHandle(this);
        moveHandle.HandlePen = blackPen;
        moveHandle.OnDrag += MoveHandleOnDrag;
        AddHandle(moveHandle);
    }

    protected override void ZoomChanged(double newZoom)
    {
        blackPen.Thickness = 1.0 / newZoom;
        blackDashedPen.Thickness = 2.0 / newZoom;
        whiteDashedPen.Thickness = 2.0 / newZoom;
    }

    public override void RenderOverlay(DrawingContext context, RectD canvasBounds)
    {
        startHandle.Position = LineStart;
        endHandle.Position = LineEnd;
        VecD center = (LineStart + LineEnd) / 2;
        VecD size = LineEnd - LineStart;
        moveHandle.Position = TransformHelper.GetHandlePos(new ShapeCorners(center, size), ZoomScale, moveHandle.Size);

        context.DrawLine(blackDashedPen, new Point(LineStart.X, LineStart.Y), new Point(LineEnd.X, LineEnd.Y));
        context.DrawLine(whiteDashedPen, new Point(LineStart.X, LineStart.Y), new Point(LineEnd.X, LineEnd.Y));
        startHandle.Draw(context);
        endHandle.Draw(context);
        moveHandle.Draw(context);
    }

    protected override void OnOverlayPointerPressed(OverlayPointerArgs args)
    {
        if (args.PointerButton != MouseButton.Left)
            return;

        movedWhileMouseDown = false;
        mouseDownPos = args.Point;
        lineStartOnMouseDown = LineStart;
        lineEndOnMouseDown = LineEnd;

        args.Pointer.Capture(this);
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

    protected override void OnOverlayPointerReleased(OverlayPointerArgs args)
    {
        if (args.InitialPressMouseButton != MouseButton.Left)
            return;

        if (movedWhileMouseDown && ActionCompleted is not null && ActionCompleted.CanExecute(null))
            ActionCompleted.Execute(null);
    }

    private static void RenderAffectingPropertyChanged(AvaloniaPropertyChangedEventArgs<VecD> e)
    {
        if (e.Sender is LineToolOverlay overlay)
        {
            overlay.Refresh();
        }
    }
}

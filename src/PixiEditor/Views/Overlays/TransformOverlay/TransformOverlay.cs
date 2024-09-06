﻿using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Windows.Input;
using Avalonia;
using Avalonia.Input;
using Avalonia.Media;
using ChunkyImageLib.DataHolders;
using PixiEditor.Helpers;
using PixiEditor.Helpers.Extensions;
using PixiEditor.DrawingApi.Core.Numerics;
using PixiEditor.Extensions.UI.Overlays;
using PixiEditor.Helpers.UI;
using PixiEditor.Numerics;
using PixiEditor.Views.Overlays.Handles;
using Point = Avalonia.Point;

namespace PixiEditor.Views.Overlays.TransformOverlay;
#nullable enable
internal class TransformOverlay : Overlay
{
    public static readonly StyledProperty<ShapeCorners> CornersProperty =
        AvaloniaProperty.Register<TransformOverlay, ShapeCorners>(nameof(Corners), defaultValue: default(ShapeCorners));

    public ShapeCorners Corners
    {
        get => GetValue(CornersProperty);
        set => SetValue(CornersProperty, value);
    }

    public static readonly StyledProperty<TransformSideFreedom> SideFreedomProperty =
        AvaloniaProperty.Register<TransformOverlay, TransformSideFreedom>(nameof(SideFreedom), defaultValue: TransformSideFreedom.Locked);

    public TransformSideFreedom SideFreedom
    {
        get => GetValue(SideFreedomProperty);
        set => SetValue(SideFreedomProperty, value);
    }

    public static readonly StyledProperty<TransformCornerFreedom> CornerFreedomProperty =
        AvaloniaProperty.Register<TransformOverlay, TransformCornerFreedom>(nameof(CornerFreedom), defaultValue: TransformCornerFreedom.Locked);

    public TransformCornerFreedom CornerFreedom
    {
        get => GetValue(CornerFreedomProperty);
        set => SetValue(CornerFreedomProperty, value);
    }

    public static readonly StyledProperty<bool> LockRotationProperty =
        AvaloniaProperty.Register<TransformOverlay, bool>(nameof(LockRotation), defaultValue: false);

    public bool LockRotation
    {
        get => GetValue(LockRotationProperty);
        set => SetValue(LockRotationProperty, value);
    }

    public static readonly StyledProperty<bool> SnapToAnglesProperty =
        AvaloniaProperty.Register<TransformOverlay, bool>(nameof(SnapToAngles), defaultValue: false);

    public bool SnapToAngles
    {
        get => GetValue(SnapToAnglesProperty);
        set => SetValue(SnapToAnglesProperty, value);
    }

    public static readonly StyledProperty<TransformState> InternalStateProperty =
        AvaloniaProperty.Register<TransformOverlay, TransformState>(nameof(InternalState), defaultValue: default(TransformState));

    public TransformState InternalState
    {
        get => GetValue(InternalStateProperty);
        set => SetValue(InternalStateProperty, value);
    }

    public static readonly StyledProperty<double> ZoomboxAngleProperty =
        AvaloniaProperty.Register<TransformOverlay, double>(nameof(ZoomboxAngle), defaultValue: 0.0);

    public double ZoomboxAngle
    {
        get => GetValue(ZoomboxAngleProperty);
        set => SetValue(ZoomboxAngleProperty, value);
    }

    public static readonly StyledProperty<bool> CoverWholeScreenProperty =
        AvaloniaProperty.Register<TransformOverlay, bool>(nameof(CoverWholeScreen), defaultValue: true);

    public bool CoverWholeScreen
    {
        get => GetValue(CoverWholeScreenProperty);
        set => SetValue(CoverWholeScreenProperty, value);
    }

    public static readonly StyledProperty<ExecutionTrigger<ShapeCorners>> RequestCornersExecutorProperty = AvaloniaProperty.Register<TransformOverlay, ExecutionTrigger<ShapeCorners>>(
        nameof(RequestCornersExecutor));

    public ExecutionTrigger<ShapeCorners> RequestCornersExecutor
    {
        get => GetValue(RequestCornersExecutorProperty);
        set => SetValue(RequestCornersExecutorProperty, value);
    }

    public static readonly StyledProperty<ICommand?> ActionCompletedProperty =
        AvaloniaProperty.Register<TransformOverlay, ICommand?>(nameof(ActionCompleted));

    public ICommand? ActionCompleted
    {
        get => GetValue(ActionCompletedProperty);
        set => SetValue(ActionCompletedProperty, value);
    }

    static TransformOverlay()
    {
        AffectsRender<TransformOverlay>(CornersProperty, ZoomScaleProperty, SideFreedomProperty, CornerFreedomProperty, LockRotationProperty, SnapToAnglesProperty, InternalStateProperty, ZoomboxAngleProperty, CoverWholeScreenProperty);

        RequestCornersExecutorProperty.Changed.Subscribe(OnCornersExecutorChanged);
    }

    private const int anchorSizeMultiplierForRotation = 15;

    private bool isMoving = false;
    private VecD mousePosOnStartMove = new();
    private VecD originOnStartMove = new();
    private ShapeCorners cornersOnStartMove = new();

    private bool isRotating = false;
    private VecD mousePosOnStartRotate = new();
    private ShapeCorners cornersOnStartRotate = new();
    private double propAngle1OnStartRotate = 0;
    private double propAngle2OnStartRotate = 0;

    private Anchor? capturedAnchor;
    private ShapeCorners cornersOnStartAnchorDrag;
    private VecD mousePosOnStartAnchorDrag;
    private VecD originOnStartAnchorDrag;

    private Pen blackPen = new Pen(Brushes.Black, 1);
    private Pen blackDashedPen = new Pen(Brushes.Black, 1) { DashStyle = new DashStyle(new double[] { 2, 4 }, 0) };
    private Pen whiteDashedPen = new Pen(Brushes.White, 1) { DashStyle = new DashStyle(new double[] { 2, 4 }, 2) };
    private Pen blackFreqDashedPen = new Pen(Brushes.Black, 1) { DashStyle = new DashStyle(new double[] { 2, 2 }, 0) };
    private Pen whiteFreqDashedPen = new Pen(Brushes.White, 1) { DashStyle = new DashStyle(new double[] { 2, 2 }, 2) };

    private AnchorHandle topLeftHandle;
    private AnchorHandle topRightHandle;
    private AnchorHandle bottomLeftHandle;
    private AnchorHandle bottomRightHandle;
    private AnchorHandle topHandle;
    private AnchorHandle bottomHandle;
    private AnchorHandle leftHandle;
    private AnchorHandle rightHandle;
    private RectangleHandle centerHandle;
    private OriginAnchor originHandle;
    private TransformHandle moveHandle;

    private Dictionary<Handle, Anchor> anchorMap = new();
    private List<Handle> snapPoints = new();
    private Handle? snapHandleOfOrigin;

    private Geometry rotateCursorGeometry = Handle.GetHandleGeometry("RotateHandle");

    private VecD lastPointerPos;

    public TransformOverlay()
    {
        topLeftHandle = new AnchorHandle(this);
        topRightHandle = new AnchorHandle(this);
        bottomLeftHandle = new AnchorHandle(this);
        bottomRightHandle = new AnchorHandle(this);
        topHandle = new AnchorHandle(this);
        bottomHandle = new AnchorHandle(this);
        leftHandle = new AnchorHandle(this);
        rightHandle = new AnchorHandle(this);
        moveHandle = new(this);
        centerHandle = new RectangleHandle(this);
        centerHandle.Size = rightHandle.Size;

        originHandle = new(this)
        {
            HandlePen = blackFreqDashedPen, SecondaryHandlePen = whiteFreqDashedPen, HandleBrush = Brushes.Transparent
        };

        AddHandle(originHandle);
        AddHandle(topLeftHandle);
        AddHandle(topRightHandle);
        AddHandle(bottomLeftHandle);
        AddHandle(bottomRightHandle);
        AddHandle(topHandle);
        AddHandle(bottomHandle);
        AddHandle(leftHandle);
        AddHandle(rightHandle);
        AddHandle(centerHandle);
        AddHandle(moveHandle);

        anchorMap.Add(topLeftHandle, Anchor.TopLeft);
        anchorMap.Add(topRightHandle, Anchor.TopRight);
        anchorMap.Add(bottomLeftHandle, Anchor.BottomLeft);
        anchorMap.Add(bottomRightHandle, Anchor.BottomRight);
        anchorMap.Add(topHandle, Anchor.Top);
        anchorMap.Add(bottomHandle, Anchor.Bottom);
        anchorMap.Add(leftHandle, Anchor.Left);
        anchorMap.Add(rightHandle, Anchor.Right);
        anchorMap.Add(originHandle, Anchor.Origin);

        ForAllHandles<RectangleHandle>(snapPoints.Add);

        ForAllHandles<AnchorHandle>(x =>
        {
            x.OnPress += OnAnchorHandlePressed;
            x.OnRelease += OnAnchorHandleReleased;
        });

        originHandle.OnPress += OnAnchorHandlePressed;
        originHandle.OnRelease += OnAnchorHandleReleased;

        moveHandle.OnPress += OnMoveHandlePressed;
        moveHandle.OnRelease += OnMoveHandleReleased;
    }

    public override void RenderOverlay(DrawingContext drawingContext, RectD canvasBounds)
    {
        base.Render(drawingContext);
        DrawOverlay(drawingContext, new(Bounds.Width, Bounds.Height), Corners, InternalState.Origin, ZoomScale);

        if (capturedAnchor is null)
            UpdateRotationCursor(lastPointerPos);
    }

    private void DrawMouseInputArea(DrawingContext context, VecD size)
    {
        if (CoverWholeScreen)
        {
            context.DrawRectangle(Brushes.Transparent, null, new Rect(new Point(-size.X * 50, -size.Y * 50), new Size(size.X * 101, size.Y * 101)));
            return;
        }

        StreamGeometry geometry = new();
        using (StreamGeometryContext ctx = geometry.Open())
        {
            ctx.BeginFigure(TransformHelper.ToPoint(Corners.TopLeft), true);
            ctx.LineTo(TransformHelper.ToPoint(Corners.TopRight));
            ctx.LineTo(TransformHelper.ToPoint(Corners.BottomRight));
            ctx.LineTo(TransformHelper.ToPoint(Corners.BottomLeft));
            ctx.EndFigure(true);
        }

        context.DrawGeometry(Brushes.Transparent, null, geometry);
    }

    private void DrawOverlay
        (DrawingContext context, VecD size, ShapeCorners corners, VecD origin, double zoomboxScale)
    {
        // draw transparent background to enable mouse input
        DrawMouseInputArea(context, size);

        blackPen.Thickness = 1 / zoomboxScale;
        blackDashedPen.Thickness = 1 / zoomboxScale;
        whiteDashedPen.Thickness = 1 / zoomboxScale;
        blackFreqDashedPen.Thickness = 1 / zoomboxScale;
        whiteFreqDashedPen.Thickness = 1 / zoomboxScale;

        VecD topLeft = corners.TopLeft;
        VecD topRight = corners.TopRight;
        VecD bottomLeft = corners.BottomLeft;
        VecD bottomRight = corners.BottomRight;
        VecD top = (topLeft + topRight) / 2;
        VecD bottom = (bottomLeft + bottomRight) / 2;
        VecD left = (topLeft + bottomLeft) / 2;
        VecD right = (topRight + bottomRight) / 2;

        // lines
        context.DrawLine(blackDashedPen, TransformHelper.ToPoint(topLeft), TransformHelper.ToPoint(topRight));
        context.DrawLine(whiteDashedPen, TransformHelper.ToPoint(topLeft), TransformHelper.ToPoint(topRight));
        context.DrawLine(blackDashedPen, TransformHelper.ToPoint(topLeft), TransformHelper.ToPoint(bottomLeft));
        context.DrawLine(whiteDashedPen, TransformHelper.ToPoint(topLeft), TransformHelper.ToPoint(bottomLeft));
        context.DrawLine(blackDashedPen, TransformHelper.ToPoint(bottomRight), TransformHelper.ToPoint(bottomLeft));
        context.DrawLine(whiteDashedPen, TransformHelper.ToPoint(bottomRight), TransformHelper.ToPoint(bottomLeft));
        context.DrawLine(blackDashedPen, TransformHelper.ToPoint(bottomRight), TransformHelper.ToPoint(topRight));
        context.DrawLine(whiteDashedPen, TransformHelper.ToPoint(bottomRight), TransformHelper.ToPoint(topRight));
        
        // corner anchors

        centerHandle.Position = VecD.Zero;
        topLeftHandle.Position = topLeft;
        topRightHandle.Position = topRight;
        bottomLeftHandle.Position = bottomLeft;
        bottomRightHandle.Position = bottomRight;
        topHandle.Position = top;
        bottomHandle.Position = bottom;
        leftHandle.Position = left;
        rightHandle.Position = right;
        originHandle.Position = InternalState.Origin;
        moveHandle.Position = TransformHelper.GetHandlePos(Corners, ZoomScale, moveHandle.Size);

        topLeftHandle.Draw(context);
        topRightHandle.Draw(context);
        bottomLeftHandle.Draw(context);
        bottomRightHandle.Draw(context);
        topHandle.Draw(context);
        bottomHandle.Draw(context);
        leftHandle.Draw(context);
        rightHandle.Draw(context);
        originHandle.Draw(context);
        moveHandle.Draw(context);

        if (capturedAnchor == Anchor.Origin)
        {
            centerHandle.Position = Corners.RectCenter;
            centerHandle.Draw(context);
        }

        // rotate cursor
        context.DrawGeometry(Brushes.White, blackPen, rotateCursorGeometry);
    }

    private void OnAnchorHandlePressed(Handle source, VecD position)
    {
        capturedAnchor = anchorMap[source];
        cornersOnStartAnchorDrag = Corners;
        originOnStartAnchorDrag = InternalState.Origin;
        mousePosOnStartAnchorDrag = lastPointerPos;

        if (source == originHandle)
        {
            snapHandleOfOrigin = null;
        }
    }

    private void OnMoveHandlePressed(Handle source, VecD position)
    {
        StartMoving(position);
    }

    protected override void OnOverlayPointerExited(OverlayPointerArgs args)
    {
        rotateCursorGeometry.Transform = new ScaleTransform(0, 0);
        Refresh();
    }

    protected override void OnOverlayPointerPressed(OverlayPointerArgs args)
    {
        if (args.PointerButton != MouseButton.Left)
            return;

        if(Handles.Any(x => x.IsWithinHandle(x.Position, args.Point, ZoomScale))) return;

        if (!CanRotate(args.Point))
        {
            StartMoving(args.Point);
        }
        else if (!LockRotation)
        {
            isRotating = true;
            mousePosOnStartRotate = args.Point;
            cornersOnStartRotate = Corners;
            propAngle1OnStartRotate = InternalState.ProportionalAngle1;
            propAngle2OnStartRotate = InternalState.ProportionalAngle2;
        }
        else
        {
            return;
        }
        
        args.Pointer.Capture(this);
        args.Handled = true;
    }

    protected override void OnOverlayPointerMoved(OverlayPointerArgs e)
    {
        Cursor finalCursor = new Cursor(StandardCursorType.Arrow);

        lastPointerPos = e.Point;
        VecD pos = lastPointerPos;

        if (isMoving)
        {
            HandleTransform(pos);
            finalCursor = new Cursor(StandardCursorType.DragMove);
        }

        if (capturedAnchor is not null)
        {
            HandleCapturedAnchorMovement(e);
            return;
        }

        if (UpdateRotationCursor(e.Point))
        {
            finalCursor = new Cursor(StandardCursorType.None);
        }

        Anchor? anchor = TransformHelper.GetAnchorInPosition(pos, Corners, InternalState.Origin, ZoomScale, topLeftHandle.Size);

        if (isRotating)
        {
            finalCursor = HandleRotate(pos);
        }
        else if (anchor is not null)
        {
            if ((TransformHelper.IsCorner((Anchor)anchor) && CornerFreedom == TransformCornerFreedom.Free) ||
                (TransformHelper.IsSide((Anchor)anchor) && SideFreedom == TransformSideFreedom.Free))
                finalCursor = new Cursor(StandardCursorType.Arrow);
            else
                finalCursor = TransformHelper.GetResizeCursor((Anchor)anchor, Corners, ZoomboxAngle);
        }

        if (Cursor != finalCursor)
            Cursor = finalCursor;

        Refresh();
    }

    protected override void OnOverlayPointerReleased(OverlayPointerArgs e)
    {
        if (e.InitialPressMouseButton != MouseButton.Left)
            return;

        if (isRotating)
        {
            isRotating = false;
            e.Pointer.Capture(null);
            Cursor = new Cursor(StandardCursorType.Arrow);
            var pos = e.Point;
            UpdateRotationCursor(pos);
        }

        StopMoving();
    }

    public override bool TestHit(VecD point)
    {
        return base.TestHit(point) || Corners.AsScaled(1.25f).IsPointInside(point);
    }

    private void OnMoveHandleReleased(Handle obj)
    {
        StopMoving();
    }

    private void StopMoving()
    {
        isMoving = false;

        if (ActionCompleted is not null && ActionCompleted.CanExecute(null))
            ActionCompleted.Execute(null);
    }

    private void StartMoving(VecD position)
    {
        isMoving = true;
        mousePosOnStartMove = position;
        originOnStartMove = InternalState.Origin;
        cornersOnStartMove = Corners;
    }

    private void HandleTransform(VecD pos)
    {
        VecD delta = pos - mousePosOnStartMove;

        if (Corners.IsSnappedToPixels)
            delta = delta.Round();

        Corners = new ShapeCorners()
        {
            BottomLeft = cornersOnStartMove.BottomLeft + delta,
            BottomRight = cornersOnStartMove.BottomRight + delta,
            TopLeft = cornersOnStartMove.TopLeft + delta,
            TopRight = cornersOnStartMove.TopRight + delta,
        };

        InternalState = InternalState with { Origin = originOnStartMove + delta };
    }

    private Cursor HandleRotate(VecD pos)
    {
        Cursor finalCursor;
        finalCursor = new Cursor(StandardCursorType.None);
        double angle = (mousePosOnStartRotate - InternalState.Origin).CCWAngleTo(pos - InternalState.Origin);
        if (SnapToAngles)
            angle = TransformHelper.FindSnappingAngle(cornersOnStartRotate, angle);
        InternalState = InternalState with
        {
            ProportionalAngle1 = propAngle1OnStartRotate + angle, ProportionalAngle2 = propAngle2OnStartRotate + angle,
        };

        Corners = TransformUpdateHelper.UpdateShapeFromRotation(cornersOnStartRotate, InternalState.Origin, angle);

        return finalCursor;
    }

    private bool CanRotate(VecD mousePos)
    {
        return !Corners.IsPointInside(mousePos) && Handles.All(x => !x.IsWithinHandle(x.Position, mousePos, ZoomScale)) && TestHit(mousePos);
    }

    private bool UpdateRotationCursor(VecD mousePos)
    {
        if ((!CanRotate(mousePos) && !isRotating) || LockRotation)
        {
            rotateCursorGeometry.Transform = new ScaleTransform(0, 0);
            return false;
        }

        var matrix = new TranslateTransform(mousePos.X, mousePos.Y).Value;
        double angle = (mousePos - InternalState.Origin).Angle * 180 / Math.PI - 90;
        matrix = matrix.RotateAt(angle, mousePos.X, mousePos.Y);
        matrix = matrix.ScaleAt(8 / ZoomScale, 8 / ZoomScale, mousePos.X, mousePos.Y);
        rotateCursorGeometry.Transform = new MatrixTransform(matrix);
        return true;
    }

    private void HandleCapturedAnchorMovement(OverlayPointerArgs e)
    {
        if (capturedAnchor is null)
            throw new InvalidOperationException("No anchor is captured");

        if ((TransformHelper.IsCorner((Anchor)capturedAnchor) && CornerFreedom == TransformCornerFreedom.Locked) ||
            (TransformHelper.IsSide((Anchor)capturedAnchor) && SideFreedom == TransformSideFreedom.Locked))
            return;

        VecD pos = e.Point;

        if (TransformHelper.IsCorner((Anchor)capturedAnchor))
        {
            VecD targetPos = TransformHelper.GetAnchorPosition(cornersOnStartAnchorDrag, (Anchor)capturedAnchor) + pos - mousePosOnStartAnchorDrag;
            ShapeCorners? newCorners = TransformUpdateHelper.UpdateShapeFromCorner
                ((Anchor)capturedAnchor, CornerFreedom, InternalState.ProportionalAngle1, InternalState.ProportionalAngle2, cornersOnStartAnchorDrag, targetPos);
            if (newCorners is not null)
            {
                bool shouldSnap = (CornerFreedom is TransformCornerFreedom.ScaleProportionally or TransformCornerFreedom.Scale) && Corners.IsSnappedToPixels;
                Corners = shouldSnap ? TransformHelper.SnapToPixels((ShapeCorners)newCorners) : (ShapeCorners)newCorners;
            }
            UpdateOriginPos();
        }
        else if (TransformHelper.IsSide((Anchor)capturedAnchor))
        {
            VecD targetPos = TransformHelper.GetAnchorPosition(cornersOnStartAnchorDrag, (Anchor)capturedAnchor) + pos - mousePosOnStartAnchorDrag;
            ShapeCorners? newCorners = TransformUpdateHelper.UpdateShapeFromSide
                ((Anchor)capturedAnchor, SideFreedom, InternalState.ProportionalAngle1, InternalState.ProportionalAngle2, cornersOnStartAnchorDrag, targetPos);
            if (newCorners is not null)
            {
                bool shouldSnap = (SideFreedom is TransformSideFreedom.ScaleProportionally or TransformSideFreedom.Stretch) && Corners.IsSnappedToPixels;
                Corners = shouldSnap ? TransformHelper.SnapToPixels((ShapeCorners)newCorners) : (ShapeCorners)newCorners;
            }
            UpdateOriginPos();
        }
        else if (capturedAnchor == Anchor.Origin)
        {
            pos = HandleSnap(pos, out bool snapped);
            InternalState = InternalState with { OriginWasManuallyDragged = !snapped, Origin = pos, };
        }

        Refresh();
    }

    private void UpdateOriginPos()
    {
        if (!InternalState.OriginWasManuallyDragged)
        {
            if (snapHandleOfOrigin == centerHandle)
            {
                snapHandleOfOrigin.Position = TransformHelper.OriginFromCorners(Corners);
            }

            InternalState = InternalState with
            {
                Origin = snapHandleOfOrigin?.Position ?? TransformHelper.OriginFromCorners(Corners)
            };
        }
    }

    private VecD HandleSnap(VecD pos, out bool snapped)
    {
        foreach (var snapPoint in snapPoints)
        {
            if (snapPoint == originHandle)
                continue;

            if (TransformHelper.IsWithinHandle(snapPoint.Position, pos, ZoomScale, topHandle.Size))
            {
                snapped = true;
                return snapPoint.Position;
            }
        }

        snapped = false;
        return originOnStartAnchorDrag + pos - mousePosOnStartAnchorDrag;
    }

    private void OnAnchorHandleReleased(Handle source)
    {
        capturedAnchor = null;

        if(source == originHandle)
        {
            snapHandleOfOrigin = GetSnapHandleOfOrigin();
            InternalState = InternalState with { OriginWasManuallyDragged = snapHandleOfOrigin is null };
        }

        if (ActionCompleted is not null && ActionCompleted.CanExecute(null))
            ActionCompleted.Execute(null);
    }

    private Handle? GetSnapHandleOfOrigin()
    {
        foreach (var snapPoint in snapPoints)
        {
            if (snapPoint == originHandle)
                continue;

            if (originHandle.Position == snapPoint.Position)
            {
                return snapPoint;
            }
        }

        return null;
    }

    private void OnRequestedCorners(object sender, ShapeCorners corners)
    {
        isMoving = false;
        isRotating = false;
        Corners = corners; 
        InternalState = new()
        {
            ProportionalAngle1 = (Corners.BottomRight - Corners.TopLeft).Angle,
            ProportionalAngle2 = (Corners.TopRight - Corners.BottomLeft).Angle,
            OriginWasManuallyDragged = false,
            Origin = TransformHelper.OriginFromCorners(Corners),
        };
    }
    
    private static void OnCornersExecutorChanged(AvaloniaPropertyChangedEventArgs<ExecutionTrigger<ShapeCorners>> args)
    {
        TransformOverlay overlay = (TransformOverlay)args.Sender;
        if (args.OldValue != null)
            args.OldValue.Value.Triggered -= overlay.OnRequestedCorners;
        if (args.NewValue != null)
            args.NewValue.Value.Triggered += overlay.OnRequestedCorners;
    }
}

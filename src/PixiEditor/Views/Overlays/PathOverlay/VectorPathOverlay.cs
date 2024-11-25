﻿using System.Windows.Input;
using Avalonia;
using Avalonia.Input;
using Drawie.Backend.Core.Numerics;
using Drawie.Backend.Core.Vector;
using Drawie.Numerics;
using PixiEditor.Extensions.UI.Overlays;
using PixiEditor.Models.Controllers.InputDevice;
using PixiEditor.Views.Overlays.Drawables;
using PixiEditor.Views.Overlays.Handles;
using Canvas = Drawie.Backend.Core.Surfaces.Canvas;

namespace PixiEditor.Views.Overlays.PathOverlay;

// If you need to make any changes in this overlay, I feel sorry for you
public class VectorPathOverlay : Overlay
{
    public static readonly StyledProperty<VectorPath> PathProperty =
        AvaloniaProperty.Register<VectorPathOverlay, VectorPath>(
            nameof(Path));

    public static readonly StyledProperty<SnappingController> SnappingControllerProperty =
        AvaloniaProperty.Register<VectorPathOverlay, SnappingController>(
            nameof(SnappingController));

    public SnappingController SnappingController
    {
        get => GetValue(SnappingControllerProperty);
        set => SetValue(SnappingControllerProperty, value);
    }

    public VectorPath Path
    {
        get => GetValue(PathProperty);
        set => SetValue(PathProperty, value);
    }

    public static readonly StyledProperty<ICommand> AddToUndoCommandProperty =
        AvaloniaProperty.Register<VectorPathOverlay, ICommand>(
            nameof(AddToUndoCommand));

    public ICommand AddToUndoCommand
    {
        get => GetValue(AddToUndoCommandProperty);
        set => SetValue(AddToUndoCommandProperty, value);
    }

    private DashedStroke dashedStroke = new DashedStroke();
    private TransformHandle transformHandle;

    private List<AnchorHandle> anchorHandles = new();
    private List<ControlPointHandle> controlPointHandles = new();

    private VecD posOnStartDrag;
    private VectorPath pathOnStartDrag;

    static VectorPathOverlay()
    {
        AffectsOverlayRender(PathProperty);
        PathProperty.Changed.Subscribe(OnPathChanged);
    }

    public VectorPathOverlay()
    {
        transformHandle = new TransformHandle(this);
        transformHandle.OnPress += MoveHandlePress;
        transformHandle.OnDrag += MoveHandleDrag;

        AddHandle(transformHandle);
    }

    protected override void ZoomChanged(double newZoom)
    {
        dashedStroke.UpdateZoom((float)newZoom);
        transformHandle.ZoomScale = newZoom;
    }

    public override void RenderOverlay(Canvas context, RectD canvasBounds)
    {
        if (Path is null)
        {
            return;
        }

        dashedStroke.Draw(context, Path);

        AdjustHandles(Path.PointCount);
        RenderHandles(context);

        if (IsOverAnyHandle())
        {
            TryHighlightSnap(null, null);
        }
    }

    public override bool CanRender()
    {
        return Path != null;
    }

    private void RenderHandles(Canvas context)
    {
        bool anySelected = false;
        int anchor = 0;
        int controlPoint = 0;
        foreach (var verb in Path)
        {
            if (anchor == Path.VerbCount - 1 && !anySelected)
            {
                GetHandleAt(anchor).IsSelected = true;
            }

            anySelected = anySelected || GetHandleAt(anchor).IsSelected;

            VecF verbPointPos = GetVerbPointPos(verb);

            if (verb.verb == PathVerb.Cubic)
            {
                VecD controlPoint1 = (VecD)verb.points[1];
                VecD controlPoint2 = (VecD)verb.points[2];

                controlPointHandles[controlPoint].Position = controlPoint1;
                controlPointHandles[controlPoint].Draw(context);

                controlPointHandles[controlPoint + 1].Position = controlPoint2;
                controlPointHandles[controlPoint + 1].Draw(context);

                controlPoint += 2;
            }
            else if (verb.verb == PathVerb.Close)
            {
                continue;
            }

            anchorHandles[anchor].Position = new VecD(verbPointPos.X, verbPointPos.Y);
            anchorHandles[anchor].Draw(context);

            anchor++;
        }

        transformHandle.Position = Path.TightBounds.BottomRight + new VecD(1, 1);
        transformHandle.Draw(context);
    }

    private void AdjustHandles(int pointsCount)
    {
        int anchorCount = anchorHandles.Count;
        int totalHandles = anchorCount + controlPointHandles.Count;
        if (totalHandles != pointsCount)
        {
            if (totalHandles > pointsCount)
            {
                RecreateHandles();
            }
            else
            {
                int missingControlPoints = CalculateMissingHandles(controlPointHandles.Count, true);
                int missingAnchors = CalculateMissingHandles(anchorHandles.Count, false);
                for (int i = 0; i < missingAnchors; i++)
                {
                    CreateHandle(anchorHandles.Count);
                }

                for (int i = 0; i < missingControlPoints; i++)
                {
                    CreateHandle(controlPointHandles.Count, true);
                }


                SelectAnchor(GetHandleAt(pointsCount - 1));
            }

            ConnectControlPointsToAnchors();
        }
    }

    private void ConnectControlPointsToAnchors()
    {
        int controlPointIndex = 0;
        int anchorIndex = 0;
        foreach (var data in Path)
        {
            if (data.verb == PathVerb.Cubic)
            {
                AnchorHandle previousAnchor = anchorHandles.ElementAtOrDefault(anchorIndex - 1);
                AnchorHandle nextAnchor = anchorHandles.ElementAtOrDefault(anchorIndex);

                if (previousAnchor != null)
                {
                    controlPointHandles[controlPointIndex].ConnectedTo = previousAnchor; 
                }

                controlPointHandles[controlPointIndex + 1].ConnectedTo = nextAnchor;
                controlPointIndex += 2;
            }

            anchorIndex++;
        }
    }

    private int CalculateMissingHandles(int handleCount, bool isControlPoint)
    {
        int totalHandles = 0;
        int totalControlPoints = 0;

        foreach (var point in Path)
        {
            if (point.verb == PathVerb.Cubic)
            {
                totalControlPoints += 2;
            }

            totalHandles++;
        }

        return isControlPoint ? totalControlPoints - handleCount : totalHandles - handleCount;
    }

    private void RecreateHandles()
    {
        int previouslySelectedIndex = -1;

        for (int i = anchorHandles.Count - 1; i >= 0; i--)
        {
            var handle = anchorHandles[i];
            handle.OnPress -= OnHandlePress;
            handle.OnDrag -= OnHandleDrag;
            handle.OnRelease -= OnHandleRelease;
            handle.OnTap -= OnHandleTap;
            if (handle is { IsSelected: true })
            {
                previouslySelectedIndex = i;
            }

            Handles.Remove(handle);
        }

        for (int i = controlPointHandles.Count - 1; i >= 0; i--)
        {
            Handles.Remove(controlPointHandles[i]);
        }

        anchorHandles.Clear();
        controlPointHandles.Clear();
        SnappingController.RemoveAll("editingPath");

        foreach (var path in Path)
        {
            CreateHandle(anchorHandles.Count);
            if (path.verb == PathVerb.Cubic)
            {
                CreateHandle(controlPointHandles.Count, true);
                CreateHandle(controlPointHandles.Count, true);
            }
        }
    }

    private bool IsOverAnyHandle()
    {
        return Handles.Any(handle => handle.IsHovered);
    }

    private void MoveHandlePress(Handle source, OverlayPointerArgs args)
    {
        posOnStartDrag = args.Point;
        pathOnStartDrag?.Dispose();
        pathOnStartDrag = new VectorPath(Path);
        TryHighlightSnap(null, null);
        args.Pointer.Capture(this);
        args.Handled = true;
    }


    private void MoveHandleDrag(Handle source, OverlayPointerArgs args)
    {
        var delta = args.Point - posOnStartDrag;

        VectorPath updatedPath = new VectorPath(pathOnStartDrag);

        delta = TryFindAnySnap(delta, pathOnStartDrag, out string axisX, out string axisY);
        updatedPath.Transform(Matrix3X3.CreateTranslation((float)delta.X, (float)delta.Y));

        TryHighlightSnap(axisX, axisY);

        Path = updatedPath;
        args.Handled = true;
    }

    private void CreateHandle(int atIndex, bool isControlPoint = false)
    {
        if (!isControlPoint)
        {
            AnchorHandle anchor = new AnchorHandle(this);
            anchorHandles.Add(anchor);

            anchor.OnPress += OnHandlePress;
            anchor.OnDrag += OnHandleDrag;
            anchor.OnRelease += OnHandleRelease;
            anchor.OnTap += OnHandleTap;
            AddHandle(anchor);
            SnappingController.AddXYAxis($"editingPath[{atIndex}]", () => anchor.Position);
        }
        else
        {
            var controlPoint = new ControlPointHandle(this);
            controlPoint.OnDrag += OnControlPointDrag;
            controlPointHandles.Add(controlPoint);
            AddHandle(controlPoint);
        }
    }

    private void OnHandleTap(Handle handle, OverlayPointerArgs args)
    {
        if (handle is not AnchorHandle anchorHandle)
        {
            return;
        }

        if (Path.IsClosed)
        {
            return;
        }

        VectorPath newPath = new VectorPath(Path);
        if (args.Modifiers.HasFlag(KeyModifiers.Control))
        {
            SelectAnchor(anchorHandle);
            return;
        }

        if (IsFirstHandle(anchorHandle))
        {
            newPath.Close();
        }
        else
        {
            VecD pos = anchorHandle.Position;
            newPath.LineTo(new VecF((float)pos.X, (float)pos.Y));
        }

        Path = newPath;
    }

    private bool IsFirstHandle(AnchorHandle handle)
    {
        return anchorHandles.IndexOf(handle) == 0;
    }

    private void SelectAnchor(AnchorHandle handle)
    {
        foreach (var anchorHandle in anchorHandles)
        {
            anchorHandle.IsSelected = anchorHandle == handle;
        }
    }

    private void OnHandlePress(Handle source, OverlayPointerArgs args)
    {
        if (source is AnchorHandle anchorHandle)
        {
            SnappingController.RemoveAll($"editingPath[{anchorHandles.IndexOf(anchorHandle)}]");
            CaptureHandle(source);

            if (!args.Modifiers.HasFlag(KeyModifiers.Control)) return;

            var newPath = ConvertTouchingLineVerbsToCubic(anchorHandle);

            Path = newPath;
        }
    }

    // To have continous spline, verb before and after a point must be a cubic with proper control points
    private VectorPath ConvertTouchingLineVerbsToCubic(AnchorHandle anchorHandle)
    {
        bool convertNextToCubic = false;
        int i = 0;
        VectorPath newPath = new VectorPath();
        int index = anchorHandles.IndexOf(anchorHandle);

        foreach (var data in Path)
        {
            if (data.verb == PathVerb.Line)
            {
                if (i == index)
                {
                    newPath.CubicTo(data.points[0], data.points[1], data.points[1]);
                    convertNextToCubic = true;
                }
                else
                {
                    if (convertNextToCubic)
                    {
                        newPath.CubicTo(data.points[0], data.points[1], data.points[1]);
                        convertNextToCubic = false;
                    }
                    else
                    {
                        newPath.LineTo(data.points[1]);
                    }
                }
            }
            else
            {
                DefaultPathVerb(data, newPath);
            }

            i++;
        }

        return newPath;
    }

    private void OnHandleDrag(Handle source, OverlayPointerArgs args)
    {
        if (source is not AnchorHandle handle)
        {
            return;
        }

        var index = anchorHandles.IndexOf(handle);
        VectorPath newPath = new VectorPath();

        bool pointHandled = false;
        int i = 0;

        VecF previousDelta = new VecF();
        bool wasPreviousControlPoint = false;
        VecF previousControlPoint = new VecF();
        int controlPointIndex = 0;
        var targetControlPoint = controlPointHandles.FirstOrDefault(x => x.ConnectedTo == handle);
        int targetControlPointIndex = controlPointHandles.IndexOf(targetControlPoint);

        foreach (var data in Path)
        {
            VecF point;
            switch (data.verb)
            {
                case PathVerb.Move:
                    point = data.points[0];
                    point = TryApplyNewPos(args, i, index, point);

                    newPath.MoveTo(point);
                    previousDelta = point - data.points[0];
                    i++;
                    break;
                case PathVerb.Line:
                    point = data.points[1];
                    point = TryApplyNewPos(args, i, index, point);

                    newPath.LineTo(point);

                    i++;
                    break;
                case PathVerb.Cubic:
                    if (args.Modifiers.HasFlag(KeyModifiers.Control))
                    {
                        HandleCubicControlContinousDrag(args, controlPointIndex, targetControlPointIndex, data, ref wasPreviousControlPoint,
                            ref previousControlPoint, newPath);
                        controlPointIndex += 2;
                    }
                    else
                    {
                        point = data.points[3];
                        point = TryApplyNewPos(args, i, index, point);

                        VecF mid1Delta = previousDelta;

                        VecF mid2Delta = point - data.points[3];
                        
                        newPath.CubicTo(data.points[1] + mid1Delta, data.points[2] + mid2Delta, point);
                        
                        previousDelta = mid2Delta;
                    }

                    i++;
                    break;
                case PathVerb.Quad:
                    newPath.QuadTo(data.points[1], data.points[2]);
                    break;
                case PathVerb.Conic:
                    newPath.ConicTo(data.points[1], data.points[2], data.points[3].X);
                    break;
                case PathVerb.Close:
                    newPath.Close();
                    i++;
                    break;
                case PathVerb.Done:
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        Path = newPath;
    }

    private void OnControlPointDrag(Handle source, OverlayPointerArgs args)
    {
        if (source is not ControlPointHandle controlPointHandle)
        {
            return;
        }

        var index = controlPointHandles.IndexOf(controlPointHandle);
        VectorPath newPath = new VectorPath();

        int i = 0;
        bool wasPreviousControlPoint = false;
        VecF previousControlPoint = new VecF();

        foreach (var data in Path)
        {
            VecF point;
            switch (data.verb)
            {
                case PathVerb.Move:
                    newPath.MoveTo(data.points[0]);
                    break;
                case PathVerb.Line:
                    point = data.points[1];
                    newPath.LineTo(point);
                    break;
                case PathVerb.Cubic:
                    HandleCubicControlContinousDrag(args, i, index, data, ref wasPreviousControlPoint,
                        ref previousControlPoint,
                        newPath);
                    i += 2;
                    break;
                case PathVerb.Quad:
                    newPath.QuadTo(data.points[1], data.points[2]);
                    break;
                case PathVerb.Conic:
                    newPath.ConicTo(data.points[1], data.points[2], data.points[3].X);
                    break;
                case PathVerb.Close:
                    newPath.Close();
                    i++;
                    break;
                case PathVerb.Done:
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        Path = newPath;
    }

    private void HandleCubicControlContinousDrag(OverlayPointerArgs args, int i, int index,
        (PathVerb verb, VecF[] points) data, ref bool wasPreviousControlPoint, ref VecF previousControlPoint,
        VectorPath newPath)
    {
        bool isFirstControlPoint = i == index;
        bool isSecondControlPoint = i + 1 == index;
        bool isNextFirstControlPoint = i + 2 == index;

        VecF controlPoint1 = data.points[1];
        VecF controlPoint2 = data.points[2];
        VecF endPoint = data.points[3];

        if (isFirstControlPoint)
        {
            controlPoint1 = TryApplyNewPos(args, i, index, controlPoint1);
        }
        else if (isSecondControlPoint)
        {
            controlPoint2 = TryApplyNewPos(args, i + 1, index, controlPoint2);
            wasPreviousControlPoint = true;
            previousControlPoint = controlPoint2;
        }
        else if (isNextFirstControlPoint)
        {
            VecD mirroredControlPoint = GetMirroredControlPoint(
                TryApplyNewPos(args, i + 2, index, controlPoint1), endPoint);
            controlPoint2 = (VecF)mirroredControlPoint;
        }
        else if (wasPreviousControlPoint)
        {
            VecD mirroredControlPoint = GetMirroredControlPoint(
                previousControlPoint, data.points[0]);
            controlPoint1 = (VecF)mirroredControlPoint;
            wasPreviousControlPoint = false;
        }

        newPath.CubicTo(controlPoint1, controlPoint2, endPoint);
    }

    private VecD GetMirroredControlPoint(VecF controlPoint, VecF endPoint)
    {
        return new VecD(2 * endPoint.X - controlPoint.X, 2 * endPoint.Y - controlPoint.Y);
    }

    private VecF GetVerbPointPos((PathVerb verb, VecF[] points) data)
    {
        switch (data.verb)
        {
            case PathVerb.Move:
                return data.points[0];
            case PathVerb.Line:
                return data.points[1];
            case PathVerb.Quad:
                return data.points[2];
            case PathVerb.Cubic:
                return data.points[3];
            case PathVerb.Conic:
                return data.points[2];
            case PathVerb.Close:
                return data.points[0];
            case PathVerb.Done:
                return new VecF();
            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    private VecF TryApplyNewPos(OverlayPointerArgs args, int i, int index, VecF point)
    {
        if (i == index)
        {
            var snappedPoint = SnappingController.GetSnapPoint(args.Point, out string axisX, out string axisY);
            point = new VecF((float)snappedPoint.X, (float)snappedPoint.Y);
            TryHighlightSnap(axisX, axisY);
        }

        return point;
    }

    private void OnHandleRelease(Handle source, OverlayPointerArgs args)
    {
        if (source is not AnchorHandle anchorHandle)
        {
            return;
        }

        AddToUndoCommand.Execute(Path);

        SnappingController.AddXYAxis($"editingPath[{anchorHandles.IndexOf(anchorHandle)}]", () => source.Position);

        SnappingController.HighlightedXAxis = null;
        SnappingController.HighlightedYAxis = null;

        Refresh();
    }

    private void TryHighlightSnap(string axisX, string axisY)
    {
        SnappingController.HighlightedXAxis = axisX;
        SnappingController.HighlightedYAxis = axisY;
        SnappingController.HighlightedPoint = null;
    }

    private AnchorHandle? GetHandleAt(int index)
    {
        if (index < 0 || index >= anchorHandles.Count)
        {
            return null;
        }

        return anchorHandles[index];
    }

    private void ClearAnchorHandles()
    {
        foreach (var handle in anchorHandles)
        {
            handle.OnPress -= OnHandlePress;
            handle.OnDrag -= OnHandleDrag;
            handle.OnRelease -= OnHandleRelease;
            handle.OnTap -= OnHandleTap;
            Handles.Remove(handle);
        }

        anchorHandles.Clear();
    }

    private VecD TryFindAnySnap(VecD delta, VectorPath path, out string? axisX, out string? axisY)
    {
        VecD closestSnapDelta = new VecD(double.PositiveInfinity, double.PositiveInfinity);
        axisX = null;
        axisY = null;

        SnappingController.RemoveAll("editingPath");

        foreach (var point in path.Points)
        {
            var snap = SnappingController.GetSnapDeltaForPoint((VecD)point + delta, out string x, out string y);
            if (snap.X < closestSnapDelta.X && !string.IsNullOrEmpty(x))
            {
                closestSnapDelta = new VecD(snap.X, closestSnapDelta.Y);
                axisX = x;
            }

            if (snap.Y < closestSnapDelta.Y && !string.IsNullOrEmpty(y))
            {
                closestSnapDelta = new VecD(closestSnapDelta.X, snap.Y);
                axisY = y;
            }
        }

        AddAllSnaps();

        if (closestSnapDelta.X == double.PositiveInfinity)
        {
            closestSnapDelta = new VecD(0, closestSnapDelta.Y);
        }

        if (closestSnapDelta.Y == double.PositiveInfinity)
        {
            closestSnapDelta = new VecD(closestSnapDelta.X, 0);
        }

        return delta + closestSnapDelta;
    }

    private void AddAllSnaps()
    {
        for (int i = 0; i < anchorHandles.Count; i++)
        {
            var i1 = i;
            SnappingController.AddXYAxis($"editingPath[{i}]", () => anchorHandles[i1].Position);
        }
    }

    private static void DefaultPathVerb((PathVerb verb, VecF[] points) data, VectorPath newPath)
    {
        switch (data.verb)
        {
            case PathVerb.Move:
                newPath.MoveTo(data.points[0]);
                break;
            case PathVerb.Line:
                newPath.LineTo(data.points[1]);
                break;
            case PathVerb.Quad:
                newPath.QuadTo(data.points[1], data.points[2]);
                break;
            case PathVerb.Conic:
                newPath.ConicTo(data.points[1], data.points[2], data.points[3].X);
                break;
            case PathVerb.Cubic:
                newPath.CubicTo(data.points[1], data.points[2], data.points[3]);
                break;
            case PathVerb.Close:
                newPath.Close();
                break;
            case PathVerb.Done:
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    private static void OnPathChanged(AvaloniaPropertyChangedEventArgs<VectorPath> args)
    {
        if (args.NewValue.Value == null)
        {
            var overlay = args.Sender as VectorPathOverlay;
            overlay.SnappingController.RemoveAll("editingPath");
            overlay.ClearAnchorHandles();
        }
    }
}

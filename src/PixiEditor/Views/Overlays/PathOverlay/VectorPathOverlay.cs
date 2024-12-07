﻿using System.Windows.Input;
using Avalonia;
using Avalonia.Input;
using Drawie.Backend.Core.ColorsImpl;
using Drawie.Backend.Core.Numerics;
using Drawie.Backend.Core.Surfaces.PaintImpl;
using Drawie.Backend.Core.Text;
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
        int globalAnchor = 0;
        int controlPoint = 0;
        int subPath = 1;
        int anchorCount = GetAnchorCount();
        int closeAtIndexInSubPath = GetCloseIndexInSubPath(Path, subPath);

        foreach (var verb in Path)
        {
            if (globalAnchor == anchorCount - 1 && !anySelected)
            {
                GetHandleAt(globalAnchor).IsSelected = true;
            }

            anySelected = anySelected || GetHandleAt(globalAnchor).IsSelected;

            VecF verbPointPos = GetVerbPointPos(verb);

            if (verb.verb == PathVerb.Cubic)
            {
                VecD controlPoint1 = (VecD)verb.points[1];
                VecD controlPoint2 = (VecD)verb.points[2];

                var controlPointHandle1 = controlPointHandles[controlPoint];
                var controlPointHandle2 = controlPointHandles[controlPoint + 1];

                controlPointHandle1.HitTestVisible = controlPoint1 != controlPointHandle1.ConnectedTo.Position;
                controlPointHandle2.HitTestVisible = controlPoint2 != controlPointHandle2.ConnectedTo.Position;

                controlPointHandle1.Position = controlPoint1;

                if (controlPointHandle1.HitTestVisible)
                {
                    controlPointHandle1.Draw(context);
                }

                controlPointHandle2.Position = controlPoint2;

                if (controlPointHandle2.HitTestVisible)
                {
                    controlPointHandle2.Draw(context);
                }

                controlPoint += 2;
            }
            else if (verb.verb == PathVerb.Close)
            {
                subPath++;
                closeAtIndexInSubPath = GetCloseIndexInSubPath(Path, subPath);
                continue;
            }

            if (globalAnchor == closeAtIndexInSubPath - 1)
            {
                continue;
            }

            if (globalAnchor == anchorCount)
            {
                break;
            }

            anchorHandles[globalAnchor].Position = new VecD(verbPointPos.X, verbPointPos.Y);
            anchorHandles[globalAnchor].Draw(context);

            globalAnchor++;
        }

        transformHandle.Position = Path.TightBounds.BottomRight + new VecD(1, 1);
        transformHandle.Draw(context);
    }

    private int GetAnchorCount()
    {
        int closeVerbs = Path.Count(x => x.verb == PathVerb.Close);
        return Path.VerbCount - closeVerbs * 2;
    }

    private int GetCloseIndexInSubPath(VectorPath path, int subPath)
    {
        int closeIndex = 0;
        int subPathIndex = 1;

        foreach (var data in path)
        {
            if (data.verb == PathVerb.Close)
            {
                if (subPathIndex == subPath)
                {
                    return closeIndex;
                }

                subPathIndex++;
            }
            else
            {
                closeIndex++;
            }
        }

        return -1;
    }

    private void AdjustHandles(VectorPath path)
    {
        int pointsCount = GetPointCount(path);
        int anchorCount = anchorHandles.Count;
        int totalHandles = anchorCount + controlPointHandles.Count;
        if (totalHandles != pointsCount)
        {
            if (totalHandles > pointsCount)
            {
                RemoveAllHandles();
            }

            int missingControlPoints = CalculateMissingControlPoints(controlPointHandles.Count);
            int missingAnchors = GetAnchorCount() - anchorHandles.Count;
            for (int i = 0; i < missingAnchors; i++)
            {
                CreateHandle(anchorHandles.Count);
            }

            for (int i = 0; i < missingControlPoints; i++)
            {
                CreateHandle(controlPointHandles.Count, true);
            }


            SelectAnchor(GetHandleAt(pointsCount - 1));

            ConnectControlPointsToAnchors();
        }

        Refresh();
    }

    private void ConnectControlPointsToAnchors()
    {
        int controlPointIndex = 0;
        int anchorIndex = 0;
        int startingIndexInSubPath = 0;
        int lastAnchorIndexInSubPath = CountAnchorsInSubPath(Path, 1) - 1;
        int subPath = 1;
        foreach (var data in Path)
        {
            int localAnchorIndex = anchorIndex - startingIndexInSubPath;
            if (data.verb == PathVerb.Cubic)
            {
                int targetAnchorIndex1 = anchorIndex - 1;
                if (targetAnchorIndex1 < startingIndexInSubPath)
                {
                    targetAnchorIndex1 = lastAnchorIndexInSubPath;
                }

                AnchorHandle previousAnchor = anchorHandles.ElementAtOrDefault(targetAnchorIndex1);

                int targetAnchorIndex2 = anchorIndex;
                if (targetAnchorIndex2 > lastAnchorIndexInSubPath)
                {
                    targetAnchorIndex2 = startingIndexInSubPath;
                }

                AnchorHandle nextAnchor = anchorHandles.ElementAtOrDefault(targetAnchorIndex2);

                if (previousAnchor != null)
                {
                    controlPointHandles[controlPointIndex].ConnectedTo = previousAnchor;
                }

                controlPointHandles[controlPointIndex + 1].ConnectedTo = nextAnchor;
                controlPointIndex += 2;
            }
            
            if (data.verb == PathVerb.Close)
            {
                subPath++;
                anchorIndex--;
                startingIndexInSubPath = anchorIndex;
                lastAnchorIndexInSubPath = CountAnchorsInSubPath(Path, subPath) - 1 + startingIndexInSubPath;
            }
            else 
            {
                anchorIndex++;
            }
        }
    }

    private int GetPointCount(VectorPath path)
    {
        return path.PointCount - path.Count(x => x.verb == PathVerb.Close);
    }

    private int CalculateMissingControlPoints(int handleCount)
    {
        int totalControlPoints = 0;

        foreach (var point in Path)
        {
            if (point.verb == PathVerb.Cubic)
            {
                totalControlPoints += 2;
            }
        }

        return totalControlPoints - handleCount;
    }

    private void RemoveAllHandles()
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
            var handle = controlPointHandles[i];
            handle.OnDrag -= OnControlPointDrag;
            handle.OnRelease -= OnHandleRelease;

            Handles.Remove(controlPointHandles[i]);
        }

        anchorHandles.Clear();
        controlPointHandles.Clear();
        SnappingController.RemoveAll("editingPath");
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
            controlPoint.OnRelease += OnHandleRelease;
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
            newPath.LineTo((VecF)anchorHandle.Position);
            newPath.Close();
        }
        else
        {
            VecD pos = anchorHandle.Position;
            newPath.LineTo(new VecF((float)pos.X, (float)pos.Y));
        }

        Path = newPath;
    }

    private int CountAnchorsInSubPath(VectorPath path, int subPath)
    {
        int anchorCount = 0;
        int subPathIndex = 1;

        foreach (var data in path)
        {
            if (data.verb == PathVerb.Close)
            {
                if (subPathIndex == subPath)
                {
                    return anchorCount - 1;
                }

                subPathIndex++;
                anchorCount = 0;
            }
            else if (data.verb != PathVerb.Done)
            {
                anchorCount++;
            }
        }

        return anchorCount;
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
        VectorPath newPath = new VectorPath();
        int index = anchorHandles.IndexOf(anchorHandle);
        
        int anchorIndex1 = index - 1;
        int anchorIndex2 = index;
        
        int targetSubPath = GetSubPathIndexForAnchor(Path, index);
        
        int lastAnchorIndexInSubPath = GetCloseAnchorIndexInSubPath(Path, targetSubPath);
        int firstAnchorIndexInSubPath = lastAnchorIndexInSubPath - CountAnchorsInSubPath(Path, targetSubPath);
        
        if(anchorIndex1 < firstAnchorIndexInSubPath)
        {
            anchorIndex1 = lastAnchorIndexInSubPath - 1;
        }
        
        if(anchorIndex2 >= lastAnchorIndexInSubPath)
        {
            anchorIndex2 = firstAnchorIndexInSubPath;
        }

        int anchor = 0;
        foreach (var data in Path)
        {
            if (data.verb == PathVerb.Close)
            {
                DefaultPathVerb(data, newPath);
            }
            else
            {
                if (anchor == anchorIndex1 || anchor == anchorIndex2)
                {
                    if (data.verb == PathVerb.Line)
                    {
                        newPath.CubicTo(data.points[0], data.points[1], data.points[1]);
                    }
                    else
                    {
                        DefaultPathVerb(data, newPath);
                    }
                }
                else
                {
                    DefaultPathVerb(data, newPath);
                }

                if (data.verb != PathVerb.Move)
                {
                    anchor++;
                }
            }
        }

        return newPath;
    }
    
    private int GetSubPathIndexForAnchor(VectorPath path, int anchorIndex)
    {
        int subPath = 1;
        int anchor = 0;

        foreach (var data in path)
        {
            if (data.verb == PathVerb.Close)
            {
                subPath++;
                anchor--;
            }
            else
            {
                if (anchor == anchorIndex)
                {
                    return subPath;
                }

                if (data.verb != PathVerb.Move)
                {
                    anchor++;
                }
            }
        }

        return -1;
    }

    private void OnHandleDrag(Handle source, OverlayPointerArgs args)
    {
        if (source is not AnchorHandle handle)
        {
            return;
        }

        var index = anchorHandles.IndexOf(handle);
        VectorPath newPath = new VectorPath(Path);
        newPath.Reset();

        bool pointHandled = false;
        int i = 0;

        VecF previousDelta = new VecF();

        int controlPointIndex = 0;
        var connectedControlPoints = controlPointHandles.Where(x => x.ConnectedTo == handle).ToList();
        int targetControlPointIndex = controlPointHandles.IndexOf(connectedControlPoints.FirstOrDefault());
        int symmetricControlPointIndex = controlPointHandles.IndexOf(connectedControlPoints.LastOrDefault());

        VecD targetPos = ApplySymmetry(args.Point);
        VecD targetSymmetryPos = GetMirroredControlPoint((VecF)targetPos, (VecF)handle.Position);

        bool ctrlPressed = args.Modifiers.HasFlag(KeyModifiers.Control);
        int subPathNum = 1;

        VecF firstSubPathPoint = Path.Points.First();
        bool isSubPathClosed = IsSubPathClosed(subPathNum, Path);
        int lastAnchorAt = GetCloseAnchorIndexInSubPath(Path, subPathNum);
        foreach (var data in Path)
        {
            VecF point;
            switch (data.verb)
            {
                case PathVerb.Move:
                    if (ctrlPressed)
                    {
                        DefaultPathVerb(data, newPath);
                        break;
                    }

                    point = data.points[0];
                    point = TryApplyNewPos(args, i, index, point, isSubPathClosed, data.points[0], lastAnchorAt);

                    newPath.MoveTo(point);
                    firstSubPathPoint = point;
                    previousDelta = point - data.points[0];
                    break;
                case PathVerb.Line:
                    if (ctrlPressed)
                    {
                        DefaultPathVerb(data, newPath);
                        break;
                    }

                    point = data.points[1];
                    point = TryApplyNewPos(args, i, index, point, isSubPathClosed, firstSubPathPoint, lastAnchorAt);

                    newPath.LineTo(point);
                    break;
                case PathVerb.Cubic:
                    if (ctrlPressed)
                    {
                        HandleCubicControlContinousDrag(controlPointIndex, targetControlPointIndex,
                            symmetricControlPointIndex,
                            targetPos, targetSymmetryPos, data, newPath);
                        controlPointIndex += 2;
                    }
                    else
                    {
                        point = data.points[3];
                        point = TryApplyNewPos(args, i, index, point, isSubPathClosed, firstSubPathPoint, lastAnchorAt);

                        VecF mid1Delta = previousDelta;

                        VecF mid2Delta = point - data.points[3];

                        newPath.CubicTo(data.points[1] + mid1Delta, data.points[2] + mid2Delta, point);

                        previousDelta = mid2Delta;
                    }

                    break;
                default:
                    DefaultPathVerb(data, newPath);
                    break;
            }

            if (data.verb == PathVerb.Close)
            {
                subPathNum++;
                isSubPathClosed = IsSubPathClosed(subPathNum, Path);
                lastAnchorAt = GetCloseAnchorIndexInSubPath(Path, subPathNum);
                i--;
            }
            else
            {
                i++;
            }
        }

        Path = newPath;
    }
    
    private int GetCloseAnchorIndexInSubPath(VectorPath path, int subPath)
    {
        int anchorIndex = 0;
        int subPathIndex = 1;

        foreach (var data in path)
        {
            if (data.verb == PathVerb.Close)
            {
                if (subPathIndex == subPath)
                {
                    return anchorIndex - 1;
                }

                subPathIndex++;
                anchorIndex--;
            }
            else
            {
                anchorIndex++;
            }
        }

        return -1;
    }

    private void OnControlPointDrag(Handle source, OverlayPointerArgs args)
    {
        if (source is not ControlPointHandle controlPointHandle)
        {
            return;
        }

        var targetIndex = controlPointHandles.IndexOf(controlPointHandle);
        int symmetricIndex = controlPointHandles.IndexOf(controlPointHandles.FirstOrDefault(x =>
            x.ConnectedTo == controlPointHandle.ConnectedTo && x != controlPointHandle));
        VecD targetPos = ApplySymmetry(args.Point);
        VecD targetSymmetryPos =
            GetMirroredControlPoint((VecF)targetPos, (VecF)controlPointHandle.ConnectedTo.Position);
        VectorPath newPath = new VectorPath();

        if (args.Modifiers.HasFlag(KeyModifiers.Alt))
        {
            symmetricIndex = -1;
        }

        int i = 0;

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
                    HandleCubicControlContinousDrag(i, targetIndex, symmetricIndex,
                        targetPos, targetSymmetryPos, data, newPath);
                    i += 2;
                    break;
                default:
                    i++;
                    DefaultPathVerb(data, newPath);
                    break;
            }
        }

        Path = newPath;
    }

    private void HandleCubicControlContinousDrag(int i, int targetIndex, int symmetricIndex,
        VecD targetPos, VecD targetSymmetryPos,
        (PathVerb verb, VecF[] points) data,
        VectorPath newPath)
    {
        bool isFirstControlPoint = i == targetIndex;
        bool isSecondControlPoint = i + 1 == targetIndex;

        bool isFirstSymmetricControlPoint = i == symmetricIndex;
        bool isSecondSymmetricControlPoint = i + 1 == symmetricIndex;

        VecF controlPoint1 = data.points[1];
        VecF controlPoint2 = data.points[2];
        VecF endPoint = data.points[3];

        if (isFirstControlPoint)
        {
            controlPoint1 = (VecF)targetPos;
        }
        else if (isSecondControlPoint)
        {
            controlPoint2 = (VecF)targetPos;
        }
        else if (isFirstSymmetricControlPoint)
        {
            controlPoint1 = (VecF)targetSymmetryPos;
        }
        else if (isSecondSymmetricControlPoint)
        {
            controlPoint2 = (VecF)targetSymmetryPos;
        }

        newPath.CubicTo(controlPoint1, controlPoint2, endPoint);
    }

    private bool IsSubPathClosed(int subPathIndex, VectorPath path)
    {
        int subPath = 1;

        foreach (var data in path)
        {
            if (data.verb == PathVerb.Close)
            {
                if (subPathIndex == subPath)
                {
                    return true;
                }

                subPath++;
            }
        }

        return false;
    }

    private VecD GetMirroredControlPoint(VecF controlPoint, VecF anchor)
    {
        return new VecD(2 * anchor.X - controlPoint.X, 2 * anchor.Y - controlPoint.Y);
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

    private VecF TryApplyNewPos(OverlayPointerArgs args, int i, int index, VecF point, bool firstIsLast,
        VecF firstPoint, int lastAnchorAt)
    {
        if (i == index && i != lastAnchorAt)
        {
            point = (VecF)ApplySymmetry(args.Point);
        }
        else if (firstIsLast && i == lastAnchorAt)
        {
            point = firstPoint;
        }

        return point;
    }

    private VecD ApplySymmetry(VecD point)
    {
        var snappedPoint = SnappingController.GetSnapPoint(point, out string axisX, out string axisY);
        var snapped = new VecD((float)snappedPoint.X, (float)snappedPoint.Y);
        TryHighlightSnap(axisX, axisY);
        return snapped;
    }

    private void OnHandleRelease(Handle source, OverlayPointerArgs args)
    {
        AddToUndoCommand.Execute(Path);

        if (source is AnchorHandle anchorHandle)
        {
            SnappingController.AddXYAxis($"editingPath[{anchorHandles.IndexOf(anchorHandle)}]", () => source.Position);
        }

        TryHighlightSnap(null, null);

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

    private void PathChanged(VectorPath newPath)
    {
        AdjustHandles(newPath);
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
        var overlay = args.Sender as VectorPathOverlay;
        if (args.NewValue.Value == null)
        {
            overlay.SnappingController.RemoveAll("editingPath");
            overlay.ClearAnchorHandles();
            overlay.IsVisible = false;
        }
        else
        {
            var path = args.NewValue.Value;
            overlay.AdjustHandles(path);
            overlay.IsVisible = true;
        }

        if (args.OldValue.Value != null)
        {
            args.OldValue.Value.Changed -= overlay.PathChanged;
        }

        if (args.NewValue.Value != null)
        {
            args.NewValue.Value.Changed += overlay.PathChanged;
        }
    }
}

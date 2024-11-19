﻿using Avalonia;
using ChunkyImageLib.Operations;
using Drawie.Backend.Core.Numerics;
using Drawie.Backend.Core.Surfaces.PaintImpl;
using Drawie.Backend.Core.Vector;
using PixiEditor.Extensions.UI.Overlays;
using Drawie.Numerics;
using PixiEditor.Views.Rendering;
using Canvas = Drawie.Backend.Core.Surfaces.Canvas;
using Colors = Drawie.Backend.Core.ColorsImpl.Colors;

namespace PixiEditor.Views.Overlays.BrushShapeOverlay;
#nullable enable
internal class BrushShapeOverlay : Overlay
{
    public static readonly StyledProperty<int> BrushSizeProperty =
        AvaloniaProperty.Register<BrushShapeOverlay, int>(nameof(BrushSize), defaultValue: 1);

    public static readonly StyledProperty<BrushShape> BrushShapeProperty =
        AvaloniaProperty.Register<BrushShapeOverlay, BrushShape>(nameof(BrushShape),
            defaultValue: BrushShape.CirclePixelated);

    public static readonly StyledProperty<Scene> SceneProperty = AvaloniaProperty.Register<BrushShapeOverlay, Scene>(
        nameof(Scene));

    public Scene Scene
    {
        get => GetValue(SceneProperty);
        set => SetValue(SceneProperty, value);
    }

    public BrushShape BrushShape
    {
        get => (BrushShape)GetValue(BrushShapeProperty);
        set => SetValue(BrushShapeProperty, value);
    }

    public int BrushSize
    {
        get => (int)GetValue(BrushSizeProperty);
        set => SetValue(BrushSizeProperty, value);
    }

    private Paint paint = new Paint() { Color = Colors.LightGray, StrokeWidth = 1, Style = PaintStyle.Stroke };
    private VecD lastMousePos = new();

    private VectorPath threePixelCircle;
    private int lastSize;
    private VectorPath lastNonTranslatedCircle;


    static BrushShapeOverlay()
    {
        AffectsOverlayRender(BrushShapeProperty, BrushSizeProperty);
    }

    public BrushShapeOverlay()
    {
        IsHitTestVisible = false;
        threePixelCircle = CreateThreePixelCircle();
    }

    protected override void OnOverlayPointerMoved(OverlayPointerArgs args)
    {
        if (BrushShape == BrushShape.Hidden)
            return;

        VecD rawPoint = args.Point;
        lastMousePos = rawPoint;
        Refresh();
    }

    public override void RenderOverlay(Canvas context, RectD canvasBounds) => Render(context);

    public void Render(Canvas targetCanvas)
    {
        var winRect = new RectD(
            (VecD)(new VecD(Math.Floor(lastMousePos.X), Math.Floor(lastMousePos.Y)) -
                   new VecD(BrushSize / 2, BrushSize / 2)),
            new VecD(BrushSize, BrushSize));
        switch (BrushShape)
        {
            case BrushShape.Pixel:
                paint.IsAntiAliased = false;
                targetCanvas.DrawRect(
                    new RectD(new VecD(Math.Floor(lastMousePos.X), Math.Floor(lastMousePos.Y)), new VecD(1, 1)),
                    paint);
                break;
            case BrushShape.Square:
                targetCanvas.DrawRect(winRect, paint);
                break;
            case BrushShape.CirclePixelated:
                DrawCircleBrushShape(targetCanvas, winRect);
                break;
            case BrushShape.CircleSmooth:
                DrawCircleBrushShapeSmooth(targetCanvas, lastMousePos, BrushSize / 2f);
                break;
        }
    }

    private void DrawCircleBrushShape(Canvas drawingContext, RectD winRect)
    {
        paint.IsAntiAliased = false;
        
        var rectI = new RectI((int)winRect.X, (int)winRect.Y, (int)winRect.Width, (int)winRect.Height);
        if (BrushSize < 3)
        {
            drawingContext.DrawRect(winRect, paint);
        }
        else if (BrushSize == 3)
        {
            var lp = new VecI((int)lastMousePos.X, (int)lastMousePos.Y);
            using VectorPath shifted = new VectorPath(threePixelCircle);
            shifted.Transform(Matrix3X3.CreateTranslation(lp.X, lp.Y));
            drawingContext.DrawPath(shifted, paint);
        }
        else if (BrushSize > 200)
        {
            VecD center = rectI.Center;
            drawingContext.DrawOval(new VecD(center.X, center.Y), new VecD(rectI.Width / 2.0, rectI.Height / 2.0),
                paint);
        }
        else
        {
            if (BrushSize != lastSize)
            {
                var geometry = ConstructEllipseOutline(new RectI(0, 0, rectI.Width, rectI.Height));
                lastNonTranslatedCircle = new VectorPath(geometry);
                lastSize = BrushSize;
            }

            var lp = new VecI((int)lastMousePos.X, (int)lastMousePos.Y);
            using VectorPath shifted = new VectorPath(lastNonTranslatedCircle);
            shifted.Transform(Matrix3X3.CreateTranslation(lp.X - rectI.Width / 2,
                lp.Y - rectI.Height / 2)); // don't use float, truncation is intended 
            drawingContext.DrawPath(shifted, paint);
        }
    }

    private void DrawCircleBrushShapeSmooth(Canvas drawingContext, VecD lastMousePos, float radius)
    {
        VecD center = lastMousePos; 
        paint.IsAntiAliased = true;
        
        drawingContext.DrawOval(new VecD(center.X, center.Y), new VecD(radius, radius),
            paint);
    }

    protected override void ZoomChanged(double newZoom)
    {
        paint.StrokeWidth = (float)(1.0f / newZoom);
    }

    private static int Mod(int x, int m) => (x % m + m) % m;

    private static VectorPath CreateThreePixelCircle()
    {
        var path = new VectorPath();
        path.MoveTo(new VecF(0, 0));
        path.LineTo(new VecF(0, -1));
        path.LineTo(new VecF(1, -1));
        path.LineTo(new VecF(1, 0));
        path.LineTo(new VecF(2, 0));
        path.LineTo(new VecF(2, 1));
        path.LineTo(new VecF(2, 1));
        path.LineTo(new VecF(1, 1));
        path.LineTo(new VecF(1, 2));
        path.LineTo(new VecF(0, 2));
        path.LineTo(new VecF(0, 1));
        path.LineTo(new VecF(-1, 1));
        path.LineTo(new VecF(-1, 0));
        path.Close();
        return path;
    }

    private static VectorPath ConstructEllipseOutline(RectI rectangle)
    {
        var center = rectangle.Center;
        var points = EllipseHelper.GenerateEllipseFromRect(rectangle, 0).ToList();
        points.Sort((vec, vec2) => Math.Sign((vec - center).Angle - (vec2 - center).Angle));
        List<VecI> finalPoints = new();
        for (int i = 0; i < points.Count; i++)
        {
            VecI prev = points[Mod(i - 1, points.Count)];
            VecI point = points[i];
            VecI next = points[Mod(i + 1, points.Count)];

            bool atBottom = point.Y >= center.Y;
            bool onRight = point.X >= center.X;
            if (atBottom)
            {
                if (onRight)
                {
                    if (prev.Y != point.Y)
                        finalPoints.Add(new(point.X + 1, point.Y));
                    finalPoints.Add(new(point.X + 1, point.Y + 1));
                    if (next.X != point.X)
                        finalPoints.Add(new(point.X, point.Y + 1));
                }
                else
                {
                    if (prev.X != point.X)
                        finalPoints.Add(new(point.X + 1, point.Y + 1));
                    finalPoints.Add(new(point.X, point.Y + 1));
                    if (next.Y != point.Y)
                        finalPoints.Add(point);
                }
            }
            else
            {
                if (onRight)
                {
                    if (prev.X != point.X)
                        finalPoints.Add(point);
                    finalPoints.Add(new(point.X + 1, point.Y));
                    if (next.Y != point.Y)
                        finalPoints.Add(new(point.X + 1, point.Y + 1));
                }
                else
                {
                    if (prev.Y != point.Y)
                        finalPoints.Add(new(point.X, point.Y + 1));
                    finalPoints.Add(point);
                    if (next.X != point.X)
                        finalPoints.Add(new(point.X + 1, point.Y));
                }
            }
        }

        VectorPath path = new();

        path.MoveTo(new VecF(finalPoints[0].X, finalPoints[0].Y));
        for (var index = 1; index < finalPoints.Count; index++)
        {
            var point = finalPoints[index];
            path.LineTo(new VecF(point.X, point.Y));
        }

        path.Close();

        return path;
    }
}

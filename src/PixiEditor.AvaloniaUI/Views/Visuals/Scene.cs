using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Avalonia;
using Avalonia.Media;
using Avalonia.OpenGL;
using Avalonia.OpenGL.Controls;
using ChunkyImageLib;
using ChunkyImageLib.DataHolders;
using PixiEditor.AvaloniaUI.ViewModels.Document;
using PixiEditor.DrawingApi.Core.Numerics;
using PixiEditor.DrawingApi.Core.Surface.ImageData;
using PixiEditor.DrawingApi.Skia;

namespace PixiEditor.AvaloniaUI.Views.Visuals;

internal class Scene : OpenGlControlBase
{
    public static readonly StyledProperty<Surface> SurfaceProperty = AvaloniaProperty.Register<SurfaceControl, Surface>(
        nameof(Surface));

    public static readonly StyledProperty<double> ScaleProperty = AvaloniaProperty.Register<Scene, double>(
        nameof(Scale), 1);

    public static readonly StyledProperty<VecI> ContentPositionProperty = AvaloniaProperty.Register<Scene, VecI>(
        nameof(ContentPosition));

    public static readonly StyledProperty<DocumentViewModel> DocumentProperty = AvaloniaProperty.Register<Scene, DocumentViewModel>(
        nameof(Document));

    public static readonly StyledProperty<double> AngleProperty = AvaloniaProperty.Register<Scene, double>(
        nameof(Angle), 0);

    public double Angle
    {
        get => GetValue(AngleProperty);
        set => SetValue(AngleProperty, value);
    }

    public DocumentViewModel Document
    {
        get => GetValue(DocumentProperty);
        set => SetValue(DocumentProperty, value);
    }

    public VecI ContentPosition
    {
        get => GetValue(ContentPositionProperty);
        set => SetValue(ContentPositionProperty, value);
    }

    public double Scale
    {
        get => GetValue(ScaleProperty);
        set => SetValue(ScaleProperty, value);
    }

    public Surface Surface
    {
        get => GetValue(SurfaceProperty);
        set => SetValue(SurfaceProperty, value);
    }

    public Rect FinalBounds => new Rect(100, 100, Bounds.Width - 200, Bounds.Height - 200);

    private SKSurface _outputSurface;
    private SKPaint _paint = new SKPaint();
    private GRContext? gr;

    static Scene()
    {
        BoundsProperty.Changed.AddClassHandler<Scene>(BoundsChanged);
        WidthProperty.Changed.AddClassHandler<Scene>(BoundsChanged);
        HeightProperty.Changed.AddClassHandler<Scene>(BoundsChanged);
    }

    public Scene()
    {
        ClipToBounds = true;
    }

    protected override void OnOpenGlInit(GlInterface gl)
    {
        gr = GRContext.CreateGl(GRGlInterface.Create(gl.GetProcAddress));
        CreateOutputSurface();
    }

    private void CreateOutputSurface()
    {
        if (gr == null) return;

        _outputSurface?.Dispose();
        GRGlFramebufferInfo frameBuffer = new GRGlFramebufferInfo(0, SKColorType.Rgba8888.ToGlSizedFormat());
        GRBackendRenderTarget desc = new GRBackendRenderTarget((int)Bounds.Width, (int)Bounds.Height, 4, 0, frameBuffer);
        _outputSurface = SKSurface.Create(gr, desc, GRSurfaceOrigin.BottomLeft, SKImageInfo.PlatformColorType);
    }

    protected override void OnOpenGlRender(GlInterface gl, int fb)
    {
        if (Surface == null || Document == null) return;

        SKCanvas canvas = _outputSurface.Canvas;

        canvas.Save();
        canvas.ClipRect(new SKRect(0, 0, (float)Bounds.Width, (float)Bounds.Height));
        canvas.Clear(SKColors.Transparent);

        var scale = CalculateFinalScale();

        /*canvas.RotateDegrees((float)Angle, ContentPosition.X, ContentPosition.Y);
        canvas.Scale(scale, scale, ContentPosition.X, ContentPosition.Y);

        canvas.Translate(ContentPosition.X, ContentPosition.Y);*/

        float radians = (float)(Angle * Math.PI / 180);
        VecD topLeft = SurfaceToViewport(new VecI(0, 0), scale, radians);
        VecD bottomRight = SurfaceToViewport(Surface.Size, scale, radians);
        VecD topRight = SurfaceToViewport(new VecI(Surface.Size.X, 0), scale, radians);
        VecD bottomLeft = SurfaceToViewport(new VecI(0, Surface.Size.Y), scale, radians);

        bool topLeftWithinBounds = IsWithinBounds(topLeft, FinalBounds);
        bool bottomRightWithinBounds = IsWithinBounds(bottomRight, FinalBounds);
        bool topRightWithinBounds = IsWithinBounds(topRight, FinalBounds);
        bool bottomLeftWithinBounds = IsWithinBounds(bottomLeft, FinalBounds);

        int withinBoundsCount = 0;
        if (topLeftWithinBounds) withinBoundsCount++;
        if (bottomRightWithinBounds) withinBoundsCount++;
        if (topRightWithinBounds) withinBoundsCount++;
        if (bottomLeftWithinBounds) withinBoundsCount++;

        RectD viewport = new RectD(FinalBounds.X, FinalBounds.Y, FinalBounds.Width, FinalBounds.Height);
        RectD canvasViewRectAabb = RectD.CreateAABB(topLeft, bottomRight, topRight, bottomLeft);

        bool isViewportContained = canvasViewRectAabb.IntersectsWithInclusive(viewport);

        _paint.Color = SKColors.Green;
        canvas.DrawCircle((float)topLeft.X, (float)topLeft.Y, 5, _paint);
        canvas.DrawCircle((float)bottomRight.X, (float)bottomRight.Y, 5, _paint);
        canvas.DrawCircle((float)topRight.X, (float)topRight.Y, 5, _paint);
        canvas.DrawCircle((float)bottomLeft.X, (float)bottomLeft.Y, 5, _paint);

        _paint.Color = SKColors.Blue;
        DrawDebugRect(canvas, viewport);

        if (withinBoundsCount == 0 && !isViewportContained)
        {
            canvas.Restore();
            canvas.Flush();
            RequestNextFrameRendering();
            return;
        }

        RectI surfaceRectToRender = new RectI(0, 0, Surface.Size.X, Surface.Size.Y);

        if(withinBoundsCount < 4)
        {
            var topLeftCorner = new CornerInViewport(topLeft, topLeftWithinBounds, VecI.Zero);
            var topRightCorner = new CornerInViewport(topRight, topRightWithinBounds, new VecI(Surface.Size.X, 0));
            var bottomRightCorner = new CornerInViewport(bottomRight, bottomRightWithinBounds, Surface.Size);
            var bottomLeftCorner = new CornerInViewport(bottomLeft, bottomLeftWithinBounds, new VecI(0, Surface.Size.Y));

            CornersInViewport cornersInViewport = new CornersInViewport(topLeftCorner, topRightCorner, bottomRightCorner, bottomLeftCorner);

            surfaceRectToRender = cornersInViewport.ShrinkToViewport(surfaceRectToRender, IsSurfaceWithinViewportBounds);
        }

        canvas.RotateDegrees((float)Angle, ContentPosition.X, ContentPosition.Y);
        canvas.Scale(scale, scale, ContentPosition.X, ContentPosition.Y);

        canvas.Translate(ContentPosition.X, ContentPosition.Y);


        _paint.Color = SKColors.Red;
        DrawDebugRect(canvas, (RectD)surfaceRectToRender);
        /*using Image snapshot = Surface.DrawingSurface.Snapshot(surfaceRectToRender);
        canvas.DrawImage((SKImage)snapshot.Native, surfaceRectToRender.X, surfaceRectToRender.Y, _paint);*/

        canvas.Restore();

        canvas.Flush();
        RequestNextFrameRendering();
    }

    private void DrawDebugRect(SKCanvas canvas, RectD rect)
    {
        canvas.DrawLine((float)rect.X, (float)rect.Y, (float)rect.Right, (float)rect.Y, _paint);
        canvas.DrawLine((float)rect.Right, (float)rect.Y, (float)rect.Right, (float)rect.Bottom, _paint);
        canvas.DrawLine((float)rect.Right, (float)rect.Bottom, (float)rect.X, (float)rect.Bottom, _paint);

        canvas.DrawLine((float)rect.X, (float)rect.Bottom, (float)rect.X, (float)rect.Y, _paint);
    }

    private RectI ViewportToSurface(RectD viewportRect, float scale, float angleRadians)
    {
        VecI start = ViewportToSurface(viewportRect.TopLeft, scale, angleRadians);
        VecI end = ViewportToSurface(viewportRect.BottomRight, scale, angleRadians);
        return RectI.FromTwoPoints(start, end);
    }

    private bool IsWithinBounds(VecD point, Rect bounds)
    {
        return point.X >= bounds.X && point.X <= bounds.Right && point.Y >= bounds.Y && point.Y <= bounds.Bottom;
    }

    private bool IsSurfaceWithinViewportBounds(VecI surfacePoint)
    {
        float angle = (float)(Angle * Math.PI / 180);
        VecD viewportPoint = SurfaceToViewport(surfacePoint, (float)Scale, angle);
        return IsWithinBounds(viewportPoint, FinalBounds);
    }

    private float CalculateFinalScale()
    {
        float scaleX = (float)Document.Width / Surface.Size.X;
        float scaleY = (float)Document.Height / Surface.Size.Y;
        var scaleUniform = Math.Min(scaleX, scaleY);

        float scale = (float)Scale * scaleUniform;
        return scale;
    }

    private bool IsOutOfBounds(VecI surfaceStart, VecI surfaceEnd)
    {
        return surfaceStart.X >= Surface.Size.X || surfaceStart.Y >= Surface.Size.Y || surfaceEnd.X <= 0 || surfaceEnd.Y <= 0;
    }

    /*private VecI ViewportToSurface(VecI surfacePoint, float scale)
    {
        float radians = (float)(Angle * Math.PI / 180);
        VecD rotatedSurfacePoint = ((VecD)surfacePoint).Rotate(radians, ContentPosition / scale);
        return new VecI(
            (int)((rotatedSurfacePoint.X - ContentPosition.X) / scale),
            (int)((rotatedSurfacePoint.Y - ContentPosition.Y) / scale));
    }*/

    private VecD SurfaceToViewport(VecI surfacePoint, float scale, float angleRadians)
    {
        VecD unscaledPoint = surfacePoint * scale;
        VecD offseted = unscaledPoint + ContentPosition;

        return offseted.Rotate(angleRadians, ContentPosition);
    }

    private VecI ViewportToSurface(VecD viewportPoint, float scale, float angleRadians)
    {
        VecD rotatedViewportPoint = (viewportPoint).Rotate(-angleRadians, ContentPosition);
        VecD unscaledPoint = rotatedViewportPoint - ContentPosition;
        return new VecI(
            (int)Math.Round(unscaledPoint.X / scale),
            (int)Math.Round(unscaledPoint.Y / scale));
    }

    private static void BoundsChanged(Scene sender, AvaloniaPropertyChangedEventArgs e)
    {
        if (e.NewValue is Rect bounds)
        {
            sender.CreateOutputSurface();
        }
    }
}

class CornerInViewport(VecD position, bool isWithinBounds, VecI surfacePosition)
{
    public VecD Position { get; set; } = position;
    public VecI SurfacePosition { get; set; } = surfacePosition;
    public bool IsWithinBounds { get; set; } = isWithinBounds;
    public CornerInViewport[] ConnectedCorners { get; set; }
}

class CornersInViewport
{
    public CornerInViewport TopLeft { get; set; }
    public CornerInViewport TopRight { get; set; }
    public CornerInViewport BottomRight { get; set; }
    public CornerInViewport BottomLeft { get; set; }

    public CornersInViewport(CornerInViewport topLeft, CornerInViewport topRight, CornerInViewport bottomRight, CornerInViewport bottomLeft)
    {
        TopLeft = topLeft;
        TopRight = topRight;
        BottomRight = bottomRight;
        BottomLeft = bottomLeft;

        TopLeft.ConnectedCorners = new[] { TopRight, BottomLeft };
        TopRight.ConnectedCorners = new[] { TopLeft, BottomRight };
        BottomRight.ConnectedCorners = new[] { BottomLeft, TopRight };
        BottomLeft.ConnectedCorners = new[] { TopLeft, BottomRight };
    }

    public RectI ShrinkToViewport(RectI surfaceToRender, Func<VecI, bool> isWithinBoundsAction)
    {
        // start with first corner that is not within bounds, find next that is not within bounds and go to the direction of visible corner
        // check if each pixel column and row is within bounds, if both are not visible, shrink surfaceToRender by 1

        VecI maxSize = surfaceToRender.Size;

        CornerInViewport firstNotWithinBounds = GetStartingCorner();
        CornerInViewport? invisibleNeighbor = firstNotWithinBounds.ConnectedCorners.FirstOrDefault(x => !x.IsWithinBounds);
        if (invisibleNeighbor == null) return surfaceToRender;

        surfaceToRender = TraverseShrinkCorners(surfaceToRender, isWithinBoundsAction, invisibleNeighbor, firstNotWithinBounds, maxSize);
        invisibleNeighbor = firstNotWithinBounds.ConnectedCorners.First(x => x != invisibleNeighbor);
        if (!invisibleNeighbor.IsWithinBounds)
        {
            surfaceToRender = TraverseShrinkCorners(surfaceToRender, isWithinBoundsAction, invisibleNeighbor,
                firstNotWithinBounds, maxSize);
        }

        /*invisibleNeighbor = GetOppositeCorner(invisibleNeighbor);
        if (!invisibleNeighbor.IsWithinBounds)
        {
            surfaceToRender = TraverseShrinkCorners(surfaceToRender, isWithinBoundsAction, invisibleNeighbor,
                firstNotWithinBounds, maxSize);
        }*/

        return surfaceToRender;
    }

    private CornerInViewport GetOppositeCorner(CornerInViewport invisibleNeighbor)
    {
        if (invisibleNeighbor == TopLeft) return BottomRight;
        if (invisibleNeighbor == TopRight) return BottomLeft;
        if (invisibleNeighbor == BottomRight) return TopLeft;
        if (invisibleNeighbor == BottomLeft) return TopRight;

        throw new Exception("Invalid corner");
    }

    private static RectI TraverseShrinkCorners(RectI surfaceToRender, Func<VecI, bool> isWithinBoundsAction,
        CornerInViewport invisibleNeighbor, CornerInViewport firstNotWithinBounds, VecI maxSize)
    {
        VecI crossDirection = firstNotWithinBounds.ConnectedCorners.First(x => x != invisibleNeighbor).SurfacePosition -
                              firstNotWithinBounds.SurfacePosition;
        VecI crossDirectionNormalized = crossDirection.SignsWithZero();

        VecI currentSurfacePosition = firstNotWithinBounds.SurfacePosition;
        VecI oppositeSurfacePosition = invisibleNeighbor.SurfacePosition;

        if (!isWithinBoundsAction(currentSurfacePosition) && !isWithinBoundsAction(oppositeSurfacePosition))
        {
            surfaceToRender = ShrinkToDir(surfaceToRender, crossDirectionNormalized);

            VecI currentDirection = crossDirectionNormalized;

            int maxIterations = currentDirection.X != 0 ? maxSize.X : maxSize.Y;
            for (int i = 0; i < maxIterations; i++)
            {
                VecI nextSurfacePosition = currentSurfacePosition + currentDirection;
                VecI oppositeNextSurfacePosition = oppositeSurfacePosition + currentDirection;
                if (!isWithinBoundsAction(nextSurfacePosition) && !isWithinBoundsAction(oppositeNextSurfacePosition))
                {
                    surfaceToRender = ShrinkToDir(surfaceToRender, currentDirection);
                    currentSurfacePosition = nextSurfacePosition;
                    oppositeSurfacePosition = oppositeNextSurfacePosition;
                }
                else
                {
                    break;
                }
            }
        }

        return surfaceToRender;
    }

    private static RectI ShrinkToDir(RectI surfaceToRender, VecI crossDirectionNormalized)
    {
        // if crossDirectionNormalized is (1, 0) then we shrink width, if it's (0, 1) we shrink height, if it's (-1, 0) we shrink X, if it's (0, -1) we shrink Y

        int x = surfaceToRender.X;
        int y = surfaceToRender.Y;
        int width = surfaceToRender.Width;
        int height = surfaceToRender.Height;

        if (crossDirectionNormalized.X == 1)
        {
            x++;
            width--;
        }
        else if (crossDirectionNormalized.X == -1)
        {
            width--;
        }
        else if (crossDirectionNormalized.Y == 1)
        {
            y++;
            height--;
        }
        else if (crossDirectionNormalized.Y == -1)
        {
            height--;
        }

        return new RectI(x, y, width, height);
    }

    private CornerInViewport GetStartingCorner()
    {
       CornerInViewport[] corners = { TopLeft, TopRight, BottomRight, BottomLeft };
       CornerInViewport? firstWithInvisibleNeighbors = corners.FirstOrDefault(x => !x.IsWithinBounds && x.ConnectedCorners.All(y => !y.IsWithinBounds));

       if (firstWithInvisibleNeighbors != null)
       {
           return firstWithInvisibleNeighbors;
       }

       return corners.First(x => !x.IsWithinBounds);
    }
}

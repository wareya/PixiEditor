using System.Collections.ObjectModel;
using System.Collections.Specialized;
using Avalonia;
using Avalonia.Animation;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Rendering;
using Avalonia.Rendering.Composition;
using Avalonia.Rendering.SceneGraph;
using Avalonia.Skia;
using Avalonia.Threading;
using Avalonia.VisualTree;
using ChunkyImageLib.DataHolders;
using Drawie.Backend.Core;
using Drawie.Backend.Core.Bridge;
using Drawie.Backend.Core.Numerics;
using Drawie.Backend.Core.Shaders;
using Drawie.Backend.Core.Surfaces;
using Drawie.Backend.Core.Surfaces.PaintImpl;
using Drawie.Interop.Avalonia.Core;
using PixiEditor.Extensions.UI.Overlays;
using PixiEditor.Helpers;
using PixiEditor.Helpers.Converters;
using PixiEditor.Models.DocumentModels;
using PixiEditor.Models.Rendering;
using Drawie.Numerics;
using Drawie.Skia;
using PixiEditor.ViewModels.Document;
using PixiEditor.Views.Overlays;
using PixiEditor.Views.Overlays.Pointers;
using PixiEditor.Views.Visuals;
using Bitmap = Drawie.Backend.Core.Surfaces.Bitmap;
using Point = Avalonia.Point;

namespace PixiEditor.Views.Rendering;

internal class Scene : Zoombox.Zoombox, ICustomHitTest
{
    public static readonly StyledProperty<DocumentViewModel> DocumentProperty =
        AvaloniaProperty.Register<Scene, DocumentViewModel>(
            nameof(Document));

    public static readonly StyledProperty<bool> FadeOutProperty = AvaloniaProperty.Register<Scene, bool>(
        nameof(FadeOut), false);

    public static readonly StyledProperty<ObservableCollection<Overlay>> AllOverlaysProperty =
        AvaloniaProperty.Register<Scene, ObservableCollection<Overlay>>(
            nameof(AllOverlays));

    public static readonly StyledProperty<string> CheckerImagePathProperty = AvaloniaProperty.Register<Scene, string>(
        nameof(CheckerImagePath));

    public static readonly StyledProperty<Cursor> DefaultCursorProperty = AvaloniaProperty.Register<Scene, Cursor>(
        nameof(DefaultCursor));

    public static readonly StyledProperty<ViewportColorChannels> ChannelsProperty =
        AvaloniaProperty.Register<Scene, ViewportColorChannels>(
            nameof(Channels));

    public static readonly StyledProperty<SceneRenderer> SceneRendererProperty =
        AvaloniaProperty.Register<Scene, SceneRenderer>(
            nameof(SceneRenderer));

    public SceneRenderer SceneRenderer
    {
        get => GetValue(SceneRendererProperty);
        set => SetValue(SceneRendererProperty, value);
    }

    public Cursor DefaultCursor
    {
        get => GetValue(DefaultCursorProperty);
        set => SetValue(DefaultCursorProperty, value);
    }

    public string CheckerImagePath
    {
        get => GetValue(CheckerImagePathProperty);
        set => SetValue(CheckerImagePathProperty, value);
    }

    public ObservableCollection<Overlay> AllOverlays
    {
        get => GetValue(AllOverlaysProperty);
        set => SetValue(AllOverlaysProperty, value);
    }

    public bool FadeOut
    {
        get => GetValue(FadeOutProperty);
        set => SetValue(FadeOutProperty, value);
    }

    public DocumentViewModel Document
    {
        get => GetValue(DocumentProperty);
        set => SetValue(DocumentProperty, value);
    }

    public ViewportColorChannels Channels
    {
        get => GetValue(ChannelsProperty);
        set => SetValue(ChannelsProperty, value);
    }


    private Bitmap? checkerBitmap;

    private Overlay? capturedOverlay;

    private List<Overlay> mouseOverOverlays = new();

    private double sceneOpacity = 1;

    private Paint checkerPaint;

    private CompositionSurfaceVisual surfaceVisual;
    private Compositor compositor;

    private readonly Action update;
    private bool updateQueued;

    private CompositionDrawingSurface? surface;

    private string info = string.Empty;
    private bool initialized = false;
    private RenderApiResources resources;
    private DrawingSurface renderSurface;
    private PixelSize lastSize = PixelSize.Empty;
    private Cursor lastCursor;

    static Scene()
    {
        AffectsRender<Scene>(BoundsProperty, WidthProperty, HeightProperty, ScaleProperty, AngleRadiansProperty,
            FlipXProperty,
            FlipYProperty, DocumentProperty, AllOverlaysProperty);

        FadeOutProperty.Changed.AddClassHandler<Scene>(FadeOutChanged);
        CheckerImagePathProperty.Changed.AddClassHandler<Scene>(CheckerImagePathChanged);
        AllOverlaysProperty.Changed.AddClassHandler<Scene>(ActiveOverlaysChanged);
        DefaultCursorProperty.Changed.AddClassHandler<Scene>(DefaultCursorChanged);
        ChannelsProperty.Changed.AddClassHandler<Scene>(Refresh);
        DocumentProperty.Changed.AddClassHandler<Scene>(DocumentChanged);
        FlipXProperty.Changed.AddClassHandler<Scene>(Refresh);
        FlipYProperty.Changed.AddClassHandler<Scene>(Refresh);
    }

    private static void Refresh(Scene scene, AvaloniaPropertyChangedEventArgs args)
    {
        scene.InvalidateVisual();
    }

    public Scene()
    {
        ClipToBounds = true;
        Transitions = new Transitions
        {
            new DoubleTransition { Property = OpacityProperty, Duration = new TimeSpan(0, 0, 0, 0, 100) }
        };

        update = UpdateFrame;
        QueueNextFrame();
    }

    private ChunkResolution CalculateResolution()
    {
        VecD densityVec = Dimensions.Divide(RealDimensions);
        double density = Math.Min(densityVec.X, densityVec.Y);
        return density switch
        {
            > 8.01 => ChunkResolution.Eighth,
            > 4.01 => ChunkResolution.Quarter,
            > 2.01 => ChunkResolution.Half,
            _ => ChunkResolution.Full
        };
    }

    protected override void OnLoaded(RoutedEventArgs e)
    {
        base.OnLoaded(e);
        InitializeComposition();
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        if (initialized)
        {
            FreeGraphicsResources();
        }

        initialized = false;
        base.OnDetachedFromVisualTree(e);
    }

    private async void InitializeComposition()
    {
        try
        {
            var selfVisual = ElementComposition.GetElementVisual(this);
            if (selfVisual == null)
            {
                return;
            }

            compositor = selfVisual.Compositor;

            surface = compositor.CreateDrawingSurface();
            surfaceVisual = compositor.CreateSurfaceVisual();

            surfaceVisual.Size = new Vector(Bounds.Width, Bounds.Height);

            surfaceVisual.Surface = surface;
            ElementComposition.SetElementChildVisual(this, surfaceVisual);
            var (result, initInfo) = await DoInitialize(compositor, surface);
            info = initInfo;

            initialized = result;
            QueueNextFrame();
        }
        catch (Exception e)
        {
            info = e.Message;
            throw;
        }
    }

    public new void InvalidateVisual()
    {
        QueueNextFrame();
    }

    public void Draw(DrawingSurface renderTexture)
    {
        if (Document == null || SceneRenderer == null) return;

        renderTexture.Canvas.Save();
        var matrix = CalculateTransformMatrix();

        renderTexture.Canvas.SetMatrix(matrix.ToSKMatrix().ToMatrix3X3());

        RectD dirtyBounds = new RectD(0, 0, Document.Width, Document.Height);
        RenderScene(dirtyBounds);

        renderTexture.Canvas.Restore();
    }

    private void RenderScene(RectD bounds)
    {
        DrawCheckerboard(bounds);
        DrawOverlays(renderSurface, bounds, OverlayRenderSorting.Background);
        SceneRenderer.RenderScene(renderSurface, CalculateResolution());
        DrawOverlays(renderSurface, bounds, OverlayRenderSorting.Foreground);
    }

    private void DrawCheckerboard(RectD dirtyBounds)
    {
        if (checkerBitmap == null) return;

        RectD operationSurfaceRectToRender = new RectD(0, 0, dirtyBounds.Width, dirtyBounds.Height);
        float checkerScale = (float)ZoomToViewportConverter.ZoomToViewport(16, Scale) * 0.25f;
        checkerPaint?.Dispose();
        checkerPaint = new Paint
        {
            Shader = Shader.CreateBitmap(
                checkerBitmap,
                ShaderTileMode.Repeat, ShaderTileMode.Repeat,
                Matrix3X3.CreateScale(checkerScale, checkerScale)),
            FilterQuality = FilterQuality.None
        };

        renderSurface.Canvas.DrawRect(operationSurfaceRectToRender, checkerPaint);
    }

    private void DrawOverlays(DrawingSurface renderSurface, RectD dirtyBounds, OverlayRenderSorting sorting)
    {
        if (AllOverlays != null)
        {
            foreach (Overlay overlay in AllOverlays)
            {
                if (!overlay.IsVisible || overlay.OverlayRenderSorting != sorting)
                {
                    continue;
                }

                overlay.ZoomScale = Scale;

                if (!overlay.CanRender()) continue;

                overlay.RenderOverlay(renderSurface.Canvas, dirtyBounds);
            }
        }
    }

    protected override void OnPointerEntered(PointerEventArgs e)
    {
        base.OnPointerEntered(e);
        if (AllOverlays != null)
        {
            OverlayPointerArgs args = ConstructPointerArgs(e);
            foreach (Overlay overlay in AllOverlays)
            {
                if (!overlay.IsVisible || mouseOverOverlays.Contains(overlay) || !overlay.TestHit(args.Point)) continue;
                overlay.EnterPointer(args);
                mouseOverOverlays.Add(overlay);
            }

            e.Handled = args.Handled;
        }
    }

    protected override void OnPointerMoved(PointerEventArgs e)
    {
        base.OnPointerMoved(e);
        if (AllOverlays != null)
        {
            OverlayPointerArgs args = ConstructPointerArgs(e);

            Cursor = DefaultCursor;
            
            if (capturedOverlay != null)
            {
                capturedOverlay.MovePointer(args);
            }
            else
            {
                foreach (Overlay overlay in AllOverlays)
                {
                    if (!overlay.IsVisible) continue;

                    if (overlay.TestHit(args.Point))
                    {
                        if (!mouseOverOverlays.Contains(overlay))
                        {
                            overlay.EnterPointer(args);
                            mouseOverOverlays.Add(overlay);
                        }
                    }
                    else
                    {
                        if (mouseOverOverlays.Contains(overlay))
                        {
                            overlay.ExitPointer(args);
                            mouseOverOverlays.Remove(overlay);

                            e.Handled = args.Handled;
                            return;
                        }
                    }

                    overlay.MovePointer(args);
                    if (overlay.IsHitTestVisible)
                    {
                        Cursor = overlay.Cursor ?? DefaultCursor;
                    }
                }
            }

            e.Handled = args.Handled;
        }
    }

    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        base.OnPointerPressed(e);
        if (AllOverlays != null)
        {
            OverlayPointerArgs args = ConstructPointerArgs(e);
            if (capturedOverlay != null)
            {
                capturedOverlay?.PressPointer(args);
            }
            else
            {
                for (var i = 0; i < mouseOverOverlays.Count; i++)
                {
                    var overlay = mouseOverOverlays[i];
                    if (args.Handled) break;
                    if (!overlay.IsVisible) continue;
                    overlay.PressPointer(args);
                }
            }

            e.Handled = args.Handled;
        }
    }

    protected override void OnPointerExited(PointerEventArgs e)
    {
        base.OnPointerExited(e);
        if (AllOverlays != null)
        {
            OverlayPointerArgs args = ConstructPointerArgs(e);
            for (var i = 0; i < mouseOverOverlays.Count; i++)
            {
                var overlay = mouseOverOverlays[i];
                if (args.Handled) break;
                if (!overlay.IsVisible) continue;

                overlay.ExitPointer(args);
                mouseOverOverlays.Remove(overlay);
                i--;
            }

            e.Handled = args.Handled;
        }
    }

    protected override void OnPointerReleased(PointerReleasedEventArgs e)
    {
        base.OnPointerExited(e);
        if (AllOverlays != null)
        {
            OverlayPointerArgs args = ConstructPointerArgs(e);

            if (capturedOverlay != null)
            {
                capturedOverlay.ReleasePointer(args);
                capturedOverlay = null;
            }
            else
            {
                foreach (Overlay overlay in mouseOverOverlays)
                {
                    if (args.Handled) break;
                    if (!overlay.IsVisible) continue;

                    overlay.ReleasePointer(args);
                }
            }
        }
    }

    private OverlayPointerArgs ConstructPointerArgs(PointerEventArgs e)
    {
        return new OverlayPointerArgs
        {
            Point = ToCanvasSpace(e.GetPosition(this)),
            Modifiers = e.KeyModifiers,
            Pointer = new MouseOverlayPointer(e.Pointer, CaptureOverlay),
            PointerButton = e.GetMouseButton(this),
            InitialPressMouseButton = e is PointerReleasedEventArgs released
                ? released.InitialPressMouseButton
                : MouseButton.None,
        };
    }

    private VecD ToCanvasSpace(Point scenePosition)
    {
        Matrix transform = CalculateTransformMatrix();
        Point transformed = transform.Invert().Transform(scenePosition);
        return new VecD(transformed.X, transformed.Y);
    }

    private Matrix CalculateTransformMatrix()
    {
        Matrix transform = Matrix.Identity;
        transform = transform.Append(Matrix.CreateRotation((float)AngleRadians));
        transform = transform.Append(Matrix.CreateScale(FlipX ? -1 : 1, FlipY ? -1 : 1));
        transform = transform.Append(Matrix.CreateScale((float)Scale, (float)Scale));
        transform = transform.Append(Matrix.CreateTranslation(CanvasPos.X, CanvasPos.Y));
        return transform;
    }

    private float CalculateResolutionScale()
    {
        var resolution = CalculateResolution();
        return (float)resolution.InvertedMultiplier();
    }

    private void CaptureOverlay(Overlay? overlay, IPointer pointer)
    {
        if (AllOverlays == null) return;
        if (overlay == null)
        {
            pointer.Capture(null);
            mouseOverOverlays.Clear();
            capturedOverlay = null;
            return;
        }

        if (!AllOverlays.Contains(overlay)) return;

        pointer.Capture(this);
        capturedOverlay = overlay;
        mouseOverOverlays.Clear();
        mouseOverOverlays.Add(overlay);
    }

    private void OverlayCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        InvalidateVisual();
        if (e.OldItems != null)
        {
            foreach (Overlay overlay in e.OldItems)
            {
                overlay.RefreshRequested -= QueueRender;
                overlay.RefreshCursorRequested -= RefreshCursor;
            }
        }

        if (e.NewItems != null)
        {
            foreach (Overlay overlay in e.NewItems)
            {
                overlay.RefreshRequested += QueueRender;
                overlay.RefreshCursorRequested += RefreshCursor;
            }
        }
    }

    #region Interop

    void UpdateFrame()
    {
        updateQueued = false;
        var root = this.GetVisualRoot();
        if (root == null)
        {
            return;
        }

        surfaceVisual.Size = new Vector(Bounds.Width, Bounds.Height);

        if (double.IsNaN(surfaceVisual.Size.X) || double.IsNaN(surfaceVisual.Size.Y))
        {
            return;
        }

        var size = PixelSize.FromSize(Bounds.Size, root.RenderScaling);
        RenderFrame(size);
    }

    public void QueueNextFrame()
    {
        if (initialized && !updateQueued && compositor != null)
        {
            updateQueued = true;
            compositor.RequestCompositionUpdate(update);
        }
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        if (change.Property == BoundsProperty)
        {
            QueueNextFrame();
        }

        base.OnPropertyChanged(change);
    }

    private async Task<(bool success, string info)> DoInitialize(Compositor compositor,
        CompositionDrawingSurface surface)
    {
        var interop = await compositor.TryGetCompositionGpuInterop();
        if (interop == null)
        {
            return (false, "Composition interop not available");
        }

        return InitializeGraphicsResources(compositor, surface, interop);
    }

    protected (bool success, string info) InitializeGraphicsResources(Compositor targetCompositor,
        CompositionDrawingSurface compositionDrawingSurface, ICompositionGpuInterop interop)
    {
        resources = IDrawieInteropContext.Current.CreateResources(compositionDrawingSurface, interop);

        return (true, string.Empty);
    }

    protected void FreeGraphicsResources()
    {
        resources?.DisposeAsync();
        renderSurface?.Dispose();
        renderSurface = null;
        resources = null;
    }

    protected  void RenderFrame(PixelSize size)
    {
        if (resources != null)
        {
            if (size.Width == 0 || size.Height == 0)
            {
                return;
            }

            if (renderSurface == null || lastSize != size)
            {
                resources.CreateTemporalObjects(size);

                VecI sizeVec = new VecI(size.Width, size.Height);

                renderSurface?.Dispose();

                renderSurface =
                    DrawingBackendApi.Current.CreateRenderSurface(sizeVec,
                        resources.Texture, SurfaceOrigin.BottomLeft);

                lastSize = size;
            }

            resources.Render(size, () =>
            {
                renderSurface.Canvas.Clear();
                Draw(renderSurface);
                renderSurface.Flush();
            });
        }
    }

    #endregion

    public void RefreshCursor()
    {
        Cursor = DefaultCursor;
        if (AllOverlays != null)
        {
            foreach (Overlay overlay in AllOverlays)
            {
                if (!overlay.IsVisible) continue;
                
                if (overlay.IsHitTestVisible)
                {
                    Cursor = overlay.Cursor ?? DefaultCursor;
                }
            }
        }
    }
    
    private void QueueRender()
    {
        Dispatcher.UIThread.Post(InvalidateVisual, DispatcherPriority.Render);
        QueueNextFrame();
    }

    private static void FadeOutChanged(Scene scene, AvaloniaPropertyChangedEventArgs e)
    {
        scene.sceneOpacity = e.NewValue is true ? 0 : 1;
        scene.InvalidateVisual();
    }

    private static void ActiveOverlaysChanged(Scene scene, AvaloniaPropertyChangedEventArgs e)
    {
        if (e.OldValue is ObservableCollection<Overlay> oldOverlays)
        {
            oldOverlays.CollectionChanged -= scene.OverlayCollectionChanged;
        }

        if (e.NewValue is ObservableCollection<Overlay> newOverlays)
        {
            newOverlays.CollectionChanged += scene.OverlayCollectionChanged;
        }
    }

    private static void CheckerImagePathChanged(Scene scene, AvaloniaPropertyChangedEventArgs e)
    {
        if (e.NewValue is string path)
        {
            scene.checkerBitmap = ImagePathToBitmapConverter.LoadDrawingApiBitmapFromRelativePath(path);
        }
        else
        {
            scene.checkerBitmap = null;
        }
    }

    private static void DocumentChanged(Scene scene, AvaloniaPropertyChangedEventArgs e)
    {
        if (e.NewValue is DocumentViewModel documentViewModel)
        {
            scene.ContentDimensions = documentViewModel.SizeBindable;
        }
    }

    private static void DefaultCursorChanged(Scene scene, AvaloniaPropertyChangedEventArgs e)
    {
        if (e.NewValue is Cursor cursor)
        {
            scene.Cursor = cursor;
        }
    }

    bool ICustomHitTest.HitTest(Point point)
    {
        return Bounds.Contains(point);
    }
}

internal class DrawSceneOperation : SkiaDrawOperation
{
    public DocumentViewModel Document { get; set; }
    public VecD ContentPosition { get; set; }
    public double Scale { get; set; }
    public double ResolutionScale { get; set; }
    public double Angle { get; set; }
    public bool FlipX { get; set; }
    public bool FlipY { get; set; }
    public Rect ViewportBounds { get; }


    public Action<DrawingSurface> RenderScene;

    private Texture renderTexture;

    private double opacity;

    public DrawSceneOperation(Action<DrawingSurface> renderAction, DocumentViewModel document, VecD contentPosition,
        double scale,
        double resolutionScale,
        double opacity,
        double angle, bool flipX, bool flipY, Rect dirtyBounds, Rect viewportBounds,
        Texture renderTexture) : base(dirtyBounds)
    {
        RenderScene = renderAction;
        Document = document;
        ContentPosition = contentPosition;
        Scale = scale;
        Angle = angle;
        FlipX = flipX;
        FlipY = flipY;
        ViewportBounds = viewportBounds;
        ResolutionScale = resolutionScale;
        this.opacity = opacity;
        this.renderTexture = renderTexture;
    }

    public override void Render(ISkiaSharpApiLease lease)
    {
        if (Document == null) return;

        SKCanvas canvas = lease.SkCanvas;

        int count = canvas.Save();

        //using var ctx = DrawingBackendApi.Current.RenderOnDifferentGrContext(lease.GrContext);

        DrawingSurface surface = DrawingSurface.FromNative(lease.SkSurface);

        surface.Canvas.DrawSurface(renderTexture.DrawingSurface, 0, 0);

        RenderScene?.Invoke(surface);

        canvas.RestoreToCount(count);
        DrawingSurface.Unmanage(surface);
    }

    public override bool Equals(ICustomDrawOperation? other)
    {
        return false;
    }
}

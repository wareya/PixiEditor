using Avalonia;
using Avalonia.Media;
using ChunkyImageLib;
using PixiEditor.DrawingApi.Core;

namespace PixiEditor.AvaloniaUI.Views.Visuals;

public class SurfaceImage : IImage
{
    public Surface Surface { get; set; }
    public Stretch Stretch { get; set; } = Stretch.Uniform;

    public Size Size { get; }

    public SurfaceImage(Surface surface)
    {
        Surface = surface;
        Size = new Size(surface.Size.X, surface.Size.Y);
    }

    public void Draw(DrawingContext context, Rect sourceRect, Rect destRect)
    {
        context.Custom(new DrawSurfaceOperation(destRect, Surface, Stretch));
    }
}

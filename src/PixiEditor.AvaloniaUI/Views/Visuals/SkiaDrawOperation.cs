﻿using Avalonia;
using Avalonia.Media;
using Avalonia.Platform;
using Avalonia.Rendering.SceneGraph;
using Avalonia.Skia;

namespace PixiEditor.AvaloniaUI.Views.Visuals;

internal abstract class SkiaDrawOperation : ICustomDrawOperation
{
    public Rect Bounds { get; }

    public SkiaDrawOperation(Rect bounds)
    {
        Bounds = bounds;
    }

    public abstract bool Equals(ICustomDrawOperation? other);

    public virtual void Dispose() { }

    public bool HitTest(Point p) => false;

    public void Render(ImmediateDrawingContext context)
    {
        if (!context.TryGetFeature(out ISkiaSharpApiLeaseFeature leaseFeature))
        {
            throw new InvalidOperationException("SkiaSharp API lease feature is not available.");
        }

        using var lease = leaseFeature.Lease();

        Render(lease);
    }

    public abstract void Render(ISkiaSharpApiLease lease);
}

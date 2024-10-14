﻿using ChunkyImageLib.DataHolders;
using PixiEditor.ChangeableDocument.Changeables.Animations;
using PixiEditor.ChangeableDocument.Changeables.Graph.Interfaces;
using PixiEditor.DrawingApi.Core.Surfaces;
using PixiEditor.Numerics;

namespace PixiEditor.Models.Rendering;

public class PreviewPainter
{
    public RectD? Bounds { get; set; }
    public string ElementToRenderName { get; set; }
    public IPreviewRenderable PreviewRenderable { get; set; }
    public event Action RequestRepaint;
    
    public PreviewPainter(IPreviewRenderable previewRenderable, RectD? tightBounds, string elementToRenderName = "")
    {
        PreviewRenderable = previewRenderable;
        Bounds = tightBounds;
        ElementToRenderName = elementToRenderName;
    }

    public void Paint(DrawingSurface renderOn, ChunkResolution resolution, KeyFrameTime frame) 
    {
        if (PreviewRenderable == null || Bounds == null)
        {
            return;
        }

        PreviewRenderable.RenderPreview(renderOn, resolution, frame.Frame, ElementToRenderName);
    }

    public void Repaint()
    {
        RequestRepaint?.Invoke();
    }
}

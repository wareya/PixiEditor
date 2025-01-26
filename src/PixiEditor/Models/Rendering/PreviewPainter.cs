﻿using Avalonia;
using Avalonia.Threading;
using ChunkyImageLib.DataHolders;
using Drawie.Backend.Core;
using Drawie.Backend.Core.Numerics;
using PixiEditor.ChangeableDocument.Changeables.Animations;
using PixiEditor.ChangeableDocument.Changeables.Graph.Interfaces;
using Drawie.Backend.Core.Surfaces;
using Drawie.Backend.Core.Surfaces.ImageData;
using Drawie.Numerics;
using PixiEditor.ChangeableDocument.Rendering;

namespace PixiEditor.Models.Rendering;

public class PreviewPainter
{
    public string ElementToRenderName { get; set; }
    public IPreviewRenderable PreviewRenderable { get; set; }
    public ColorSpace ProcessingColorSpace { get; set; }
    public KeyFrameTime FrameTime { get; set; }
    public VecI DocumentSize { get; set; }
    public DocumentRenderer Renderer { get; set; }

    private Dictionary<int, Texture> renderTextures = new();
    private Dictionary<int, PainterInstance> painterInstances = new();

    private HashSet<int> dirtyTextures = new HashSet<int>();
    private HashSet<int> repaintingTextures = new HashSet<int>();

    private Dictionary<int, VecI> pendingResizes = new();
    private HashSet<int> pendingRemovals = new();

    private int lastRequestId = 0;

    public PreviewPainter(DocumentRenderer renderer, IPreviewRenderable previewRenderable, KeyFrameTime frameTime,
        VecI documentSize, ColorSpace processingColorSpace, string elementToRenderName = "")
    {
        PreviewRenderable = previewRenderable;
        ElementToRenderName = elementToRenderName;
        ProcessingColorSpace = processingColorSpace;
        FrameTime = frameTime;
        DocumentSize = documentSize;
        Renderer = renderer;
    }

    public void Paint(DrawingSurface renderOn, int painterId)
    {
        if (!renderTextures.TryGetValue(painterId, out Texture? renderTexture))
        {
            return;
        }

        if (renderTexture == null || renderTexture.IsDisposed)
        {
            return;
        }

        renderOn.Canvas.DrawSurface(renderTexture.DrawingSurface, 0, 0);
    }

    public PainterInstance AttachPainterInstance()
    {
        int requestId = lastRequestId++;

        PainterInstance painterInstance = new() { RequestId = requestId };

        painterInstances[requestId] = painterInstance;

        return painterInstance;
    }

    public void ChangeRenderTextureSize(int requestId, VecI size)
    {
        if (repaintingTextures.Contains(requestId))
        {
            pendingResizes[requestId] = size;
            return;
        }

        if (renderTextures.ContainsKey(requestId))
        {
            renderTextures[requestId].Dispose();
        }

        renderTextures[requestId] = Texture.ForProcessing(size, ProcessingColorSpace);
    }

    public void RemovePainterInstance(int requestId)
    {
        painterInstances.Remove(requestId);
        dirtyTextures.Remove(requestId);
        
        if (repaintingTextures.Contains(requestId))
        {
            pendingRemovals.Add(requestId);
            return;
        }

        renderTextures[requestId]?.Dispose();
        renderTextures.Remove(requestId);
    }

    public void Repaint()
    {
        foreach (var texture in renderTextures)
        {
            dirtyTextures.Add(texture.Key);
        }

        RepaintDirty();
    }

    public void RepaintFor(int requestId)
    {
        dirtyTextures.Add(requestId);
        RepaintDirty();
    }

    private void RepaintDirty()
    {
        var dirtyArray = dirtyTextures.ToArray();
        foreach (var texture in dirtyArray)
        {
            if (!renderTextures.TryGetValue(texture, out var renderTexture))
            {
                continue;
            }

            repaintingTextures.Add(texture);

            renderTexture.DrawingSurface.Canvas.Clear();
            renderTexture.DrawingSurface.Canvas.Save();

            PainterInstance painterInstance = painterInstances[texture];

            Matrix3X3? matrix = painterInstance.RequestMatrix?.Invoke();

            renderTexture.DrawingSurface.Canvas.SetMatrix(matrix ?? Matrix3X3.Identity);

            RenderContext context = new(renderTexture.DrawingSurface, FrameTime, ChunkResolution.Full, DocumentSize,
                ProcessingColorSpace);

            dirtyTextures.Remove(texture);
            Renderer.RenderNodePreview(PreviewRenderable, renderTexture.DrawingSurface, context, ElementToRenderName)
                .ContinueWith(_ =>
                {
                    Dispatcher.UIThread.Invoke(() =>
                    {
                        if(pendingRemovals.Contains(texture))
                        {
                            renderTexture.Dispose();
                            renderTextures.Remove(texture);
                            pendingRemovals.Remove(texture);
                            pendingResizes.Remove(texture);
                            dirtyTextures.Remove(texture);
                            return;
                        }
                        
                        if (renderTexture is { IsDisposed: false })
                        {
                            renderTexture.DrawingSurface.Canvas.Restore();
                        }

                        painterInstance.RequestRepaint?.Invoke();
                        repaintingTextures.Remove(texture);

                        if (pendingResizes.Remove(texture, out VecI size))
                        {
                            ChangeRenderTextureSize(texture, size);
                            dirtyTextures.Add(texture);
                        }

                        if (repaintingTextures.Count == 0 && dirtyTextures.Count > 0)
                        {
                            RepaintDirty();
                        }
                    });
                });
        }
    }
}

public class PainterInstance
{
    public int RequestId { get; set; }
    public Func<Matrix3X3?>? RequestMatrix;
    public Action RequestRepaint;
}

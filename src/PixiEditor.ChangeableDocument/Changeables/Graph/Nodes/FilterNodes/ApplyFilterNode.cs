﻿using PixiEditor.ChangeableDocument.Changeables.Graph.Interfaces;
using PixiEditor.ChangeableDocument.Helpers;
using PixiEditor.ChangeableDocument.Rendering;
using Drawie.Backend.Core;
using Drawie.Backend.Core.Numerics;
using Drawie.Backend.Core.Surfaces;
using Drawie.Backend.Core.Surfaces.ImageData;
using Drawie.Backend.Core.Surfaces.PaintImpl;
using Drawie.Numerics;

namespace PixiEditor.ChangeableDocument.Changeables.Graph.Nodes.FilterNodes;

[NodeInfo("ApplyFilter")]
public class ApplyFilterNode : RenderNode, IRenderInput
{
    private Paint _paint = new();
    public InputProperty<Filter?> Filter { get; }

    public RenderInputProperty Background { get; }

    public ApplyFilterNode()
    {
        Background = CreateRenderInput("Input", "IMAGE");
        Filter = CreateInput<Filter>("Filter", "FILTER", null);
        Output.FirstInChain = null;
    }

    protected override void OnPaint(RenderContext context, DrawingSurface surface)
    {
        if (_paint == null)
            return;

        _paint.SetFilters(Filter.Value);

        if (!context.ProcessingColorSpace.IsSrgb)
        {
            var target = Texture.ForProcessing(surface, ColorSpace.CreateSrgb());

            int saved = surface.Canvas.Save();
            surface.Canvas.SetMatrix(Matrix3X3.Identity);

            target.DrawingSurface.Canvas.SaveLayer(_paint);
            Background.Value?.Paint(context, target.DrawingSurface);
            target.DrawingSurface.Canvas.Restore();

            surface.Canvas.DrawSurface(target.DrawingSurface, 0, 0);
            surface.Canvas.RestoreToCount(saved);
            target.Dispose();
        }
        else
        {
            int layer = surface.Canvas.SaveLayer(_paint);
            Background.Value?.Paint(context, surface);
            surface.Canvas.RestoreToCount(layer);
        }
    }

    public override RectD? GetPreviewBounds(int frame, string elementToRenderName = "")
    {
        return PreviewUtils.FindPreviewBounds(Background.Connection, frame, elementToRenderName);
    }

    public override bool RenderPreview(DrawingSurface renderOn, RenderContext context,
        string elementToRenderName)
    {
        int layer = renderOn.Canvas.SaveLayer(_paint);
        Background.Value?.Paint(context, renderOn);
        renderOn.Canvas.RestoreToCount(layer);

        return true;
    }

    public override Node CreateCopy() => new ApplyFilterNode();
}

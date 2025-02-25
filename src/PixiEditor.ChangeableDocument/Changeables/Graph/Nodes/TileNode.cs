﻿using Drawie.Backend.Core;
using Drawie.Backend.Core.Numerics;
using Drawie.Backend.Core.Shaders;
using Drawie.Backend.Core.Surfaces;
using Drawie.Backend.Core.Surfaces.ImageData;
using Drawie.Backend.Core.Surfaces.PaintImpl;
using Drawie.Numerics;
using PixiEditor.ChangeableDocument.Changeables.Graph.Interfaces;
using PixiEditor.ChangeableDocument.Helpers;
using PixiEditor.ChangeableDocument.Rendering;

namespace PixiEditor.ChangeableDocument.Changeables.Graph.Nodes;

[NodeInfo("Tile")]
public class TileNode : RenderNode
{
    public InputProperty<Texture> Image { get; }
    public InputProperty<ShaderTileMode> TileModeX { get; }
    public InputProperty<ShaderTileMode> TileModeY { get; }
    public InputProperty<Matrix3X3> Matrix { get; }

    private Image lastImage;
    private Shader tileShader;
    private Paint paint;

    public TileNode()
    {
        Image = CreateInput<Texture>("Image", "IMAGE", null);
        TileModeX = CreateInput<ShaderTileMode>("TileModeX", "TILE_MODE_X", ShaderTileMode.Repeat);
        TileModeY = CreateInput<ShaderTileMode>("TileModeY", "TILE_MODE_Y", ShaderTileMode.Repeat);
        Matrix = CreateInput<Matrix3X3>("Matrix", "MATRIX", Matrix3X3.Identity);

        Output.FirstInChain = null;
    }

    protected override bool ExecuteOnlyOnCacheChange => true;
    protected override CacheTriggerFlags CacheTrigger => CacheTriggerFlags.Inputs;

    protected override void OnExecute(RenderContext context)
    {
        base.OnExecute(context);

        lastImage?.Dispose();
        tileShader?.Dispose();
        if (paint != null)
        {
            paint.Shader = null;
        }

        if (Image.Value == null)
            return;

        lastImage = Image.Value.DrawingSurface.Snapshot();
        tileShader = Shader.CreateImage(lastImage, TileModeX.Value, TileModeY.Value, Matrix.Value);

        paint ??= new();
        paint.Shader = tileShader;
    }

    protected override void OnPaint(RenderContext context, DrawingSurface surface)
    {
        if (paint == null)
            return;

        surface.Canvas.DrawRect(0, 0, context.DocumentSize.X, context.DocumentSize.Y, paint);
    }

    public override Node CreateCopy()
    {
        return new TileNode();
    }

    public override RectD? GetPreviewBounds(int frame, string elementToRenderName = "")
    {
        return null;
    }

    public override bool RenderPreview(DrawingSurface renderOn, RenderContext context, string elementToRenderName)
    {
        return false;
    }

}

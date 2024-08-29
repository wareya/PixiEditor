﻿using PixiEditor.ChangeableDocument.Rendering;
using PixiEditor.DrawingApi.Core;
using PixiEditor.DrawingApi.Core.ColorsImpl;
using PixiEditor.DrawingApi.Core.Surfaces;
using PixiEditor.DrawingApi.Core.Surfaces.PaintImpl;
using PixiEditor.Numerics;
using ShapeData = PixiEditor.ChangeableDocument.Changeables.Graph.Nodes.Shapes.Data.ShapeData;

namespace PixiEditor.ChangeableDocument.Changeables.Graph.Nodes.Shapes;

[NodeInfo("RasterizeShape", "RASTERIZE_SHAPE", Category = "SHAPE")]
public class RasterizeShape : Node
{
    public OutputProperty<Texture> Image { get; }

    public InputProperty<ShapeData> Data { get; }


    public RasterizeShape()
    {
        Image = CreateOutput<Texture>("Image", "IMAGE", null);
        Data = CreateInput<ShapeData>("Points", "POINTS", null);
    }

    protected override Texture? OnExecute(RenderingContext context)
    {
        var shape = Data.Value;

        if (shape == null || !shape.IsValid())
            return null;

        var size = context.DocumentSize;
        var image = RequestTexture(0, size);
        
        shape.Rasterize(image.DrawingSurface);

        Image.Value = image;
        
        return image;
    }

    public override Node CreateCopy() => new RasterizeShape();
}

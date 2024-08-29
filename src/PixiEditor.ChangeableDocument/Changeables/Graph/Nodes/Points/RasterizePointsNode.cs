﻿using PixiEditor.ChangeableDocument.Changeables.Graph.Context;
using PixiEditor.ChangeableDocument.Rendering;
using PixiEditor.DrawingApi.Core;
using PixiEditor.DrawingApi.Core.ColorsImpl;
using PixiEditor.DrawingApi.Core.Shaders.Generation;
using PixiEditor.DrawingApi.Core.Shaders.Generation.Expressions;
using PixiEditor.DrawingApi.Core.Surfaces;
using PixiEditor.DrawingApi.Core.Surfaces.PaintImpl;
using PixiEditor.Numerics;

namespace PixiEditor.ChangeableDocument.Changeables.Graph.Nodes.Points;

[NodeInfo("RasterizePoints")]
public class RasterizePointsNode : Node
{
    private Paint _paint = new() { Color = Colors.White };

    public OutputProperty<Texture> Image { get; }

    public InputProperty<PointList> Points { get; }


    public RasterizePointsNode()
    {
        Image = CreateOutput<Texture>("Image", "IMAGE", null);
        Points = CreateInput("Points", "POINTS", PointList.Empty);
    }

    protected override Texture? OnExecute(RenderingContext context)
    {
        var points = Points.Value;

        if (points.Count == 0)
            return null;

        var size = context.DocumentSize;
        var image = RequestTexture(0, size);

        image.DrawingSurface.Canvas.DrawPoints(
            PointMode.Points, 
            points.Select(x => new Point((float)x.X * size.X, (float)x.Y * size.Y)).ToArray(),
            _paint);

        Image.Value = image;
        
        return image;
    }

    public override Node CreateCopy() => new RasterizePointsNode();
}

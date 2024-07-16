﻿using PixiEditor.ChangeableDocument.Rendering;
using PixiEditor.DrawingApi.Core.Surface.PaintImpl;
using PixiEditor.Numerics;

namespace PixiEditor.ChangeableDocument.Changeables.Graph.Nodes;

public class MatrixTransformNode : Node
{
    private Matrix4x5F previousMatrix = new(
        (1, 0, 0, 0, 0),
        (0, 1, 0, 0, 0),
        (0, 0, 1, 0, 0),
        (0, 0, 0, 1, 0));
    
    private Paint paint;
    
    public OutputProperty<Surface> Transformed { get; }
    
    public InputProperty<Surface?> Input { get; }
    
    public InputProperty<Matrix4x5F> Matrix { get; }

    public MatrixTransformNode()
    {
        Transformed = CreateOutput<Surface>(nameof(Transformed), "TRANSFORMED", null);
        Input = CreateInput<Surface>(nameof(Input), "INPUT", null);
        Matrix = CreateInput(nameof(Matrix), "MATRIX", previousMatrix);

        paint = new Paint { ColorFilter = ColorFilter.CreateColorMatrix(previousMatrix) };
    }
    
    protected override Surface? OnExecute(RenderingContext context)
    {
        var currentMatrix = Matrix.Value;
        if (previousMatrix != currentMatrix)
        {
            paint.ColorFilter = ColorFilter.CreateColorMatrix(Matrix.Value);
            previousMatrix = currentMatrix;
        }

        var workingSurface = new Surface(Input.Value.Size);
        
        workingSurface.DrawingSurface.Canvas.DrawSurface(Input.Value.DrawingSurface, 0, 0, paint);

        Transformed.Value = workingSurface;
        
        return Transformed.Value;
    }

    public override bool Validate() => Input.Value != null;

    public override Node CreateCopy() => new MatrixTransformNode();
}

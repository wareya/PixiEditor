﻿using PixiEditor.ChangeableDocument.Changeables.Animations;
using PixiEditor.ChangeableDocument.Changeables.Graph.Interfaces;
using PixiEditor.DrawingApi.Core.ColorsImpl;

namespace PixiEditor.ChangeableDocument.Changeables.Graph.Nodes;

public class CombineColor : Node
{
    public FieldOutputProperty<Color> Color { get; }
    
    public FieldInputProperty<double> R { get; }
    
    public FieldInputProperty<double> G { get; }
    
    public FieldInputProperty<double> B { get; }
    
    public FieldInputProperty<double> A { get; }

    public CombineColor()
    {
        Color = CreateFieldOutput(nameof(Color), "COLOR", GetColor);
        
        R = CreateFieldInput("R", "R", _ => 0d);
        G = CreateFieldInput("G", "G", _ => 0d);
        B = CreateFieldInput("B", "B", _ => 0d);
        A = CreateFieldInput("A", "A", _ => 1d);
    }

    private Color GetColor(FieldContext ctx)
    {
        var r = R.Value(ctx) * 255;
        var g = G.Value(ctx) * 255;
        var b = B.Value(ctx) * 255;
        var a = A.Value(ctx) * 255;

        return new Color((byte)r, (byte)g, (byte)b, (byte)a);
    }
    
    protected override ChunkyImage? OnExecute(KeyFrameTime frameTime)
    {
        return null;
    }

    public override bool Validate() => true;

    public override Node CreateCopy() => new SeparateVecI();
}

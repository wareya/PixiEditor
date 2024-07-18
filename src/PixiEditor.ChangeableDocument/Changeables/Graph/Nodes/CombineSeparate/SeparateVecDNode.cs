﻿using PixiEditor.ChangeableDocument.Rendering;
using PixiEditor.Numerics;

namespace PixiEditor.ChangeableDocument.Changeables.Graph.Nodes.CombineSeparate;

public class SeparateVecDNode : Node
{
    public FieldInputProperty<VecD> Vector { get; }
    
    public FieldOutputProperty<double> X { get; }
    
    public FieldOutputProperty<double> Y { get; }

    public SeparateVecDNode()
    {
        X = CreateFieldOutput("X", "X", ctx => Vector.Value(ctx).X);
        Y = CreateFieldOutput("Y", "Y", ctx => Vector.Value(ctx).Y);
        Vector = CreateFieldInput("Vector", "VECTOR", new VecD(0, 0));
    }

    protected override string NodeUniqueName => "SeparateVecD";

    protected override Surface? OnExecute(RenderingContext context)
    {
        return null;
    }

    public override bool Validate() => true;

    public override Node CreateCopy() => new SeparateVecDNode();
}

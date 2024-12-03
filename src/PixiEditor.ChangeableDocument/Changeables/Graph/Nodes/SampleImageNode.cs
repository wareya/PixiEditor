﻿using PixiEditor.ChangeableDocument.Changeables.Graph.Context;
using PixiEditor.ChangeableDocument.Rendering;
using Drawie.Backend.Core;
using Drawie.Backend.Core.ColorsImpl;
using Drawie.Backend.Core.Shaders.Generation.Expressions;
using Drawie.Backend.Core.Surfaces;
using Drawie.Numerics;

namespace PixiEditor.ChangeableDocument.Changeables.Graph.Nodes;

[NodeInfo("SampleImage")]
public class SampleImageNode : Node
{
    public InputProperty<Texture?> Image { get; }

    public FuncInputProperty<Float2> Coordinate { get; }

    public FuncOutputProperty<Half4> Color { get; }

    public SampleImageNode()
    {
        Image = CreateInput<Texture>(nameof(Texture), "IMAGE", null);
        Coordinate = CreateFuncInput<Float2>(nameof(Coordinate), "UV", VecD.Zero);
        Color = CreateFuncOutput(nameof(Color), "COLOR", GetColor);
    }

    private Half4 GetColor(FuncContext context)
    {
        context.ThrowOnMissingContext();

        if (Image.Value is null)
        {
            return new Half4("");
        }

        Expression uv = context.GetValue(Coordinate);

        return context.SampleSurface(Image.Value.DrawingSurface, uv);
    }

    protected override void OnExecute(RenderContext context)
    {
        
    }

    public override Node CreateCopy() => new SampleImageNode();
}

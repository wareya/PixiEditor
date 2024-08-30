﻿using PixiEditor.ChangeableDocument.Changeables.Graph.Context;
using PixiEditor.ChangeableDocument.Enums;
using PixiEditor.ChangeableDocument.Rendering;
using PixiEditor.DrawingApi.Core;
using PixiEditor.DrawingApi.Core.ColorsImpl;
using PixiEditor.DrawingApi.Core.Shaders.Generation.Expressions;

namespace PixiEditor.ChangeableDocument.Changeables.Graph.Nodes.CombineSeparate;

[NodeInfo("SeparateColor", "SEPARATE_COLOR_NODE", Category = "COLOR")]
public class SeparateColorNode : Node
{
    private readonly NodeVariableAttachments contextVariables = new();
    
    public FuncInputProperty<Half4> Color { get; }
    
    public InputProperty<CombineSeparateColorMode> Mode { get; }

    /// <summary>
    /// Represents either Red 'R' or Hue 'H' depending on <see cref="Mode"/>
    /// </summary>
    public FuncOutputProperty<Float1> V1 { get; }
    
    /// <summary>
    /// Represents either Green 'G' or Saturation 'S' depending on <see cref="Mode"/>
    /// </summary>
    public FuncOutputProperty<Float1> V2 { get; }
    
    /// <summary>
    /// Represents either Blue 'B', Value 'V' or Lightness 'L' depending on <see cref="Mode"/>
    /// </summary>
    public FuncOutputProperty<Float1> V3 { get; }
    
    public FuncOutputProperty<Float1> A { get; }
    
    public SeparateColorNode()
    {
        V1 = CreateFuncOutput<Float1>("R", "R", ctx => GetColor(ctx).R);
        V2 = CreateFuncOutput<Float1>("G", "G", ctx => GetColor(ctx).G);
        V3 = CreateFuncOutput<Float1>("B", "B", ctx => GetColor(ctx).B);
        A = CreateFuncOutput<Float1>("A", "A", ctx => GetColor(ctx).A);
        Mode = CreateInput("Mode", "MODE", CombineSeparateColorMode.RGB);
        Color = CreateFuncInput<Half4>(nameof(Color), "COLOR", new Color());
    }

    protected override Texture? OnExecute(RenderingContext context)
    {
        return null;
    }
    
    private Half4 GetColor(FuncContext ctx) =>
        Mode.Value switch
        {
            CombineSeparateColorMode.RGB => GetRgba(ctx),
            CombineSeparateColorMode.HSV => GetHsva(ctx),
            CombineSeparateColorMode.HSL => GetHsla(ctx)
        };

    private Half4 GetRgba(FuncContext ctx) => 
        contextVariables.GetOrAttachNew(ctx, Color, () => Color.Value(ctx));

    private Half4 GetHsva(FuncContext ctx) =>
        contextVariables.GetOrAttachNew(ctx, Color, () => ctx.RgbaToHsva(ctx.GetValue(Color)));

    private Half4 GetHsla(FuncContext ctx) =>
        contextVariables.GetOrAttachNew(ctx, Color, () => ctx.RgbaToHsla(ctx.GetValue(Color)));

    public override Node CreateCopy() => new SeparateColorNode();
}

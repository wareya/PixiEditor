﻿using PixiEditor.ChangeableDocument.Changeables.Animations;
using PixiEditor.ChangeableDocument.Changeables.Graph.Interfaces;
using PixiEditor.DrawingApi.Core.ColorsImpl;
using PixiEditor.DrawingApi.Core.Surface;
using PixiEditor.Numerics;

namespace PixiEditor.ChangeableDocument.Changeables.Graph.Nodes;

public class CreateImageNode : Node
{
    public InputProperty<VecI> Size { get; }
    
    public FieldInputProperty<Color> Color { get; }
    
    public OutputProperty<ChunkyImage> Output { get; }
    
    
    public CreateImageNode() 
    {
        Size = CreateInput(nameof(Size), "SIZE", new VecI(32, 32));
        Color = CreateFieldInput(nameof(Color), "COLOR", _ => new Color(0, 0, 0, 255));
        Output = CreateOutput<ChunkyImage>(nameof(Output), "OUTPUT", null);
    }

    protected override ChunkyImage? OnExecute(KeyFrameTime frameTime)
    {
        var width = Size.Value.X;
        var height = Size.Value.Y;

        Output.Value = new ChunkyImage(Size.Value);

        for (int y = 0; y < width; y++)
        {
            for (int x = 0; x < height; x++)
            {
                var context = new CreateImageContext(new VecD((double)x / width, (double)y / width), new VecI(width, height));
                var color = Color.Value(context);

                Output.Value.EnqueueDrawPixel(new VecI(x, y), color, BlendMode.Src);
            }
        }

        Output.Value.CommitChanges();

        return Output.Value;
    }

    public override bool Validate() => true;

    public override Node CreateCopy() => new CreateImageNode();

    record CreateImageContext(VecD Position, VecI Size) : IFieldContext
    {
    }
}

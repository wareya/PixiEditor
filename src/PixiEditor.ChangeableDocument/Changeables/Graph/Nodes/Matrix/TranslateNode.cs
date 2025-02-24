﻿using Drawie.Backend.Core.Numerics;
using Drawie.Numerics;

namespace PixiEditor.ChangeableDocument.Changeables.Graph.Nodes.Matrix;

[NodeInfo("Translate")]
public class TranslateNode : Matrix3X3BaseNode
{
    public InputProperty<VecD> Translation { get; }

    public TranslateNode()
    {
        Translation = CreateInput("Translation", "TRANSLATION", VecD.Zero);
    }

    protected override Matrix3X3 CalculateMatrix(Matrix3X3 input)
    {
        Matrix3X3 matrix = Matrix3X3.CreateTranslation((float)(Translation.Value.X), (float)(Translation.Value.Y));
        return input.PostConcat(matrix);
    }

    public override Node CreateCopy()
    {
        return new TranslateNode();
    }
}

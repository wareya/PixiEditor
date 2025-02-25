﻿using Drawie.Numerics;
using PixiEditor.ChangeableDocument.Rendering;

namespace PixiEditor.ChangeableDocument.Changeables.Graph.Nodes;

[NodeInfo("DocumentInfo")]
public class DocumentInfoNode : Node
{
    public OutputProperty<VecI> Size { get; }
    public OutputProperty<VecD> Center { get; }

    public DocumentInfoNode()
    {
        Size = CreateOutput("Size", "SIZE", new VecI(0, 0));
        Center = CreateOutput("Center", "CENTER", new VecD(0, 0));
    }

    protected override void OnExecute(RenderContext context)
    {
        Size.Value = context.DocumentSize;
        Center.Value = new VecD(context.DocumentSize.X / 2.0, context.DocumentSize.Y / 2.0);
    }

    public override Node CreateCopy()
    {
        return new DocumentInfoNode();
    }
}

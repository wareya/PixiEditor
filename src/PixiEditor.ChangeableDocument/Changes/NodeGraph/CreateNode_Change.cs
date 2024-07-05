﻿using System.Collections.Immutable;
using PixiEditor.ChangeableDocument.Changeables.Graph.Interfaces;
using PixiEditor.ChangeableDocument.Changeables.Graph.Nodes;
using PixiEditor.ChangeableDocument.ChangeInfos.NodeGraph;
using PixiEditor.Numerics;

namespace PixiEditor.ChangeableDocument.Changes.NodeGraph;

internal class CreateNode_Change : Change
{
    private Type nodeType;
    private Guid id;
    
    [GenerateMakeChangeAction]
    public CreateNode_Change(Type nodeType, Guid id)
    {
        this.id = id;
        this.nodeType = nodeType;
    }
    
    public override bool InitializeAndValidate(Document target)
    {
        return nodeType.IsSubclassOf(typeof(Node));
    }

    public override OneOf<None, IChangeInfo, List<IChangeInfo>> Apply(Document target, bool firstApply, out bool ignoreInUndo)
    {
        if(id == Guid.Empty)
            id = Guid.NewGuid();
        
        Node node = (Node)Activator.CreateInstance(nodeType);
        node.Position = new VecD(100, 100);
        node.Id = id;
        target.NodeGraph.AddNode(node);
        ignoreInUndo = false;
        
        var inputInfos = CreatePropertyInfos(node.InputProperties, true);
        var outputInfos = CreatePropertyInfos(node.OutputProperties, false);
        
        return new CreateNode_ChangeInfo(nodeType.Name, node.Position, id, inputInfos, outputInfos);
    }

    public override OneOf<None, IChangeInfo, List<IChangeInfo>> Revert(Document target)
    {
        Node node = target.FindNodeOrThrow<Node>(id);
        target.NodeGraph.RemoveNode(node);
        
        return new DeleteNode_ChangeInfo(id);
    }
    
    private ImmutableArray<NodePropertyInfo> CreatePropertyInfos(IEnumerable<INodeProperty> properties, bool isInput)
    {
        return properties.Select(p => new NodePropertyInfo(p.Name, p.ValueType, isInput, id)).ToImmutableArray();
    }
}

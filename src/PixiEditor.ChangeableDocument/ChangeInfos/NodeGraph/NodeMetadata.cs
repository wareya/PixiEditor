﻿using System.Reflection;
using PixiEditor.ChangeableDocument.Changeables.Graph;
using PixiEditor.ChangeableDocument.Changeables.Graph.Interfaces;

namespace PixiEditor.ChangeableDocument.ChangeInfos.NodeGraph;

public class NodeMetadata
{
    public bool IsPairNode { get; private set; }
    public bool IsPairNodeStart { get; private set; }
    public bool IsPairNodeEnd => IsPairNode && !IsPairNodeStart;

    public Guid? PairNodeGuid { get; set; }
    public string? ZoneUniqueName { get; private set; }
    
    public string? Category { get; private set; }

    public void AddAttributes(IReadOnlyNode node)
    {
        Type type = node.GetType();
        
        AddNodeInfoAttribute(type);
        AddPairAttributes(type);
    }

    private void AddNodeInfoAttribute(Type type)
    {
        var attribute = type.GetCustomAttribute<NodeInfoAttribute>();

        if (attribute == null)
            return;

        Category = attribute.Category;
    }

    private void AddPairAttributes(Type type)
    {
        var attribute = type.GetCustomAttribute<PairNodeAttribute>();

        if (attribute == null)
            return;

        ZoneUniqueName = attribute.ZoneUniqueName;
        IsPairNode = true;
        IsPairNodeStart = attribute.IsStartingType;
    }
}

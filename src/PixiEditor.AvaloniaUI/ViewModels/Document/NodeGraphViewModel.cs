﻿using System.Collections.ObjectModel;
using PixiEditor.AvaloniaUI.Models.Handlers;
using PixiEditor.AvaloniaUI.ViewModels.Nodes;

namespace PixiEditor.AvaloniaUI.ViewModels.Document;

internal class NodeGraphViewModel : ViewModelBase, INodeGraphHandler
{
    public DocumentViewModel DocumentViewModel { get; }
    public ObservableCollection<INodeHandler> AllNodes { get; } = new();
    public ObservableCollection<NodeConnectionViewModel> Connections { get; } = new();
    public INodeHandler? OutputNode { get; private set; }

    public NodeGraphViewModel(DocumentViewModel documentViewModel)
    {
        DocumentViewModel = documentViewModel;
    }
    
    public void AddNode(INodeHandler node)
    {
        if(OutputNode == null)
        {
            OutputNode = node; // TODO: this is not really correct yet, a way to check what node type is added is needed
        }
        
        AllNodes.Add(node);
    }
    
    public void RemoveNode(Guid nodeId)
    {
        var node = AllNodes.FirstOrDefault(x => x.Id == nodeId);
        if (node != null)
        {
            AllNodes.Remove(node);
        }
    }

    public bool TryTraverse(Func<INodeHandler, bool> func)
    {
        if (OutputNode == null) return false;

        var queue = CalculateExecutionQueue(OutputNode);

        while (queue.Count > 0)
        {
            var node = queue.Dequeue();
            func(node);
        }

        return true;
    }
    
    private Queue<INodeHandler> CalculateExecutionQueue(INodeHandler outputNode)
       {
           // backwards breadth-first search
           var visited = new HashSet<INodeHandler>();
           var queueNodes = new Queue<INodeHandler>();
           List<INodeHandler> finalQueue = new();
           queueNodes.Enqueue(outputNode);
   
           while (queueNodes.Count > 0)
           {
               var node = queueNodes.Dequeue();
               if (!visited.Add(node))
               {
                   continue;
               }
   
               finalQueue.Add(node);
   
               foreach (var input in node.Inputs)
               {
                   if (input.ConnectedOutput == null)
                   {
                       continue;
                   }
   
                   queueNodes.Enqueue(input.ConnectedOutput.Node);
               }
           }
   
           finalQueue.Reverse();
           return new Queue<INodeHandler>(finalQueue);
       } 
}

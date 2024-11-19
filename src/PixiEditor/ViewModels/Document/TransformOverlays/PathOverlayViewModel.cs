﻿using CommunityToolkit.Mvvm.ComponentModel;
using Drawie.Backend.Core.Vector;
using PixiEditor.Models.DocumentModels;
using PixiEditor.Models.Handlers;

namespace PixiEditor.ViewModels.Document.TransformOverlays;

internal class PathOverlayViewModel : ObservableObject, IPathOverlayHandler
{
    private DocumentViewModel documentViewModel;
    private DocumentInternalParts internals;
    
    private VectorPath path;
    public VectorPath Path
    {
        get => path;
        set => SetProperty(ref path, value);
    }
    
    public PathOverlayViewModel(DocumentViewModel documentViewModel, DocumentInternalParts internals)
    {
        this.documentViewModel = documentViewModel;
        this.internals = internals;
    }


    public void Show(VectorPath path)
    {
        Path = path;        
    }
}

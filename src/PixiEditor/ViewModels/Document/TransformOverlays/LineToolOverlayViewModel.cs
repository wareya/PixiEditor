﻿using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PixiEditor.DrawingApi.Core.Numerics;
using PixiEditor.Models.Handlers;
using PixiEditor.Numerics;

namespace PixiEditor.ViewModels.Document.TransformOverlays;

internal class LineToolOverlayViewModel : ObservableObject, ILineOverlayHandler
{
    public event EventHandler<(VecD, VecD)>? LineMoved;

    private TransformOverlayUndoStack<(VecD, VecD)>? undoStack = null;

    private VecD lineStart;
    private Matrix3X3 transformationMatrix;

    public VecD LineStart
    {
        get => lineStart;
        set
        {
            if (SetProperty(ref lineStart, value))
                LineMoved?.Invoke(this, (lineStart, lineEnd));
        }
    }

    private VecD lineEnd;

    public VecD LineEnd
    {
        get => lineEnd;
        set
        {
            if (SetProperty(ref lineEnd, value))
                LineMoved?.Invoke(this, (lineStart, lineEnd));
        }
    }

    public Matrix3X3 TransformationMatrix
    {
        get => transformationMatrix;
        set => SetProperty(ref transformationMatrix, value);
    }
    
    private bool isEnabled;

    public bool IsEnabled
    {
        get => isEnabled;
        set => SetProperty(ref isEnabled, value);
    }

    private ICommand? actionCompletedCommand = null;

    public ICommand? ActionCompletedCommand
    {
        get => actionCompletedCommand;
        set => SetProperty(ref actionCompletedCommand, value);
    }

    private bool showApplyButton;

    public bool ShowApplyButton
    {
        get => showApplyButton;
        set => SetProperty(ref showApplyButton, value);
    }

    public LineToolOverlayViewModel()
    {
        ActionCompletedCommand =
            new RelayCommand(() => undoStack?.AddState((LineStart, LineEnd), TransformOverlayStateType.Move));
    }

    public void Show(VecD lineStart, VecD endPos, Matrix3X3 transformationMatrix, bool showApplyButton)
    {
        if (undoStack is not null)
            return;
        undoStack = new();
        
        undoStack.AddState((lineStart, endPos), TransformOverlayStateType.Initial);

        LineStart = lineStart;
        LineEnd = endPos; 
        TransformationMatrix = transformationMatrix;
        IsEnabled = true;
        ShowApplyButton = showApplyButton;
    }

    public void Hide()
    {
        if (undoStack is null)
            return;
        undoStack = null;
        IsEnabled = false;
        ShowApplyButton = false;
    }

    public bool Nudge(VecD distance)
    {
        if (undoStack is null)
            return false;
        LineStart = LineStart + distance;
        LineEnd = LineEnd + distance;
        undoStack.AddState((lineStart, lineEnd), TransformOverlayStateType.Nudge);
        return true;
    }

    public bool Undo()
    {
        if (undoStack is null)
            return false;

        var newState = undoStack.Undo();
        if (newState is null)
            return false;
        LineStart = newState.Value.Item1;
        LineEnd = newState.Value.Item2;
        return true;
    }

    public bool Redo()
    {
        if (undoStack is null)
            return false;

        var newState = undoStack.Redo();
        if (newState is null)
            return false;
        LineStart = newState.Value.Item1;
        LineEnd = newState.Value.Item2;
        return true;
    }
}

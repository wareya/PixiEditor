﻿using Avalonia.Input;
using PixiEditor.AvaloniaUI.Models.Commands.Attributes.Commands;
using PixiEditor.AvaloniaUI.Models.Commands.Attributes.Evaluators;
using PixiEditor.ChangeableDocument.Enums;
using PixiEditor.DrawingApi.Core.Numerics;

namespace PixiEditor.AvaloniaUI.ViewModels.SubViewModels;

[Command.Group("PixiEditor.Selection", "SELECTION")]
internal class SelectionViewModel : SubViewModel<ViewModelMain>
{
    public SelectionViewModel(ViewModelMain owner)
        : base(owner)
    {
    }

    [Command.Basic("PixiEditor.Selection.SelectAll", "SELECT_ALL", "SELECT_ALL_DESCRIPTIVE", CanExecute = "PixiEditor.HasDocument", Key = Key.A, Modifiers = KeyModifiers.Control,
        MenuItemPath = "SELECT/SELECT_ALL", MenuItemOrder = 8)]
    public void SelectAll()
    {
        var doc = Owner.DocumentManagerSubViewModel.ActiveDocument;
        if (doc is null)
            return;
        doc.Operations.SelectAll();
    }

    [Command.Basic("PixiEditor.Selection.Clear", "CLEAR_SELECTION", "CLEAR_SELECTION", CanExecute = "PixiEditor.Selection.IsNotEmpty", Key = Key.D, Modifiers = KeyModifiers.Control,
        MenuItemPath = "SELECT/DESELECT", MenuItemOrder = 9)]
    public void ClearSelection()
    {
        var doc = Owner.DocumentManagerSubViewModel.ActiveDocument;
        if (doc is null)
            return;
        doc.Operations.ClearSelection();
    }

    [Command.Basic("PixiEditor.Selection.InvertSelection", "INVERT_SELECTION", "INVERT_SELECTION_DESCRIPTIVE", CanExecute = "PixiEditor.Selection.IsNotEmpty", Key = Key.I, Modifiers = KeyModifiers.Control,
        MenuItemPath = "SELECT/INVERT", MenuItemOrder = 10)]
    public void InvertSelection()
    {
        Owner.DocumentManagerSubViewModel.ActiveDocument?.Operations.InvertSelection();
    }

    [Evaluator.CanExecute("PixiEditor.Selection.IsNotEmpty")]
    public bool SelectionIsNotEmpty()
    {
        return !Owner.DocumentManagerSubViewModel.ActiveDocument?.SelectionPathBindable?.IsEmpty ?? false;
    }

    [Evaluator.CanExecute("PixiEditor.Selection.IsNotEmptyAndHasMask")]
    public bool SelectionIsNotEmptyAndHasMask()
    {
        return SelectionIsNotEmpty() && (Owner.DocumentManagerSubViewModel.ActiveDocument?.SelectedStructureMember?.HasMaskBindable ?? false);
    }

    [Command.Basic("PixiEditor.Selection.TransformArea", "TRANSFORM_SELECTED_AREA", "TRANSFORM_SELECTED_AREA", CanExecute = "PixiEditor.Selection.IsNotEmpty", Key = Key.T, Modifiers = KeyModifiers.Control)]
    public void TransformSelectedArea()
    {
        Owner.DocumentManagerSubViewModel.ActiveDocument?.Operations.TransformSelectedArea(false);
    }

    [Command.Basic("PixiEditor.Selection.NudgeSelectedObjectLeft", "NUDGE_SELECTED_LEFT", "NUDGE_SELECTED_LEFT", Key = Key.Left, Parameter = new int[] { -1, 0 }, IconPath = "E76B", IconEvaluator = "PixiEditor.FontIcon", CanExecute = "PixiEditor.Selection.CanNudgeSelectedObject")]
    [Command.Basic("PixiEditor.Selection.NudgeSelectedObjectRight", "NUDGE_SELECTED_RIGHT", "NUDGE_SELECTED_RIGHT", Key = Key.Right, Parameter = new int[] { 1, 0 }, IconPath = "E76C", IconEvaluator = "PixiEditor.FontIcon", CanExecute = "PixiEditor.Selection.CanNudgeSelectedObject")]
    [Command.Basic("PixiEditor.Selection.NudgeSelectedObjectUp", "NUDGE_SELECTED_UP", "NUDGE_SELECTED_UP", Key = Key.Up, Parameter = new int[] { 0, -1 }, IconPath = "E70E", IconEvaluator = "PixiEditor.FontIcon", CanExecute = "PixiEditor.Selection.CanNudgeSelectedObject")]
    [Command.Basic("PixiEditor.Selection.NudgeSelectedObjectDown", "NUDGE_SELECTED_DOWN", "NUDGE_SELECTED_DOWN", Key = Key.Down, Parameter = new int[] { 0, 1 }, IconPath = "E70D", IconEvaluator = "PixiEditor.FontIcon", CanExecute = "PixiEditor.Selection.CanNudgeSelectedObject")]
    public void NudgeSelectedObject(int[] dist)
    {
        VecI distance = new(dist[0], dist[1]);
        Owner.DocumentManagerSubViewModel.ActiveDocument?.Operations.NudgeSelectedObject(distance);
    }

    [Command.Basic("PixiEditor.Selection.NewToMask", SelectionMode.New, "MASK_FROM_SELECTION", "MASK_FROM_SELECTION_DESCRIPTIVE", CanExecute = "PixiEditor.Selection.IsNotEmpty",
        MenuItemPath = "SELECT/SELECTION_TO_MASK/TO_NEW_MASK", MenuItemOrder = 12)]
    [Command.Basic("PixiEditor.Selection.AddToMask", SelectionMode.Add, "ADD_SELECTION_TO_MASK", "ADD_SELECTION_TO_MASK", CanExecute = "PixiEditor.Selection.IsNotEmpty",
        MenuItemPath = "SELECT/SELECTION_TO_MASK/ADD_TO_MASK", MenuItemOrder = 13)]
    [Command.Basic("PixiEditor.Selection.SubtractFromMask", SelectionMode.Subtract, "SUBTRACT_SELECTION_FROM_MASK", "SUBTRACT_SELECTION_FROM_MASK", CanExecute = "PixiEditor.Selection.IsNotEmptyAndHasMask",
        MenuItemPath = "SELECT/SELECTION_TO_MASK/SUBTRACT_FROM_MASK", MenuItemOrder = 14)]
    [Command.Basic("PixiEditor.Selection.IntersectSelectionMask", SelectionMode.Intersect, "INTERSECT_SELECTION_MASK", "INTERSECT_SELECTION_MASK", CanExecute = "PixiEditor.Selection.IsNotEmptyAndHasMask",
        MenuItemPath = "SELECT/SELECTION_TO_MASK/INTERSECT_WITH_MASK", MenuItemOrder = 15)]
    [Command.Filter("PixiEditor.Selection.ToMaskMenu", "SELECTION_TO_MASK", "SELECTION_TO_MASK", Key = Key.M, Modifiers = KeyModifiers.Control)]
    public void SelectionToMask(SelectionMode mode)
    {
        Owner.DocumentManagerSubViewModel.ActiveDocument?.Operations.SelectionToMask(mode);
    }

    [Command.Basic("PixiEditor.Selection.CropToSelection", "CROP_TO_SELECTION", "CROP_TO_SELECTION_DESCRIPTIVE", CanExecute = "PixiEditor.Selection.IsNotEmpty",
        MenuItemPath = "SELECT/CROP_TO_SELECTION", MenuItemOrder = 11)]
    public void CropToSelection()
    {
        var document = Owner.DocumentManagerSubViewModel.ActiveDocument;
        
        document!.Operations.CropToSelection();
    }

    [Evaluator.CanExecute("PixiEditor.Selection.CanNudgeSelectedObject")]
    public bool CanNudgeSelectedObject(int[] dist) => Owner.DocumentManagerSubViewModel.ActiveDocument?.UpdateableChangeActive == true;
}

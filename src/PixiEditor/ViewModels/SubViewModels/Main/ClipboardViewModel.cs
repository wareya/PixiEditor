﻿using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using PixiEditor.Models.Commands.Attributes.Commands;
using PixiEditor.Models.Controllers;

namespace PixiEditor.ViewModels.SubViewModels.Main;
#nullable enable
[Command.Group("PixiEditor.Clipboard", "Clipboard")]
internal class ClipboardViewModel : SubViewModel<ViewModelMain>
{
    public ClipboardViewModel(ViewModelMain owner)
        : base(owner)
    {
    }

    [Command.Basic("PixiEditor.Clipboard.Cut", "Cut", "Cut selected area/layer", CanExecute = "PixiEditor.Selection.IsNotEmpty", Key = Key.X, Modifiers = ModifierKeys.Control)]
    public void Cut()
    {
        var doc = Owner.DocumentManagerSubViewModel.ActiveDocument;
        if (doc is null)
            return;
        Copy();
        doc.Operations.DeleteSelectedPixels(true);
    }

    [Command.Basic("PixiEditor.Clipboard.Paste", "Paste", "Paste from clipboard", CanExecute = "PixiEditor.Clipboard.CanPaste", Key = Key.V, Modifiers = ModifierKeys.Control)]
    public void Paste()
    {
        if (Owner.DocumentManagerSubViewModel.ActiveDocument is null) 
            return;
        ClipboardController.TryPasteFromClipboard(Owner.DocumentManagerSubViewModel.ActiveDocument);
    }

    [Command.Basic("PixiEditor.Clipboard.PasteColor", "Paste color", "Paste color from clipboard", CanExecute = "PixiEditor.Clipboard.CanPasteColor", IconEvaluator = "PixiEditor.Clipboard.PasteColorIcon")]
    public void PasteColor()
    {
        Owner.ColorsSubViewModel.PrimaryColor = SKColor.Parse(Clipboard.GetText().Trim());
    }

    [Command.Basic("PixiEditor.Clipboard.Copy", "Copy", "Copy to clipboard", CanExecute = "PixiEditor.Selection.IsNotEmpty", Key = Key.C, Modifiers = ModifierKeys.Control)]
    public void Copy()
    {
        var doc = Owner.DocumentManagerSubViewModel.ActiveDocument;
        if (doc is null)
            return;
        ClipboardController.CopyToClipboard(doc);
    }

    [Evaluator.CanExecute("PixiEditor.Clipboard.CanPaste")]
    public bool CanPaste()
    {
        return Owner.DocumentIsNotNull(null) && ClipboardController.IsImageInClipboard();
    }

    [Evaluator.CanExecute("PixiEditor.Clipboard.CanPasteColor")]
    public static bool CanPasteColor() => Regex.IsMatch(Clipboard.GetText().Trim(), "^#?([a-fA-F0-9]{8}|[a-fA-F0-9]{6}|[a-fA-F0-9]{3})$");

    [Evaluator.Icon("PixiEditor.Clipboard.PasteColorIcon")]
    public static ImageSource GetPasteColorIcon()
    {
        Color color;

        if (CanPasteColor())
        {
            color = SKColor.Parse(Clipboard.GetText().Trim()).ToOpaqueColor();
        }
        else
        {
            color = Colors.Transparent;
        }

        return ColorSearchResult.GetIcon(color.ToOpaqueSKColor());
    }
}

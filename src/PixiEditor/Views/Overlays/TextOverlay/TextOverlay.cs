﻿using Avalonia;
using Avalonia.Input;
using Avalonia.Threading;
using Drawie.Backend.Core.Numerics;
using Drawie.Backend.Core.Text;
using Drawie.Numerics;
using PixiEditor.Helpers.UI;
using PixiEditor.Models.Controllers;
using PixiEditor.Models.Input;
using PixiEditor.OperatingSystem;
using Canvas = Drawie.Backend.Core.Surfaces.Canvas;

namespace PixiEditor.Views.Overlays.TextOverlay;

internal class TextOverlay : Overlay
{
    public static readonly StyledProperty<string> TextProperty = AvaloniaProperty.Register<TextOverlay, string>(
        nameof(Text));

    public string Text
    {
        get => GetValue(TextProperty);
        set => SetValue(TextProperty, value);
    }

    public static readonly StyledProperty<VecD> PositionProperty = AvaloniaProperty.Register<TextOverlay, VecD>(
        nameof(Position));

    public VecD Position
    {
        get => GetValue(PositionProperty);
        set => SetValue(PositionProperty, value);
    }

    public static readonly StyledProperty<Font> FontProperty = AvaloniaProperty.Register<TextOverlay, Font>(
        nameof(Font));

    public Font Font
    {
        get => GetValue(FontProperty);
        set => SetValue(FontProperty, value);
    }

    public static readonly StyledProperty<int> CursorPositionProperty = AvaloniaProperty.Register<TextOverlay, int>(
        nameof(CursorPosition), coerce: ClampValue);

    public int CursorPosition
    {
        get => GetValue(CursorPositionProperty);
        set => SetValue(CursorPositionProperty, value);
    }

    public static readonly StyledProperty<int> SelectionLengthProperty = AvaloniaProperty.Register<TextOverlay, int>(
        nameof(SelectionLength));

    public int SelectionLength
    {
        get => GetValue(SelectionLengthProperty);
        set => SetValue(SelectionLengthProperty, value);
    }

    public static readonly StyledProperty<Matrix3X3> MatrixProperty = AvaloniaProperty.Register<TextOverlay, Matrix3X3>(
        nameof(Matrix), Matrix3X3.Identity);

    public Matrix3X3 Matrix
    {
        get => GetValue(MatrixProperty);
        set => SetValue(MatrixProperty, value);
    }

    public static readonly StyledProperty<ExecutionTrigger<string>> RequestEditTextProperty =
        AvaloniaProperty.Register<TextOverlay, ExecutionTrigger<string>>(
            nameof(RequestEditText));

    public ExecutionTrigger<string> RequestEditText
    {
        get => GetValue(RequestEditTextProperty);
        set => SetValue(RequestEditTextProperty, value);
    }

    public static readonly StyledProperty<bool> IsEditingProperty = AvaloniaProperty.Register<TextOverlay, bool>(
        nameof(IsEditing));

    public bool IsEditing
    {
        get => GetValue(IsEditingProperty);
        set => SetValue(IsEditingProperty, value);
    }

    private Dictionary<KeyCombination, Action> shortcuts;

    private Blinker blinker = new Blinker();
    private VecF[] glyphPositions;
    private float[] glyphWidths;

    static TextOverlay()
    {
        IsVisibleProperty.Changed.Subscribe(IsVisibleChanged);
        RequestEditTextProperty.Changed.Subscribe(RequestEditTextChanged);
        IsEditingProperty.Changed.Subscribe(IsEditingChanged);
        TextProperty.Changed.Subscribe(TextChanged);
        FontProperty.Changed.Subscribe(FontChanged);

        AffectsOverlayRender(FontProperty, TextProperty, CursorPositionProperty, SelectionLengthProperty);
    }

    public TextOverlay()
    {
        shortcuts = new Dictionary<KeyCombination, Action>
        {
            { new KeyCombination(Key.V, KeyModifiers.Control), PasteText },
            { new KeyCombination(Key.Delete, KeyModifiers.None), () => DeleteChar(0) },
            { new KeyCombination(Key.Back, KeyModifiers.None), () => DeleteChar(-1) },
            { new KeyCombination(Key.Left, KeyModifiers.None), () => MoveCursorBy(-1) },
            { new KeyCombination(Key.Right, KeyModifiers.None), () => MoveCursorBy(1) }
        };
    }


    public override void RenderOverlay(Canvas context, RectD canvasBounds)
    {
        if (!IsEditing) return;

        blinker.BlinkerPosition = CursorPosition;
        blinker.FontSize = Font.Size;
        blinker.GlyphPositions = glyphPositions;
        blinker.GlyphWidths = glyphWidths;
        blinker.Offset = Position;
        
        int saved = context.Save();
        
        context.SetMatrix(context.TotalMatrix.Concat(Matrix));

        blinker.BlinkerWidth = 3f / (float)ZoomScale;
        blinker.Render(context);
        
        context.RestoreToCount(saved);
    }

    protected override void OnKeyPressed(Key key, KeyModifiers keyModifiers, string? keySymbol)
    {
        if (!IsEditing) return;
        if (IsShortcut(key, keyModifiers))
        {
            ExecuteShortcut(key, keyModifiers);
            return;
        }

        InsertChar(key, keySymbol);
    }

    private void InsertChar(Key key, string symbol)
    {
        if (key == Key.Enter)
        {
            InsertTextAtCursor("\n");
        }
        else if (key == Key.Space)
        {
            InsertTextAtCursor(" ");
        }
        else
        {
            if (symbol is { Length: 1 })
            {
                char symbolChar = symbol[0];
                if (char.IsControl(symbolChar)) return;
                InsertTextAtCursor(symbol);
            }
        }
    }
    
    private void InsertTextAtCursor(string toAdd)
    {
        Text = Text.Insert(CursorPosition, toAdd);
        CursorPosition += toAdd.Length;
    }

    private bool IsShortcut(Key key, KeyModifiers keyModifiers)
    {
        return shortcuts.ContainsKey(new KeyCombination(key, keyModifiers));
    }

    private void ExecuteShortcut(Key key, KeyModifiers keyModifiers)
    {
        KeyCombination shortcut = new(key, keyModifiers);
        if (shortcuts.ContainsKey(shortcut))
        {
            shortcuts[shortcut].Invoke();
        }
    }

    private void PasteText()
    {
        ClipboardController.GetTextFromClipboard().ContinueWith(
            t =>
            {
                Dispatcher.UIThread.Invoke(() => Text += t.Result);
            }, TaskContinuationOptions.OnlyOnRanToCompletion);
    }

    private void DeleteChar(int direction)
    {
        if (Text.Length > 0 && CursorPosition + direction >= 0 && CursorPosition + direction < Text.Length)
        {
            Text = Text.Remove(CursorPosition + direction, 1);
            CursorPosition = CursorPosition + direction;
        }
    }

    private void MoveCursorBy(int direction)
    {
        CursorPosition += direction;
    }

    private void RequestEditTextTriggered(object? sender, string e)
    {
        IsEditing = true;
    }

    private void UpdateGlyphs()
    {
        if (Font == null) return;

        RichText richText = new(Text);
        glyphPositions = richText.GetGlyphPositions(Font);
        glyphWidths = richText.GetGlyphWidths(Font);
    }

    private static void IsVisibleChanged(AvaloniaPropertyChangedEventArgs<bool> args)
    {
        TextOverlay sender = args.Sender as TextOverlay;
        if (sender == null) return;

        if (!args.NewValue.Value)
        {
            sender.IsEditing = false;
        }
    }

    private static void RequestEditTextChanged(AvaloniaPropertyChangedEventArgs<ExecutionTrigger<string>> args)
    {
        var sender = args.Sender as TextOverlay;
        if (args.OldValue.Value != null)
        {
            args.OldValue.Value.Triggered -= sender.RequestEditTextTriggered;
        }

        if (args.NewValue.Value != null)
        {
            args.NewValue.Value.Triggered += sender.RequestEditTextTriggered;
        }
    }

    private static void IsEditingChanged(AvaloniaPropertyChangedEventArgs<bool> args)
    {
        if (args.NewValue.Value)
        {
            ShortcutController.BlockShortcutExecution(nameof(TextOverlay));
        }
        else
        {
            ShortcutController.UnblockShortcutExecution(nameof(TextOverlay));
        }
    }

    private static void TextChanged(AvaloniaPropertyChangedEventArgs<string> args)
    {
        TextOverlay sender = args.Sender as TextOverlay;
        sender.UpdateGlyphs();
    }

    private static void FontChanged(AvaloniaPropertyChangedEventArgs<Font> args)
    {
        TextOverlay sender = args.Sender as TextOverlay;
        sender.UpdateGlyphs();
    }

    private static int ClampValue(AvaloniaObject sender, int newPos)
    {
        TextOverlay textOverlay = sender as TextOverlay;
        if (textOverlay == null) return newPos;

        return Math.Clamp(newPos, 0, textOverlay.Text.Length);
    }
}

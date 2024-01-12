﻿using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using PixiEditor.ChangeableDocument.Enums;

namespace PixiEditor.AvaloniaUI.Views.Input;

/// <summary>
///     Interaction logic for AnchorPointPicker.xaml
/// </summary>
internal partial class AnchorPointPicker : UserControl
{
    public static readonly StyledProperty<ResizeAnchor> AnchorPointProperty =
        AvaloniaProperty.Register<AnchorPointPicker, ResizeAnchor>(nameof(AnchorPoint), ResizeAnchor.TopLeft);

    public ResizeAnchor AnchorPoint
    {
        get => GetValue(AnchorPointProperty);
        set => SetValue(AnchorPointProperty, value);
    }

    static AnchorPointPicker()
    {
        AnchorPointProperty.Changed.Subscribe(OnAnchorPointChanged);
    }

    private ToggleButton _selectedToggleButton;

    public AnchorPointPicker()
    {
        InitializeComponent();
        _selectedToggleButton = topLeft;
    }

    private static void OnAnchorPointChanged(AvaloniaPropertyChangedEventArgs e)
    {
        AnchorPointPicker picker = (AnchorPointPicker)e.Sender;
        if (e.NewValue is ResizeAnchor anchor)
        {
            switch (anchor)
            {
                case ResizeAnchor.TopLeft:
                    picker.topLeft.IsChecked = true;
                    break;
                case ResizeAnchor.Top:
                    picker.topCenter.IsChecked = true;
                    break;
                case ResizeAnchor.TopRight:
                    picker.topRight.IsChecked = true;
                    break;
                case ResizeAnchor.Left:
                    picker.middleLeft.IsChecked = true;
                    break;
                case ResizeAnchor.Center:
                    picker.middleCenter.IsChecked = true;
                    break;
                case ResizeAnchor.Right:
                    picker.middleRight.IsChecked = true;
                    break;
                case ResizeAnchor.BottomLeft:
                    picker.bottomLeft.IsChecked = true;
                    break;
                case ResizeAnchor.Bottom:
                    picker.bottomCenter.IsChecked = true;
                    break;
                case ResizeAnchor.BottomRight:
                    picker.bottomRight.IsChecked = true;
                    break;
            }
        }
    }

    private void ToggleButton_Checked(object sender, RoutedEventArgs e)
    {
        ToggleButton btn = (ToggleButton)sender;
        int row = Grid.GetRow(btn);
        int column = Grid.GetColumn(btn);
        AnchorPoint = (column, row) switch
        {
            (0, 0) => ResizeAnchor.TopLeft,
            (1, 0) => ResizeAnchor.Top,
            (2, 0) => ResizeAnchor.TopRight,
            (0, 1) => ResizeAnchor.Left,
            (1, 1) => ResizeAnchor.Center,
            (2, 1) => ResizeAnchor.Right,
            (0, 2) => ResizeAnchor.BottomLeft,
            (1, 2) => ResizeAnchor.Bottom,
            (2, 2) => ResizeAnchor.BottomRight,
            _ => throw new NotImplementedException()
        };
        if (_selectedToggleButton != null) _selectedToggleButton.IsChecked = false;
        _selectedToggleButton = btn;
    }

    private void ToggleButton_Click(object sender, PointerPressedEventArgs e)
    {
        if ((sender as ToggleButton).IsChecked.Value)
            e.Handled = true;
    }
}

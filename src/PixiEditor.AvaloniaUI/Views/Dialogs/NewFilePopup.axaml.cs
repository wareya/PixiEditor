﻿using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using CommunityToolkit.Mvvm.Input;

namespace PixiEditor.AvaloniaUI.Views.Dialogs;

/// <summary>
///     Interaction logic for NewFilePopup.xaml.
/// </summary>
internal partial class NewFilePopup : Window
{
    public static readonly StyledProperty<int> FileHeightProperty =
        AvaloniaProperty.Register<NewFilePopup, int>(nameof(FileHeight));

    public static readonly StyledProperty<int> FileWidthProperty =
        AvaloniaProperty.Register<NewFilePopup, int>(nameof(FileWidth));

    public NewFilePopup()
    {
        InitializeComponent();
        DataContext = this;
        Loaded += OnDialogShown;
    }

    private void OnDialogShown(object sender, RoutedEventArgs e)
    {
        MinWidth = Width;
        sizePicker.FocusWidthPicker();
    }

    public int FileHeight
    {
        get => (int)GetValue(FileHeightProperty);
        set => SetValue(FileHeightProperty, value);
    }

    public int FileWidth
    {
        get => (int)GetValue(FileWidthProperty);
        set => SetValue(FileWidthProperty, value);
    }


    [RelayCommand]
    private void SetResultAndClose(bool property)
    {
        Close(true);
    }

    [RelayCommand]
    private void Cancel()
    {
        Close(false);
    }
}

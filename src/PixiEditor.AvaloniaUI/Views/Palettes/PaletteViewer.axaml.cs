﻿using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Platform.Storage;
using PixiEditor.AvaloniaUI.Helpers;
using PixiEditor.AvaloniaUI.Helpers.Extensions;
using PixiEditor.AvaloniaUI.Models.AppExtensions.Services;
using PixiEditor.AvaloniaUI.Models.Dialogs;
using PixiEditor.AvaloniaUI.Models.Structures;
using PixiEditor.AvaloniaUI.ViewModels;
using PixiEditor.AvaloniaUI.Views.Dialogs;
using PixiEditor.AvaloniaUI.Views.Windows;
using PixiEditor.Extensions.Palettes;
using PixiEditor.Models.IO;

namespace PixiEditor.AvaloniaUI.Views.Palettes;

/// <summary>
/// Interaction logic for Palette.xaml
/// </summary>
internal partial class PaletteViewer : UserControl
{
    public static readonly StyledProperty<ObservableRangeCollection<PaletteColor>> SwatchesProperty =
        AvaloniaProperty.Register<PaletteViewer, ObservableRangeCollection<PaletteColor>>(
            nameof(Swatches),
            default(ObservableRangeCollection<PaletteColor>));

    public ObservableRangeCollection<PaletteColor> Swatches
    {
        get => GetValue(SwatchesProperty);
        set => SetValue(SwatchesProperty, value);
    }

    public static readonly StyledProperty<ObservableRangeCollection<PaletteColor>> ColorsProperty =
        AvaloniaProperty.Register<PaletteViewer, ObservableRangeCollection<PaletteColor>>(
            nameof(Colors));

    public ObservableRangeCollection<PaletteColor> Colors
    {
        get => GetValue(ColorsProperty);
        set => SetValue(ColorsProperty, value);
    }

    public static readonly StyledProperty<Color> HintColorProperty =
        AvaloniaProperty.Register<PaletteViewer, Color>(
            nameof(HintColor),
            default(Color));

    public Color HintColor
    {
        get => GetValue(HintColorProperty);
        set => SetValue(HintColorProperty, value);
    }

    public static readonly StyledProperty<ICommand> ReplaceColorsCommandProperty =
        AvaloniaProperty.Register<PaletteViewer, ICommand>(
            nameof(ReplaceColorsCommand),
            default(ICommand));

    public ICommand ReplaceColorsCommand
    {
        get => GetValue(ReplaceColorsCommandProperty);
        set => SetValue(ReplaceColorsCommandProperty, value);
    }

    public static readonly StyledProperty<ICommand> SelectColorCommandProperty =
        AvaloniaProperty.Register<PaletteViewer, ICommand>(
            nameof(SelectColorCommand));

    public ICommand SelectColorCommand
    {
        get => GetValue(SelectColorCommandProperty);
        set => SetValue(SelectColorCommandProperty, value);
    }

    public static readonly StyledProperty<ICommand> ImportPaletteCommandProperty =
        AvaloniaProperty.Register<PaletteViewer, ICommand>(
            nameof(ImportPaletteCommand));

    public ICommand ImportPaletteCommand
    {
        get => GetValue(ImportPaletteCommandProperty);
        set => SetValue(ImportPaletteCommandProperty, value);
    }

    public static readonly StyledProperty<PaletteProvider> PaletteProviderProperty =
        AvaloniaProperty.Register<PaletteViewer, PaletteProvider>(
            nameof(PaletteProvider),
            default(PaletteProvider));

    public PaletteProvider PaletteProvider
    {
        get => GetValue(PaletteProviderProperty);
        set => SetValue(PaletteProviderProperty, value);
    }

    public PaletteViewer()
    {
        InitializeComponent();
    }

    private void RemoveColorMenuItem_OnClick(object sender, RoutedEventArgs e)
    {
        MenuItem menuItem = (MenuItem)sender;
        PaletteColor colorControl = (PaletteColor)menuItem.CommandParameter;
        if (Colors.Contains(colorControl))
        {
            Colors.Remove(colorControl);
        }
    }

    private async void ImportPalette_OnClick(object sender, RoutedEventArgs e)
    {
        await Application.Current.ForDesktopMainWindowAsync(async window =>
        {
            var file = await window.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions()
            {
                FileTypeFilter = PaletteHelpers.GetFilter(PaletteProvider.AvailableParsers, true),
            });

            if (file is null || file.Count == 0) return;

            await ImportPalette(file[0].Path.AbsolutePath);
        });
    }

    private async Task ImportPalette(string filePath)
    {
        var parser =
            PaletteProvider.AvailableParsers.FirstOrDefault(x =>
                x.SupportedFileExtensions.Contains(Path.GetExtension(filePath)));
        if (parser == null) return;
        var data = await parser.Parse(filePath);
        if (data.IsCorrupted || data.Colors.Length == 0) return;
        Colors.Clear();
        Colors.AddRange(data.Colors);
    }

    private async void SavePalette_OnClick(object sender, RoutedEventArgs e)
    {
        await Application.Current.ForDesktopMainWindowAsync(async window =>
        {
            var file = await window.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions()
            {
                FileTypeChoices = PaletteHelpers.GetFilter(
                    PaletteProvider.AvailableParsers.Where(x => x.CanSave).ToList(), false)
            });

            if (file is null) return;

            string fileName = file.Name;
            var foundParser =
                PaletteProvider.AvailableParsers.First(x =>
                    x.SupportedFileExtensions.Contains(Path.GetExtension(fileName)));
            if (Colors == null || Colors.Count == 0)
            {
                NoticeDialog.Show("NO_COLORS_TO_SAVE", "ERROR");
                return;
            }

            bool saved = await foundParser.Save(fileName, new PaletteFileData(Colors.ToArray()));
            if (!saved)
            {
                NoticeDialog.Show("COULD_NOT_SAVE_PALETTE", "ERROR");
            }
        });
    }

    private void Grid_PreviewDragEnter(object sender, DragEventArgs e)
    {
        if (IsSupportedFilePresent(e, out _))
        {
            dragDropGrid.IsVisible = true;
            ViewModelMain.Current.ActionDisplays[nameof(PaletteViewer)] = "IMPORT_PALETTE_FILE";
        }
        else if (ColorHelper.ParseAnyFormatList(e.Data, out var list))
        {
            e.DragEffects = DragDropEffects.Copy;
            ViewModelMain.Current.ActionDisplays[nameof(PaletteViewer)] =
                list.Count > 1 ? "IMPORT_MULTIPLE_PALETTE_COLORS" : "IMPORT_SINGLE_PALETTE_COLOR";
            e.Handled = true;
        }
    }

    private void Grid_PreviewDragLeave(object sender, DragEventArgs e)
    {
        dragDropGrid.IsVisible = false;
        ViewModelMain.Current.ActionDisplays[nameof(PaletteViewer)] = null;
    }

    private async void Grid_Drop(object sender, DragEventArgs e)
    {
        ViewModelMain.Current.ActionDisplays[nameof(PaletteViewer)] = null;

        if (!IsSupportedFilePresent(e, out string filePath))
        {
            if (!ColorHelper.ParseAnyFormatList(e.Data, out var colors))
            {
                return;
            }

            List<PaletteColor> paletteColors = colors.Select(x => new PaletteColor(x.R, x.G, x.B)).ToList();

            e.DragEffects = DragDropEffects.Copy;
            Colors.AddRange(paletteColors.Where(x => !Colors.Contains(new PaletteColor(x.R, x.G, x.B))).ToList());
            e.Handled = true;
            return;
        }

        e.Handled = true;
        await ImportPalette(filePath);
        dragDropGrid.IsVisible = false;
    }

    private bool IsSupportedFilePresent(DragEventArgs e, out string filePath)
    {
        if (e.Data.Contains(DataFormats.Files))
        {
            string[] files = (string[])e.Data.Get(DataFormats.Files);
            if (files is { Length: > 0 })
            {
                var fileName = files[0];
                var foundParser = PaletteProvider.AvailableParsers.FirstOrDefault(x =>
                    x.SupportedFileExtensions.Contains(Path.GetExtension(fileName)));
                if (foundParser != null)
                {
                    filePath = fileName;
                    return true;
                }
            }
        }

        filePath = null;
        return false;
    }

    private void PaletteColor_Drop(object sender, DragEventArgs e)
    {
        if (e.Data.Contains(PaletteColorControl.PaletteColorDaoFormat))
        {
            string data = (string)e.Data.Get(PaletteColorControl.PaletteColorDaoFormat);

            PaletteColor paletteColor = PaletteColor.Parse(data);
            if (Colors.Contains(paletteColor))
            {
                PaletteColorControl paletteColorControl = sender as PaletteColorControl;
                int currIndex = Colors.IndexOf(paletteColor);
                if (paletteColorControl != null)
                {
                    int newIndex = Colors.IndexOf(paletteColorControl.Color);
                    Colors.RemoveAt(currIndex);
                    Colors.Insert(newIndex, paletteColor);
                }
            }
        }
    }

    private async void BrowsePalettes_Click(object sender, RoutedEventArgs e)
    {
        var browser = PalettesBrowser.Open(PaletteProvider, ImportPaletteCommand, Colors);
        await browser.UpdatePaletteList();
    }

    private void ReplaceColor_OnClick(object sender, RoutedEventArgs e)
    {
        MenuItem menuItem = (MenuItem)sender;
        PaletteColor color = (PaletteColor)menuItem.CommandParameter;
        Replacer.ColorToReplace = color;
        Replacer.VisibilityCheckbox.IsChecked = false;
    }

    private void MenuItem_OnClick(object sender, RoutedEventArgs e)
    {
        MenuItem origin = (MenuItem)sender;
        if (SelectColorCommand.CanExecute(origin.CommandParameter))
        {
            SelectColorCommand.Execute(origin.CommandParameter);
        }
    }

    private async void DiscardPalette_OnClick(object sender, RoutedEventArgs e)
    {
        if (await ConfirmationDialog.Show("DISCARD_PALETTE_CONFIRMATION", "DISCARD_PALETTE") == ConfirmationType.Yes)
        {
            Colors.Clear();
        }
    }
}

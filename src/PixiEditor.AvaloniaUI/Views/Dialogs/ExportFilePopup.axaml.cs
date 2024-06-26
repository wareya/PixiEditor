﻿using System.ComponentModel;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using ChunkyImageLib;
using CommunityToolkit.Mvvm.Input;
using PixiEditor.AvaloniaUI.Helpers;
using PixiEditor.AvaloniaUI.Models.Files;
using PixiEditor.AvaloniaUI.Models.IO;
using PixiEditor.AvaloniaUI.ViewModels.Document;
using PixiEditor.Extensions.Common.Localization;
using PixiEditor.Numerics;
using Image = PixiEditor.DrawingApi.Core.Surface.ImageData.Image;

namespace PixiEditor.AvaloniaUI.Views.Dialogs;

internal partial class ExportFilePopup : PixiEditorPopup
{
    public static readonly StyledProperty<int> SaveHeightProperty =
        AvaloniaProperty.Register<ExportFilePopup, int>(nameof(SaveHeight), 32);

    public static readonly StyledProperty<int> SaveWidthProperty =
        AvaloniaProperty.Register<ExportFilePopup, int>(nameof(SaveWidth), 32);

    public static readonly StyledProperty<RelayCommand> SetBestPercentageCommandProperty =
        AvaloniaProperty.Register<ExportFilePopup, RelayCommand>(nameof(SetBestPercentageCommand));

    public static readonly StyledProperty<string?> SavePathProperty =
        AvaloniaProperty.Register<ExportFilePopup, string?>(nameof(SavePath), "");

    public static readonly StyledProperty<IoFileType> SaveFormatProperty =
        AvaloniaProperty.Register<ExportFilePopup, IoFileType>(nameof(SaveFormat), new PngFileType());

    public static readonly StyledProperty<AsyncRelayCommand> ExportCommandProperty =
        AvaloniaProperty.Register<ExportFilePopup, AsyncRelayCommand>(
            nameof(ExportCommand));

    public static readonly StyledProperty<string> SuggestedNameProperty =
        AvaloniaProperty.Register<ExportFilePopup, string>(
            nameof(SuggestedName));

    public static readonly StyledProperty<Surface> ExportPreviewProperty =
        AvaloniaProperty.Register<ExportFilePopup, Surface>(
            nameof(ExportPreview));

    public static readonly StyledProperty<int> SelectedExportIndexProperty =
        AvaloniaProperty.Register<ExportFilePopup, int>(
            nameof(SelectedExportIndex), 0);

    public int SelectedExportIndex
    {
        get => GetValue(SelectedExportIndexProperty);
        set => SetValue(SelectedExportIndexProperty, value);
    }

    public int SaveWidth
    {
        get => (int)GetValue(SaveWidthProperty);
        set => SetValue(SaveWidthProperty, value);
    }


    public int SaveHeight
    {
        get => (int)GetValue(SaveHeightProperty);
        set => SetValue(SaveHeightProperty, value);
    }

    public string? SavePath
    {
        get => (string)GetValue(SavePathProperty);
        set => SetValue(SavePathProperty, value);
    }

    public IoFileType SaveFormat
    {
        get => (IoFileType)GetValue(SaveFormatProperty);
        set => SetValue(SaveFormatProperty, value);
    }

    public Surface ExportPreview
    {
        get => GetValue(ExportPreviewProperty);
        set => SetValue(ExportPreviewProperty, value);
    }

    public string SuggestedName
    {
        get => GetValue(SuggestedNameProperty);
        set => SetValue(SuggestedNameProperty, value);
    }

    public AsyncRelayCommand ExportCommand
    {
        get => GetValue(ExportCommandProperty);
        set => SetValue(ExportCommandProperty, value);
    }

    public RelayCommand SetBestPercentageCommand
    {
        get => (RelayCommand)GetValue(SetBestPercentageCommandProperty);
        set => SetValue(SetBestPercentageCommandProperty, value);
    }

    public bool IsVideoExport => SelectedExportIndex == 1;
    public string SizeHint => new LocalizedString("EXPORT_SIZE_HINT", GetBestPercentage());
    public bool IsPreviewGenerating
    {
        get
        {
            return isPreviewGenerating;
        }
        private set
        {
            isPreviewGenerating = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsPreviewGenerating)));
        }
    }

    private DocumentViewModel document;
    private Image[] videoPreviewFrames = [];
    private DispatcherTimer videoPreviewTimer = new DispatcherTimer();
    private int activeFrame = 0;
    private CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();
    private bool isPreviewGenerating = false;
    
    public event PropertyChangedEventHandler? PropertyChanged;

    static ExportFilePopup()
    {
        SaveWidthProperty.Changed.Subscribe(RerenderPreview);
        SaveHeightProperty.Changed.Subscribe(RerenderPreview);
        SelectedExportIndexProperty.Changed.Subscribe(RerenderPreview);
    }

    public ExportFilePopup(int imageWidth, int imageHeight, DocumentViewModel document)
    {
        SaveWidth = imageWidth;
        SaveHeight = imageHeight;

        InitializeComponent();
        DataContext = this;
        Loaded += (_, _) => sizePicker.FocusWidthPicker();

        SaveWidth = imageWidth;
        SaveHeight = imageHeight;

        SetBestPercentageCommand = new RelayCommand(SetBestPercentage);
        ExportCommand = new AsyncRelayCommand(Export);
        this.document = document;
        videoPreviewTimer = new DispatcherTimer(DispatcherPriority.Normal)
        {
            Interval = TimeSpan.FromMilliseconds(1000f / document.AnimationDataViewModel.FrameRate)
        };
        videoPreviewTimer.Tick += OnVideoPreviewTimerOnTick;

        RenderPreview();
    }

    protected override void OnClosing(WindowClosingEventArgs e)
    {
        base.OnClosing(e);
        videoPreviewTimer.Stop();
        videoPreviewTimer.Tick -= OnVideoPreviewTimerOnTick;
        videoPreviewTimer = null;
        cancellationTokenSource.Dispose();

        if (ExportPreview != null)
        {
            ExportPreview.Dispose();
        }

        if (videoPreviewFrames != null)
        {
            foreach (var frame in videoPreviewFrames)
            {
                frame.Dispose();
            }
        }
    }

    private void OnVideoPreviewTimerOnTick(object? o, EventArgs eventArgs)
    {
        if (videoPreviewFrames.Length > 0)
        {
            ExportPreview.DrawingSurface.Canvas.Clear();
            ExportPreview.DrawingSurface.Canvas.DrawImage(videoPreviewFrames[activeFrame], 0, 0);
            activeFrame = (activeFrame + 1) % videoPreviewFrames.Length;
        }
        else
        {
            videoPreviewTimer.Stop();
        }
    }

    private void RenderPreview()
    {
        if (document == null)
        {
            return;
        }
        
        IsPreviewGenerating = true;

        videoPreviewTimer.Stop();
        if (IsVideoExport)
        {
            StartRenderAnimationJob();
            videoPreviewTimer.Interval = TimeSpan.FromMilliseconds(1000f / document.AnimationDataViewModel.FrameRate);
        }
        else
        {
            var rendered = document.TryRenderWholeImage();
            if (rendered.IsT1)
            {
                VecI previewSize = CalculatePreviewSize(rendered.AsT1.Size);
                ExportPreview = rendered.AsT1.ResizeNearestNeighbor(previewSize);
                rendered.AsT1.Dispose();
                IsPreviewGenerating = false;
            }
        }
    }

    private void StartRenderAnimationJob()
    {
        if (cancellationTokenSource.Token != null && cancellationTokenSource.Token.CanBeCanceled)
        {
            cancellationTokenSource.Cancel();
        }

        cancellationTokenSource = new CancellationTokenSource();

        Task.Run(
            () =>
            {
                videoPreviewFrames = document.RenderFrames(surface =>
                {
                    return Dispatcher.UIThread.Invoke(() =>
                    {
                        Surface original = surface;
                        if (SaveWidth != surface.Size.X || SaveHeight != surface.Size.Y)
                        {
                            original = surface.ResizeNearestNeighbor(new VecI(SaveWidth, SaveHeight));
                            surface.Dispose();
                        }

                        VecI previewSize = CalculatePreviewSize(original.Size);
                        if (previewSize != original.Size)
                        {
                            var resized = original.ResizeNearestNeighbor(previewSize);
                            original.Dispose();
                            return resized;
                        }

                        return original;
                    });
                });
            }, cancellationTokenSource.Token).ContinueWith(_ =>
        {
            Dispatcher.UIThread.Invoke(() =>
            {
                VecI previewSize = CalculatePreviewSize(new VecI(SaveWidth, SaveHeight));
                if (previewSize != ExportPreview.Size)
                {
                    ExportPreview?.Dispose();
                    ExportPreview = new Surface(previewSize);
                }
            });

            IsPreviewGenerating = false;
            videoPreviewTimer.Start();
        });
    }

    private VecI CalculatePreviewSize(VecI imageSize)
    {
        VecI maxPreviewSize = new VecI(150, 200);
        if (imageSize.X > maxPreviewSize.X || imageSize.Y > maxPreviewSize.Y)
        {
            float scaleX = maxPreviewSize.X / (float)imageSize.X;
            float scaleY = maxPreviewSize.Y / (float)imageSize.Y;

            float scale = Math.Min(scaleX, scaleY);

            int newWidth = (int)(imageSize.X * scale);
            int newHeight = (int)(imageSize.Y * scale);

            return new VecI(newWidth, newHeight);
        }

        return imageSize;
    }

    private async Task Export()
    {
        SavePath = await ChoosePath();
        if (SavePath != null)
        {
            Close(true);
        }
    }

    /// <summary>
    ///     Command that handles Path choosing to save file
    /// </summary>
    private async Task<string?> ChoosePath()
    {
        FilePickerSaveOptions options = new FilePickerSaveOptions
        {
            Title = new LocalizedString("EXPORT_SAVE_TITLE"),
            SuggestedFileName = SuggestedName,
            SuggestedStartLocation =
                await GetTopLevel(this).StorageProvider.TryGetWellKnownFolderAsync(WellKnownFolder.Documents),
            FileTypeChoices =
                SupportedFilesHelper.BuildSaveFilter(SelectedExportIndex == 1
                    ? FileTypeDialogDataSet.SetKind.Video
                    : FileTypeDialogDataSet.SetKind.Image),
            ShowOverwritePrompt = true
        };

        IStorageFile file = await GetTopLevel(this).StorageProvider.SaveFilePickerAsync(options);
        if (file != null)
        {
            if (string.IsNullOrEmpty(file.Name) == false)
            {
                SaveFormat = SupportedFilesHelper.GetSaveFileType(
                    SelectedExportIndex == 1
                        ? FileTypeDialogDataSet.SetKind.Video
                        : FileTypeDialogDataSet.SetKind.Image, file);
                if (SaveFormat == null)
                {
                    return null;
                }

                string fileName = SupportedFilesHelper.FixFileExtension(file.Path.LocalPath, SaveFormat);

                return fileName;
            }
        }

        return null;
    }

    private int GetBestPercentage()
    {
        int maxDim = Math.Max(SaveWidth, SaveHeight);
        for (int i = 16; i >= 1; i--)
        {
            if (maxDim * i <= 1280)
                return i * 100;
        }

        return 100;
    }

    private void SetBestPercentage()
    {
        sizePicker.ChosenPercentageSize = GetBestPercentage();
        sizePicker.PercentageRb.IsChecked = true;
        sizePicker.PercentageLostFocus();
    }

    private static void RerenderPreview(AvaloniaPropertyChangedEventArgs e)
    {
        if (e.Sender is ExportFilePopup popup)
        {
            popup.RenderPreview();
        }
    }
}

﻿using System.Threading.Tasks;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.Input;
using PixiDocks.Core.Docking;
using PixiDocks.Core.Docking.Events;
using PixiEditor.AvaloniaUI.Helpers.UI;
using PixiEditor.AvaloniaUI.Models.DocumentModels;
using PixiEditor.AvaloniaUI.ViewModels.Document;
using PixiEditor.AvaloniaUI.Views.Visuals;
using PixiEditor.DrawingApi.Core.Numerics;

namespace PixiEditor.AvaloniaUI.ViewModels.SubViewModels;
#nullable enable
internal class ViewportWindowViewModel : SubViewModel<WindowViewModel>, IDockableContent, IDockableCloseEvents, IDockableSelectionEvents
{
    public DocumentViewModel Document { get; }
    public ExecutionTrigger<VecI> CenterViewportTrigger { get; } = new ExecutionTrigger<VecI>();
    public ExecutionTrigger<double> ZoomViewportTrigger { get; } = new ExecutionTrigger<double>();

    public string Index => _index;

    public string Id => id;
    public string Title => $"{Document.FileName}{Index}";
    public bool CanFloat => true;
    public bool CanClose => true;
    public TabCustomizationSettings TabCustomizationSettings { get; } = new(showCloseButton: true);

    private bool _closeRequested;
    private string _index = "";

    private bool _flipX;
    private string id = Guid.NewGuid().ToString();

    public bool FlipX
    {
        get => _flipX;
        set
        {
            _flipX = value;
            OnPropertyChanged(nameof(FlipX));
        }
    }
    
    private bool _flipY;

    public bool FlipY
    {
        get => _flipY;
        set
        {
            _flipY = value;
            OnPropertyChanged(nameof(FlipY));
        }
    }

    public void IndexChanged()
    {
        _index = Owner.CalculateViewportIndex(this) ?? "";
        OnPropertyChanged(nameof(Index));
        OnPropertyChanged(nameof(Title));
        OnPropertyChanged(nameof(Id));
    }

    public ViewportWindowViewModel(WindowViewModel owner, DocumentViewModel document) : base(owner)
    {
        Document = document;
        Document.SizeChanged += DocumentOnSizeChanged;
        TabCustomizationSettings.Icon = new SurfaceImage(Document.PreviewSurface);
    }

    ~ViewportWindowViewModel()
    {
        Document.SizeChanged -= DocumentOnSizeChanged;
    }

    private void DocumentOnSizeChanged(object? sender, DocumentSizeChangedEventArgs e)
    {
        TabCustomizationSettings.Icon = new SurfaceImage(Document.PreviewSurface);
        OnPropertyChanged(nameof(TabCustomizationSettings));
    }

    bool IDockableCloseEvents.OnClose()
    {
        if (!_closeRequested)
        {
            Task.Run(async () =>
            {
                await Dispatcher.UIThread.InvokeAsync(async () =>
                {
                    _closeRequested =
                        await Owner.OnViewportWindowCloseButtonPressed(this);
                });
            });
        }

        return _closeRequested;
    }

    void IDockableSelectionEvents.OnSelected()
    {
        Owner.ActiveWindow = this;
    }

    void IDockableSelectionEvents.OnDeselected()
    {

    }
}

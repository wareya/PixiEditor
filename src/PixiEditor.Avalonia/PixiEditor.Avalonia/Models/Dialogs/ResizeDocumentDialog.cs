﻿using System.Threading.Tasks;
using Avalonia;
using PixiEditor.Avalonia.Helpers.Extensions;
using PixiEditor.ChangeableDocument.Enums;
using PixiEditor.Models.Enums;
using PixiEditor.Views.Dialogs;

namespace PixiEditor.Models.Dialogs;

internal class ResizeDocumentDialog : CustomDialog
{
    private int height;
    private int width;

    public ResizeDocumentDialog(int currentWidth, int currentHeight, bool openResizeCanvas = false)
    {
        Width = currentWidth;
        Height = currentHeight;
        OpenResizeCanvas = openResizeCanvas;
    }

    public bool OpenResizeCanvas { get; set; }

    public ResizeAnchor ResizeAnchor { get; set; }

    public int Width
    {
        get => width;
        set
        {
            if (width != value)
            {
                width = value;
                OnPropertyChanged(nameof(Width));
            }
        }
    }

    public int Height
    {
        get => height;
        set
        {
            if (height != value)
            {
                height = value;
                OnPropertyChanged(nameof(Height));
            }
        }
    }

    public override async Task<bool> ShowDialog()
    {
        return OpenResizeCanvas ? await ShowResizeCanvasDialog() : await ShowResizeDocumentCanvas();
    }

    async Task<bool> ShowDialog<T>()
        where T : ResizeablePopup, new()
    {
        T popup = new T()
        {
            NewAbsoluteHeight = Height,
            NewAbsoluteWidth = Width,
            NewPercentageSize = 100,
            NewSelectedUnit = SizeUnit.Pixel
        };

        await Application.Current.ForDesktopMainWindowAsync(async window =>
        {
            var result = await popup.ShowDialog<bool>(window);
            if (result)
            {
                Width = popup.NewAbsoluteWidth;
                Height = popup.NewAbsoluteHeight;
                if (popup is ResizeCanvasPopup resizeCanvas)
                {
                    ResizeAnchor = resizeCanvas.SelectedAnchorPoint;
                }
            }
        });

        return false;
    }

    private async Task<bool> ShowResizeDocumentCanvas()
    {
        return await ShowDialog<ResizeDocumentPopup>();
    }

    private async Task<bool> ShowResizeCanvasDialog()
    {
        return await ShowDialog<ResizeCanvasPopup>();
    }
}

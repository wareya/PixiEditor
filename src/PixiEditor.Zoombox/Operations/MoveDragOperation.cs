﻿using System.Windows.Input;
using Avalonia.Input;
using ChunkyImageLib.DataHolders;
using PixiEditor.DrawingApi.Core.Numerics;

namespace PixiEditor.Zoombox.Operations;

internal class MoveDragOperation : IDragOperation
{
    private Zoombox parent;
    private VecD prevMousePos;
    private IPointer? capturedPointer = null!;

    public MoveDragOperation(Zoombox zoomBox)
    {
        parent = zoomBox;
    }

    public void Start(PointerEventArgs e)
    {
        prevMousePos = Zoombox.ToVecD(e.GetPosition(parent.mainCanvas));
        e.Pointer.Capture(parent.mainGrid);
        capturedPointer = e.Pointer;
    }

    public void Update(PointerEventArgs e)
    {
        var curMousePos = Zoombox.ToVecD(e.GetPosition(parent.mainCanvas));
        parent.Center += parent.ToZoomboxSpace(prevMousePos) - parent.ToZoomboxSpace(curMousePos);
        prevMousePos = curMousePos;
    }

    public void Terminate()
    {
        capturedPointer?.Capture(null);
        capturedPointer = null!;
    }
}

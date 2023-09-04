﻿using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Input;
using Avalonia.Media;
using PixiEditor.AvaloniaUI.Helpers;
using PixiEditor.AvaloniaUI.Views.Overlays.TransformOverlay;
using PixiEditor.DrawingApi.Core.Numerics;
using PixiEditor.Extensions.UI.Overlays;

namespace PixiEditor.AvaloniaUI.Views.Overlays.Handles;

public delegate void HandleDrag(VecD newPosition);
public abstract class Handle : IHandle
{
    public IBrush HandleBrush { get; set; } = GetBrush("HandleBackgroundBrush");
    public IPen? HandlePen { get; set; }
    public double ZoomboxScale { get; set; } = 1.0;
    public Control Owner { get; set; } = null!;
    public VecD Position { get; set; }
    public VecD Size { get; set; }
    public RectD HandleRect => new(Position, Size);

    public event Action OnPress;
    public event HandleDrag OnDrag;
    public event Action OnRelease;
    public event Action OnHover;
    public event Action OnExit;

    private bool isPressed;
    private bool isHovered;

    public Handle(Control owner, VecD position, VecD size)
    {
        Owner = owner;
        Position = position;
        Size = size;

        Owner.PointerPressed += OnPointerPressed;
        Owner.PointerMoved += OnPointerMoved;
        Owner.PointerReleased += OnPointerReleased;
    }

    public abstract void Draw(DrawingContext context);

    public virtual void OnPressed(PointerPressedEventArgs args) { }

    protected virtual bool IsWithinHandle(VecD handlePos, VecD pos, double zoomboxScale)
    {
        return TransformHelper.IsWithinHandle(handlePos, pos, zoomboxScale, Size);
    }

    protected static Geometry GetHandleGeometry(string handleName)
    {
        if (Application.Current.Styles.TryGetResource(handleName, null, out object shape))
        {
            return ((Path)shape).Data.Clone();
        }

        return Geometry.Parse("M 0 0 L 1 0 M 0 0 L 0 1");
    }

    protected static IBrush GetBrush(string key)
    {
        if (Application.Current.Styles.TryGetResource(key, null, out object brush))
        {
            return (IBrush)brush;
        }

        return Brushes.Black;
    }

    private void OnPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (e.GetMouseButton(Owner) != MouseButton.Left)
        {
            return;
        }

        VecD pos = TransformHelper.ToVecD(e.GetPosition(Owner));
        VecD handlePos = Position;

        if (IsWithinHandle(handlePos, pos, ZoomboxScale))
        {
            e.Handled = true;
            OnPressed(e);
            OnPress?.Invoke();
            isPressed = true;
            e.Pointer.Capture(Owner);
        }
    }

    private void OnPointerMoved(object? sender, PointerEventArgs e)
    {
        VecD pos = TransformHelper.ToVecD(e.GetPosition(Owner));
        VecD handlePos = Position;

        bool isWithinHandle = IsWithinHandle(handlePos, pos, ZoomboxScale);

        if (!isHovered && isWithinHandle)
        {
            isHovered = true;
            OnHover?.Invoke();
        }
        else if (isHovered && isWithinHandle)
        {
            isHovered = false;
            OnExit?.Invoke();
        }

        if (!isPressed)
        {
            return;
        }

        OnDrag?.Invoke(pos);
    }

    private void OnPointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        if (e.InitialPressMouseButton != MouseButton.Left)
        {
            return;
        }

        if (isPressed)
        {
            isPressed = false;
            OnRelease?.Invoke();
            e.Pointer.Capture(null);
        }
    }
}

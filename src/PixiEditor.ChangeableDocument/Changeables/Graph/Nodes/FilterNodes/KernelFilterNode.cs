﻿using PixiEditor.DrawingApi.Core.Surfaces;
using PixiEditor.DrawingApi.Core.Surfaces.PaintImpl;
using PixiEditor.Numerics;

namespace PixiEditor.ChangeableDocument.Changeables.Graph.Nodes.FilterNodes;

[NodeInfo("KernelFilter", "KERNEL_FILTER_NODE")]
public class KernelFilterNode : FilterNode
{
    private readonly Paint _paint = new();
    
    public InputProperty<Kernel> Kernel { get; }
    
    public InputProperty<double> Gain { get; }

    public InputProperty<double> Bias { get; }

    public InputProperty<TileMode> Tile { get; }

    public InputProperty<bool> OnAlpha { get; }

    private ImageFilter filter;
    private Kernel lastKernel;

    public KernelFilterNode()
    {
        Kernel = CreateInput(nameof(Kernel), "KERNEL", Numerics.Kernel.Identity(3, 3));
        Gain = CreateInput(nameof(Gain), "GAIN", 1d);
        Bias = CreateInput(nameof(Bias), "BIAS", 0d);
        Tile = CreateInput(nameof(Tile), "TILE_MODE", TileMode.Clamp);
        OnAlpha = CreateInput(nameof(OnAlpha), "ON_ALPHA", false);
    }

    protected override ImageFilter? GetImageFilter()
    {
        var kernel = Kernel.Value;
        
        if (kernel.Equals(lastKernel))
            return filter;
        
        lastKernel = kernel;
        
        filter?.Dispose();
        
        var kernelOffset = new VecI(kernel.RadiusX, kernel.RadiusY);
        
        filter = ImageFilter.CreateMatrixConvolution(kernel, (float)Gain.Value, (float)Bias.Value, kernelOffset, Tile.Value, OnAlpha.Value);
        return filter;
    }

    public override Node CreateCopy() => new KernelFilterNode();
}

﻿using PixiEditor.ChangeableDocument.Changeables.Animations;
using PixiEditor.ChangeableDocument.Rendering;
using PixiEditor.DrawingApi.Core;
using PixiEditor.DrawingApi.Core.ColorsImpl;
using PixiEditor.DrawingApi.Core.Shaders;
using PixiEditor.DrawingApi.Core.Surfaces.PaintImpl;
using PixiEditor.Numerics;

namespace PixiEditor.ChangeableDocument.Changeables.Graph.Nodes;

[NodeInfo("Noise", "NOISE_NODE")]
public class NoiseNode : Node
{
    private double previousScale = double.NaN;
    private double previousSeed = double.NaN;
    private NoiseType previousNoiseType = Nodes.NoiseType.FractalPerlin;
    private int previousOctaves = -1;

    private Paint paint = new();

    private static readonly ColorFilter grayscaleFilter = ColorFilter.CreateColorMatrix(
        ColorMatrix.MapAlphaToRedGreenBlue + ColorMatrix.OpaqueAlphaOffset);

    public OutputProperty<Texture> Noise { get; }

    public InputProperty<NoiseType> NoiseType { get; }
    public InputProperty<VecI> Size { get; }

    public InputProperty<double> Scale { get; }

    public InputProperty<int> Octaves { get; }

    public InputProperty<double> Seed { get; }

    public NoiseNode()
    {
        Noise = CreateOutput<Texture>(nameof(Noise), "NOISE", null);
        NoiseType = CreateInput(nameof(NoiseType), "NOISE_TYPE", Nodes.NoiseType.FractalPerlin);
        Size = CreateInput(nameof(Size), "SIZE", new VecI(64, 64))
            .WithRules(v =>
                v.Min(
                    VecI.One,
                    vector => new VecI(Math.Max(1, vector.X), Math.Max(1, vector.Y))
                )
            );

        Scale = CreateInput(nameof(Scale), "SCALE", 10d).WithRules(v => v.Min(0.1));
        Octaves = CreateInput(nameof(Octaves), "OCTAVES", 1)
            .WithRules(validator => validator.Min(1));

        Seed = CreateInput(nameof(Seed), "SEED", 0d);
    }

    protected override Texture OnExecute(RenderingContext context)
    {
        if (Math.Abs(previousScale - Scale.Value) > 0.000001
            || previousSeed != Seed.Value
            || previousOctaves != Octaves.Value
            || previousNoiseType != NoiseType.Value
            || double.IsNaN(previousScale))
        {
            if (Scale.Value < 0.000001)
            {
                Noise.Value = null;
                return null;
            }

            var shader = SelectShader();
            if (shader == null)
            {
                Noise.Value = null;
                return null;
            }

            paint.Shader = shader;

            // Define a grayscale color filter to apply to the image
            paint.ColorFilter = grayscaleFilter;

            previousScale = Scale.Value;
            previousSeed = Seed.Value;
            previousOctaves = Octaves.Value;
            previousNoiseType = NoiseType.Value;
        }

        var size = Size.Value;

        if (size.X < 1 || size.Y < 1)
        {
            Noise.Value = null;
            return null;
        }

        var workingSurface = RequestTexture(0, size);

        workingSurface.DrawingSurface.Canvas.DrawPaint(paint);

        Noise.Value = workingSurface;

        return Noise.Value;
    }

    private Shader SelectShader()
    {
        int octaves = Math.Max(1, Octaves.Value);
        Shader shader = NoiseType.Value switch
        {
            Nodes.NoiseType.TurbulencePerlin => Shader.CreatePerlinNoiseTurbulence(
                (float)(1d / Scale.Value),
                (float)(1d / Scale.Value), octaves, (float)Seed.Value),
            Nodes.NoiseType.FractalPerlin => Shader.CreatePerlinFractalNoise(
                (float)(1d / Scale.Value),
                (float)(1d / Scale.Value), octaves, (float)Seed.Value),
            _ => null
        };

        return shader;
    }

    public override Node CreateCopy() => new NoiseNode();
}

public enum NoiseType
{
    TurbulencePerlin,
    FractalPerlin
}

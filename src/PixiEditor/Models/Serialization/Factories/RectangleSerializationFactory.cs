﻿using PixiEditor.ChangeableDocument.Changeables.Graph.Nodes.Shapes.Data;
using PixiEditor.DrawingApi.Core.ColorsImpl;
using PixiEditor.DrawingApi.Core.Numerics;
using PixiEditor.Numerics;

namespace PixiEditor.Models.Serialization.Factories;

internal class RectangleSerializationFactory : VectorShapeSerializationFactory<RectangleVectorData> 
{
    public override string DeserializationId { get; } = "PixiEditor.RectangleData";


    protected override void AddSpecificData(ByteBuilder builder, RectangleVectorData original)
    {
        builder.AddVecD(original.Center);
        builder.AddVecD(original.Size);
    }

    protected override bool DeserializeVectorData(ByteExtractor extractor, Matrix3X3 matrix, Color strokeColor, Color fillColor,
        int strokeWidth, out RectangleVectorData original)
    {
        VecD center = extractor.GetVecD();
        VecD size = extractor.GetVecD();

        original = new RectangleVectorData(center, size)
        {
            StrokeColor = strokeColor,
            FillColor = fillColor,
            StrokeWidth = strokeWidth,
            TransformationMatrix = matrix
        };

        return true;
    }
}

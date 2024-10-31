﻿using PixiEditor.ChangeableDocument.Changeables.Graph.Nodes.Shapes.Data;
using Drawie.Backend.Core.ColorsImpl;
using Drawie.Backend.Core.Numerics;
using Drawie.Numerics;

namespace PixiEditor.Models.Serialization.Factories;

public class EllipseSerializationFactory : VectorShapeSerializationFactory<EllipseVectorData> 
{
    public override string DeserializationId { get; } = "PixiEditor.EllipseData";

    protected override void AddSpecificData(ByteBuilder builder, EllipseVectorData original)
    {
        builder.AddVecD(original.Center);
        builder.AddVecD(original.Radius);
    }

    protected override bool DeserializeVectorData(ByteExtractor extractor, Matrix3X3 matrix, Color strokeColor, Color fillColor,
        int strokeWidth, out EllipseVectorData original)
    {
        VecD center = extractor.GetVecD();
        VecD radius = extractor.GetVecD();

        original = new EllipseVectorData(center, radius)
        {
            StrokeColor = strokeColor,
            FillColor = fillColor,
            StrokeWidth = strokeWidth,
            TransformationMatrix = matrix
        };

        return true;
    }
}

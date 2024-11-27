﻿using ChunkyImageLib.DataHolders;
using ChunkyImageLib.Operations;
using PixiEditor.ChangeableDocument.Actions;
using PixiEditor.ChangeableDocument.Actions.Generated;
using PixiEditor.ChangeableDocument.Changeables.Graph.Nodes.Shapes.Data;
using Drawie.Backend.Core.Numerics;
using PixiEditor.Models.Handlers.Tools;
using PixiEditor.Models.Tools;
using Drawie.Numerics;
using PixiEditor.ChangeableDocument.Changeables.Graph.Interfaces;

namespace PixiEditor.Models.DocumentModels.UpdateableChangeExecutors;

internal class VectorRectangleToolExecutor : DrawableShapeToolExecutor<IVectorRectangleToolHandler>
{
    public override ExecutorType Type => ExecutorType.ToolLinked;
    protected override DocumentTransformMode TransformMode => DocumentTransformMode.Scale_Rotate_Shear_NoPerspective;

    private VecD firstSize;
    private VecD firstCenter;

    private Matrix3X3 lastMatrix = Matrix3X3.Identity;

    protected override bool AlignToPixels => false;

    protected override bool InitShapeData(IReadOnlyShapeVectorData data)
    {
        if (data is not RectangleVectorData rectData)
            return false;

        firstCenter = rectData.Center;
        firstSize = rectData.Size;
        lastMatrix = rectData.TransformationMatrix;
        return true;
    }

    protected override void DrawShape(VecD curPos, double rotationRad, bool firstDraw)
    {
        RectD rect;
        VecD startPos = Snap(startDrawingPos, curPos);
        if (firstDraw)
        {
            rect = new RectD(curPos, VecD.Zero);
        }
        else
        {
            rect = RectD.FromTwoPoints(startPos, curPos);
        }

        firstCenter = rect.Center;
        firstSize = rect.Size;

        RectangleVectorData data = new RectangleVectorData(firstCenter, firstSize)
        {
            StrokeColor = StrokeColor, FillColor = FillColor, StrokeWidth = StrokeWidth,
        };

        lastRect = rect;

        internals!.ActionAccumulator.AddActions(new SetShapeGeometry_Action(memberId, data));
    }

    protected override IAction SettingsChangedAction()
    {
        return new SetShapeGeometry_Action(memberId,
            new RectangleVectorData(firstCenter, firstSize)
            {
                StrokeColor = StrokeColor,
                FillColor = FillColor,
                StrokeWidth = StrokeWidth,
                TransformationMatrix = lastMatrix
            });
    }

    protected override IAction TransformMovedAction(ShapeData data, ShapeCorners corners)
    {
        if (firstCenter == default || firstSize == default)
        {
            firstCenter = data.Center;
            firstSize = data.Size;
        }

        Matrix3X3 matrix = Matrix3X3.Identity;
        
        if (!corners.IsRect)
        {
            RectD firstRect = RectD.FromCenterAndSize(firstCenter, firstSize);
            matrix = OperationHelper.CreateMatrixFromPoints(corners, firstSize);
            matrix = matrix.Concat(
                Matrix3X3.CreateTranslation(-(float)firstRect.TopLeft.X, -(float)firstRect.TopLeft.Y));
        }
        else
        {
            firstCenter = data.Center;
            firstSize = data.Size;
            
            if(corners.RectRotation != 0)
                matrix = Matrix3X3.CreateRotation((float)corners.RectRotation, (float)firstCenter.X, (float)firstCenter.Y);
        }

        RectangleVectorData newData = new RectangleVectorData(firstCenter, firstSize)
        {
            StrokeColor = data.StrokeColor,
            FillColor = data.FillColor,
            StrokeWidth = data.StrokeWidth,
            TransformationMatrix = matrix
        };

        lastMatrix = matrix;

        return new SetShapeGeometry_Action(memberId, newData);
    }

    protected override IAction EndDrawAction()
    {
        return new EndSetShapeGeometry_Action();
    }
}

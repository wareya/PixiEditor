﻿using Avalonia.Media;
using ChunkyImageLib.DataHolders;
using Drawie.Backend.Core.Vector;
using Drawie.Numerics;
using PixiEditor.ChangeableDocument.Changeables.Graph.Nodes.Shapes.Data;
using PixiEditor.Models.Handlers;
using PixiEditor.Models.Handlers.Tools;
using PixiEditor.ChangeableDocument.Actions.Generated;
using PixiEditor.Helpers.Extensions;
using PixiEditor.Models.Controllers.InputDevice;
using PixiEditor.Models.DocumentModels.UpdateableChangeExecutors.Features;
using PixiEditor.Models.Handlers.Toolbars;
using PixiEditor.Models.Tools;
using Color = Drawie.Backend.Core.ColorsImpl.Color;
using Colors = Drawie.Backend.Core.ColorsImpl.Colors;

namespace PixiEditor.Models.DocumentModels.UpdateableChangeExecutors;

internal class VectorPathToolExecutor : UpdateableChangeExecutor, IPathExecutorFeature, IMidChangeUndoableExecutor
{
    private IStructureMemberHandler member;
    private VectorPath startingPath;
    private IVectorPathToolHandler vectorPathToolHandler;
    private IBasicShapeToolbar toolbar;

    public override ExecutorType Type => ExecutorType.ToolLinked;

    public bool CanUndo => document.PathOverlayHandler.HasUndo;
    public bool CanRedo => document.PathOverlayHandler.HasRedo;

    public override bool BlocksOtherActions => false;

    public override ExecutionState Start()
    {
        vectorPathToolHandler = GetHandler<IVectorPathToolHandler>();

        member = document.SelectedStructureMember;

        if (member is null)
        {
            return ExecutionState.Error;
        }

        toolbar = (IBasicShapeToolbar)vectorPathToolHandler.Toolbar;

        if (member is IVectorLayerHandler vectorLayerHandler)
        {
            var shapeData = vectorLayerHandler.GetShapeData(document.AnimationHandler.ActiveFrameTime);
            bool wasNull = false;
            if (shapeData is PathVectorData pathData)
            {
                startingPath = new VectorPath(pathData.Path);
                startingPath.Transform(pathData.TransformationMatrix);
            }
            else if (shapeData is null)
            {
                wasNull = true;
                startingPath = new VectorPath();
            }
            else
            {
                return ExecutionState.Error;
            }

            if (controller.LeftMousePressed)
            {
                if (wasNull)
                {
                    startingPath.MoveTo((VecF)controller.LastPrecisePosition);
                }
                else
                {
                    startingPath.LineTo((VecF)controller.LastPrecisePosition);
                }

                //below forces undo before starting new path
                internals.ActionAccumulator.AddFinishedActions(new EndSetShapeGeometry_Action());
                
                internals.ActionAccumulator.AddActions(new SetShapeGeometry_Action(member.Id, ConstructShapeData()));
            }

            document.PathOverlayHandler.Show(startingPath, false);
        }
        else
        {
            return ExecutionState.Error;
        }

        document.SnappingHandler.Remove(member.Id.ToString()); // This disables self-snapping
        return ExecutionState.Success;
    }

    public override void OnLeftMouseButtonDown(MouseOnCanvasEventArgs args)
    {
        if (startingPath.IsClosed)
        {
            return;
        }

        startingPath.LineTo((VecF)args.PositionOnCanvas);
        PathVectorData vectorData = ConstructShapeData();

        internals.ActionAccumulator.AddActions(new SetShapeGeometry_Action(member.Id, vectorData));
    }


    public override void OnColorChanged(Color color, bool primary)
    {
        if (primary && toolbar.SyncWithPrimaryColor)
        {
            toolbar.StrokeColor = color.ToColor();
            toolbar.FillColor = color.ToColor();
        }
    }

    public override void OnSettingsChanged(string name, object value)
    {
        internals.ActionAccumulator.AddActions(new SetShapeGeometry_Action(member.Id, ConstructShapeData()));
    }

    public override void ForceStop()
    {
        document.PathOverlayHandler.Hide();
        document.SnappingHandler.AddFromBounds(member.Id.ToString(), () => member.TightBounds ?? RectD.Empty);
        internals.ActionAccumulator.AddFinishedActions(new EndSetShapeGeometry_Action());
    }

    private PathVectorData ConstructShapeData()
    {
        return new PathVectorData(new VectorPath(startingPath))
        {
            StrokeWidth = toolbar.ToolSize,
            StrokeColor = toolbar.StrokeColor.ToColor(),
            FillColor = toolbar.Fill ? toolbar.FillColor.ToColor() : Colors.Transparent,
        };
    }

    public void OnPathChanged(VectorPath path)
    {
        if (document.PathOverlayHandler.IsActive)
        {
            startingPath = path;
            internals.ActionAccumulator.AddActions(new SetShapeGeometry_Action(member.Id, ConstructShapeData()));
        }
    }

    public bool IsFeatureEnabled(IExecutorFeature feature)
    {
        return feature switch
        {
            IPathExecutorFeature _ => true,
            IMidChangeUndoableExecutor _ => true,
            ITransformableExecutor _ => true,
            _ => false
        };
    }

    public void OnMidChangeUndo()
    {
        document.PathOverlayHandler.Undo();
    }

    public void OnMidChangeRedo()
    {
        document.PathOverlayHandler.Redo();
    }
}

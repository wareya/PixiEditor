﻿using PixiEditor.ChangeableDocument.Changeables.Graph.Context;
using PixiEditor.ChangeableDocument.Changeables.Graph.Interfaces;
using PixiEditor.ChangeableDocument.Rendering;
using Drawie.Backend.Core;
using Drawie.Backend.Core.Shaders.Generation;
using Drawie.Backend.Core.Shaders.Generation.Expressions;
using Drawie.Backend.Core.Surfaces;
using Drawie.Backend.Core.Surfaces.PaintImpl;
using Drawie.Numerics;

namespace PixiEditor.ChangeableDocument.Changeables.Graph.Nodes;

[NodeInfo("ModifyImageRight")]
[PairNode(typeof(ModifyImageLeftNode), "ModifyImageZone")]
public class ModifyImageRightNode : RenderNode, IPairNode, ICustomShaderNode
{
    public Guid OtherNode { get; set; }

    private Paint drawingPaint = new Paint() { BlendMode = BlendMode.SrcOver };

    public FuncInputProperty<Float2> Coordinate { get; }
    public FuncInputProperty<Half4> Color { get; }


    private string _lastSksl;


    public ModifyImageRightNode()
    {
        Coordinate = CreateFuncInput(nameof(Coordinate), "UV", new Float2("coords"));
        Color = CreateFuncInput(nameof(Color), "COLOR", new Half4(""));
    }

    protected override void OnPaint(RenderContext renderContext, DrawingSurface targetSurface)
    {
        if (OtherNode == null || OtherNode == default)
        {
            OtherNode = FindStartNode()?.Id ?? default;
            if (OtherNode == null || OtherNode == default)
            {
                return;
            }
        }

        var startNode = FindStartNode();
        if (startNode == null)
        {
            return;
        }
        
        OtherNode = startNode.Id;

        if (startNode.Image.Value is not { Size: var size })
        {
            return;
        }

        ShaderBuilder builder = new(size);
        FuncContext context = new(renderContext, builder);

        if (Coordinate.Connection != null)
        {
            var coordinate = Coordinate.Value(context);
            if (string.IsNullOrEmpty(coordinate.VariableName))
            {
                builder.SetConstant(context.SamplePosition, coordinate);
            }
            else
            {
                builder.Set(context.SamplePosition, coordinate);
            }
        }
        else
        {
            var constCoords = Coordinate.NonOverridenValue(FuncContext.NoContext);
            constCoords.VariableName = "constCords";
            builder.AddUniform(constCoords.VariableName, constCoords.ConstantValue);
            builder.Set(context.SamplePosition, constCoords);
        }

        if (Color.Connection != null)
        {
            builder.ReturnVar(Color.Value(context));
        }
        else
        {
            Half4 color = Color.NonOverridenValue(FuncContext.NoContext);
            color.VariableName = "color";
            builder.AddUniform(color.VariableName, color.ConstantValue);
            builder.ReturnVar(color);
        }

        string sksl = builder.ToSkSl();
        if (sksl != _lastSksl)
        {
            _lastSksl = sksl;
            drawingPaint?.Shader?.Dispose();
            drawingPaint.Shader = builder.BuildShader();
        }
        else
        {
            drawingPaint.Shader = drawingPaint.Shader.WithUpdatedUniforms(builder.Uniforms);
        }

        targetSurface.Canvas.DrawRect(0, 0, size.X, size.Y, drawingPaint);
        builder.Dispose();
    }

    public override RectD? GetPreviewBounds(int frame, string elementToRenderName = "")
    {
        var startNode = FindStartNode();
        if (startNode != null)
        {
            return startNode.GetPreviewBounds(frame, elementToRenderName);
        }
        
        return null;
    }

    public override bool RenderPreview(DrawingSurface renderOn, RenderContext context, string elementToRenderName)
    {
        var startNode = FindStartNode();
        if (drawingPaint != null && startNode != null && startNode.Image.Value != null)
        {
            int saved = renderOn.Canvas.SaveLayer(drawingPaint);
            
            renderOn.Canvas.DrawRect(0, 0, startNode.Image.Value.Size.X, startNode.Image.Value.Size.Y, drawingPaint);
            
            renderOn.Canvas.RestoreToCount(saved);
            
            return true;
        }

        return false;
    }

    public override void Dispose()
    {
        base.Dispose();
        drawingPaint?.Dispose();
    }

    private ModifyImageLeftNode FindStartNode()
    {
        ModifyImageLeftNode startNode = null;
        TraverseBackwards(node =>
        {
            if (node is ModifyImageLeftNode leftNode)
            {
                startNode = leftNode;
                return false;
            }

            return true;
        });

        return startNode;
    }

    public override Node CreateCopy() => new ModifyImageRightNode();
}

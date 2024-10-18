﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using PixiEditor.DrawingApi.Core.ColorsImpl;
using PixiEditor.DrawingApi.Core.Shaders.Generation.Expressions;
using PixiEditor.DrawingApi.Core.Surfaces;
using PixiEditor.Numerics;

namespace PixiEditor.DrawingApi.Core.Shaders.Generation;

public class ShaderBuilder
{
    public Uniforms Uniforms { get; } = new Uniforms();

    private StringBuilder _bodyBuilder = new StringBuilder();

    private List<ShaderExpressionVariable> _variables = new List<ShaderExpressionVariable>();

    private Dictionary<DrawingSurface, SurfaceSampler> _samplers = new Dictionary<DrawingSurface, SurfaceSampler>();

    public BuiltInFunctions Functions { get; } = new();

    public ShaderBuilder(VecI resolution)
    {
        AddUniform("iResolution", resolution);
    }
    
    public Shader BuildShader()
    {
        string generatedSksl = ToSkSl();
        /*string testSksl = """
                          
                          

                          """;*/
        return new Shader(generatedSksl, Uniforms);
    }

    public string ToSkSl()
    {
        StringBuilder sb = new StringBuilder();
        AppendUniforms(sb);

        sb.AppendLine(Functions.BuildFunctions());
        
        sb.AppendLine("half4 main(float2 coords)");
        sb.AppendLine("{");
        sb.AppendLine("coords = coords / iResolution;");
        sb.Append(_bodyBuilder);
        sb.AppendLine("}");

        return sb.ToString();
    }

    private void AppendUniforms(StringBuilder sb)
    {
        foreach (var uniform in Uniforms)
        {
            string layout = string.IsNullOrEmpty(uniform.Value.LayoutOf) ? string.Empty : $"layout({uniform.Value.LayoutOf}) ";
            sb.AppendLine($"{layout}uniform {uniform.Value.UniformName} {uniform.Value.Name};");
        }
    }

    public SurfaceSampler AddOrGetSurface(DrawingSurface surface)
    {
        if (_samplers.TryGetValue(surface, out var sampler))
        {
            return sampler;
        }

        string name = $"texture_{GetUniqueNameNumber()}";
        using var snapshot = surface.Snapshot(surface.DeviceClipBounds);
        Uniforms[name] = new Uniform(name, snapshot.ToShader());
        var newSampler = new SurfaceSampler(name);
        _samplers[surface] = newSampler;

        return newSampler;
    }

    public Half4 Sample(SurfaceSampler texName, Float2 pos)
    {
        string resultName = $"color_{GetUniqueNameNumber()}";
        Half4 result = new Half4(resultName);
        _variables.Add(result);
        _bodyBuilder.AppendLine($"half4 {resultName} = sample({texName.VariableName}, {pos.VariableName} * iResolution);");
        return result;
    }

    public void ReturnVar(Half4 colorValue)
    {
        string alphaExpression = colorValue.A.ExpressionValue;
        _bodyBuilder.AppendLine($"half4 premultiplied = half4({colorValue.R.ExpressionValue} * {alphaExpression}, {colorValue.G.ExpressionValue} * {alphaExpression}, {colorValue.B.ExpressionValue} * {alphaExpression}, {alphaExpression});");
        _bodyBuilder.AppendLine($"return premultiplied;"); 
    }

    public void ReturnConst(Half4 colorValue)
    {
        _bodyBuilder.AppendLine($"return {colorValue.ConstantValueString};");
    }

    public void AddUniform(string uniformName, Color color)
    {
        Uniforms[uniformName] = new Uniform(uniformName, color);
    }

    public void AddUniform(string coords, VecD constCoordsConstantValue)
    {
        Uniforms[coords] = new Uniform(coords, constCoordsConstantValue);
    }

    public void AddUniform(string uniformName, float floatValue)
    {
        Uniforms[uniformName] = new Uniform(uniformName, floatValue);
    }

    public void Set<T>(T contextPosition, T coordinateValue) where T : ShaderExpressionVariable
    {
        if (contextPosition.VariableName == coordinateValue.VariableName)
        {
            return;
        }
        
        _bodyBuilder.AppendLine($"{contextPosition.VariableName} = {coordinateValue.VariableName};");
    }

    public void SetConstant<T>(T contextPosition, T constantValueVar) where T : ShaderExpressionVariable
    {
        _bodyBuilder.AppendLine($"{contextPosition.VariableName} = {constantValueVar.ConstantValueString};");
    }

    public Float2 ConstructFloat2(Expression x, Expression y)
    {
        string name = $"vec2_{GetUniqueNameNumber()}";
        Float2 result = new Float2(name);
        _variables.Add(result);

        string xExpression = x.ExpressionValue;
        string yExpression = y.ExpressionValue;

        _bodyBuilder.AppendLine($"float2 {name} = float2({xExpression}, {yExpression});");
        return result;
    }

    public Float1 ConstructFloat1(Expression assignment)
    {
        string name = $"float_{GetUniqueNameNumber()}";
        Float1 result = new Float1(name);
        _variables.Add(result);

        _bodyBuilder.AppendLine($"float {name} = {assignment.ExpressionValue};");
        return result;
    }

    public Int2 ConstructInt2(Expression first, Expression second)
    {
        string name = $"int2_{GetUniqueNameNumber()}";
        Int2 result = new Int2(name);
        _variables.Add(result);

        string firstExpression = first.ExpressionValue;
        string secondExpression = second.ExpressionValue;

        _bodyBuilder.AppendLine($"int2 {name} = int2({firstExpression}, {secondExpression});");
        return result;
    }

    public Half4 ConstructHalf4(Expression r, Expression g, Expression b, Expression a)
    {
        string name = $"color_{GetUniqueNameNumber()}";
        Half4 result = new Half4(name);
        _variables.Add(result);

        _bodyBuilder.AppendLine($"half4 {name} = {Half4.ConstructorText(r, g, b, a)};");
        return result;
    }


    public Half4 AssignNewHalf4(Expression assignment) => AssignNewHalf4($"color_{GetUniqueNameNumber()}", assignment);

    public Half4 AssignNewHalf4(string name, Expression assignment)
    {
        Half4 result = new Half4(name);
        _variables.Add(result);

        _bodyBuilder.AppendLine($"half4 {name} = {assignment.ExpressionValue};");
        return result;
    }

    public void Dispose()
    {
        _bodyBuilder.Clear();
        _variables.Clear();
        _samplers.Clear();

        foreach (var uniform in Uniforms)
        {
            uniform.Value.Dispose();
        }
    }

    public string GetUniqueNameNumber()
    {
        return (_variables.Count + Uniforms.Count + 1).ToString();
    }
}

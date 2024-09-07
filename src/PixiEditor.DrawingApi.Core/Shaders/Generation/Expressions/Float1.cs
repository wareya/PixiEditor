﻿namespace PixiEditor.DrawingApi.Core.Shaders.Generation.Expressions;

/// <summary>
///     This is a shader type that represents a high precision floating point value. For medium precision see Short type.
/// </summary>
/// <param name="name">Name of the variable in shader code</param>
/// <param name="constant">Constant value of the variable.</param>
public class Float1(string name) : ShaderExpressionVariable<double>(name)
{
    public override string ConstantValueString =>
        ConstantValue.ToString(System.Globalization.CultureInfo.InvariantCulture);

    public override Expression? OverrideExpression { get; set; }

    public static implicit operator Float1(double value) => new("") { ConstantValue = value };

    public static explicit operator double(Float1 value) => value.ConstantValue;

    public byte FullSizeByteConstant => (byte)(ConstantValue * 255);
}

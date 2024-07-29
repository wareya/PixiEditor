﻿namespace PixiEditor.Numerics;

/// <summary>
/// A helper class for creating 4x5 color matrices
/// </summary>
public record struct ColorMatrix
{
    /// <summary>
    /// All values are set to 0. <br/>
    /// (_, _, _, _) => (0, 0, 0, 0)
    /// </summary>
    public static ColorMatrix Zero => new(
        (0, 0, 0, 0, 0),
        (0, 0, 0, 0, 0),
        (0, 0, 0, 0, 0),
        (0, 0, 0, 0, 0)
    );

    /// <summary>
    /// All values stay the same. <br/>
    /// (x, y, z, w) => (x, y, z, w)
    /// </summary>
    public static ColorMatrix Identity => new(
        (1, 0, 0, 0, 0),
        (0, 1, 0, 0, 0),
        (0, 0, 1, 0, 0),
        (0, 0, 0, 1, 0)
    );

    /// <summary>
    /// Values are offset by r, g, b and a <br/>
    /// (x, y, z, w) => (x + <paramref name="r"/>, y + <paramref name="g"/>, z + <paramref name="b"/>, w + <paramref name="a"/>)
    /// </summary>
    public static ColorMatrix Offset(float r, float g, float b, float a) => new(
        (0, 0, 0, 0, r),
        (0, 0, 0, 0, g),
        (0, 0, 0, 0, b),
        (0, 0, 0, 0, a)
    );

    /// <summary>
    /// The Red value is mapped to the Green and Blue values. The Red and Alpha values become 0. <br/><br/>
    /// 
    /// Adding UseRed + UseAlpha will result in grayscale <br/>
    /// (x, _, _, _) => (0, x, x, 0)
    /// </summary>
    public static ColorMatrix MapRedToGreenBlue => new(
        (0, 0, 0, 0, 0),
        (1, 0, 0, 0, 0),
        (1, 0, 0, 0, 0),
        (0, 0, 0, 0, 0)
    );
    
    /// <summary>
    /// The Green value is mapped to the Red and Blue values. The Green and Alpha values become 0. <br/><br/>
    /// 
    /// Adding UseGreen + UseAlpha will result in grayscale <br/>
    /// (_, y, _, _) => (y, 0, y, 0)
    /// </summary>
    public static ColorMatrix MapGreenToRedBlue => new(
        (0, 1, 0, 0, 0),
        (0, 0, 0, 0, 0),
        (0, 1, 0, 0, 0),
        (0, 0, 0, 0, 0)
    );

    /// <summary>
    /// The Blue value is mapped to the Red and Green values. The Blue and Alpha values become 0. <br/><br/>
    /// 
    /// Adding UseBlue + UseAlpha will result in grayscale <br/>
    /// (_, _, z, _) => (z, z, 0, 0)
    /// </summary>
    public static ColorMatrix MapBlueToRedGreen => new(
        (0, 0, 1, 0, 0),
        (0, 0, 1, 0, 0),
        (0, 0, 0, 0, 0),
        (0, 0, 0, 0, 0)
    );

    /// <summary>
    /// The Alpha value is mapped to the Red, Green and Blue values. The Alpha values becomes 0. <br/>
    /// 
    /// (_, _, _, w) => (w, w, w, 0)
    /// </summary>
    public static ColorMatrix MapAlphaToRedGreenBlue => new(
        (0, 0, 0, 1, 0),
        (0, 0, 0, 1, 0),
        (0, 0, 0, 1, 0),
        (0, 0, 0, 0, 0)
    );

    /// <summary>
    /// The red value will stay the red value <br/>
    /// (x, _, _, _) => (x, 0, 0, 0)
    /// </summary>
    public static ColorMatrix UseRed => new(
        (1, 0, 0, 0, 0),
        (0, 0, 0, 0, 0),
        (0, 0, 0, 0, 0),
        (0, 0, 0, 0, 0)
    );
    
    /// <summary>
    /// The green value will stay the green value <br/>
    /// (_, y, _, _) => (0, y, 0, 0)
    /// </summary>
    public static ColorMatrix UseGreen => new(
        (0, 0, 0, 0, 0),
        (0, 1, 0, 0, 0),
        (0, 0, 0, 0, 0),
        (0, 0, 0, 0, 0)
    );
    
    /// <summary>
    /// The blue value will stay the blue value <br/>
    /// (_, _, z, _) => (0, 0, z, 0)
    /// </summary>
    public static ColorMatrix UseBlue => new(
        (0, 0, 0, 0, 0),
        (0, 0, 0, 0, 0),
        (0, 0, 1, 0, 0),
        (0, 0, 0, 0, 0)
    );
    
    /// <summary>
    /// The alpha value will stay the alpha value <br/>
    /// (_, _, _, w) => (0, 0, 0, w)
    /// </summary>
    public static ColorMatrix UseAlpha => new(
        (0, 0, 0, 0, 0),
        (0, 0, 0, 0, 0),
        (0, 0, 0, 0, 0),
        (0, 0, 0, 1, 0)
    );

    /// <summary>
    /// The alpha value will be offset by 1 <br/>
    /// (_, _, _, w) => (0, 0, 0, w + 1)
    /// </summary>
    public static ColorMatrix OpaqueAlphaOffset => Offset(0, 0, 0, 1);
    
    public static ColorMatrix operator +(ColorMatrix left, ColorMatrix right) => new(left.M11 + right.M11,
        left.M12 + right.M12, left.M13 + right.M13, left.M14 + right.M14, left.M15 + right.M15, left.M21 + right.M21,
        left.M22 + right.M22, left.M23 + right.M23, left.M24 + right.M24, left.M25 + right.M25, left.M31 + right.M31,
        left.M32 + right.M32, left.M33 + right.M33, left.M34 + right.M34, left.M35 + right.M35, left.M41 + right.M41,
        left.M42 + right.M42, left.M43 + right.M43, left.M44 + right.M44, left.M45 + right.M45);

    public static ColorMatrix operator -(ColorMatrix left, ColorMatrix right) => new(left.M11 - right.M11,
        left.M12 - right.M12, left.M13 - right.M13, left.M14 - right.M14, left.M15 - right.M15, left.M21 - right.M21,
        left.M22 - right.M22, left.M23 - right.M23, left.M24 - right.M24, left.M25 - right.M25, left.M31 - right.M31,
        left.M32 - right.M32, left.M33 - right.M33, left.M34 - right.M34, left.M35 - right.M35, left.M41 - right.M41,
        left.M42 - right.M42, left.M43 - right.M43, left.M44 - right.M44, left.M45 - right.M45);

    public static ColorMatrix operator *(ColorMatrix left, ColorMatrix right) => new(
        left.M11 * right.M11 + left.M12 * right.M21 + left.M13 * right.M31 + left.M14 * right.M41,
        left.M21 * right.M11 + left.M22 * right.M21 + left.M23 * right.M31 + left.M24 * right.M41,
        left.M31 * right.M11 + left.M32 * right.M21 + left.M33 * right.M31 + left.M34 * right.M41,
        left.M41 * right.M11 + left.M42 * right.M21 + left.M43 * right.M31 + left.M44 * right.M41,
        left.M11 * right.M12 + left.M12 * right.M22 + left.M13 * right.M32 + left.M14 * right.M42,
        left.M21 * right.M12 + left.M22 * right.M22 + left.M23 * right.M32 + left.M24 * right.M42,
        left.M31 * right.M12 + left.M32 * right.M22 + left.M33 * right.M32 + left.M34 * right.M42,
        left.M41 * right.M12 + left.M42 * right.M22 + left.M43 * right.M32 + left.M44 * right.M42,
        left.M11 * right.M13 + left.M12 * right.M23 + left.M13 * right.M33 + left.M14 * right.M43,
        left.M21 * right.M13 + left.M22 * right.M23 + left.M23 * right.M33 + left.M24 * right.M43,
        left.M31 * right.M13 + left.M32 * right.M23 + left.M33 * right.M33 + left.M34 * right.M43,
        left.M41 * right.M13 + left.M42 * right.M23 + left.M43 * right.M33 + left.M44 * right.M43,
        left.M11 * right.M14 + left.M12 * right.M24 + left.M13 * right.M34 + left.M14 * right.M44,
        left.M21 * right.M14 + left.M22 * right.M24 + left.M23 * right.M34 + left.M24 * right.M44,
        left.M31 * right.M14 + left.M32 * right.M24 + left.M33 * right.M34 + left.M34 * right.M44,
        left.M41 * right.M14 + left.M42 * right.M24 + left.M43 * right.M34 + left.M44 * right.M44,
        left.M11 * right.M15 + left.M12 * right.M25 + left.M13 * right.M35 + left.M14 * right.M45,
        left.M21 * right.M15 + left.M22 * right.M25 + left.M23 * right.M35 + left.M24 * right.M45,
        left.M31 * right.M15 + left.M32 * right.M25 + left.M33 * right.M35 + left.M34 * right.M45,
        left.M41 * right.M15 + left.M42 * right.M25 + left.M43 * right.M35 + left.M44 * right.M45);

    public static implicit operator ColorMatrix(Matrix4x5F toConvert) => new(
        toConvert.M11,
        toConvert.M12,
        toConvert.M13,
        toConvert.M14,
        toConvert.M15,
        toConvert.M21,
        toConvert.M22,
        toConvert.M23,
        toConvert.M24,
        toConvert.M25,
        toConvert.M31,
        toConvert.M32,
        toConvert.M33,
        toConvert.M34,
        toConvert.M35,
        toConvert.M41,
        toConvert.M42,
        toConvert.M43,
        toConvert.M44,
        toConvert.M45);
    
    private ColorMatrix(float m11, float m12, float m13, float m14, float m15, float m21, float m22, float m23, float m24,
        float m25, float m31, float m32, float m33, float m34, float m35, float m41, float m42, float m43, float m44,
        float m45)
    {
        M11 = m11;
        M12 = m12;
        M13 = m13;
        M14 = m14;
        M15 = m15;
        M21 = m21;
        M22 = m22;
        M23 = m23;
        M24 = m24;
        M25 = m25;
        M31 = m31;
        M32 = m32;
        M33 = m33;
        M34 = m34;
        M35 = m35;
        M41 = m41;
        M42 = m42;
        M43 = m43;
        M44 = m44;
        M45 = m45;
    }

    public ColorMatrix(
        (float m11, float m12, float m13, float m14, float m15) row1, 
        (float m21, float m22, float m23, float m24, float m25) row2,
        (float m31, float m32, float m33, float m34, float m35) row3,
        (float m41, float m42, float m43, float m44, float m45) row4)
    {
        (M11, M12, M13, M14, M15) = row1;
        (M21, M22, M23, M24, M25) = row2;
        (M31, M32, M33, M34, M35) = row3;
        (M41, M42, M43, M44, M45) = row4;
    }

    public float[] ToArray()
    {
        var buffer = new float[Width * Height];

        TryGetMembers(buffer);
        
        return buffer;
    }

    public bool TryGetMembers(Span<float> members)
    {
        if (members.Length < 20)
            return false;
        members[0] = M11;
        members[1] = M12;
        members[2] = M13;
        members[3] = M14;
        members[4] = M15;
        members[5] = M21;
        members[6] = M22;
        members[7] = M23;
        members[8] = M24;
        members[9] = M25;
        members[10] = M31;
        members[11] = M32;
        members[12] = M33;
        members[13] = M34;
        members[14] = M35;
        members[15] = M41;
        members[16] = M42;
        members[17] = M43;
        members[18] = M44;
        members[19] = M45;
        return true;
    }

    public static ColorMatrix CreateFromMembers(ReadOnlySpan<float> members)
    {
        if (members.Length < 20)
            throw new IndexOutOfRangeException($"{nameof(members)} must have at least 20 elements. Actual length was {members.Length}");

        var m11 = members[0];
        var m12 = members[1];
        var m13 = members[2];
        var m14 = members[3];
        var m15 = members[4];
        var m21 = members[5];
        var m22 = members[6];
        var m23 = members[7];
        var m24 = members[8];
        var m25 = members[9];
        var m31 = members[10];
        var m32 = members[11];
        var m33 = members[12];
        var m34 = members[13];
        var m35 = members[14];
        var m41 = members[15];
        var m42 = members[16];
        var m43 = members[17];
        var m44 = members[18];
        var m45 = members[19];

        return new ColorMatrix(m11, m12, m13, m14, m15, m21, m22, m23, m24, m25, m31, m32, m33, m34, m35, m41, m42, m43,
            m44, m45);
    }

    public bool TryGetRow(int row, Span<float> members)
    {
        if (row < 0 || row >= 4)
            throw new ArgumentOutOfRangeException(nameof(row));
        if (members.Length < 5)
            return false;
        switch (row)
        {
            case 0:
                members[0] = M11;
                members[1] = M12;
                members[2] = M13;
                members[3] = M14;
                members[4] = M15;
                break;
            case 1:
                members[0] = M21;
                members[1] = M22;
                members[2] = M23;
                members[3] = M24;
                members[4] = M25;
                break;
            case 2:
                members[0] = M31;
                members[1] = M32;
                members[2] = M33;
                members[3] = M34;
                members[4] = M35;
                break;
            case 3:
                members[0] = M41;
                members[1] = M42;
                members[2] = M43;
                members[3] = M44;
                members[4] = M45;
                break;
        }

        return true;
    }

    public bool TryGetColumn(int column, Span<float> members)
    {
        if (column < 0 || column >= 5)
            throw new global::System.ArgumentOutOfRangeException(nameof(column));
        if (members.Length < 4)
            return false;
        switch (column)
        {
            case 0:
                members[0] = M11;
                members[1] = M21;
                members[2] = M31;
                members[3] = M41;
                break;
            case 1:
                members[0] = M12;
                members[1] = M22;
                members[2] = M32;
                members[3] = M42;
                break;
            case 2:
                members[0] = M13;
                members[1] = M23;
                members[2] = M33;
                members[3] = M43;
                break;
            case 3:
                members[0] = M14;
                members[1] = M24;
                members[2] = M34;
                members[3] = M44;
                break;
            case 4:
                members[0] = M15;
                members[1] = M25;
                members[2] = M35;
                members[3] = M45;
                break;
        }

        return true;
    }

    public float M11 { get; }

    public float M12 { get; }

    public float M13 { get; }

    public float M14 { get; }

    public float M15 { get; }

    public float M21 { get; }

    public float M22 { get; }

    public float M23 { get; }

    public float M24 { get; }

    public float M25 { get; }

    public float M31 { get; }

    public float M32 { get; }

    public float M33 { get; }

    public float M34 { get; }

    public float M35 { get; }

    public float M41 { get; }

    public float M42 { get; }

    public float M43 { get; }

    public float M44 { get; }

    public float M45 { get; }

    public static int Width { get => 5; }
    public static int Height { get => 4; }
}

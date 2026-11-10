namespace HelixEngine.Shaders;

// Column major matrices: c0..c3 are the columns, so the memory layout matches GLSL mat and HLSL column_major.

[IntrinsicType("float2x2")]
public struct float2x2(float2 c0, float2 c1)
{
    public float2 c0 = c0, c1 = c1;

    /// <summary>A column.</summary>
    public float2 this[int column]
    {
        readonly get => column == 0 ? c0 : c1;
        set { if (column == 0) c0 = value; else c1 = value; }
    }

    public static float2x2 Identity => new(new float2(1, 0), new float2(0, 1));
    public static float2x2 operator *(float2x2 m, float s) => new(m.c0 * s, m.c1 * s);
    public static float2x2 operator +(float2x2 a, float2x2 b) => new(a.c0 + b.c0, a.c1 + b.c1);
}

[IntrinsicType("float3x3")]
public struct float3x3(float3 c0, float3 c1, float3 c2)
{
    public float3 c0 = c0, c1 = c1, c2 = c2;

    /// <summary>A column.</summary>
    public float3 this[int column]
    {
        readonly get => column switch { 0 => c0, 1 => c1, _ => c2 };
        set { switch (column) { case 0: c0 = value; break; case 1: c1 = value; break; default: c2 = value; break; } }
    }

    public static float3x3 Identity => new(new float3(1, 0, 0), new float3(0, 1, 0), new float3(0, 0, 1));
    public static float3x3 operator *(float3x3 m, float s) => new(m.c0 * s, m.c1 * s, m.c2 * s);
    public static float3x3 operator +(float3x3 a, float3x3 b) => new(a.c0 + b.c0, a.c1 + b.c1, a.c2 + b.c2);
}

[IntrinsicType("float4x4")]
public struct float4x4(float4 c0, float4 c1, float4 c2, float4 c3)
{
    public float4 c0 = c0, c1 = c1, c2 = c2, c3 = c3;

    /// <summary>A column.</summary>
    public float4 this[int column]
    {
        readonly get => column switch { 0 => c0, 1 => c1, 2 => c2, _ => c3 };
        set { switch (column) { case 0: c0 = value; break; case 1: c1 = value; break; case 2: c2 = value; break; default: c3 = value; break; } }
    }

    public static float4x4 Identity => new(new float4(1, 0, 0, 0), new float4(0, 1, 0, 0), new float4(0, 0, 1, 0), new float4(0, 0, 0, 1));
    public static float4x4 operator *(float4x4 m, float s) => new(m.c0 * s, m.c1 * s, m.c2 * s, m.c3 * s);
    public static float4x4 operator +(float4x4 a, float4x4 b) => new(a.c0 + b.c0, a.c1 + b.c1, a.c2 + b.c2, a.c3 + b.c3);

    public static float4x4 Translation(float3 t) => new(new float4(1, 0, 0, 0), new float4(0, 1, 0, 0), new float4(0, 0, 1, 0), new float4(t, 1));
    public static float4x4 Scale(float3 s) => new(new float4(s.x, 0, 0, 0), new float4(0, s.y, 0, 0), new float4(0, 0, s.z, 0), new float4(0, 0, 0, 1));
}

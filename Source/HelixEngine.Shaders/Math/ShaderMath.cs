namespace HelixEngine.Shaders;

/// <summary>
/// Shader functions. Every method runs as written on the CPU and is replaced by the matching GPU operation by the
/// shader compiler. Use `using static HelixEngine.Shaders.ShaderMath;` to call them without the class name.
/// </summary>
public static partial class ShaderMath
{
    private static float SmoothstepScalar(float edge0, float edge1, float x)
    {
        float t = System.Math.Clamp((x - edge0) / (edge1 - edge0), 0.0f, 1.0f);
        return t * t * (3.0f - 2.0f * t);
    }

    [Intrinsic("cross")]
    public static float3 Cross(float3 a, float3 b) => new(a.y * b.z - a.z * b.y, a.z * b.x - a.x * b.z, a.x * b.y - a.y * b.x);

    [Intrinsic("refract")]
    public static float3 Refract(float3 incident, float3 normal, float eta)
    {
        float d = Dot(normal, incident);
        float k = 1.0f - eta * eta * (1.0f - d * d);
        return k < 0.0f ? new float3(0.0f) : eta * incident - (eta * d + System.MathF.Sqrt(k)) * normal;
    }

    [Intrinsic("select")] public static float Select(bool mask, float a, float b) => mask ? a : b;
    [Intrinsic("select")] public static int Select(bool mask, int a, int b) => mask ? a : b;
    [Intrinsic("select")] public static uint Select(bool mask, uint a, uint b) => mask ? a : b;

    // Screen space derivatives only exist on the GPU; on the CPU they are zero.
    [Intrinsic("ddx")] public static float Ddx(float a) => 0.0f;
    [Intrinsic("ddy")] public static float Ddy(float a) => 0.0f;
    [Intrinsic("fwidth")] public static float Fwidth(float a) => 0.0f;

    /// <summary>Drops the fragment. On the CPU the caller sees it through <see cref="Discarded"/>.</summary>
    [Intrinsic("discard")]
    public static void Discard() => Discarded = true;

    [System.ThreadStatic] public static bool Discarded;

    // Matrices are column major: Mul(m, v) is m times the column vector v, Mul(a, b) applies b first.
    [Intrinsic("mul")] public static float2 Mul(float2x2 m, float2 v) => m.c0 * v.x + m.c1 * v.y;
    [Intrinsic("mul")] public static float3 Mul(float3x3 m, float3 v) => m.c0 * v.x + m.c1 * v.y + m.c2 * v.z;
    [Intrinsic("mul")] public static float4 Mul(float4x4 m, float4 v) => m.c0 * v.x + m.c1 * v.y + m.c2 * v.z + m.c3 * v.w;
    [Intrinsic("mul")] public static float2x2 Mul(float2x2 a, float2x2 b) => new(Mul(a, b.c0), Mul(a, b.c1));
    [Intrinsic("mul")] public static float3x3 Mul(float3x3 a, float3x3 b) => new(Mul(a, b.c0), Mul(a, b.c1), Mul(a, b.c2));
    [Intrinsic("mul")] public static float4x4 Mul(float4x4 a, float4x4 b) => new(Mul(a, b.c0), Mul(a, b.c1), Mul(a, b.c2), Mul(a, b.c3));

    [Intrinsic("transpose")]
    public static float2x2 Transpose(float2x2 m) => new(new float2(m.c0.x, m.c1.x), new float2(m.c0.y, m.c1.y));

    [Intrinsic("transpose")]
    public static float3x3 Transpose(float3x3 m) =>
        new(new float3(m.c0.x, m.c1.x, m.c2.x), new float3(m.c0.y, m.c1.y, m.c2.y), new float3(m.c0.z, m.c1.z, m.c2.z));

    [Intrinsic("transpose")]
    public static float4x4 Transpose(float4x4 m) =>
        new(new float4(m.c0.x, m.c1.x, m.c2.x, m.c3.x), new float4(m.c0.y, m.c1.y, m.c2.y, m.c3.y), new float4(m.c0.z, m.c1.z, m.c2.z, m.c3.z),
            new float4(m.c0.w, m.c1.w, m.c2.w, m.c3.w));
}

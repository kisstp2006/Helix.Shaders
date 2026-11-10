namespace HelixEngine.Shaders;

public enum SamplerFilter
{
    Nearest,
    Linear,
}

public enum SamplerAddress
{
    Repeat,
    Clamp,
}

/// <summary>Sampler state. On the GPU it is the sampler bound at the slot; the fields drive CPU sampling only.</summary>
[IntrinsicType("SamplerState")]
public struct SamplerState
{
    public SamplerFilter Filter;
    public SamplerAddress Address;
}

/// <summary>
/// Sampler state of a comparison sampler (shadow maps): every texel is compared with a reference value and filtering
/// averages the results. On the GPU it is the sampler bound at the slot; the fields drive CPU sampling only.
/// </summary>
[IntrinsicType("SamplerComparisonState")]
public struct SamplerComparisonState
{
    public SamplerFilter Filter;
    public SamplerAddress Address;
    /// <summary>Passes where reference OP texel holds.</summary>
    public CompareFunction Compare;
}

/// <summary>
/// A 2D texture whose texels have the type T: float, int or uint, a scalar or a vector of 2 to 4 components. On the GPU it is
/// the texture bound at the slot; on the CPU it reads the texels given to <see cref="FromTexels"/>, rows top first, so
/// shaders can run in tests and under a debugger. Only textures of float texels can be sampled; integer textures are read
/// with <see cref="Load"/>.
/// </summary>
[IntrinsicType("Texture2D")]
public struct Texture2D<T> where T : unmanaged
{
    private T[]? texels;
    private int width, height;

    public static Texture2D<T> FromTexels(T[] texels, int width, int height) => new() { texels = texels, width = width, height = height };

    /// <summary>The CPU side texels, null for a texture that only exists on the GPU.</summary>
    public readonly T[]? Texels => texels;

    [Intrinsic("texture.size")]
    public readonly int2 Size => new(width, height);

    [Intrinsic("texture.load")]
    public readonly T Load(int2 texel, int mip = 0)
    {
        if (texels == null)
            return default;
        int x = System.Math.Clamp(texel.x, 0, width - 1), y = System.Math.Clamp(texel.y, 0, height - 1);
        return texels[y * width + x];
    }

    [Intrinsic("texture.sample")]
    public readonly T Sample(SamplerState sampler, float2 uv) => SampleLevel(sampler, uv, 0.0f);

    [Intrinsic("texture.sampleLevel")]
    public readonly T SampleLevel(SamplerState sampler, float2 uv, float level) => TexelMath.Sample2D(texels, 0, width, height, sampler, uv);

    /// <summary>The share of the filtered texels for which reference OP texel holds; float texels only.</summary>
    [Intrinsic("texture.samplecmp")]
    public readonly float SampleCmp(SamplerComparisonState sampler, float2 uv, float reference) =>
        TexelMath.Compare2D(texels, 0, width, height, sampler, uv, reference);

    [Intrinsic("texture.samplecmplevelzero")]
    public readonly float SampleCmpLevelZero(SamplerComparisonState sampler, float2 uv, float reference) =>
        TexelMath.Compare2D(texels, 0, width, height, sampler, uv, reference);
}

/// <summary>Conversions between the float texel types and float4, for filtering on the CPU.</summary>
internal static class TexelMath
{
    public static int Wrap(SamplerState sampler, int i, int n) =>
        sampler.Address == SamplerAddress.Repeat ? ((i % n) + n) % n : System.Math.Clamp(i, 0, n - 1);

    public static bool Passes(CompareFunction f, float reference, float texel) => f switch
    {
        CompareFunction.Never => false,
        CompareFunction.Less => reference < texel,
        CompareFunction.Equal => reference == texel,
        CompareFunction.LessEqual => reference <= texel,
        CompareFunction.Greater => reference > texel,
        CompareFunction.NotEqual => reference != texel,
        CompareFunction.GreaterEqual => reference >= texel,
        _ => true,
    };

    // Comparison sampling of one 2D image: the texels are compared first, then filtered like colors.
    public static float Compare2D<T>(T[]? texels, int offset, int width, int height, SamplerComparisonState sampler, float2 uv, float reference)
        where T : unmanaged
    {
        if (typeof(T) != typeof(float))
            throw new System.InvalidOperationException("comparison sampling needs a texture of float texels");
        if (texels == null)
            return 0.0f;
        float[] values = (float[])(object)texels;
        float[] results = new float[width * height];
        for (int i = 0; i < results.Length; i++)
            results[i] = Passes(sampler.Compare, reference, values[offset + i]) ? 1.0f : 0.0f;
        SamplerState plain = new() { Filter = sampler.Filter, Address = sampler.Address };
        return Sample2D(results, 0, width, height, plain, uv);
    }

    // One 2D image starting at `offset` in the texels: nearest or bilinear, as the GPU filters a single mip level.
    public static T Sample2D<T>(T[]? texels, int offset, int width, int height, SamplerState sampler, float2 uv) where T : unmanaged
    {
        if (!IsFloat<T>())
            throw new System.InvalidOperationException("only textures of float texels can be sampled; use Load");
        if (texels == null)
            return default;
        T Texel(int x, int y) => texels[offset + Wrap(sampler, y, height) * width + Wrap(sampler, x, width)];
        float fx = uv.x * width - 0.5f, fy = uv.y * height - 0.5f;
        if (sampler.Filter == SamplerFilter.Nearest)
            return Texel((int)System.MathF.Floor(fx + 0.5f), (int)System.MathF.Floor(fy + 0.5f));
        int x0 = (int)System.MathF.Floor(fx), y0 = (int)System.MathF.Floor(fy);
        float tx = fx - x0, ty = fy - y0;
        float4 top = ShaderMath.Lerp(ToFloat4(Texel(x0, y0)), ToFloat4(Texel(x0 + 1, y0)), tx);
        float4 bottom = ShaderMath.Lerp(ToFloat4(Texel(x0, y0 + 1)), ToFloat4(Texel(x0 + 1, y0 + 1)), tx);
        return FromFloat4<T>(ShaderMath.Lerp(top, bottom, ty));
    }

    public static bool IsFloat<T>() => typeof(T) == typeof(float) || typeof(T) == typeof(float2) || typeof(T) == typeof(float3) || typeof(T) == typeof(float4);

    public static float4 ToFloat4<T>(T v) where T : unmanaged => v switch
    {
        float f => new float4(f, 0, 0, 1),
        float2 f => new float4(f.x, f.y, 0, 1),
        float3 f => new float4(f, 1),
        float4 f => f,
        _ => throw new System.InvalidOperationException("only textures of float texels can be sampled; use Load"),
    };

    public static T FromFloat4<T>(float4 v) where T : unmanaged
    {
        object result = typeof(T) == typeof(float) ? v.x : typeof(T) == typeof(float2) ? v.xy : typeof(T) == typeof(float3) ? v.xyz : v;
        return (T)result;
    }
}

/// <summary>Read only array of structs bound as a storage buffer; on the CPU it wraps a managed array.</summary>
[IntrinsicType("StructuredBuffer")]
public struct StructuredBuffer<T> where T : unmanaged
{
    private T[]? items;

    public static StructuredBuffer<T> FromArray(T[] items) => new() { items = items };

    [Intrinsic("buffer.length")]
    public readonly int Length => items?.Length ?? 0;

    public readonly T this[int index]
    {
        [Intrinsic("buffer.load")] get => items![index];
    }
}

/// <summary>Read write array of structs bound as a storage buffer in compute shaders.</summary>
[IntrinsicType("RWStructuredBuffer")]
public struct RWStructuredBuffer<T> where T : unmanaged
{
    private T[]? items;

    public static RWStructuredBuffer<T> FromArray(T[] items) => new() { items = items };

    [Intrinsic("buffer.length")]
    public readonly int Length => items?.Length ?? 0;

    public readonly T this[int index]
    {
        [Intrinsic("buffer.load")] get => items![index];
        [Intrinsic("buffer.store")] set => items![index] = value;
    }

    internal readonly T[] Items => items!;
}

/// <summary>
/// A 2D texture a compute shader writes, bound as a storage texture. On the CPU it keeps the texels written,
/// rows top first.
/// </summary>
[IntrinsicType("RWTexture2D")]
public struct RWTexture2D
{
    private float4[]? pixels;
    private int width, height;

    public static RWTexture2D Create(int width, int height) => new() { pixels = new float4[width * height], width = width, height = height };

    public readonly float4[]? Pixels => pixels;

    [Intrinsic("image.size")]
    public readonly int2 Size => new(width, height);

    public readonly float4 this[int2 texel]
    {
        [Intrinsic("image.store")] set => pixels![texel.y * width + texel.x] = value;
    }
}

public static partial class ShaderMath
{
    /// <summary>Adds to one element of a buffer as a single step even when many threads do it; returns the value before.</summary>
    [Intrinsic("interlockedadd")]
    public static uint InterlockedAdd(RWStructuredBuffer<uint> buffer, int index, uint value) =>
        System.Threading.Interlocked.Add(ref buffer.Items[index], value) - value;

    [Intrinsic("interlockedadd")]
    public static int InterlockedAdd(RWStructuredBuffer<int> buffer, int index, int value) =>
        System.Threading.Interlocked.Add(ref buffer.Items[index], value) - value;
}

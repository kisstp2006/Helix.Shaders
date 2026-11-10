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
/// A 2D texture of float4 texels. On the GPU it is the texture bound at the slot; on the CPU it reads the pixels
/// given to <see cref="FromPixels"/>, rows top first, so shaders can run in tests and under a debugger.
/// </summary>
[IntrinsicType("Texture2D")]
public struct Texture2D
{
    private float4[]? pixels;
    private int width, height;

    public static Texture2D FromPixels(float4[] pixels, int width, int height) => new() { pixels = pixels, width = width, height = height };

    /// <summary>The CPU side pixels, null for a texture that only exists on the GPU.</summary>
    public readonly float4[]? Pixels => pixels;

    [Intrinsic("texture.size")]
    public readonly int2 Size => new(width, height);

    [Intrinsic("texture.load")]
    public readonly float4 Load(int2 texel, int mip = 0)
    {
        if (pixels == null)
            return default;
        int x = System.Math.Clamp(texel.x, 0, width - 1), y = System.Math.Clamp(texel.y, 0, height - 1);
        return pixels[y * width + x];
    }

    [Intrinsic("texture.sample")]
    public readonly float4 Sample(SamplerState sampler, float2 uv) => SampleLevel(sampler, uv, 0.0f);

    [Intrinsic("texture.sampleLevel")]
    public readonly float4 SampleLevel(SamplerState sampler, float2 uv, float level)
    {
        if (pixels == null)
            return default;
        float fx = uv.x * width - 0.5f, fy = uv.y * height - 0.5f;
        if (sampler.Filter == SamplerFilter.Nearest)
            return Texel(sampler, (int)System.MathF.Floor(fx + 0.5f), (int)System.MathF.Floor(fy + 0.5f));
        int x0 = (int)System.MathF.Floor(fx), y0 = (int)System.MathF.Floor(fy);
        float tx = fx - x0, ty = fy - y0;
        float4 top = ShaderMath.Lerp(Texel(sampler, x0, y0), Texel(sampler, x0 + 1, y0), tx);
        float4 bottom = ShaderMath.Lerp(Texel(sampler, x0, y0 + 1), Texel(sampler, x0 + 1, y0 + 1), tx);
        return ShaderMath.Lerp(top, bottom, ty);
    }

    private readonly float4 Texel(SamplerState sampler, int x, int y)
    {
        if (sampler.Address == SamplerAddress.Repeat)
        {
            x = ((x % width) + width) % width;
            y = ((y % height) + height) % height;
        }
        return Load(new int2(x, y));
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

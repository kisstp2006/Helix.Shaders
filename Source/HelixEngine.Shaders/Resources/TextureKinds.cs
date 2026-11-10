namespace HelixEngine.Shaders;

/// <summary>Layers of 2D texels; the third coordinate of Sample and Load picks the layer (rounded to the nearest one).</summary>
[IntrinsicType("Texture2DArray")]
public struct Texture2DArray<T> where T : unmanaged
{
    private T[]? texels;
    private int width, height, layers;

    /// <summary>Texels layer by layer, rows top first.</summary>
    public static Texture2DArray<T> FromTexels(T[] texels, int width, int height, int layers) =>
        new() { texels = texels, width = width, height = height, layers = layers };

    public readonly T[]? Texels => texels;

    [Intrinsic("texture.size")]
    public readonly int3 Size => new(width, height, layers);

    [Intrinsic("texture.load")]
    public readonly T Load(int3 texel, int mip = 0)
    {
        if (texels == null)
            return default;
        int layer = System.Math.Clamp(texel.z, 0, layers - 1);
        return texels[layer * width * height + System.Math.Clamp(texel.y, 0, height - 1) * width + System.Math.Clamp(texel.x, 0, width - 1)];
    }

    [Intrinsic("texture.sample")]
    public readonly T Sample(SamplerState sampler, float3 uvLayer) => SampleLevel(sampler, uvLayer, 0.0f);

    [Intrinsic("texture.sampleLevel")]
    public readonly T SampleLevel(SamplerState sampler, float3 uvLayer, float level)
    {
        int layer = System.Math.Clamp((int)System.MathF.Floor(uvLayer.z + 0.5f), 0, layers - 1);
        return TexelMath.Sample2D(texels, layer * width * height, width, height, sampler, uvLayer.xy);
    }
}

/// <summary>A volume of texels, filtered in three dimensions.</summary>
[IntrinsicType("Texture3D")]
public struct Texture3D<T> where T : unmanaged
{
    private T[]? texels;
    private int width, height, depth;

    /// <summary>Texels slice by slice, rows top first.</summary>
    public static Texture3D<T> FromTexels(T[] texels, int width, int height, int depth) =>
        new() { texels = texels, width = width, height = height, depth = depth };

    public readonly T[]? Texels => texels;

    [Intrinsic("texture.size")]
    public readonly int3 Size => new(width, height, depth);

    [Intrinsic("texture.load")]
    public readonly T Load(int3 texel, int mip = 0)
    {
        if (texels == null)
            return default;
        int x = System.Math.Clamp(texel.x, 0, width - 1), y = System.Math.Clamp(texel.y, 0, height - 1), z = System.Math.Clamp(texel.z, 0, depth - 1);
        return texels[(z * height + y) * width + x];
    }

    [Intrinsic("texture.sample")]
    public readonly T Sample(SamplerState sampler, float3 uvw) => SampleLevel(sampler, uvw, 0.0f);

    [Intrinsic("texture.sampleLevel")]
    public readonly T SampleLevel(SamplerState sampler, float3 uvw, float level)
    {
        if (!TexelMath.IsFloat<T>())
            throw new System.InvalidOperationException("only textures of float texels can be sampled; use Load");
        if (texels == null)
            return default;
        float fz = uvw.z * depth - 0.5f;
        if (sampler.Filter == SamplerFilter.Nearest)
        {
            int z = TexelMath.Wrap(sampler, (int)System.MathF.Floor(fz + 0.5f), depth);
            return TexelMath.Sample2D(texels, z * width * height, width, height, sampler, uvw.xy);
        }
        int z0 = (int)System.MathF.Floor(fz);
        float tz = fz - z0;
        float4 near = TexelMath.ToFloat4(TexelMath.Sample2D(texels, TexelMath.Wrap(sampler, z0, depth) * width * height, width, height, sampler, uvw.xy));
        float4 far = TexelMath.ToFloat4(TexelMath.Sample2D(texels, TexelMath.Wrap(sampler, z0 + 1, depth) * width * height, width, height, sampler, uvw.xy));
        return TexelMath.FromFloat4<T>(ShaderMath.Lerp(near, far, tz));
    }
}

/// <summary>
/// Six square faces (+X, -X, +Y, -Y, +Z, -Z) sampled by a direction. The largest component of the direction picks the face;
/// on the CPU filtering stays inside the face, the GPU also blends across face edges.
/// </summary>
[IntrinsicType("TextureCube")]
public struct TextureCube<T> where T : unmanaged
{
    private T[]? texels;
    private int size;

    /// <summary>Texels face by face in the order +X, -X, +Y, -Y, +Z, -Z, rows top first.</summary>
    public static TextureCube<T> FromTexels(T[] texels, int size) => new() { texels = texels, size = size };

    public readonly T[]? Texels => texels;

    [Intrinsic("texture.size")]
    public readonly int2 Size => new(size, size);

    [Intrinsic("texture.sample")]
    public readonly T Sample(SamplerState sampler, float3 direction) => SampleLevel(sampler, direction, 0.0f);

    [Intrinsic("texture.sampleLevel")]
    public readonly T SampleLevel(SamplerState sampler, float3 direction, float level)
    {
        float ax = System.MathF.Abs(direction.x), ay = System.MathF.Abs(direction.y), az = System.MathF.Abs(direction.z);
        int face;
        float sc, tc, ma;
        if (ax >= ay && ax >= az)
        {
            face = direction.x >= 0 ? 0 : 1;
            sc = direction.x >= 0 ? -direction.z : direction.z;
            tc = -direction.y;
            ma = ax;
        }
        else if (ay >= az)
        {
            face = direction.y >= 0 ? 2 : 3;
            sc = direction.x;
            tc = direction.y >= 0 ? direction.z : -direction.z;
            ma = ay;
        }
        else
        {
            face = direction.z >= 0 ? 4 : 5;
            sc = direction.z >= 0 ? direction.x : -direction.x;
            tc = -direction.y;
            ma = az;
        }
        float2 uv = new((sc / ma + 1.0f) * 0.5f, (tc / ma + 1.0f) * 0.5f);
        SamplerState clamped = sampler;
        clamped.Address = SamplerAddress.Clamp;
        return TexelMath.Sample2D(texels, face * size * size, size, size, clamped, uv);
    }
}

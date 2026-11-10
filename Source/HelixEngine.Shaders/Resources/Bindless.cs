using System;
using System.Collections.Generic;

namespace HelixEngine.Shaders;

/// <summary>
/// A texture reached by its bindless index instead of a binding slot. The renderer registers a texture with a sampler
/// and passes the index (in a uniform block, a buffer or a vertex attribute); the shader samples through it. On the CPU
/// the index points into <see cref="Bindless"/>.
/// </summary>
[IntrinsicType("BindlessTexture")]
public struct BindlessTexture
{
    private uint index;

    public BindlessTexture(uint index) => this.index = index;

    public readonly uint Index => index;

    [Intrinsic("bindless.sample")]
    public readonly float4 Sample(float2 uv) => Bindless.Texture(index).Sample(uv);

    [Intrinsic("bindless.samplelevel")]
    public readonly float4 SampleLevel(float2 uv, float level) => Bindless.Texture(index).Sample(uv);
}

/// <summary>
/// A buffer reached by its bindless index: raw bytes, read and written four bytes at a time at byte offsets that are
/// multiples of four. Writing is only possible in compute shaders. On the CPU the index points into <see cref="Bindless"/>.
/// </summary>
[IntrinsicType("BindlessBuffer")]
public struct BindlessBuffer
{
    private uint index;

    public BindlessBuffer(uint index) => this.index = index;

    public readonly uint Index => index;

    /// <summary>Size in bytes.</summary>
    [Intrinsic("bindless.size")]
    public readonly uint Size => (uint)Bindless.Buffer(index).Length * 4u;

    [Intrinsic("bindless.load")] public readonly uint Load(uint byteOffset) => Word(byteOffset, 0);
    [Intrinsic("bindless.load")] public readonly uint2 Load2(uint byteOffset) => new(Word(byteOffset, 0), Word(byteOffset, 1));
    [Intrinsic("bindless.load")] public readonly uint3 Load3(uint byteOffset) => new(Word(byteOffset, 0), Word(byteOffset, 1), Word(byteOffset, 2));

    [Intrinsic("bindless.load")]
    public readonly uint4 Load4(uint byteOffset) => new(Word(byteOffset, 0), Word(byteOffset, 1), Word(byteOffset, 2), Word(byteOffset, 3));

    [Intrinsic("bindless.store")] public readonly void Store(uint byteOffset, uint value) => SetWord(byteOffset, 0, value);

    [Intrinsic("bindless.store")]
    public readonly void Store2(uint byteOffset, uint2 value)
    {
        SetWord(byteOffset, 0, value.x);
        SetWord(byteOffset, 1, value.y);
    }

    [Intrinsic("bindless.store")]
    public readonly void Store3(uint byteOffset, uint3 value)
    {
        SetWord(byteOffset, 0, value.x);
        SetWord(byteOffset, 1, value.y);
        SetWord(byteOffset, 2, value.z);
    }

    [Intrinsic("bindless.store")]
    public readonly void Store4(uint byteOffset, uint4 value)
    {
        SetWord(byteOffset, 0, value.x);
        SetWord(byteOffset, 1, value.y);
        SetWord(byteOffset, 2, value.z);
        SetWord(byteOffset, 3, value.w);
    }

    private readonly uint Word(uint byteOffset, uint word) => Bindless.Buffer(index)[(int)(byteOffset / 4u + word)];

    private readonly void SetWord(uint byteOffset, uint word, uint value) => Bindless.Buffer(index)[(int)(byteOffset / 4u + word)] = value;
}

/// <summary>
/// What bindless indices mean when shaders run on the CPU (tests, debugging). On the GPU the renderer registers the
/// resources and this class is not used.
/// </summary>
public static class Bindless
{
    public readonly struct Entry(Texture2D texture, SamplerState sampler)
    {
        public Texture2D Texture { get; } = texture;
        public SamplerState Sampler { get; } = sampler;

        public float4 Sample(float2 uv) => Texture.Sample(Sampler, uv);
    }

    private static readonly List<Entry> textures = new();
    private static readonly List<uint[]> buffers = new();

    /// <summary>Indices start at zero and follow the order of the calls.</summary>
    public static BindlessTexture Register(Texture2D texture, SamplerState sampler)
    {
        textures.Add(new Entry(texture, sampler));
        return new BindlessTexture((uint)(textures.Count - 1));
    }

    public static BindlessBuffer Register(uint[] words)
    {
        buffers.Add(words);
        return new BindlessBuffer((uint)(buffers.Count - 1));
    }

    public static int TextureCount => textures.Count;
    public static int BufferCount => buffers.Count;
    public static Entry Texture(uint index) => textures[(int)index];
    public static uint[] Buffer(uint index) => buffers[(int)index];

    public static void Clear()
    {
        textures.Clear();
        buffers.Clear();
    }
}

public static partial class ShaderMath
{
    // Bit casts: the same 32 bits read as another type.
    [Intrinsic("asfloat")] public static float AsFloat(uint a) => BitConverter.UInt32BitsToSingle(a);
    [Intrinsic("asfloat")] public static float AsFloat(int a) => BitConverter.Int32BitsToSingle(a);
    [Intrinsic("asfloat")] public static float2 AsFloat(uint2 a) => new(AsFloat(a.x), AsFloat(a.y));
    [Intrinsic("asfloat")] public static float3 AsFloat(uint3 a) => new(AsFloat(a.x), AsFloat(a.y), AsFloat(a.z));
    [Intrinsic("asfloat")] public static float4 AsFloat(uint4 a) => new(AsFloat(a.x), AsFloat(a.y), AsFloat(a.z), AsFloat(a.w));
    [Intrinsic("asuint")] public static uint AsUInt(float a) => BitConverter.SingleToUInt32Bits(a);
    [Intrinsic("asuint")] public static uint AsUInt(int a) => unchecked((uint)a);
    [Intrinsic("asuint")] public static uint2 AsUInt(float2 a) => new(AsUInt(a.x), AsUInt(a.y));
    [Intrinsic("asuint")] public static uint3 AsUInt(float3 a) => new(AsUInt(a.x), AsUInt(a.y), AsUInt(a.z));
    [Intrinsic("asuint")] public static uint4 AsUInt(float4 a) => new(AsUInt(a.x), AsUInt(a.y), AsUInt(a.z), AsUInt(a.w));
    [Intrinsic("asint")] public static int AsInt(float a) => BitConverter.SingleToInt32Bits(a);
    [Intrinsic("asint")] public static int AsInt(uint a) => unchecked((int)a);
}

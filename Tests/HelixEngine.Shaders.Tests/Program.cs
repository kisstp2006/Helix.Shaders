using System;
using HelixEngine.Shaders;
using static HelixEngine.Shaders.ShaderMath;

namespace HelixEngine.Shaders.Tests;

/// <summary>A sample shader in the supported C# subset; the tests run it on the CPU like any other code.</summary>
public static class CheckerShader
{
    public struct Draw
    {
        public float4 tint;
        public float2 scale;
    }

    public struct Varyings
    {
        [Position] public float4 position;
        [Location(0)] public float2 uv;
    }

    [Uniform(0)] public static Draw draw;
    [Texture(0)] public static Texture2D albedo;
    [Sampler(0)] public static SamplerState albedoSampler;

    [VertexShader]
    public static Varyings Vertex([Location(0)] float2 position, [Location(1)] float2 uv)
    {
        Varyings o;
        o.position = new float4(position * draw.scale, 0.5f, 1.0f);
        o.uv = uv;
        return o;
    }

    [FragmentShader]
    [return: Target(0)]
    public static float4 Fragment(Varyings input)
    {
        float4 texel = albedo.Sample(albedoSampler, input.uv);
        float stripe = Step(0.5f, Frac(input.uv.x * 4.0f));
        float3 color = Lerp(texel.xyz, texel.zyx, stripe);
        if (texel.w < 0.1f)
            Discard();
        return new float4(color, 1.0f) * draw.tint;
    }
}

internal static class Program
{
    private static int failures;
    private static int checks;

    private static void Check(bool condition, string what)
    {
        checks++;
        if (condition)
            return;
        failures++;
        Console.WriteLine("    FAILED: " + what);
    }

    private static void Near(float actual, float expected, string what) => Check(MathF.Abs(actual - expected) <= 1e-5f * MathF.Max(1.0f, MathF.Abs(expected)), $"{what}: {actual} != {expected}");
    private static void Near(float3 a, float3 e, string what) { Near(a.x, e.x, what + ".x"); Near(a.y, e.y, what + ".y"); Near(a.z, e.z, what + ".z"); }
    private static void Near(float4 a, float4 e, string what) { Near(a.xyz, e.xyz, what); Near(a.w, e.w, what + ".w"); }

    private static void Vectors()
    {
        float3 a = new(1, 2, 3), b = new(4, 5, 6);
        Near(a + b, new float3(5, 7, 9), "add");
        Near(a * 2.0f - 1.0f, new float3(1, 3, 5), "scalar ops");
        Near(-a / b, new float3(-0.25f, -0.4f, -0.5f), "divide");
        Near(new float4(a, 7).wzyx.xyz, new float3(7, 3, 2), "swizzle read");
        float4 v = new(0);
        v.zx = new float2(9, 8);
        v.yw = a.xx;
        Near(v, new float4(8, 1, 9, 1), "swizzle write");
        v[1] = 5;
        Near(v[1], 5, "indexer");
        Check(All(a < b) && !Any(a == b) && Any(a != b), "comparisons give bool vectors");
        int3 i = (int3)new float3(1.9f, -1.9f, 3.0f);
        Check(All(i == new int3(1, -1, 3)), "float to int truncates");
        uint2 u = new uint2(0xF0u, 3u) >> 1;
        Check(All(u == new uint2(0x78u, 1u)) && All((u & new uint2(8u)) == new uint2(8u, 0u)), "integer bit operations");
        Near(Select(a > new float3(1.5f), a, b), new float3(4, 2, 3), "select");
    }

    private static void Functions()
    {
        float3 a = new(1, 2, 3), b = new(4, 5, 6);
        Near(Dot(a, b), 32, "dot");
        Near(Cross(new float3(1, 0, 0), new float3(0, 1, 0)), new float3(0, 0, 1), "cross");
        Near(Length(new float3(3, 4, 12)), 13, "length");
        Near(Normalize(new float3(0, 3, 4)), new float3(0, 0.6f, 0.8f), "normalize");
        Near(Lerp(a, b, 0.25f), new float3(1.75f, 2.75f, 3.75f), "lerp");
        Near(Saturate(new float3(-1, 0.5f, 2)), new float3(0, 0.5f, 1), "saturate");
        Near(Clamp(new float3(-1, 0.5f, 2), 0.25f, 0.75f), new float3(0.25f, 0.5f, 0.75f), "clamp");
        Near(Frac(new float3(1.25f, -0.25f, 3)), new float3(0.25f, 0.75f, 0), "frac");
        Near(Smoothstep(0.0f, 1.0f, 0.5f), 0.5f, "smoothstep");
        Near(Step(new float3(2), a), new float3(0, 1, 1), "step");
        Near(Reflect(new float3(1, -1, 0), new float3(0, 1, 0)), new float3(1, 1, 0), "reflect");
        Near(Pow(new float3(2, 3, 4), 2.0f), new float3(4, 9, 16), "pow");
        Near(Sin(MathF.PI / 6), 0.5f, "sin");
        Near(Atan2(1.0f, 1.0f), MathF.PI / 4, "atan2");
        Check(Min(new int2(3, -5), new int2(1, 2)).Equals(new int2(1, -5)), "integer min");
        Near(Refract(Normalize(new float3(1, -1, 0)), new float3(0, 1, 0), 1.0f), Normalize(new float3(1, -1, 0)), "refract with eta 1");
    }

    private static void Matrices()
    {
        float4x4 t = float4x4.Translation(new float3(1, 2, 3)), s = float4x4.Scale(new float3(2, 2, 2));
        Near(Mul(t, new float4(1, 1, 1, 1)), new float4(2, 3, 4, 1), "translate");
        Near(Mul(Mul(t, s), new float4(1, 1, 1, 1)), new float4(3, 4, 5, 1), "scale then translate");
        Near(Mul(Mul(s, t), new float4(1, 1, 1, 1)), new float4(4, 6, 8, 1), "translate then scale");
        Near(Transpose(t).c0, new float4(1, 0, 0, 1), "transpose");
        float3x3 r = new(new float3(0, 1, 0), new float3(-1, 0, 0), new float3(0, 0, 1));
        Near(Mul(r, new float3(1, 0, 0)), new float3(0, 1, 0), "rotate 90 degrees");
        Near(Mul(Mul(r, r), new float3(1, 0, 0)), new float3(-1, 0, 0), "rotate twice");
    }

    private static void ShaderOnCpu()
    {
        float4[] pixels = { new(1, 0, 0, 1), new(0, 1, 0, 1), new(0, 0, 1, 1), new(1, 1, 1, 0) };
        CheckerShader.albedo = Texture2D.FromPixels(pixels, 2, 2);
        CheckerShader.albedoSampler = new SamplerState { Filter = SamplerFilter.Nearest, Address = SamplerAddress.Repeat };
        CheckerShader.draw = new CheckerShader.Draw { tint = new float4(0.5f, 1, 1, 1), scale = new float2(2, 1) };

        CheckerShader.Varyings v = CheckerShader.Vertex(new float2(0.25f, -1), new float2(0.05f, 0.25f));
        Near(v.position, new float4(0.5f, -1, 0.5f, 1), "vertex position");
        Discarded = false;
        Near(CheckerShader.Fragment(v), new float4(0.5f, 0, 0, 1), "fragment in the first stripe keeps the texel");
        v.uv = new float2(0.2f, 0.25f);
        Near(CheckerShader.Fragment(v), new float4(0, 0, 1, 1), "fragment in the second stripe swaps red and blue");
        Check(!Discarded, "opaque texels are kept");
        v.uv = new float2(0.75f, 0.75f);
        CheckerShader.Fragment(v);
        Check(Discarded, "transparent texel is discarded");

        float4 bilinear = CheckerShader.albedo.Sample(new SamplerState { Filter = SamplerFilter.Linear, Address = SamplerAddress.Clamp }, new float2(0.5f, 0.25f));
        Near(bilinear, new float4(0.5f, 0.5f, 0, 1), "bilinear sample between two texels");
        Check(CheckerShader.albedo.Size.Equals(new int2(2, 2)), "texture size");

        var buffer = RWStructuredBuffer<uint>.FromArray(new uint[4]);
        for (int i = 0; i < buffer.Length; i++)
            buffer[i] = (uint)(i * i);
        Check(buffer[3] == 9u, "storage buffer write and read");

        float3x3 columns = float3x3.Identity;
        columns[2] = new float3(4, 5, 6);
        Check(columns[2].Equals(new float3(4, 5, 6)) && columns[0].Equals(new float3(1, 0, 0)), "matrix columns by index");

        Bindless.Clear();
        BindlessTexture red = Bindless.Register(Texture2D.FromPixels(new float4[] { new(1, 0, 0, 1) }, 1, 1), default);
        BindlessTexture checker = Bindless.Register(CheckerShader.albedo, CheckerShader.albedoSampler);
        Check(red.Index == 0u && checker.Index == 1u, "bindless texture indices follow registration");
        Near(checker.Sample(new float2(0.75f, 0.25f)), new float4(0, 1, 0, 1), "bindless texture samples with its sampler");
        BindlessBuffer words = Bindless.Register(new uint[] { 1, AsUInt(2.5f), 3, 4 });
        Check(words.Size == 16u && words.Load(4) == AsUInt(2.5f) && AsFloat(words.Load2(4)).x == 2.5f, "bindless buffer loads at byte offsets");
        words.Store2(8, new uint2(7, 8));
        Check(words.Load4(0).Equals(new uint4(1, AsUInt(2.5f), 7, 8)), "bindless buffer stores");
        Check(AsInt(AsFloat(-3)) == -3 && AsUInt(-1) == 0xFFFFFFFFu, "bit casts keep the bits");
        Bindless.Clear();
    }

    private static int Main()
    {
        (string, Action)[] tests = { ("Vectors", Vectors), ("Functions", Functions), ("Matrices", Matrices), ("ShaderOnCpu", ShaderOnCpu) };
        foreach ((string name, Action run) in tests)
        {
            int before = failures;
            run();
            Console.WriteLine((failures == before ? "[  OK  ] " : "[ FAIL ] ") + name);
        }
        Console.WriteLine($"{checks} checks, {failures} failed");
        return failures == 0 ? 0 : 1;
    }
}

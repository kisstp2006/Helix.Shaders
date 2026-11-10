# Helix.Shaders

The shader language of the Helix engine: shaders are written in C#. This library (`HelixEngine.Shaders`) holds the
types, functions and attributes shader code uses. The same code runs on the CPU as ordinary C#, which makes shaders
testable and debuggable, and is compiled for the GPU from its IL by Helix.ShaderCompiler.

## Layout

| Path | Content |
|---|---|
| `Source/HelixEngine.Shaders/Types/` | Vectors `float2..4`, `int2..4`, `uint2..4`, `bool2..4` with swizzles (generated), matrices `float2x2`, `float3x3`, `float4x4` |
| `Source/HelixEngine.Shaders/Math/` | `ShaderMath`: component wise functions (generated) and geometric, matrix and derivative functions |
| `Source/HelixEngine.Shaders/Resources/` | `Texture2D`, `SamplerState`, `StructuredBuffer<T>`, `RWStructuredBuffer<T>` with CPU implementations |
| `Source/HelixEngine.Shaders/Attributes/` | Entry point, stage input and output, resource binding and shader definition attributes |
| `Tests/HelixEngine.Shaders.Tests/` | Console test program, including a sample shader run on the CPU |
| `Tools/GenerateTypes.py` | Generates the vector types and the component wise functions |

## A shader

```csharp
using HelixEngine.Shaders;
using static HelixEngine.Shaders.ShaderMath;

public static class Unlit
{
    public struct Draw { public float4x4 modelViewProjection; public float4 tint; }
    public struct Varyings { [Position] public float4 position; [Location(0)] public float2 uv; }

    [Uniform(0)] public static Draw draw;
    [Texture(0)] public static Texture2D albedo;
    [Sampler(0)] public static SamplerState albedoSampler;

    [VertexShader]
    public static Varyings Vertex([Location(0)] float3 position, [Location(1)] float2 uv)
    {
        Varyings o;
        o.position = Mul(draw.modelViewProjection, new float4(position, 1.0f));
        o.uv = uv;
        return o;
    }

    [FragmentShader]
    [return: Target(0)]
    public static float4 Fragment(Varyings input) => albedo.Sample(albedoSampler, input.uv) * draw.tint;
}
```

- Entry points are static methods marked `[VertexShader]`, `[FragmentShader]` or `[ComputeShader(x, y, z)]`.
- Stage inputs and outputs are parameters, return values or struct fields with `[Position]`, `[Location(n)]`, `[Target(n)]`,
  `[Depth]`, `[VertexId]`, `[InstanceId]`, `[FrontFacing]`, `[DispatchThreadId]`.
- Resources are static fields with `[Uniform(slot)]`, `[Texture(slot)]`, `[Sampler(slot)]`, `[Storage(slot)]`.
- Matrices are column major: `Mul(m, v)` multiplies the column vector `v`, `Mul(a, b)` applies `b` first.
- Comparisons of vectors give bool vectors; combine them with `Any`, `All` and `Select`.
- `Ddx`, `Ddy` and `Fwidth` are zero on the CPU; `Discard()` sets `ShaderMath.Discarded` there.

Shader code may use structs, static methods, generics, loops, `switch`, `ref`/`out`/`in` and local functions. Classes, heap
allocation, virtual calls, recursion, strings and exceptions are not available in code that is compiled for the GPU.

## Shader definitions

A class derived from `ShaderDefinition` describes one shader with everything around the code:

```csharp
[Shader("Game/Glass", Fallback = "Game/Unlit")]
[Tag("Queue", "Transparent")]
public sealed class Glass : ShaderDefinition
{
    [Property("Tint"), MainColor, Default(1.0f, 1.0f, 1.0f, 0.5f)] public float4 tint;
    [Property("Base Map"), MainTexture, DefaultTexture("white")] public Texture2D baseMap;
    [Property("Smoothness"), Range(0.0f, 1.0f), Default(0.5f)] public float smoothness;

    [Keyword(KeywordScope.Global)] public static readonly bool Fog;
    [Keyword(ShaderFeature = true)] public static readonly Quality Detail;

    [Pass("Forward", LightMode = "ForwardBase", ZWrite = false)]
    [Blend(BlendFactor.SrcAlpha, BlendFactor.OneMinusSrcAlpha)]
    public static class Forward
    {
        [VertexShader] public static Varyings Vertex(...) { ... }
        [FragmentShader] public static float4 Fragment(...) { ... }
    }
}
```

- Shader: `[Shader(name)]` with `Fallback`, `CustomEditor`, `LOD`; `[Tag(name, value)]` on the shader or on a pass.
- Properties are the instance fields: `[Property(display name)]` with `[Range]`, `[Default]`, `[DefaultTexture]`, `[HDR]`,
  `[NoScaleOffset]`, `[MainTexture]`, `[MainColor]`, `[PerRendererData]`, `[HideInInspector]`, `[Toggle(keyword)]`,
  `[KeywordEnum(keyword)]`.
- Keywords are static `bool` or enum fields with `[Keyword]`; the compiler builds one variant per combination in use.
- Passes are nested static classes with `[Pass(name)]` in the order written: `LightMode`, `Cull`, `ZWrite`, `ZTest`,
  `ZClip`, `OffsetFactor`, `OffsetUnits`, `ColorMask`, `AlphaToMask`, `Conservative`, plus `[Blend(...)]` (per target with
  `Target`) and `[Stencil(...)]` (per face with `Face`).

## Build and test

```
git clone https://github.com/kisstp2006/Helix.Shaders
dotnet run --project Helix.Shaders/Tests/HelixEngine.Shaders.Tests -c Release
```

Requires the .NET 11 SDK. No packages are used.

## License

GPL-3.0, see `LICENSE`.

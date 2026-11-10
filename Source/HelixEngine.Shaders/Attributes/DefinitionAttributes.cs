using System;

namespace HelixEngine.Shaders;

// Shader definition: the declarative part around the code. A class derived from ShaderDefinition is one shader; its
// instance fields are the material properties, its static keyword fields select variants and its nested static classes
// marked [Pass] hold the entry points of the passes, in the order they are written.

[AttributeUsage(AttributeTargets.Class)]
public sealed class ShaderAttribute(string name) : Attribute
{
    public string Name { get; } = name;
    /// <summary>Name of the shader used when no pass of this one can run.</summary>
    public string Fallback { get; set; } = "";
    public string CustomEditor { get; set; } = "";
    public int LOD { get; set; }
}

/// <summary>A name and value pair on a shader or a pass, such as Queue or RenderType.</summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
public sealed class TagAttribute(string name, string value) : Attribute
{
    public string Name { get; } = name;
    public string Value { get; } = value;
}

public enum CullMode
{
    Off,
    Front,
    Back,
}

public enum CompareFunction
{
    Never,
    Less,
    Equal,
    LessEqual,
    Greater,
    NotEqual,
    GreaterEqual,
    Always,
}

public enum BlendFactor
{
    Zero,
    One,
    SrcColor,
    OneMinusSrcColor,
    DstColor,
    OneMinusDstColor,
    SrcAlpha,
    OneMinusSrcAlpha,
    DstAlpha,
    OneMinusDstAlpha,
    SrcAlphaSaturate,
}

public enum BlendOperation
{
    Add,
    Subtract,
    ReverseSubtract,
    Min,
    Max,
}

[Flags]
public enum ColorWriteMask
{
    None = 0,
    Red = 1,
    Green = 2,
    Blue = 4,
    Alpha = 8,
    All = 15,
}

public enum StencilOperation
{
    Keep,
    Zero,
    Replace,
    IncrementSaturate,
    DecrementSaturate,
    Invert,
    IncrementWrap,
    DecrementWrap,
}

public enum StencilFace
{
    FrontAndBack,
    Front,
    Back,
}

[AttributeUsage(AttributeTargets.Class)]
public sealed class PassAttribute(string name) : Attribute
{
    public string Name { get; } = name;
    public string LightMode { get; set; } = "";
    public CullMode Cull { get; set; } = CullMode.Back;
    public bool ZWrite { get; set; } = true;
    public CompareFunction ZTest { get; set; } = CompareFunction.LessEqual;
    public bool ZClip { get; set; } = true;
    public float OffsetFactor { get; set; }
    public float OffsetUnits { get; set; }
    public ColorWriteMask ColorMask { get; set; } = ColorWriteMask.All;
    public bool AlphaToMask { get; set; }
    public bool Conservative { get; set; }
}

/// <summary>
/// A group of passes in a nested static class: the renderer uses the first SubShader whose LOD is within its limit. Its
/// render state, [Tag], [Blend] and [Stencil] apply to its passes unless a pass sets its own. Without SubShader classes the
/// passes of a definition form one SubShader with the LOD and tags of the [Shader].
/// </summary>
[AttributeUsage(AttributeTargets.Class)]
public sealed class SubShaderAttribute : Attribute
{
    public int LOD { get; set; }
    public CullMode Cull { get; set; } = CullMode.Back;
    public bool ZWrite { get; set; } = true;
    public CompareFunction ZTest { get; set; } = CompareFunction.LessEqual;
    public bool ZClip { get; set; } = true;
    public float OffsetFactor { get; set; }
    public float OffsetUnits { get; set; }
    public ColorWriteMask ColorMask { get; set; } = ColorWriteMask.All;
    public bool AlphaToMask { get; set; }
    public bool Conservative { get; set; }
}

/// <summary>
/// An empty nested static class that stands for a pass of another shader, "Shader/PASS": its code, render state and tags.
/// The pass name is matched without case; the first SubShader of that shader with such a pass gives it.
/// </summary>
[AttributeUsage(AttributeTargets.Class)]
public sealed class UsePassAttribute(string path) : Attribute
{
    public string Path { get; } = path;
}

/// <summary>Blending of a pass; without it the pass overwrites the target. Target -1 applies to every target.</summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
public sealed class BlendAttribute(BlendFactor source, BlendFactor destination, BlendFactor sourceAlpha, BlendFactor destinationAlpha) : Attribute
{
    public BlendAttribute(BlendFactor source, BlendFactor destination) : this(source, destination, source, destination) { }

    public BlendFactor Source { get; } = source;
    public BlendFactor Destination { get; } = destination;
    public BlendFactor SourceAlpha { get; } = sourceAlpha;
    public BlendFactor DestinationAlpha { get; } = destinationAlpha;
    public BlendOperation Operation { get; set; } = BlendOperation.Add;
    public BlendOperation OperationAlpha { get; set; } = BlendOperation.Add;
    public int Target { get; set; } = -1;
}

[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
public sealed class StencilAttribute : Attribute
{
    public StencilFace Face { get; set; } = StencilFace.FrontAndBack;
    public int Reference { get; set; }
    public int ReadMask { get; set; } = 255;
    public int WriteMask { get; set; } = 255;
    public CompareFunction Compare { get; set; } = CompareFunction.Always;
    public StencilOperation Pass { get; set; } = StencilOperation.Keep;
    public StencilOperation Fail { get; set; } = StencilOperation.Keep;
    public StencilOperation ZFail { get; set; } = StencilOperation.Keep;
}

// Material properties: instance fields of the definition class.

[AttributeUsage(AttributeTargets.Field)]
public sealed class PropertyAttribute(string displayName) : Attribute
{
    public string DisplayName { get; } = displayName;
}

[AttributeUsage(AttributeTargets.Field)]
public sealed class RangeAttribute(float min, float max) : Attribute
{
    public float Min { get; } = min;
    public float Max { get; } = max;
}

/// <summary>Value of a numeric property in a new material.</summary>
[AttributeUsage(AttributeTargets.Field)]
public sealed class DefaultAttribute(float x, float y = 0.0f, float z = 0.0f, float w = 0.0f) : Attribute
{
    public float X { get; } = x;
    public float Y { get; } = y;
    public float Z { get; } = z;
    public float W { get; } = w;
}

/// <summary>Built in texture of a texture property in a new material: white, black, gray, bump or red.</summary>
[AttributeUsage(AttributeTargets.Field)]
public sealed class DefaultTextureAttribute(string name) : Attribute
{
    public string Name { get; } = name;
}

[AttributeUsage(AttributeTargets.Field)]
public sealed class HDRAttribute : Attribute;

[AttributeUsage(AttributeTargets.Field)]
public sealed class NoScaleOffsetAttribute : Attribute;

[AttributeUsage(AttributeTargets.Field)]
public sealed class MainTextureAttribute : Attribute;

[AttributeUsage(AttributeTargets.Field)]
public sealed class MainColorAttribute : Attribute;

[AttributeUsage(AttributeTargets.Field)]
public sealed class PerRendererDataAttribute : Attribute;

[AttributeUsage(AttributeTargets.Field)]
public sealed class HideInInspectorAttribute : Attribute;

/// <summary>Shown as a checkbox; a non zero value turns the named keyword on.</summary>
[AttributeUsage(AttributeTargets.Field)]
public sealed class ToggleAttribute(string keyword = "") : Attribute
{
    public string Keyword { get; } = keyword;
}

/// <summary>Shown as a list of the values of the named enum keyword; the value selects the keyword value.</summary>
[AttributeUsage(AttributeTargets.Field)]
public sealed class KeywordEnumAttribute(string keyword) : Attribute
{
    public string Keyword { get; } = keyword;
}

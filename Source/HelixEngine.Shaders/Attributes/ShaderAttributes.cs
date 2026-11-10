using System;

namespace HelixEngine.Shaders;

/// <summary>Maps a method to an operation of the shader compiler; the C# body is only used on the CPU.</summary>
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Constructor | AttributeTargets.Property)]
public sealed class IntrinsicAttribute(string name) : Attribute
{
    public string Name { get; } = name;
}

/// <summary>Marks a type the shader compiler knows natively (vectors, matrices, resources).</summary>
[AttributeUsage(AttributeTargets.Struct)]
public sealed class IntrinsicTypeAttribute(string name) : Attribute
{
    public string Name { get; } = name;
}

// Entry points.
[AttributeUsage(AttributeTargets.Method)]
public sealed class VertexShaderAttribute : Attribute;

[AttributeUsage(AttributeTargets.Method)]
public sealed class FragmentShaderAttribute : Attribute;

[AttributeUsage(AttributeTargets.Method)]
public sealed class ComputeShaderAttribute(int x, int y = 1, int z = 1) : Attribute
{
    public int X { get; } = x;
    public int Y { get; } = y;
    public int Z { get; } = z;
}

// Stage inputs and outputs: on fields of the input and output structs, on parameters and on return values.
[AttributeUsage(AttributeTargets.Field | AttributeTargets.Parameter | AttributeTargets.ReturnValue)]
public sealed class PositionAttribute : Attribute;

[AttributeUsage(AttributeTargets.Field | AttributeTargets.Parameter | AttributeTargets.ReturnValue)]
public sealed class LocationAttribute(int index) : Attribute
{
    public int Index { get; } = index;
}

[AttributeUsage(AttributeTargets.Field | AttributeTargets.Parameter | AttributeTargets.ReturnValue)]
public sealed class TargetAttribute(int index = 0) : Attribute
{
    public int Index { get; } = index;
}

[AttributeUsage(AttributeTargets.Field | AttributeTargets.Parameter | AttributeTargets.ReturnValue)]
public sealed class DepthAttribute : Attribute;

[AttributeUsage(AttributeTargets.Field | AttributeTargets.Parameter)]
public sealed class VertexIdAttribute : Attribute;

[AttributeUsage(AttributeTargets.Field | AttributeTargets.Parameter)]
public sealed class InstanceIdAttribute : Attribute;

[AttributeUsage(AttributeTargets.Field | AttributeTargets.Parameter)]
public sealed class FrontFacingAttribute : Attribute;

[AttributeUsage(AttributeTargets.Field | AttributeTargets.Parameter)]
public sealed class DispatchThreadIdAttribute : Attribute;

public enum InterpolationMode
{
    Linear,
    Flat,
    NoPerspective,
}

[AttributeUsage(AttributeTargets.Field | AttributeTargets.Parameter)]
public sealed class InterpolationAttribute(InterpolationMode mode) : Attribute
{
    public InterpolationMode Mode { get; } = mode;
}

// Resources: static fields of the class that holds the entry points. Slots are the RHI binding slots.
[AttributeUsage(AttributeTargets.Field)]
public sealed class UniformAttribute(int slot) : Attribute
{
    public int Slot { get; } = slot;
}

[AttributeUsage(AttributeTargets.Field)]
public sealed class TextureAttribute(int slot) : Attribute
{
    public int Slot { get; } = slot;
}

[AttributeUsage(AttributeTargets.Field)]
public sealed class SamplerAttribute(int slot) : Attribute
{
    public int Slot { get; } = slot;
}

[AttributeUsage(AttributeTargets.Field)]
public sealed class StorageAttribute(int slot) : Attribute
{
    public int Slot { get; } = slot;
}

public enum KeywordScope
{
    Global,
    Local,
}

/// <summary>A static readonly bool or enum field; the compiler treats it as a constant of the variant.</summary>
[AttributeUsage(AttributeTargets.Field)]
public sealed class KeywordAttribute(KeywordScope scope = KeywordScope.Local) : Attribute
{
    public KeywordScope Scope { get; } = scope;
    public bool ShaderFeature { get; set; }
    public bool Dynamic { get; set; }
}

/// <summary>Base class of shader definitions; properties, keywords and pass classes live in the derived class.</summary>
public abstract class ShaderDefinition;

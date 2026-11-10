namespace HelixEngine.Shaders;

/// <summary>
/// A float that needs only 16 bits of precision: 16 bits in C# memory, computed as float on the GPU (lower precision where
/// the GPU offers it). For locals, parameters and values between stages; uniform blocks, buffers and vertex inputs take float.
/// </summary>
[IntrinsicType("half")]
public struct half
{
    private System.Half value;

    public half(float value) => this.value = (System.Half)value;

    public static implicit operator float(half h) => (float)h.value;
    public static explicit operator half(float f) => new(f);
    public static explicit operator half(int i) => new(i);
    public static explicit operator half(uint i) => new(i);
    public static explicit operator int(half h) => (int)(float)h.value;
    public static explicit operator uint(half h) => (uint)(float)h.value;

    public static half operator +(half a, half b) => new((float)a + b);
    public static half operator -(half a, half b) => new((float)a - b);
    public static half operator *(half a, half b) => new((float)a * b);
    public static half operator /(half a, half b) => new((float)a / b);
    public static half operator %(half a, half b) => new((float)a % b);
    public static half operator -(half a) => new(-(float)a);
    public static bool operator <(half a, half b) => (float)a < b;
    public static bool operator >(half a, half b) => (float)a > b;
    public static bool operator <=(half a, half b) => (float)a <= b;
    public static bool operator >=(half a, half b) => (float)a >= b;
    public static bool operator ==(half a, half b) => (float)a == b;
    public static bool operator !=(half a, half b) => (float)a != b;

    public override readonly bool Equals(object? obj) => obj is half h && h == this;
    public override readonly int GetHashCode() => value.GetHashCode();
    public override readonly string ToString() => ((float)value).ToString(System.Globalization.CultureInfo.InvariantCulture);
}

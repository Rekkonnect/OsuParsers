namespace System.Runtime.CompilerServices;

[AttributeUsage(AttributeTargets.Struct, AllowMultiple = false)]
public sealed class InlineArrayAttribute : Attribute
{
    public InlineArrayAttribute(int length)
    {
        Length = length;
    }

    public int Length { get; }
}

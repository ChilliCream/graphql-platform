namespace StrawberryShake.CodeGeneration.Descriptors;

public sealed class EnumValueDescriptor : ICodeDescriptor
{
    public EnumValueDescriptor(
        string runtimeValue,
        string name,
        string? documentation,
        long? value = null)
    {
        RuntimeValue = runtimeValue;
        Name = name;
        Documentation = documentation;
        Value = value;
    }

    public string RuntimeValue { get; }

    public  string Name { get; }

    public string? Documentation { get; }

    public long? Value { get; }
}

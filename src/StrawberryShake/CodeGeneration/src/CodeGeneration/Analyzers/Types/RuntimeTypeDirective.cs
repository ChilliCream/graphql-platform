namespace StrawberryShake.CodeGeneration.Analyzers.Types;

public class RuntimeTypeDirective
{
    public RuntimeTypeDirective(string name, bool? isValueType)
    {
        Name = name;
        ValueType = isValueType;
    }

    public string Name { get; }

    public bool? ValueType { get; }
}

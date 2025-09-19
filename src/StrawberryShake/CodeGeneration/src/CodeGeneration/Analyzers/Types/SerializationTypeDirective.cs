namespace StrawberryShake.CodeGeneration.Analyzers.Types;

public class SerializationTypeDirective
{
    public SerializationTypeDirective(string name, bool? isValueType)
    {
        Name = name;
        ValueType = isValueType;
    }

    public string Name { get; }

    public bool? ValueType { get; }
}

namespace StrawberryShake.CodeGeneration.Analyzers.Types;

public class EnumValueDirective
{
    public EnumValueDirective(string value)
    {
        Value = value;
    }

    public string Value { get; }
}

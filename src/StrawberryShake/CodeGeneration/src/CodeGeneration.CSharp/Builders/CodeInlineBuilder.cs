namespace StrawberryShake.CodeGeneration.CSharp.Builders;

public class CodeInlineBuilder : ICode
{
    private string? _value;

    public static CodeInlineBuilder New() => new();

    public static CodeInlineBuilder From(string sourceText) =>
        New().SetText(sourceText);

    public CodeInlineBuilder SetText(string value)
    {
        _value = value;
        return this;
    }

    public void Build(CodeWriter writer)
    {
        if (writer is null)
        {
            throw new ArgumentNullException(nameof(writer));
        }

        if (_value is null)
        {
            return;
        }

        writer.Write(_value);
    }
}

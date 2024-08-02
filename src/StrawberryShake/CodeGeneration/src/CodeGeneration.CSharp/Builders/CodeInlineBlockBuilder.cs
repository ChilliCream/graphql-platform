namespace StrawberryShake.CodeGeneration.CSharp.Builders;

public class CodeInlineBlockBuilder : ICode
{
    private readonly List<ICode> _lineParts = [];

    public static CodeInlineBlockBuilder New() => new();

    public CodeInlineBlockBuilder AddCode(ICode value)
    {
        if (value is null)
        {
            throw new ArgumentNullException(nameof(value));
        }

        _lineParts.Add(value);
        return this;
    }

    public void Build(CodeWriter writer)
    {
        if (writer is null)
        {
            throw new ArgumentNullException(nameof(writer));
        }

        foreach (var code in _lineParts)
        {
            code.Build(writer);
        }
    }
}

namespace StrawberryShake.CodeGeneration.CSharp.Builders;

public class CodeInlineBlockBuilder : ICode
{
    private readonly List<ICode> _lineParts = [];

    public static CodeInlineBlockBuilder New() => new();

    public CodeInlineBlockBuilder AddCode(ICode value)
    {
        ArgumentNullException.ThrowIfNull(value);

        _lineParts.Add(value);
        return this;
    }

    public void Build(CodeWriter writer)
    {
        ArgumentNullException.ThrowIfNull(writer);

        foreach (var code in _lineParts)
        {
            code.Build(writer);
        }
    }
}

using System.Text;

namespace StrawberryShake.CodeGeneration.CSharp.Builders;

public class CodeBlockBuilder : ICode
{
    private readonly List<ICodeBuilder> _blockParts = [];

    public static CodeBlockBuilder New() => new CodeBlockBuilder();

    public static CodeBlockBuilder From(StringBuilder sourceText)
    {
        var builder = New();

        using var stringReader = new StringReader(sourceText.ToString());

        string? line = null;

        do
        {
            line = stringReader.ReadLine();

            if (line is not null)
            {
                builder.AddCode(CodeLineBuilder.From(line));
            }
        } while (line is not null);

        return builder;
    }

    public CodeBlockBuilder AddCode(ICodeBuilder value)
    {
        ArgumentNullException.ThrowIfNull(value);

        _blockParts.Add(value);
        return this;
    }

    public CodeBlockBuilder AddCode(string value)
    {
        ArgumentNullException.ThrowIfNull(value);

        _blockParts.Add(CodeInlineBuilder.New().SetText(value));
        return this;
    }

    public CodeBlockBuilder AddEmptyLine()
    {
        _blockParts.Add(CodeLineBuilder.New());
        return this;
    }

    public void Build(CodeWriter writer)
    {
        ArgumentNullException.ThrowIfNull(writer);

        foreach (var code in _blockParts)
        {
            code.Build(writer);
        }
    }
}

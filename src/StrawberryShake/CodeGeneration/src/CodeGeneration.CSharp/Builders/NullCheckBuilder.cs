namespace StrawberryShake.CodeGeneration.CSharp.Builders;

public class NullCheckBuilder : ICode
{
    private ICode? _condition;
    private ICode? _code;
    private bool _determineStatement = true;
    private bool _singleLine;

    public NullCheckBuilder SetCondition(ICode condition)
    {
        _condition = condition;
        return this;
    }

    public NullCheckBuilder SetCondition(string condition)
    {
        _condition = CodeInlineBuilder.From(condition);
        return this;
    }

    public NullCheckBuilder SetCode(ICode code)
    {
        _code = code;
        return this;
    }

    public NullCheckBuilder SetCode(string code)
    {
        _code = CodeInlineBuilder.From(code);
        return this;
    }

    public NullCheckBuilder SetDetermineStatement(bool value)
    {
        _determineStatement = value;
        return this;
    }

    public NullCheckBuilder SetSingleLine(bool value = true)
    {
        _singleLine = value;
        return this;
    }

    public void Build(CodeWriter writer)
    {
        if (_condition is null)
        {
            throw new ArgumentNullException(nameof(_condition));
        }

        if (_code is null)
        {
            throw new ArgumentNullException(nameof(_code));
        }

        _condition.Build(writer);

        if (!_singleLine)
        {
            writer.WriteLine();
        }

        using (writer.IncreaseIndent())
        {
            if (!_singleLine)
            {
                writer.WriteIndent();
            }
            else
            {
                writer.Write(" ");
            }

            writer.Write("?? ");
            _code.Build(writer);
        }

        if (_determineStatement)
        {
            writer.Write(";");
            writer.WriteLine();
        }
    }

    public static NullCheckBuilder New() => new();

    public static NullCheckBuilder Inline() => New()
        .SetDetermineStatement(false)
        .SetSingleLine();
}

namespace StrawberryShake.CodeGeneration.CSharp.Builders;

public class IfBuilder : ICodeContainer<IfBuilder>
{
    private readonly List<ICode> _lines = [];
    private ConditionBuilder? _condition;

    private readonly List<IfBuilder> _ifElses = [];
    private ICode? _elseCode;
    private bool _writeIndents = true;

    public static IfBuilder New() => new();

    public IfBuilder SetCondition(ConditionBuilder condition)
    {
        _condition = condition;
        return this;
    }

    public IfBuilder SkipIndents()
    {
        _writeIndents = false;
        return this;
    }

    public IfBuilder SetCondition(string condition)
    {
        _condition = ConditionBuilder.New().Set(condition);
        return this;
    }

    public IfBuilder SetCondition(ICode condition)
    {
        _condition = ConditionBuilder.New().Set(condition);
        return this;
    }

    public IfBuilder AddCode(string code, bool addIf = true)
    {
        if (addIf)
        {
            _lines.Add(CodeLineBuilder.New().SetLine(code));
        }

        return this;
    }

    public IfBuilder AddCode(ICode code, bool addIf = true)
    {
        if (addIf)
        {
            _lines.Add(code);
        }

        return this;
    }

    public IfBuilder AddEmptyLine()
    {
        _lines.Add(CodeLineBuilder.New());
        return this;
    }

    public IfBuilder AddIfElse(IfBuilder singleIf)
    {
        _ifElses.Add(singleIf);
        return this;
    }

    public IfBuilder AddElse(ICode code)
    {
        _elseCode = code;
        return this;
    }

    public void Build(CodeWriter writer)
    {
        if (_condition is null)
        {
            throw new ArgumentNullException(nameof(_condition));
        }

        if (_writeIndents)
        {
            writer.WriteIndent();
        }

        writer.Write("if (");
        _condition.Build(writer);
        writer.Write(")");
        writer.WriteLine();
        writer.WriteIndentedLine("{");

        using (writer.IncreaseIndent())
        {
            foreach (var code in _lines)
            {
                code.Build(writer);
            }
        }

        writer.WriteIndent();
        writer.Write("}");
        writer.WriteLine();

        foreach (var ifBuilder in _ifElses)
        {
            writer.WriteIndent();
            writer.Write("else ");
            ifBuilder.Build(writer);
        }

        if (_elseCode is not null)
        {
            writer.WriteIndentedLine("else");
            writer.WriteIndentedLine("{");
            using (writer.IncreaseIndent())
            {
                _elseCode.Build(writer);
            }

            writer.WriteIndentedLine("}");
        }
    }
}

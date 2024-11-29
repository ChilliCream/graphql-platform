namespace StrawberryShake.CodeGeneration.CSharp.Builders;

public class SwitchExpressionBuilder : ICode
{
    private readonly List<(ICode, ICode)> _cases = [];
    private string? _expression;
    private bool _determineStatement = true;
    private bool _setReturn;
    private string? _prefix;
    private ICode? _defaultCase;

    public static SwitchExpressionBuilder New() => new();

    public SwitchExpressionBuilder SetPrefix(string prefix)
    {
        _prefix = prefix;
        return this;
    }

    public SwitchExpressionBuilder SetReturn(bool value = true)
    {
        _setReturn = value;
        return this;
    }

    public SwitchExpressionBuilder SetExpression(string expression)
    {
        _expression = expression;
        return this;
    }

    public SwitchExpressionBuilder AddCase(ICode type, ICode action)
    {
        _cases.Add((type, action));
        return this;
    }

    public SwitchExpressionBuilder AddCase(string type, ICode action)
    {
        _cases.Add((CodeInlineBuilder.From(type), action));
        return this;
    }

    public SwitchExpressionBuilder AddCase(string type, string action)
    {
        return AddCase(CodeInlineBuilder.From(type), CodeInlineBuilder.From(action));
    }

    public SwitchExpressionBuilder SetDefaultCase(string action)
    {
        _defaultCase = CodeInlineBuilder.From(action);
        return this;
    }

    public SwitchExpressionBuilder SetDefaultCase(ICode action)
    {
        _defaultCase = action;
        return this;
    }

    public SwitchExpressionBuilder SetDetermineStatement(bool value)
    {
        _determineStatement = value;
        return this;
    }

    public void Build(CodeWriter writer)
    {
        if (writer is null)
        {
            throw new ArgumentNullException(nameof(writer));
        }

        if (_determineStatement)
        {
            writer.WriteIndent();
        }

        if (_setReturn)
        {
            writer.Write("return ");
        }

        writer.Write(_prefix);

        writer.Write(_expression);

        writer.Write(" switch");
        writer.WriteLine();

        writer.WriteIndentedLine("{");

        using (writer.IncreaseIndent())
        {
            for (var i = 0; i < _cases.Count; i++)
            {
                var (type, action) = _cases[i];

                writer.WriteIndent();
                type.Build(writer);
                writer.Write(" => ");
                action.Build(writer);

                if (i < _cases.Count - 1 || _defaultCase is not null)
                {
                    writer.Write(",");
                }

                writer.WriteLine();
            }

            if (_defaultCase is not null)
            {
                writer.WriteIndent();
                writer.Write("_ => ");
                _defaultCase.Build(writer);
                writer.WriteLine();
            }
        }

        writer.WriteIndent();
        writer.Write("}");

        if (_determineStatement)
        {
            writer.Write(";");
            writer.WriteLine();
        }
    }
}

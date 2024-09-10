namespace StrawberryShake.CodeGeneration.CSharp.Builders;

public class ConditionBuilder : ICode
{
    private readonly List<ICode> _conditions = [];
    private bool _setReturn;
    private bool _determineStatement;
    public static ConditionBuilder New() => new();

    public ConditionBuilder Set(string condition)
    {
        _conditions.Add(CodeInlineBuilder.New().SetText(condition));
        return this;
    }

    public ConditionBuilder SetReturn(bool value = true)
    {
        _setReturn = value;
        return this;
    }

    public ConditionBuilder Set(ICode condition)
    {
        _conditions.Add(condition);
        return this;
    }

    public ConditionBuilder SetDetermineStatement(bool value = true)
    {
        _determineStatement = value;
        return this;
    }

    public ConditionBuilder And(string condition, bool applyIf = true)
    {
        return applyIf ? And(CodeInlineBuilder.New().SetText(condition)) : this;
    }

    public ConditionBuilder And(ICode condition)
    {
        if (_conditions.Count == 0)
        {
            return Set(condition);
        }

        _conditions.Add(
            CodeBlockBuilder.New()
                .AddCode(CodeInlineBuilder.New().SetText("&& "))
                .AddCode(condition));
        return this;
    }

    public ConditionBuilder Or(ICode condition)
    {
        if (_conditions.Count == 0)
        {
            return Set(condition);
        }

        _conditions.Add(
            CodeBlockBuilder.New()
                .AddCode(CodeInlineBuilder.New().SetText("|| "))
                .AddCode(condition));
        return this;
    }

    public void Build(CodeWriter writer)
    {
        if (_determineStatement)
        {
            writer.WriteIndent();
        }

        if (_setReturn)
        {
            writer.Write("return ");
        }

        if (_conditions.Count != 0)
        {
            using (writer.IncreaseIndent())
            {
                WriteCondition(writer, _conditions[0]);
                for (var i = 1; i < _conditions.Count; i++)
                {
                    CodeLineBuilder.New().Build(writer);
                    writer.WriteIndent();
                    WriteCondition(writer, _conditions[i]);
                }
            }
        }

        if (_determineStatement)
        {
            writer.Write(";");
            writer.WriteLine();
        }
    }

    private void WriteCondition(CodeWriter writer, ICode condition)
    {
        if (condition is ConditionBuilder)
        {
            writer.Write("(");
            condition.Build(writer);
            writer.Write(")");
        }
        else
        {
            condition.Build(writer);
        }
    }
}

using System.Collections.Generic;

namespace StrawberryShake.CodeGeneration.CSharp.Builders
{
    public class ConditionBuilder : ICode
    {
        private readonly List<ICode> _conditions = new();
        public static ConditionBuilder New() => new();

        public ConditionBuilder Set(string condition)
        {
            _conditions.Add(CodeInlineBuilder.New().SetText(condition));
            return this;
        }

        public ConditionBuilder Set(ICode condition)
        {
            _conditions.Add(condition);
            return this;
        }

        public ConditionBuilder And(string condition, bool applyIf = true)
        {
            return applyIf ? And(CodeInlineBuilder.New().SetText(condition)) : this;
        }

        public ConditionBuilder And(ICode condition)
        {
            _conditions.Add(
                CodeBlockBuilder.New()
                    .AddCode(CodeInlineBuilder.New().SetText("&& "))
                    .AddCode(condition));
            return this;
        }

        public ConditionBuilder Or(ICode condition)
        {
            _conditions.Add(
                CodeBlockBuilder.New()
                    .AddCode(CodeInlineBuilder.New().SetText("|| "))
                    .AddCode(condition)
            );
            return this;
        }

        public void Build(CodeWriter writer)
        {
            if (_conditions.Count == 0)
            {
                return;
            }

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
}

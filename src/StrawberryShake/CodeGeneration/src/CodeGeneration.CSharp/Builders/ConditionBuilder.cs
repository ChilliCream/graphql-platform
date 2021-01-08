using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace StrawberryShake.CodeGeneration.CSharp.Builders
{
    public class ConditionBuilder : ICode
    {
        private IList<ICode> _conditions = new List<ICode>();
        public static ConditionBuilder New() => new ConditionBuilder();

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

        public ConditionBuilder And(string condition)
        {
            return And(CodeInlineBuilder.New().SetText(condition));
        }

        public ConditionBuilder And(ICode condition)
        {
            _conditions.Add(
                CodeBlockBuilder.New()
                    .AddCode(CodeInlineBuilder.New().SetText("&& "))
                    .AddCode(condition)
            );
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

        public async Task BuildAsync(CodeWriter writer)
        {
            if (_conditions.Count == 0)
            {
                return;
            }

            await WriteCondition(
                writer,
                _conditions[0]
            ).ConfigureAwait(false);

            for (int i = 1; i < _conditions.Count; i++)
            {
                using (writer.IncreaseIndent())
                {
                    await CodeLineBuilder.New().BuildAsync(writer).ConfigureAwait(false);
                    await writer.WriteIndentAsync().ConfigureAwait(false);
                    await WriteCondition(
                        writer,
                        _conditions[i]
                    ).ConfigureAwait(false);
                }
            }
        }

        private async Task WriteCondition(CodeWriter writer, ICode condition)
        {
            if (condition is ConditionBuilder)
            {
                await writer.WriteAsync("(").ConfigureAwait(false);
                await condition.BuildAsync(writer);
                await writer.WriteAsync(")").ConfigureAwait(false);
            }
            else
            {
                await condition.BuildAsync(writer);
            }
        }
    }
}

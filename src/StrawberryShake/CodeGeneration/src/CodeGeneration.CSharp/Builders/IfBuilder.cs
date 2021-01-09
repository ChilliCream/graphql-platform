using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace StrawberryShake.CodeGeneration.CSharp.Builders
{
    public class IfBuilder : ICode
    {
        private readonly List<ICode> _lines = new List<ICode>();
        private ConditionBuilder? _condition = null;

        public static IfBuilder New() => new IfBuilder();

        public IfBuilder SetCondition(ConditionBuilder condition)
        {
            _condition = condition;
            return this;
        }

        public IfBuilder SetCondition(string condition)
        {
            _condition = ConditionBuilder.New().Set(condition);
            return this;
        }

        public IfBuilder AddCode(string code)
        {
            _lines.Add(CodeLineBuilder.New().SetLine(code));
            return this;
        }

        public IfBuilder AddCode(ICode code)
        {
            _lines.Add(code);
            return this;
        }

        public async Task BuildAsync(CodeWriter writer)
        {
            if (_condition is null)
            {
                throw new ArgumentNullException(nameof(_condition));
            }

            await writer.WriteIndentAsync().ConfigureAwait(false);
            await writer.WriteAsync("if (").ConfigureAwait(false);
            await _condition.BuildAsync(writer).ConfigureAwait(false);
            await writer.WriteAsync(")").ConfigureAwait(false);
            await writer.WriteLineAsync().ConfigureAwait(false);
            await writer.WriteIndentedLineAsync("{").ConfigureAwait(false);

            using (writer.IncreaseIndent())
            {
                foreach (ICode code in _lines)
                {
                    await code.BuildAsync(writer).ConfigureAwait(false);
                }
            }

            await writer.WriteIndentedLineAsync("}").ConfigureAwait(false);
        }
    }
}

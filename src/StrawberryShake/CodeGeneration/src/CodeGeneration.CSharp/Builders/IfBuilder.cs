using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace StrawberryShake.CodeGeneration.CSharp.Builders
{
    public class IfBuilder : ICodeContainer<IfBuilder>
    {
        private readonly List<ICode> _lines = new List<ICode>();
        private ConditionBuilder? _condition = null;

        private readonly List<IfBuilder> _ifElses = new List<IfBuilder>();
        private ICode? _elseCode;
        private bool _writeIndents = true;

        public static IfBuilder New() => new IfBuilder();

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

        public IfBuilder AddCode(string code)
        {
            _lines.Add(CodeLineBuilder.New().SetLine(code));
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

        public IfBuilder AddCode(ICode code)
        {
            _lines.Add(code);
            return this;
        }


        public IfBuilder AddEmptyLine()
        {
            _lines.Add(CodeLineBuilder.New());
            return this;
        }

        public async Task BuildAsync(CodeWriter writer)
        {
            if (_condition is null)
            {
                throw new ArgumentNullException(nameof(_condition));
            }

            if (_writeIndents)
            {
                await writer.WriteIndentAsync().ConfigureAwait(false);
            }

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

            await writer.WriteIndentAsync();
            await writer.WriteAsync("}").ConfigureAwait(false);
            await writer.WriteLineAsync();

            foreach (IfBuilder ifBuilder in _ifElses)
            {
                await writer.WriteIndentAsync();
                await writer.WriteAsync("else ").ConfigureAwait(false);
                await ifBuilder.BuildAsync(writer).ConfigureAwait(false);
            }

            if (_elseCode is not null)
            {
                await writer.WriteIndentAsync();
                await writer.WriteAsync("else {");
                using (writer.IncreaseIndent())
                {
                    await writer.WriteLineAsync();
                    await writer.WriteIndentAsync();
                    await _elseCode.BuildAsync(writer).ConfigureAwait(false);
                }
                await writer.WriteLineAsync();
                await writer.WriteIndentAsync();
                await writer.WriteLineAsync("}");
            }
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
    }
}

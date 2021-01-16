using System.Collections.Generic;
using System.Threading.Tasks;

namespace StrawberryShake.CodeGeneration.CSharp.Builders
{
    public class ForEachBuilder : ICodeContainer<ForEachBuilder>
    {
        private string _loopHeader;
        private readonly List<ICode> _lines = new List<ICode>();

        public static ForEachBuilder New() => new();

        public ForEachBuilder AddCode(string code, bool addIfTrue = true)
        {
            AddCode(
                CodeLineBuilder.New().SetLine(code),
                addIfTrue
            );
            return this;
        }

        public ForEachBuilder AddCode(ICode code, bool addIf = true)
        {
            if (addIf)
            {
                _lines.Add(code);
            }

            return this;
        }

        public ForEachBuilder AddCode(string value)
        {
            _lines.Add(CodeLineBuilder.New().SetLine(value));
            return this;
        }
        public ForEachBuilder AddEmptyLine()
        {
            _lines.Add(CodeLineBuilder.New());
            return this;
        }

        public ForEachBuilder SetLoopHeader(string elementCode)
        {
            _loopHeader = elementCode;
            return this;
        }

        public async Task BuildAsync(CodeWriter writer)
        {
            await writer.WriteIndentAsync().ConfigureAwait(false);
            await writer.WriteAsync("foreach (").ConfigureAwait(false);
            await writer.WriteAsync(_loopHeader).ConfigureAwait(false);
            await writer.WriteAsync(")").ConfigureAwait(false);
            await writer.WriteLineAsync().ConfigureAwait(false);
            await writer.WriteIndentAsync().ConfigureAwait(false);
            await writer.WriteLineAsync("{");
            using (writer.IncreaseIndent())
            {
                foreach (ICode line in _lines)
                {
                    await line.BuildAsync(writer).ConfigureAwait(false);
                }
            }

            await writer.WriteIndentAsync().ConfigureAwait(false);
            await writer.WriteLineAsync("}");
        }
    }
}

using System;
using System.Threading.Tasks;

namespace StrawberryShake.CodeGeneration.CSharp.Builders
{
    public class CodeLineBuilder
        : ICode
    {
        private ICode? _value;

        public static CodeLineBuilder New() => new CodeLineBuilder();

        public CodeLineBuilder SetLine(string value)
        {
            _value = CodeInlineBuilder.New().SetText(value);
            return this;
        }

        public CodeLineBuilder SetLine(ICode value)
        {
            _value = value;
            return this;
        }

        public async Task BuildAsync(CodeWriter writer)
        {
            if (writer is null)
            {
                throw new ArgumentNullException(nameof(writer));
            }

            if (_value is not null)
            {
                await writer.WriteIndentAsync().ConfigureAwait(false);
                await _value.BuildAsync(writer).ConfigureAwait(false);
            }
            await writer.WriteLineAsync().ConfigureAwait(false);
        }
    }
}

using System;
using System.Threading.Tasks;

namespace StrawberryShake.CodeGeneration.CSharp.Builders
{
    public class CodeLineBuilder
        : ICode
    {
        private string? _value;

        public static CodeLineBuilder New() => new CodeLineBuilder();

        public CodeLineBuilder SetLine(string value)
        {
            _value = value;
            return this;
        }

        public Task BuildAsync(CodeWriter writer)
        {
            if (writer is null)
            {
                throw new ArgumentNullException(nameof(writer));
            }

            if (_value is null)
            {
                return Task.CompletedTask;
            }

            if (_value.Length == 0)
            {
                return writer.WriteLineAsync();
            }

            return writer.WriteIndentedLineAsync(_value);
        }
    }
}

using System.Threading.Tasks;
using StrawberryShake.Generators.Utilities;

namespace StrawberryShake.Generators.CSharp
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
            if (_value is null)
            {
                return Task.CompletedTask;
            }
            return writer.WriteIndentedLineAsync(_value);
        }
    }
}

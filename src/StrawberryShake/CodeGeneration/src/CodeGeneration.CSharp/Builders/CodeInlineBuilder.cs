using System;
using System.Threading.Tasks;

namespace StrawberryShake.CodeGeneration.CSharp.Builders
{
    public class CodeInlineBuilder
        : ICode
    {
        private string? _value;

        public static CodeInlineBuilder New() => new CodeInlineBuilder();

        public CodeInlineBuilder SetText(string value)
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

            return _value is null ? Task.CompletedTask : writer.WriteAsync(_value);
        }
    }
}

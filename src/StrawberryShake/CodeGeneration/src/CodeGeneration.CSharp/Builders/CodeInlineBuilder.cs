using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace StrawberryShake.CodeGeneration.CSharp.Builders
{
    public class CodeInlineBuilder : ICode
    {
        private string? _value;

        public static CodeInlineBuilder New() => new();

        public CodeInlineBuilder SetText(string value)
        {
            _value = value;
            return this;
        }

        public void Build(CodeWriter writer, HashSet<string>? builderContext = null)
        {
            if (writer is null)
            {
                throw new ArgumentNullException(nameof(writer));
            }

            if (_value is null)
            {
                return;
            }

            writer.Write(_value);
        }
    }
}

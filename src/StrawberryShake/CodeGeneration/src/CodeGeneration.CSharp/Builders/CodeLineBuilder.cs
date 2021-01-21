using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace StrawberryShake.CodeGeneration.CSharp.Builders
{
    public class CodeLineBuilder : ICode
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

        public void Build(CodeWriter writer, HashSet<string>? builderContext = null)
        {
            if (writer is null)
            {
                throw new ArgumentNullException(nameof(writer));
            }

            if (_value is not null)
            {
                writer.WriteIndent();
                _value.Build(writer);
            }

            writer.WriteLine();
        }
    }
}

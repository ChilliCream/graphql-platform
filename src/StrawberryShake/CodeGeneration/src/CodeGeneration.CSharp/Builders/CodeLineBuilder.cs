using System;

namespace StrawberryShake.CodeGeneration.CSharp.Builders
{
    public class CodeLineBuilder : ICode
    {
        private ICode? _value;
        private string? _sourceText;

        public static CodeLineBuilder New() => new CodeLineBuilder();

        public static CodeLineBuilder From(string line) => New().SetLine(line);

        public CodeLineBuilder SetLine(string value)
        {
            _sourceText = value;
            _value = null;
            return this;
        }

        public CodeLineBuilder SetLine(ICode value)
        {
            _value = value;
            _sourceText = null;
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
            else if (_sourceText is not null)
            {
                writer.WriteIndent();
                writer.Write(_sourceText);
            }

            writer.WriteLine();
        }
    }
}

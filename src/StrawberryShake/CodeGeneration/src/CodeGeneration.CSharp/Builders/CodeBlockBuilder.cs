using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace StrawberryShake.CodeGeneration.CSharp.Builders
{
    public class CodeBlockBuilder
        : ICode
    {
        private readonly List<ICode> _lines = new List<ICode>();

        public static CodeBlockBuilder New() => new CodeBlockBuilder();

        public static CodeBlockBuilder FromStringBuilder(StringBuilder sb)
        {
            if (sb is null)
            {
                throw new ArgumentNullException(nameof(sb));
            }

            return FromString(sb.ToString());
        }

        public static CodeBlockBuilder FromString(string s)
        {
            if (s is null)
            {
                throw new ArgumentNullException(nameof(s));
            }

            CodeBlockBuilder builder = new CodeBlockBuilder();

            using var stringReader = new StringReader(s);

            while (stringReader.Peek() != -1)
            {
                string? line = stringReader.ReadLine();
                if (line is { })
                {
                    builder.AddCode(line);
                }
            }

            return builder;
        }

        public CodeBlockBuilder AddCode(ICode value)
        {
            if (value is null)
            {
                throw new ArgumentNullException(nameof(value));
            }

            _lines.Add(value);
            return this;
        }

        public CodeBlockBuilder AddCode(string value)
        {
            if (value is null)
            {
                throw new ArgumentNullException(nameof(value));
            }

            _lines.Add(CodeLineBuilder.New().SetLine(value));
            return this;
        }

        public async Task BuildAsync(CodeWriter writer)
        {
            if (writer is null)
            {
                throw new ArgumentNullException(nameof(writer));
            }

            foreach (ICode code in _lines)
            {
                await code.BuildAsync(writer).ConfigureAwait(false);
            }
        }
    }
}

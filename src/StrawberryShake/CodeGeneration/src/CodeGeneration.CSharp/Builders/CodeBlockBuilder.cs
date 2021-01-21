using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace StrawberryShake.CodeGeneration.CSharp.Builders
{
    public class CodeBlockBuilder : ICode
    {
        private readonly List<ICode> _blockParts = new List<ICode>();

        public static CodeBlockBuilder New() => new CodeBlockBuilder();

        public CodeBlockBuilder AddCode(ICode value)
        {
            if (value is null)
            {
                throw new ArgumentNullException(nameof(value));
            }

            _blockParts.Add(value);
            return this;
        }

        public CodeBlockBuilder AddCode(string value)
        {
            if (value is null)
            {
                throw new ArgumentNullException(nameof(value));
            }

            _blockParts.Add(CodeInlineBuilder.New().SetText(value));
            return this;
        }

        public CodeBlockBuilder AddEmptyLine()
        {
            _blockParts.Add(CodeLineBuilder.New());
            return this;
        }

        public void Build(CodeWriter writer, HashSet<string>? builderContext = null)
        {
            if (writer is null)
            {
                throw new ArgumentNullException(nameof(writer));
            }

            foreach (ICode code in _blockParts)
            {
                code.Build(writer);
            }
        }
    }
}

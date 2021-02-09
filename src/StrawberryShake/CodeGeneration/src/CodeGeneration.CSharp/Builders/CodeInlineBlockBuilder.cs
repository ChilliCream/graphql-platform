using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace StrawberryShake.CodeGeneration.CSharp.Builders
{
    public class CodeInlineBlockBuilder : ICode
    {
        private readonly List<ICode> _lineParts = new();

        public static CodeInlineBlockBuilder New() => new();

        public CodeInlineBlockBuilder AddCode(ICode value)
        {
            if (value is null)
            {
                throw new ArgumentNullException(nameof(value));
            }

            _lineParts.Add(value);
            return this;
        }

        public void Build(CodeWriter writer)
        {
            if (writer is null)
            {
                throw new ArgumentNullException(nameof(writer));
            }

            foreach (ICode code in _lineParts)
            {
                code.Build(writer);
            }
        }
    }
}

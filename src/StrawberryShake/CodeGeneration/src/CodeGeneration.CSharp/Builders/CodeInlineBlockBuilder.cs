using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace StrawberryShake.CodeGeneration.CSharp.Builders
{
    public class CodeInlineBlockBuilder
        : ICode
    {
        private readonly List<ICode> _lineParts = new List<ICode>();

        public static CodeInlineBlockBuilder New() => new CodeInlineBlockBuilder();

        public CodeInlineBlockBuilder AddCode(ICode value)
        {
            if (value is null)
            {
                throw new ArgumentNullException(nameof(value));
            }

            _lineParts.Add(value);
            return this;
        }

        public async Task BuildAsync(CodeWriter writer)
        {
            if (writer is null)
            {
                throw new ArgumentNullException(nameof(writer));
            }

            foreach (ICode code in _lineParts)
            {
                await code.BuildAsync(writer).ConfigureAwait(false);
            }
        }
    }
}

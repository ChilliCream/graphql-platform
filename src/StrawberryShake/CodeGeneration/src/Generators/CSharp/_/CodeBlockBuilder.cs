using System.Collections.Generic;
using System.Threading.Tasks;
using StrawberryShake.Generators.Utilities;

namespace StrawberryShake.Generators.CSharp
{
    public class CodeBlockBuilder
        : ICode
    {
        private readonly List<ICode> _lines = new List<ICode>();

        public static CodeBlockBuilder New() => new CodeBlockBuilder();

        public CodeBlockBuilder AddCode(ICode value)
        {
            _lines.Add(value);
            return this;
        }

        public CodeBlockBuilder AddCode(string value)
        {
            _lines.Add(CodeLineBuilder.New().SetLine(value));
            return this;
        }

        public async Task BuildAsync(CodeWriter writer)
        {
            foreach (ICode code in _lines)
            {
                await code.BuildAsync(writer).ConfigureAwait(false);
            }
        }
    }
}

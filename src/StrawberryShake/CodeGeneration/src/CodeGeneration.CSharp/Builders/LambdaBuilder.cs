using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace StrawberryShake.CodeGeneration.CSharp.Builders
{
    public class LambdaBuilder: ICode
    {
        private List<string> _arguments = new List<string>();
        private ICode? _code;

        public LambdaBuilder AddArgument(string value)
        {
            _arguments.Add(value);
            return this;
        }

        public LambdaBuilder SetCode(ICode code)
        {
            _code = code;
            return this;
        }

        public async Task BuildAsync(CodeWriter writer)
        {
            if (_code is null)
            {
                throw new ArgumentNullException(nameof(_code));
            }

            if (_arguments.Count > 1)
            {
                await writer.WriteAsync('(').ConfigureAwait(false);
            }

            for (int i = 0; i < _arguments.Count; i++)
            {
                if (i > 0)
                {
                    await writer.WriteAsync(',').ConfigureAwait(false);
                }
                await writer.WriteAsync(_arguments[i]).ConfigureAwait(false);
            }

            if (_arguments.Count > 1)
            {
                await writer.WriteAsync(')').ConfigureAwait(false);
            }

            await writer.WriteAsync(" => ");
            await _code.BuildAsync(writer);
        }
    }
}

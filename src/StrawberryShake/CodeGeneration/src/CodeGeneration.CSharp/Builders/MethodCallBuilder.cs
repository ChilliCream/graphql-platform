using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace StrawberryShake.CodeGeneration.CSharp.Builders
{
    public class MethodCallBuilder: ICode
    {
        private string _methodName { get; set; }
        private bool _determineStatement { get; set; } = true;
        private List<ICode> _arguments = new List<ICode>();
        private List<ICode> _chainedCode = new List<ICode>();

        public static MethodCallBuilder New() => new MethodCallBuilder();

        public MethodCallBuilder SetMethodName(string methodName)
        {
            _methodName = methodName;
            return this;
        }

        public MethodCallBuilder AddChainedCode(ICode value)
        {
            _chainedCode.Add(value);
            return this;
        }

        public MethodCallBuilder AddArgument(ICode value)
        {
            _arguments.Add(value);
            return this;
        }

        public MethodCallBuilder AddArgument(string value)
        {
            _arguments.Add(CodeInlineBuilder.New().SetText(value));
            return this;
        }

        public MethodCallBuilder SetDetermineStatement(bool value)
        {
            _determineStatement = value;
            return this;
        }

        public async Task BuildAsync(CodeWriter writer)
        {
            if (writer is null)
            {
                throw new ArgumentNullException(nameof(writer));
            }

            if (_determineStatement)
            {
                await writer.WriteIndentAsync().ConfigureAwait(false);
            }
            await writer.WriteAsync(_methodName).ConfigureAwait(false);

            await writer.WriteAsync("(").ConfigureAwait(false);

            if (_arguments.Count == 0)
            {
                await writer.WriteAsync(")").ConfigureAwait(false);
            }
            else if (_arguments.Count == 1)
            {
                await _arguments[0].BuildAsync(writer).ConfigureAwait(false);
                await writer.WriteAsync(")").ConfigureAwait(false);
            }
            else
            {
                await writer.WriteLineAsync().ConfigureAwait(false);

                using (writer.IncreaseIndent())
                {
                    for (int i = 0; i < _arguments.Count; i++)
                    {
                        await writer.WriteIndentAsync().ConfigureAwait(false);
                        await _arguments[i].BuildAsync(writer).ConfigureAwait(false);
                        if (i == _arguments.Count - 1)
                        {
                            await writer.WriteLineAsync().ConfigureAwait(false);
                            writer.DecreaseIndent();
                            await writer.WriteIndentAsync();
                            await writer.WriteAsync(")").ConfigureAwait(false);
                            writer.IncreaseIndent();
                        }
                        else
                        {
                            await writer.WriteAsync(",").ConfigureAwait(false);
                            await writer.WriteLineAsync().ConfigureAwait(false);
                        }
                    }
                }
            }

            using (writer.IncreaseIndent())
            {
                foreach (ICode code in _chainedCode)
                {
                    await writer.WriteLineAsync().ConfigureAwait(false);
                    await writer.WriteIndentAsync().ConfigureAwait(false);
                    await writer.WriteAsync('.').ConfigureAwait(false);
                    await code.BuildAsync(writer).ConfigureAwait(false);
                }
            }

            if (_determineStatement)
            {
                await writer.WriteAsync(";").ConfigureAwait(false);
                await writer.WriteLineAsync().ConfigureAwait(false);
            }
        }
    }
}

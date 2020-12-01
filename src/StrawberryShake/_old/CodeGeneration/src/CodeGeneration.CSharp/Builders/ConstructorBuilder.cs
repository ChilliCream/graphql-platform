using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace StrawberryShake.CodeGeneration.CSharp.Builders
{
    public class ConstructorBuilder : ICodeBuilder
    {
        private AccessModifier _accessModifier = AccessModifier.Public;
        private string? _typeName;
        private List<ParameterBuilder> _parameters = new List<ParameterBuilder>();
        private List<ICode> _lines = new List<ICode>();

        public static ConstructorBuilder New() => new ConstructorBuilder();

        public ConstructorBuilder SetAccessModifier(AccessModifier value)
        {
            _accessModifier = value;
            return this;
        }

        public ConstructorBuilder SetTypeName(string value)
        {
            _typeName = value;
            return this;
        }

        public ConstructorBuilder AddParameter(ParameterBuilder value)
        {
            _parameters.Add(value);
            return this;
        }

        public ConstructorBuilder AddCode(ICode value)
        {
            _lines.Add(value);
            return this;
        }

        public ConstructorBuilder AddCode(string value)
        {
            _lines.Add(CodeLineBuilder.New().SetLine(value));
            return this;
        }

        public async Task BuildAsync(CodeWriter writer)
        {
            if (writer is null)
            {
                throw new ArgumentNullException(nameof(writer));
            }

            string modifier = _accessModifier.ToString().ToLowerInvariant();

            await writer.WriteIndentAsync().ConfigureAwait(false);

            await writer.WriteAsync(
                $"{modifier} {_typeName}(")
                .ConfigureAwait(false);

            if (_parameters.Count == 0)
            {
                await writer.WriteAsync(")").ConfigureAwait(false);
            }
            else if (_parameters.Count == 1)
            {
                await _parameters[0].BuildAsync(writer).ConfigureAwait(false);
                await writer.WriteAsync(")").ConfigureAwait(false);
            }
            else
            {
                await writer.WriteLineAsync().ConfigureAwait(false);

                using (writer.IncreaseIndent())
                {
                    for (int i = 0; i < _parameters.Count; i++)
                    {
                        await writer.WriteIndentAsync().ConfigureAwait(false);
                        await _parameters[i].BuildAsync(writer).ConfigureAwait(false);
                        if (i == _parameters.Count - 1)
                        {
                            await writer.WriteAsync(")").ConfigureAwait(false);
                        }
                        else
                        {
                            await writer.WriteAsync(",").ConfigureAwait(false);
                            await writer.WriteLineAsync().ConfigureAwait(false);
                        }
                    }
                }
            }

            await writer.WriteLineAsync().ConfigureAwait(false);
            await writer.WriteIndentedLineAsync("{").ConfigureAwait(false);

            using (writer.IncreaseIndent())
            {
                foreach (ICode code in _lines)
                {
                    await code.BuildAsync(writer).ConfigureAwait(false);
                }
            }

            await writer.WriteIndentedLineAsync("}").ConfigureAwait(false);
        }
    }
}

using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace StrawberryShake.CodeGeneration.CSharp.Builders
{
    public class InterfaceMethodBuilder : ICodeBuilder
    {
        private AccessModifier _accessModifier = AccessModifier.Public;
        private bool _isAsync = false;
        private string _returnType = "void";
        private string? _name;
        private readonly List<ParameterBuilder> _parameters = new List<ParameterBuilder>();

        public static InterfaceMethodBuilder New() => new InterfaceMethodBuilder();

        public InterfaceMethodBuilder SetAccessModifier(AccessModifier value)
        {
            _accessModifier = value;
            return this;
        }

        public InterfaceMethodBuilder SetAsync()
        {
            _isAsync = true;
            return this;
        }

        public InterfaceMethodBuilder SetReturnType(string value)
        {
            _returnType = value;
            return this;
        }

        public InterfaceMethodBuilder SetName(string value)
        {
            _name = value;
            return this;
        }

        public InterfaceMethodBuilder AddParameter(ParameterBuilder value)
        {
            _parameters.Add(value);
            return this;
        }

        public async Task BuildAsync(CodeWriter writer)
        {
            if (writer is null)
            {
                throw new ArgumentNullException(nameof(writer));
            }

            var modifier = _accessModifier.ToString().ToLowerInvariant();

            await writer.WriteIndentAsync().ConfigureAwait(false);

            await writer.WriteAsync($"{modifier} ").ConfigureAwait(false);

            if (_isAsync)
            {
                await writer.WriteAsync("async ").ConfigureAwait(false);
            }

            await writer.WriteAsync(
                $"{_returnType} {_name}(")
                .ConfigureAwait(false);

            switch (_parameters.Count)
            {
                case 0:
                    await writer.WriteAsync(")").ConfigureAwait(false);
                    break;

                case 1:
                    await _parameters[0].BuildAsync(writer).ConfigureAwait(false);
                    await writer.WriteAsync(")").ConfigureAwait(false);
                    break;

                default:
                {
                    await writer.WriteLineAsync().ConfigureAwait(false);

                    using (writer.IncreaseIndent())
                    {
                        for (var i = 0; i < _parameters.Count; i++)
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
                    break;
                }
            }

            await writer.WriteLineAsync(";").ConfigureAwait(false);
        }
    }
}

using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace StrawberryShake.CodeGeneration.CSharp.Builders
{
    public class InterfaceMethodBuilder : ICodeBuilder
    {
        private string _returnType = "void";
        private string? _name;
        private readonly List<ParameterBuilder> _parameters = new List<ParameterBuilder>();

        public static InterfaceMethodBuilder New() => new InterfaceMethodBuilder();
        
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

            await writer.WriteIndentAsync().ConfigureAwait(false);

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

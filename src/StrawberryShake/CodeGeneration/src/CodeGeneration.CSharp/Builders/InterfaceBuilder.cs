using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace StrawberryShake.CodeGeneration.CSharp.Builders
{
    public class InterfaceBuilder
        : ITypeBuilder
    {
        private AccessModifier _accessModifier;
        private bool _isPartial = true;
        private string? _name;
        private readonly List<string> _implements = new List<string>();
        private readonly List<InterfacePropertyBuilder> _properties =
            new List<InterfacePropertyBuilder>();
        private readonly List<InterfaceMethodBuilder> _methods =
            new List<InterfaceMethodBuilder>();

        public static InterfaceBuilder New() => new InterfaceBuilder();

        public InterfaceBuilder SetAccessModifier(AccessModifier value)
        {
            _accessModifier = value;
            return this;
        }

        public InterfaceBuilder SetName(string value)
        {
            _name = value;
            return this;
        }

        public InterfaceBuilder AddImplements(string value)
        {
            _implements.Add(value);
            return this;
        }

        public InterfaceBuilder AddProperty(InterfacePropertyBuilder property)
        {
            _properties.Add(property);
            return this;
        }

        public InterfaceBuilder AddMethod(InterfaceMethodBuilder method)
        {
            _methods.Add(method);
            return this;
        }

        public async Task BuildAsync(CodeWriter writer)
        {
            if (writer is null)
            {
                throw new ArgumentNullException(nameof(writer));
            }

            await writer.WriteGeneratedAttributeAsync().ConfigureAwait(false);

            string modifier = _accessModifier.ToString().ToLowerInvariant();

            await writer.WriteIndentAsync().ConfigureAwait(false);

            await writer.WriteAsync($"{modifier} ").ConfigureAwait(false);

            if (_isPartial)
            {
                await writer.WriteAsync("partial ").ConfigureAwait(false);
            }

            await writer.WriteAsync("interface ").ConfigureAwait(false);

            await writer.WriteLineAsync(_name).ConfigureAwait(false);

            if (_implements.Count > 0)
            {
                using (writer.IncreaseIndent())
                {
                    for (int i = 0; i < _implements.Count; i++)
                    {
                        if (i == 0)
                        {
                            await writer.WriteIndentedLineAsync(
                                $": {_implements[i]}")
                                .ConfigureAwait(false);
                        }
                        else
                        {
                            await writer.WriteIndentedLineAsync(
                                $", {_implements[i]}")
                                .ConfigureAwait(false);
                        }
                    }
                }
            }

            await writer.WriteIndentedLineAsync("{").ConfigureAwait(false);

            bool writeLine = false;

            using (writer.IncreaseIndent())
            {
                if (_properties.Count > 0)
                {
                    for (int i = 0; i < _properties.Count; i++)
                    {
                        if (writeLine || i > 0)
                        {
                            await writer.WriteLineAsync().ConfigureAwait(false);
                        }
                        await _properties[i].BuildAsync(writer).ConfigureAwait(false);
                    }
                    writeLine = true;
                }

                if (_methods.Count > 0)
                {
                    for (int i = 0; i < _methods.Count; i++)
                    {
                        if (writeLine || i > 0)
                        {
                            await writer.WriteLineAsync().ConfigureAwait(false);
                        }
                        await _methods[i].BuildAsync(writer).ConfigureAwait(false);
                    }
                    writeLine = true;
                }
            }

            await writer.WriteIndentedLineAsync("}").ConfigureAwait(false);
        }
    }
}

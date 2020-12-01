using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace StrawberryShake.CodeGeneration.CSharp.Builders
{
    public class EnumBuilder
        : ICodeBuilder
    {
        private readonly List<(string, int?)> _elements = new List<(string, int?)>();
        private string? _name;

        public static EnumBuilder New() => new EnumBuilder();

        public EnumBuilder SetName(string value)
        {
            _name = value;
            return this;
        }

        public EnumBuilder AddElement(string name, int? value = null)
        {
            _elements.Add((name, value));
            return this;
        }

        public async Task BuildAsync(CodeWriter writer)
        {
            if (writer is null)
            {
                throw new ArgumentNullException(nameof(writer));
            }

            await writer.WriteGeneratedAttributeAsync().ConfigureAwait(false);

            await writer.WriteIndentedLineAsync($"public enum {_name}").ConfigureAwait(false);
            await writer.WriteIndentedLineAsync("{").ConfigureAwait(false);

            using (writer.IncreaseIndent())
            {
                for (int i = 0; i < _elements.Count; i++)
                {
                    await writer.WriteIndentAsync().ConfigureAwait(false);
                    await writer.WriteAsync(_elements[i].Item1).ConfigureAwait(false);

                    if (_elements[i].Item2.HasValue)
                    {
                        await writer.WriteAsync($" = {_elements[i].Item2}").ConfigureAwait(false);
                    }

                    if (i + 1 == _elements.Count)
                    {
                        await writer.WriteAsync($",").ConfigureAwait(false);
                    }

                    await writer.WriteLineAsync().ConfigureAwait(false);
                }
            }

            await writer.WriteIndentedLineAsync("}").ConfigureAwait(false);
        }
    }
}

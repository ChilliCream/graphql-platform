using System;
using System.Threading.Tasks;

namespace StrawberryShake.CodeGeneration.CSharp.Builders
{
    public class InterfacePropertyBuilder : ICodeBuilder
    {
        private bool _isReadOnly = true;
        private string? _type;
        private string? _name;

        public static InterfacePropertyBuilder New() => new InterfacePropertyBuilder();

        public InterfacePropertyBuilder SetType(string value)
        {
            _type = value;
            return this;
        }

        public InterfacePropertyBuilder SetName(string value)
        {
            _name = value;
            return this;
        }

        public InterfacePropertyBuilder MakeSettable()
        {
            _isReadOnly = false;
            return this;
        }

        public async Task BuildAsync(CodeWriter writer)
        {
            if (writer is null)
            {
                throw new ArgumentNullException(nameof(writer));
            }

            await writer.WriteIndentAsync().ConfigureAwait(false);
            await writer.WriteAsync($"{_type} {_name}").ConfigureAwait(false);

            await writer.WriteAsync(" { get; ").ConfigureAwait(false);

            if (!_isReadOnly)
            {
                await writer.WriteAsync($"set; ").ConfigureAwait(false);
            }

            await writer.WriteAsync("}").ConfigureAwait(false);
            await writer.WriteLineAsync().ConfigureAwait(false);
        }
    }
}

using System.Threading.Tasks;

namespace StrawberryShake.CodeGeneration.CSharp
{
    public class FieldBuilder : ICodeBuilder
    {
        private AccessModifier _accessModifier;
        private bool _isStatic;
        private bool _isReadOnly;
        private string? _type;
        private string? _name;
        private string? _value;
        private bool _useDefaultInitializer;

        public static FieldBuilder New() => new FieldBuilder();

        public FieldBuilder SetAccessModifier(AccessModifier value)
        {
            _accessModifier = value;
            return this;
        }

        public FieldBuilder SetType(string value)
        {
            _type = value;
            return this;
        }

        public FieldBuilder SetName(string value)
        {
            _name = value;
            return this;
        }

        public FieldBuilder SetStatic()
        {
            _isStatic = true;
            return this;
        }

        public FieldBuilder SetReadOnly()
        {
            _isReadOnly = true;
            return this;
        }

        public FieldBuilder SetValue(string? value)
        {
            _value = value;
            _useDefaultInitializer = false;
            return this;
        }

        public FieldBuilder UseDefaultInitializer()
        {
            _value = null;
            _useDefaultInitializer = true;
            return this;
        }

        public async Task BuildAsync(CodeWriter writer)
        {
            string modifier = _accessModifier.ToString().ToLowerInvariant();

            await writer.WriteIndentAsync().ConfigureAwait(false);
            await writer.WriteAsync($"{modifier} ")
                .ConfigureAwait(false);

            if (_isStatic)
            {
                await writer.WriteAsync("static ").ConfigureAwait(false);
            }

            if (_isReadOnly)
            {
                await writer.WriteAsync("readonly ").ConfigureAwait(false);
            }

            await writer.WriteAsync($"{_type} {_name}").ConfigureAwait(false);

            if (_value is { })
            {
                await writer.WriteAsync($" = {_value}").ConfigureAwait(false);
            }
            else if (_useDefaultInitializer)
            {
                await writer.WriteAsync($" = new {_type}()").ConfigureAwait(false);
            }

            await writer.WriteLineAsync(";").ConfigureAwait(false);
        }
    }
}

using System;
using System.Threading.Tasks;

namespace StrawberryShake.CodeGeneration.CSharp.Builders
{
    public class FieldBuilder : ICodeBuilder
    {
        private AccessModifier _accessModifier = AccessModifier.Private;
        private bool _isConst;
        private bool _isStatic;
        private bool _isReadOnly;
        private TypeReferenceBuilder? _type;
        private string? _name;
        private string? _value;
        private bool _useDefaultInitializer;

        public static FieldBuilder New() => new FieldBuilder();

        public FieldBuilder SetAccessModifier(AccessModifier value)
        {
            _accessModifier = value;
            return this;
        }

        public FieldBuilder SetType(string value, bool condition = true)
        {
            if (condition)
            {
                _type = TypeReferenceBuilder.New().SetName(value);
            }

            return this;
        }

        public FieldBuilder SetType(TypeReferenceBuilder typeReference)
        {
            _type = typeReference;
            return this;
        }

        public FieldBuilder SetName(string value)
        {
            _name = value;
            return this;
        }

        public FieldBuilder SetConst()
        {
            _isConst = true;
            _isStatic = false;
            _isReadOnly = false;
            return this;
        }

        public FieldBuilder SetStatic()
        {
            _isStatic = true;
            _isConst = false;
            return this;
        }

        public FieldBuilder SetReadOnly()
        {
            _isReadOnly = true;
            _isConst = false;
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
            if (writer is null)
            {
                throw new ArgumentNullException(nameof(writer));
            }

            if (_type is null)
            {
                throw new ArgumentNullException(nameof(_type));
            }

            string modifier = _accessModifier.ToString().ToLowerInvariant();

            await writer.WriteIndentAsync().ConfigureAwait(false);
            await writer.WriteAsync($"{modifier} ")
                .ConfigureAwait(false);

            if (_isConst)
            {
                await writer.WriteAsync("const ").ConfigureAwait(false);
            }

            if (_isStatic)
            {
                await writer.WriteAsync("static ").ConfigureAwait(false);
            }

            if (_isReadOnly)
            {
                await writer.WriteAsync("readonly ").ConfigureAwait(false);
            }

            await _type.BuildAsync(writer).ConfigureAwait(false);
            await writer.WriteAsync(_name).ConfigureAwait(false);

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

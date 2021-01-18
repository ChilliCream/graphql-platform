using System;
using System.Threading.Tasks;

namespace StrawberryShake.CodeGeneration.CSharp.Builders
{
    public class PropertyBuilder : ICodeBuilder
    {
        private AccessModifier _accessModifier;
        private bool _isAutoProperty = true;
        private bool _isReadOnly = true;
        private string? _lambdaResolver;
        private TypeReferenceBuilder? _type;
        private string? _name;
        private string? _value;

        public static PropertyBuilder New() => new();

        public PropertyBuilder SetAccessModifier(AccessModifier value)
        {
            _accessModifier = value;
            return this;
        }

        public PropertyBuilder AsLambda(string resolveCode)
        {
            _lambdaResolver = resolveCode;
            return this;
        }

        public PropertyBuilder SetType(string value)
        {
            _type = TypeReferenceBuilder.New().SetName(value);
            return this;
        }

        public PropertyBuilder SetType(TypeReferenceBuilder value)
        {
            _type = value;
            return this;
        }

        public PropertyBuilder SetName(string value)
        {
            _name = value;
            return this;
        }

        public PropertyBuilder SetValue(string value)
        {
            _value = value;
            return this;
        }

        public PropertyBuilder MakeSettable()
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

            if (_type is null)
            {
                throw new ArgumentNullException(nameof(_type));
            }

            string modifier = _accessModifier.ToString().ToLowerInvariant();

            await writer.WriteIndentAsync().ConfigureAwait(false);
            await writer.WriteAsync(modifier).ConfigureAwait(false);
            await writer.WriteSpaceAsync().ConfigureAwait(false);
            await _type.BuildAsync(writer).ConfigureAwait(false);
            await writer.WriteAsync(_name).ConfigureAwait(false);

            if (_lambdaResolver is not null)
            {
                await writer.WriteAsync(" => ").ConfigureAwait(false);
                await writer.WriteAsync(_lambdaResolver).ConfigureAwait(false);
                await writer.WriteAsync(";").ConfigureAwait(false);
                await writer.WriteLineAsync().ConfigureAwait(false);
                return;
            }

            await writer.WriteAsync(" {").ConfigureAwait(false);
            await writer.WriteAsync(" get;").ConfigureAwait(false);
            if (!_isReadOnly)
            {
                await writer.WriteAsync(" set;").ConfigureAwait(false);
            }
            await writer.WriteAsync(" }").ConfigureAwait(false);

            if (_value is not null)
            {
                await writer.WriteAsync(" = ").ConfigureAwait(false);
                await writer.WriteAsync(_value).ConfigureAwait(false);
                await writer.WriteAsync(";").ConfigureAwait(false);
            }

            await writer.WriteLineAsync().ConfigureAwait(false);
        }
    }
}

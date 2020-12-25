using System;
using System.Threading.Tasks;

namespace StrawberryShake.CodeGeneration.CSharp.Builders
{
    public class PropertyBuilder : ICodeBuilder
    {
        private AccessModifier _accessModifier;
        private AccessModifier? _setterAccessModifier;
        private bool _isAutoProperty = true;
        private bool _isReadOnly = true;
        private string? _backingField;
        private ICode? _getCode;
        private ICode? _setCode;
        private TypeReferenceBuilder? _type;
        private string? _name;

        public static PropertyBuilder New() => new PropertyBuilder();

        public PropertyBuilder SetAccessModifier(AccessModifier value)
        {
            _accessModifier = value;
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

        public PropertyBuilder SetBackingField(string value)
        {
            _backingField = value;
            return this;
        }

        public PropertyBuilder SetSetter(ICode value)
        {
            _setCode = value;
            _isAutoProperty = _setCode is null && _getCode is null;
            return this;
        }

        public PropertyBuilder SetSetterAccessModifier(AccessModifier value)
        {
            _setterAccessModifier = value;
            return this;
        }

        public PropertyBuilder SetGetter(ICode value)
        {
            _getCode = value;
            _isAutoProperty = _setCode is null && _getCode is null;
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
            string setterModifier = string.Empty;

            if(_setterAccessModifier.HasValue)
            {
                setterModifier = _setterAccessModifier.Value
                    .ToString().ToLowerInvariant() + " ";
            }

            await writer.WriteIndentAsync().ConfigureAwait(false);
            await writer.WriteAsync(modifier).ConfigureAwait(false);
            await writer.WriteSpaceAsync().ConfigureAwait(false);
            await _type.BuildAsync(writer).ConfigureAwait(false);
            await writer.WriteAsync(_name).ConfigureAwait(false);

            if (_isAutoProperty)
            {
                if (_backingField is null)
                {
                    await writer.WriteAsync(" { get; ").ConfigureAwait(false);
                    if (!_isReadOnly)
                    {

                        await writer.WriteAsync(
                            $"{setterModifier}set; ")
                            .ConfigureAwait(false);
                    }
                    await writer.WriteAsync("}").ConfigureAwait(false);
                    await writer.WriteLineAsync().ConfigureAwait(false);
                    return;
                }
            }

            await writer.WriteLineAsync().ConfigureAwait(false);
            await writer.WriteIndentedLineAsync("{").ConfigureAwait(false);
            using (writer.IncreaseIndent())
            {
                if (_getCode is null)
                {
                    await writer.WriteIndentedLineAsync(
                        $"get => {_backingField}")
                        .ConfigureAwait(false);
                }
                else
                {
                    await writer.WriteIndentedLineAsync("get")
                        .ConfigureAwait(false);
                    await writer.WriteIndentedLineAsync("{").ConfigureAwait(false);
                    using (writer.IncreaseIndent())
                    {
                        await _getCode.BuildAsync(writer).ConfigureAwait(false);
                    }
                    await writer.WriteIndentedLineAsync("}").ConfigureAwait(false);
                }

                if (!_isReadOnly)
                {
                    if (_setCode is null)
                    {
                        await writer.WriteIndentedLineAsync(
                            $"{setterModifier}set => { _backingField} = value;")
                            .ConfigureAwait(false);
                    }
                    else
                    {
                        await writer.WriteIndentedLineAsync(
                            $"{setterModifier}set")
                            .ConfigureAwait(false);
                        await writer.WriteIndentedLineAsync("{").ConfigureAwait(false);
                        using (writer.IncreaseIndent())
                        {
                            await _setCode.BuildAsync(writer).ConfigureAwait(false);
                        }
                        await writer.WriteIndentedLineAsync("}").ConfigureAwait(false);
                    }
                }
            }
            await writer.WriteIndentedLineAsync("}").ConfigureAwait(false);
        }
    }
}

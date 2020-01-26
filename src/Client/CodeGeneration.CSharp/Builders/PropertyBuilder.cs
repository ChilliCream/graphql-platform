using System.Threading.Tasks;

namespace StrawberryShake.CodeGeneration.CSharp
{
    public class PropertyBuilder : ICodeBuilder
    {
        private AccessModifier _accessModifier;
        private bool _isAutoProperty = true;
        private bool _isReadOnly = true;
        private string? _backingField;
        private ICode? _getCode;
        private ICode? _setCode;
        private string? _type;
        private string? _name;
        private string? _default;

        public static PropertyBuilder New() => new PropertyBuilder();

        public PropertyBuilder SetAccessModifier(AccessModifier value)
        {
            _accessModifier = value;
            return this;
        }

        public PropertyBuilder SetType(string value)
        {
            _type = value;
            return this;
        }

        public PropertyBuilder SetName(string value)
        {
            _name = value;
            return this;
        }

        public PropertyBuilder SetDefault(string value = "default")
        {
            _default = value;
            return this;
        }

        public PropertyBuilder SetSetter(ICode value)
        {
            _setCode = value;
            _isAutoProperty = _setCode is null && _getCode is null;
            return this;
        }

        public PropertyBuilder SetGetter(ICode value)
        {
            _getCode = value;
            _isAutoProperty = _setCode is null && _getCode is null;
            return this;
        }

        public PropertyBuilder SetSettable()
        {
            _isReadOnly = false;
            return this;
        }

        public Task BuildAsync(CodeWriter writer) =>
            _default is null
                ? writer.WriteAsync($"{_type} {_name}")
                : writer.WriteAsync($"{_type} {_name} = {_default}");
    }
}

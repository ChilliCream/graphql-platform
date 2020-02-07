using System.Threading.Tasks;

namespace StrawberryShake.CodeGeneration.CSharp.Builders
{
    public class ParameterBuilder : ICodeBuilder
    {
        private string? _type;
        private string? _name;
        private string? _default;

        public static ParameterBuilder New() => new ParameterBuilder();

        public ParameterBuilder SetType(string value)
        {
            _type = value;
            return this;
        }

        public ParameterBuilder SetName(string value)
        {
            _name = value;
            return this;
        }

        public ParameterBuilder SetDefault(string value = "default")
        {
            _default = value;
            return this;
        }

        public Task BuildAsync(CodeWriter writer) =>
            _default is null
                ? writer.WriteAsync($"{_type} {_name}")
                : writer.WriteAsync($"{_type} {_name} = {_default}");
    }
}

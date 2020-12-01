using System.Threading.Tasks;
using StrawberryShake.Generators.Utilities;

namespace StrawberryShake.Generators.CSharp
{
    public class ParameterBuilder : ICodeBuilder
    {
        private string? _type;
        private string? _name;

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

        public Task BuildAsync(CodeWriter writer) =>
            writer.WriteAsync($"{_type} {_name}");
    }
}

using System;
using System.Threading.Tasks;

namespace StrawberryShake.CodeGeneration.CSharp.Builders
{
    public class ParameterBuilder : ICodeBuilder
    {
        private TypeBuilder? _type;
        private string? _name;
        private string? _default;

        public static ParameterBuilder New() => new ParameterBuilder();

        public ParameterBuilder SetType(TypeBuilder value, bool condition = true)
        {
            if (condition)
            {
                _type = value;
            }
            return this;
        }

        public ParameterBuilder SetType(string name)
        {
            _type = TypeBuilder.New().SetName(name);
            return this;
        }

        public ParameterBuilder SetName(string value)
        {
            _name = value;
            return this;
        }

        public ParameterBuilder SetDefault(string value = "default", bool condition = true)
        {
            if (condition)
            {
                _default = value;
            }
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

            await _type.BuildAsync(writer).ConfigureAwait(false);

            await (_default is null
                ? writer.WriteAsync($"{_name}")
                : writer.WriteAsync($"{_name} = {_default}"));
        }
    }
}

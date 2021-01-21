using System;
using System.Collections.Generic;

namespace StrawberryShake.CodeGeneration.CSharp.Builders
{
    public class ParameterBuilder : ICodeBuilder
    {
        private ITypeReferenceBuilder? _type;
        private string? _name;
        private string? _default;

        public static ParameterBuilder New() => new();

        public ParameterBuilder SetType(ITypeReferenceBuilder value, bool condition = true)
        {
            if (condition)
            {
                _type = value;
            }
            return this;
        }

        public ParameterBuilder SetType(string name)
        {
            _type = TypeReferenceBuilder.New().SetName(name);
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

        public void Build(CodeWriter writer, HashSet<string>? builderContext = null)
        {
            if (writer is null)
            {
                throw new ArgumentNullException(nameof(writer));
            }

            if (_type is null)
            {
                throw new ArgumentNullException(nameof(_type));
            }

            _type.Build(writer);
            writer.WriteSpace();

            writer.Write(_default is null ? $"{_name}" : $"{_name} = {_default}");
        }
    }
}

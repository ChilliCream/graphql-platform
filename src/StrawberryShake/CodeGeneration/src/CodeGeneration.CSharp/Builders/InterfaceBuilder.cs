using System;
using System.Collections.Generic;
using HotChocolate;
using StrawberryShake.Properties;

namespace StrawberryShake.CodeGeneration.CSharp.Builders
{
    public class InterfaceBuilder : ITypeBuilder
    {
        private NameString? _name;
        private readonly List<string> _implements = new();
        private readonly List<PropertyBuilder> _properties = new();

        public static InterfaceBuilder New() => new();

        public InterfaceBuilder SetName(NameString name)
        {
            _name = name;
            return this;
        }

        public InterfaceBuilder AddImplements(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                throw new ArgumentException(
                    Resources.ClassBuilder_AddImplements_TypeNameCannotBeNull,
                    nameof(value));
            }

            _implements.Add(value);
            return this;
        }

        public InterfaceBuilder AddProperty(PropertyBuilder property)
        {
            if (property is null)
            {
                throw new ArgumentNullException(nameof(property));
            }

            _properties.Add(property);
            return this;
        }

        public void Build(CodeWriter writer)
        {
            if (writer is null)
            {
                throw new ArgumentNullException(nameof(writer));
            }

            writer.WriteGeneratedAttribute();

            writer.WriteIndent();

            writer.Write($"public ");

            writer.Write("interface ");
            writer.WriteLine(_name);

            if (_implements.Count > 0)
            {
                using (writer.IncreaseIndent())
                {
                    for (var i = 0; i < _implements.Count; i++)
                    {
                        writer.WriteIndentedLine(
                            i == 0
                                ? $": {_implements[i]}"
                                : $", {_implements[i]}");
                    }
                }
            }

            writer.WriteIndentedLine("{");

            var writeLine = false;

            using (writer.IncreaseIndent())
            {
                if (_properties.Count > 0)
                {
                    for (var i = 0; i < _properties.Count; i++)
                    {
                        if (writeLine || i > 0)
                        {
                            writer.WriteLine();
                        }

                        _properties[i].Build(writer);
                    }

                    writeLine = true;
                }
            }

            writer.WriteIndentedLine("}");
        }
    }
}

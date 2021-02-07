using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using StrawberryShake.Properties;

namespace StrawberryShake.CodeGeneration.CSharp.Builders
{
    public class ClassBuilder : ITypeBuilder
    {
        private AccessModifier _accessModifier;
        private bool _isPartial = true;
        private bool _isStatic;
        private bool _isSealed;
        private bool _isAbstract;
        private string? _name;
        private readonly List<string> _implements = new();
        private readonly List<FieldBuilder> _fields = new();
        private readonly List<ConstructorBuilder> _constructors = new();
        private readonly List<PropertyBuilder> _properties = new();
        private readonly List<MethodBuilder> _methods = new();

        public static ClassBuilder New() => new();

        public static ClassBuilder New(string className) => new ClassBuilder().SetName(className);

        public ClassBuilder SetAccessModifier(AccessModifier value)
        {
            _accessModifier = value;
            return this;
        }

        public ClassBuilder SetName(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                throw new ArgumentException(
                    Resources.ClassBuilder_SetName_ClassNameCannotBeNull,
                    nameof(value));
            }

            _name = value;
            return this;
        }

        public ClassBuilder AddImplements(string value)
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

        public ClassBuilder AddConstructor(ConstructorBuilder constructor)
        {
            if (constructor is null)
            {
                throw new ArgumentNullException(nameof(constructor));
            }

            _constructors.Add(constructor);
            return this;
        }

        public ClassBuilder AddField(FieldBuilder field)
        {
            if (field is null)
            {
                throw new ArgumentNullException(nameof(field));
            }

            _fields.Add(field);
            return this;
        }

        public ClassBuilder AddProperty(PropertyBuilder property)
        {
            if (property is null)
            {
                throw new ArgumentNullException(nameof(property));
            }

            _properties.Add(property);
            return this;
        }

        public ClassBuilder AddMethod(MethodBuilder method)
        {
            if (method is null)
            {
                throw new ArgumentNullException(nameof(method));
            }

            _methods.Add(method);
            return this;
        }

        public ClassBuilder SetStatic()
        {
            _isStatic = true;
            _isSealed = false;
            _isAbstract = false;
            return this;
        }

        public ClassBuilder SetSealed()
        {
            _isStatic = false;
            _isSealed = true;
            _isAbstract = false;
            return this;
        }

        public ClassBuilder SetAbstract()
        {
            _isStatic = false;
            _isSealed = false;
            _isAbstract = true;
            return this;
        }

        public void Build(CodeWriter writer)
        {
            if (writer is null)
            {
                throw new ArgumentNullException(nameof(writer));
            }

            writer.WriteGeneratedAttribute();

            string modifier = _accessModifier.ToString().ToLowerInvariant();

            writer.WriteIndent();

            writer.Write($"{modifier} ");

            if (_isStatic)
            {
                writer.Write("static ");
            }
            else if (_isSealed)
            {
                writer.Write("sealed ");
            }
            else if (_isAbstract)
            {
                writer.Write("abstract ");
            }

            if (_isPartial)
            {
                writer.Write("partial ");
            }

            writer.Write("class ");
            writer.WriteLine(_name);

            if (!_isStatic && _implements.Count > 0)
            {
                using (writer.IncreaseIndent())
                {
                    for (var i = 0; i < _implements.Count; i++)
                    {
                        writer.WriteIndentedLine(i == 0
                            ? $": {_implements[i]}"
                            : $", {_implements[i]}");
                    }
                }
            }

            writer.WriteIndentedLine("{");

            var writeLine = false;

            using (writer.IncreaseIndent())
            {
                if (_fields.Count > 0)
                {
                    foreach (FieldBuilder builder in _fields)
                    {
                        builder.Build(writer);
                    }

                    writeLine = true;
                }

                if (_constructors.Count > 0)
                {
                    for (var i = 0; i < _constructors.Count; i++)
                    {
                        if (writeLine || i > 0)
                        {
                            writer.WriteLine();
                        }

                        _constructors[i]
                            .SetTypeName(_name!)
                            .Build(writer);
                    }

                    writeLine = true;
                }

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

                if (_methods.Count > 0)
                {
                    for (var i = 0; i < _methods.Count; i++)
                    {
                        if (writeLine || i > 0)
                        {
                            writer.WriteLine();
                        }

                        _methods[i].Build(writer);
                    }
                }
            }

            writer.WriteIndentedLine("}");
        }
    }
}

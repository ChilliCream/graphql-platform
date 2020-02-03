using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace StrawberryShake.CodeGeneration.CSharp
{
    public class ClassBuilder
        : ITypeBuilder
    {
        private AccessModifier _accessModifier;
        private bool _isPartial = true;
        private bool _isStatic = false;
        private bool _isSealed = false;
        private bool _isAbstract = false;
        private string? _name;
        private List<string> _implements = new List<string>();
        private List<FieldBuilder> _fields = new List<FieldBuilder>();
        private List<ConstructorBuilder> _constructors = new List<ConstructorBuilder>();
        private List<PropertyBuilder> _properties = new List<PropertyBuilder>();
        private List<MethodBuilder> _methods = new List<MethodBuilder>();

        public static ClassBuilder New() => new ClassBuilder();

        public ClassBuilder SetAccessModifier(AccessModifier value)
        {
            _accessModifier = value;
            return this;
        }

        public ClassBuilder SetName(string value)
        {
            _name = value;
            return this;
        }

        public ClassBuilder AddImplements(string value)
        {
            _implements.Add(value);
            return this;
        }

        public ClassBuilder AddConstructor(ConstructorBuilder constructor)
        {
            _constructors.Add(constructor);
            return this;
        }

        public ClassBuilder AddField(FieldBuilder field)
        {
            _fields.Add(field);
            return this;
        }

        public ClassBuilder AddProperty(PropertyBuilder property)
        {
            _properties.Add(property);
            return this;
        }

        public ClassBuilder AddMethod(MethodBuilder method)
        {
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

        public async Task BuildAsync(CodeWriter writer)
        {
            string modifier = _accessModifier.ToString().ToLowerInvariant();

            await writer.WriteIndentAsync().ConfigureAwait(false);

            await writer.WriteAsync($"{modifier} ").ConfigureAwait(false);

            if (_isStatic)
            {
                await writer.WriteAsync("static ").ConfigureAwait(false);
            }
            else if (_isSealed)
            {
                await writer.WriteAsync("sealed ").ConfigureAwait(false);
            }
            else if (_isAbstract)
            {
                await writer.WriteAsync("abstract ").ConfigureAwait(false);
            }

            if (_isPartial)
            {
                await writer.WriteAsync("partial ").ConfigureAwait(false);
            }

            await writer.WriteLineAsync(_name).ConfigureAwait(false);

            if (!_isStatic && _implements.Count > 0)
            {
                using (writer.IncreaseIndent())
                {
                    for (int i = 0; i < _implements.Count; i++)
                    {
                        if (i == 0)
                        {
                            await writer.WriteIndentedLineAsync(
                                $": {_implements[i]}")
                                .ConfigureAwait(false);
                        }
                        else
                        {
                            await writer.WriteIndentedLineAsync(
                                $", {_implements[i]}")
                                .ConfigureAwait(false);
                        }
                    }
                }
            }

            await writer.WriteIndentedLineAsync("{").ConfigureAwait(false);

            bool writeLine = false;

            using (writer.IncreaseIndent())
            {
                if (_fields.Count > 0)
                {
                    foreach (FieldBuilder builder in _fields)
                    {
                        await builder.BuildAsync(writer).ConfigureAwait(false);
                    }
                    writeLine = true;
                }

                if (_constructors.Count > 0)
                {
                    for (int i = 0; i < _constructors.Count; i++)
                    {
                        if (writeLine || i > 0)
                        {
                            await writer.WriteLineAsync().ConfigureAwait(false);
                        }
                        await _constructors[i]
                            .SetTypeName(_name!)
                            .BuildAsync(writer)
                            .ConfigureAwait(false);
                    }
                    writeLine = true;
                }

                if (_properties.Count > 0)
                {
                    for (int i = 0; i < _properties.Count; i++)
                    {
                        if (writeLine || i > 0)
                        {
                            await writer.WriteLineAsync().ConfigureAwait(false);
                        }
                        await _properties[i].BuildAsync(writer).ConfigureAwait(false);
                    }
                    writeLine = true;
                }

                if (_methods.Count > 0)
                {
                    for (int i = 0; i < _methods.Count; i++)
                    {
                        if (writeLine || i > 0)
                        {
                            await writer.WriteLineAsync().ConfigureAwait(false);
                        }
                        await _methods[i].BuildAsync(writer).ConfigureAwait(false);
                    }
                    writeLine = true;
                }
            }

            await writer.WriteIndentedLineAsync("}").ConfigureAwait(false);
        }
    }
}

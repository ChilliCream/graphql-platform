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
        private string? _name;
        private List<string> _implements = new List<string>();
        private List<ConstructorBuilder> _constructors = new List<ConstructorBuilder>();
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

        public ClassBuilder AddField()
        {
            throw new NotImplementedException();
        }

        public ClassBuilder AddProperty(PropertyBuilder property)
        {
            throw new NotImplementedException();
        }

        public ClassBuilder AddMethod(MethodBuilder method)
        {
            _methods.Add(method);
            return this;
        }

        public async Task BuildAsync(CodeWriter writer)
        {

        }
    }






    public interface ITypeBuilder : ICodeBuilder
    {

    }
}

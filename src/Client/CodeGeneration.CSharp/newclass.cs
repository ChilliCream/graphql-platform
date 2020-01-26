using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace StrawberryShake.CodeGeneration.CSharp
{
    public class ClassBuilder
        : ITypeBuilder
    {
        private AccessModifier _accessModifier;
        private string? _name;
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

        public ClassBuilder AddConstructor()
        {
            throw new NotImplementedException();
        }

        public ClassBuilder AddField()
        {
            throw new NotImplementedException();
        }

        public ClassBuilder AddProperty()
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

    public enum Inheritance
    {
        None,
        Sealed,
        Override,
        Virtual
    }


    public class Foo
    {
        public void Bar()
        {
            CodeFileBuilder.New()
                .AddUsing("System.Collections")
                .SetNamespace("Foo")
                .AddType(ClassBuilder.New()
                    .AddConstructor())
                .BuildAsync(null);

        }
    }

    public interface ICode : ICodeBuilder { }

    public interface ITypeBuilder : ICodeBuilder
    {

    }

    public interface ICodeBuilder
    {
        Task BuildAsync(CodeWriter writer);
    }
}

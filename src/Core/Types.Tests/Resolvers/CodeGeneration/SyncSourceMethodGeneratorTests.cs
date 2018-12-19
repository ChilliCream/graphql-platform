using System.Linq;
using System.Reflection;
using System.Text;
using ChilliCream.Testing;
using HotChocolate.Resolvers.CodeGeneration;
using Xunit;

namespace HotChocolate.Resolvers
{
    public class SyncSourceMethodGeneratorTests
    {
        [Fact]
        public void SyncSourceMethodGenerator_GenerateWithoutArguments()
        {
            // arrange
            var fieldMember = new FieldMember(
                "Foo", "bar",
                GetMethod<GeneratorTestDummy>("GetFoo", 0));

            var descriptor = new SourceResolverDescriptor(fieldMember);

            // act
            var source = new StringBuilder();
            var generator = new SyncSourceMethodGenerator();
            string result = generator.Generate("abc", descriptor);

            // assert
            result.Snapshot();
        }

        [Fact]
        public void SyncSourceMethodGenerator_GenerateWithOneArgument()
        {
            // arrange
            var fieldMember = new FieldMember(
               "Foo", "bar",
               GetMethod<GeneratorTestDummy>("GetFoo", 1));

            var descriptor = new SourceResolverDescriptor(fieldMember);

            // act
            var source = new StringBuilder();
            var generator = new SyncSourceMethodGenerator();
            string result = generator.Generate("abc", descriptor);

            // assert
            result.Snapshot();
        }

        [Fact]
        public void SyncSourceMethodGenerator_GenerateWithTwoArgument()
        {
            // arrange
            var fieldMember = new FieldMember(
              "Foo", "bar",
              GetMethod<GeneratorTestDummy>("GetFoo", 2));

            var descriptor = new SourceResolverDescriptor(fieldMember);

            // act
            var source = new StringBuilder();
            var generator = new SyncSourceMethodGenerator();
            string result = generator.Generate("abc", descriptor);

            // assert
            result.Snapshot();
        }

        private MethodInfo GetMethod<T>(string name, int parameters)
        {
            return typeof(T)
                .GetMethods()
                .Single(t => t.Name == name
                    && t.GetParameters().Length == parameters);
        }
    }
}

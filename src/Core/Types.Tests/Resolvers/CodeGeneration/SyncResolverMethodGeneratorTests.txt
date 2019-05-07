using System;
using System.Linq;
using System.Reflection;
using System.Text;
using HotChocolate.Resolvers.CodeGeneration;
using Snapshooter.Xunit;
using Xunit;

namespace HotChocolate.Resolvers
{
    public class SyncResolverMethodGeneratorTests
    {
        [Fact]
        public void SyncResolverMethodGenerator_GenerateWithoutArguments()
        {
            // arrange
            Type sourceType = typeof(GeneratorTestDummy);

            var fieldMember = new FieldMember(
                "Foo", "bar",
                GetMethod<GeneratorTestDummyResolver>("GetFoo", 0));

            var descriptor = new ResolverDescriptor(sourceType, fieldMember);

            // act
            var source = new StringBuilder();
            var generator = new SyncResolverMethodGenerator();
            string result = generator.Generate("abc", descriptor);

            // assert
            result.MatchSnapshot();
        }


        [Fact]
        public void SyncResolverMethodGenerator_GenerateWithSourceArgument()
        {
            // arrange
            Type sourceType = typeof(GeneratorTestDummy);

            var fieldMember = new FieldMember(
                "Foo", "bar",
                GetMethod<GeneratorTestDummyResolver>("GetFoo", 1));

            var descriptor = new ResolverDescriptor(sourceType, fieldMember);

            // act
            var source = new StringBuilder();
            var generator = new SyncResolverMethodGenerator();
            string result = generator.Generate("abc", descriptor);

            // assert
            result.MatchSnapshot();
        }

        [Fact]
        public void SyncResolverMethodGenerator_GenerateWithSourceArgumentAndArgument()
        {
            // arrange
            Type sourceType = typeof(GeneratorTestDummy);

            var fieldMember = new FieldMember(
                "Foo", "bar",
                GetMethod<GeneratorTestDummyResolver>("GetFoo", 2));

            var descriptor = new ResolverDescriptor(sourceType, fieldMember);

            // act
            var source = new StringBuilder();
            var generator = new SyncResolverMethodGenerator();
            string result = generator.Generate("abc", descriptor);

            // assert
            result.MatchSnapshot();
        }

        [Fact]
        public void SyncResolverMethodGenerator_GenerateWithSourceArgumentAndTwoArguments()
        {
            // arrange
            Type sourceType = typeof(GeneratorTestDummy);

            var fieldMember = new FieldMember(
                "Foo", "bar",
                GetMethod<GeneratorTestDummyResolver>("GetFoo", 3));

            var descriptor = new ResolverDescriptor(sourceType, fieldMember);

            // act
            var source = new StringBuilder();
            var generator = new SyncResolverMethodGenerator();
            string result = generator.Generate("abc", descriptor);

            // assert
            result.MatchSnapshot();
        }

        private ResolverDescriptor CreateDescriptor(ArgumentKind argumentKind)
        {
            Type sourceType = typeof(GeneratorTestDummy);

            var fieldMember = new FieldMember(
                "Foo", "bar",
                GetMethod<GeneratorTestDummyResolver>("GetFoo", 1));

            var argumentDescriptor = new ArgumentDescriptor(
                    "a", "b", argumentKind,
                    typeof(GeneratorTestDummy),
                    null);

            return new ResolverDescriptor(sourceType, fieldMember);
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

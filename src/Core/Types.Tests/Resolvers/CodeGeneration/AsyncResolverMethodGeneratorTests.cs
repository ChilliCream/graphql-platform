using System;
using System.Linq;
using System.Reflection;
using System.Text;
using ChilliCream.Testing;
using HotChocolate.Resolvers.CodeGeneration;
using Xunit;

namespace HotChocolate.Resolvers
{
    public class AsyncResolverMethodGeneratorTests
    {
        [Fact]
        public void AsyncResolverMethodGenerator_GenerateWithoutArguments()
        {
            // arrange
            Type sourceType = typeof(GeneratorTestDummy);

            var fieldMember = new FieldMember(
                "Foo", "bar",
                GetMethod<GeneratorTestDummyResolver>("GetFooAsync", 0));

            var descriptor = new ResolverDescriptor(sourceType, fieldMember);
            var source = new StringBuilder();
            var generator = new AsyncResolverMethodGenerator();

            // act
            string result = generator.Generate("abc", descriptor);

            // assert
            result.Snapshot();
        }

        [Fact]
        public void AsyncResolverMethodGenerator_GenerateWithSourceArgument()
        {
            // arrange
            Type sourceType = typeof(GeneratorTestDummy);

            var fieldMember = new FieldMember(
                "Foo", "bar",
                GetMethod<GeneratorTestDummyResolver>("GetFooAsync", 1));

            var descriptor = new ResolverDescriptor(sourceType, fieldMember);
            var source = new StringBuilder();
            var generator = new AsyncResolverMethodGenerator();

            // act
            string result = generator.Generate("abc", descriptor);

            // assert
            result.Snapshot();
        }

        [Fact]
        public void AsyncResolverMethodGenerator_GenerateWithSourceArgumentAndArgument()
        {
            // arrange
            Type sourceType = typeof(GeneratorTestDummy);

            var fieldMember = new FieldMember(
                "Foo", "bar",
                GetMethod<GeneratorTestDummyResolver>("GetFooAsync", 2));

            var descriptor = new ResolverDescriptor(sourceType, fieldMember);

            var source = new StringBuilder();
            var generator = new AsyncResolverMethodGenerator();

            // act
            string result = generator.Generate("abc", descriptor);

            // assert
            result.Snapshot();
        }

        [Fact]
        public void AsyncResolverMethodGenerator_GenerateWithSourceArgumentAndTwoArguments()
        {
            // arrange
            Type sourceType = typeof(GeneratorTestDummy);

            var fieldMember = new FieldMember(
                "Foo", "bar",
                GetMethod<GeneratorTestDummyResolver>("GetFooAsync", 3));

            var descriptor = new ResolverDescriptor(sourceType, fieldMember);

            var source = new StringBuilder();
            var generator = new AsyncResolverMethodGenerator();

            // act
            string result = generator.Generate("abc", descriptor);

            // assert
            result.Snapshot();
        }

        [Fact]
        public void AsyncResolverMethodGenerator_GenerateWithCancellationToken()
        {
            // arrange
            var descriptor = CreateDescriptor(ArgumentKind.CancellationToken);

            var source = new StringBuilder();
            var generator = new AsyncResolverMethodGenerator();

            // act
            string result = generator.Generate("abc", descriptor);

            // assert
            result.Snapshot();
        }

        [Fact]
        public void AsyncResolverMethodGenerator_GenerateWithContext()
        {
            // arrange
            var descriptor = CreateDescriptor(ArgumentKind.Context);

            // act
            var source = new StringBuilder();
            var generator = new AsyncResolverMethodGenerator();
            string result = generator.Generate("abc", descriptor);

            // assert
            result.Snapshot();
        }

        [Fact]
        public void AsyncResolverMethodGenerator_GenerateWithField()
        {
            // arrange
            var descriptor = CreateDescriptor(ArgumentKind.Field);

            var source = new StringBuilder();
            var generator = new AsyncResolverMethodGenerator();

            // act
            string result = generator.Generate("abc", descriptor);

            // assert
            result.Snapshot();
        }

        [Fact]
        public void AsyncResolverMethodGenerator_GenerateWithFieldSelection()
        {
            // arrange
            var descriptor = CreateDescriptor(ArgumentKind.FieldSelection);

            var source = new StringBuilder();
            var generator = new AsyncResolverMethodGenerator();

            // act
            string result = generator.Generate("abc", descriptor);

            // assert
            result.Snapshot();
        }

        [Fact]
        public void AsyncResolverMethodGenerator_GenerateWithObjectType()
        {
            // arrange
            var descriptor = CreateDescriptor(ArgumentKind.ObjectType);

            var source = new StringBuilder();
            var generator = new AsyncResolverMethodGenerator();

            // act
            string result = generator.Generate("abc", descriptor);

            // assert
            result.Snapshot();
        }

        [Fact]
        public void AsyncResolverMethodGenerator_GenerateWithOperationDefinition()
        {
            // arrange
            var descriptor = CreateDescriptor(ArgumentKind.OperationDefinition);

            var source = new StringBuilder();
            var generator = new AsyncResolverMethodGenerator();

            // act
            string result = generator.Generate("abc", descriptor);

            // assert
            result.Snapshot();
        }

        [Fact]
        public void AsyncResolverMethodGenerator_GenerateWithQueryDocument()
        {
            // arrange
            var descriptor = CreateDescriptor(ArgumentKind.QueryDocument);

            var source = new StringBuilder();
            var generator = new AsyncResolverMethodGenerator();

            // act
            string result = generator.Generate("abc", descriptor);

            // assert
            result.Snapshot();
        }

        [Fact]
        public void AsyncResolverMethodGenerator_GenerateWithQuerySchema()
        {
            // arrange
            var descriptor = CreateDescriptor(ArgumentKind.Schema);

            var source = new StringBuilder();
            var generator = new AsyncResolverMethodGenerator();

            // act
            string result = generator.Generate("abc", descriptor);

            // assert
            result.Snapshot();
        }

        [Fact]
        public void AsyncResolverMethodGenerator_GenerateWithQueryService()
        {
            // arrange
            var descriptor = CreateDescriptor(ArgumentKind.Service);

            var source = new StringBuilder();
            var generator = new AsyncResolverMethodGenerator();

            // act
            string result = generator.Generate("abc", descriptor);

            // assert
            result.Snapshot();
        }

        private ResolverDescriptor CreateDescriptor(ArgumentKind argumentKind)
        {
            Type sourceType = typeof(GeneratorTestDummy);

            var fieldMember = new FieldMember(
                "Foo", "bar",
                GetMethod<GeneratorTestDummyResolver>("GetFooAsync", 1));

            var argument = new ArgumentDescriptor(
                    "a", "b", argumentKind,
                    typeof(GeneratorTestDummy));

            return new ResolverDescriptor(sourceType, fieldMember, argument);
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

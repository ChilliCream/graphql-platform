using System;
using System.Linq;
using System.Reflection;
using System.Text;
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
            var sourceType = typeof(GeneratorTestDummy);
            MethodInfo method = typeof(GeneratorTestDummyResolver).GetMethods()
                .Single(t => t.Name == "GetFooAsync" && t.GetParameters().Length == 0);
            var descriptor = FieldResolverDescriptor
                .CreateCollectionMethod(new FieldReference("Foo", "bar"),
                    method.ReflectedType, sourceType, method, true,
                    Enumerable.Empty<FieldResolverArgumentDescriptor>());

            // act
            var source = new StringBuilder();
            var generator = new AsyncResolverMethodGenerator();
            string result = generator.Generate("abc", descriptor);

            // assert
            Assert.Equal(Snapshot.Current(), Snapshot.New(result));
        }


        [Fact]
        public void AsyncResolverMethodGenerator_GenerateWithSourceArgument()
        {
            // arrange
            var argumentDescriptor =
                new FieldResolverArgumentDescriptor("a", "b",
                    FieldResolverArgumentKind.Source,
                    typeof(GeneratorTestDummy));

            var sourceType = typeof(GeneratorTestDummy);
            MethodInfo method = typeof(GeneratorTestDummyResolver).GetMethods()
                .Single(t => t.Name == "GetFooAsync" && t.GetParameters().Length == 1);
            var descriptor = FieldResolverDescriptor
                .CreateCollectionMethod(new FieldReference("Foo", "bar"),
                    method.ReflectedType, sourceType, method, true,
                    new[] { argumentDescriptor });

            // act
            var source = new StringBuilder();
            var generator = new AsyncResolverMethodGenerator();
            string result = generator.Generate("abc", descriptor);

            // assert
            Assert.Equal(Snapshot.Current(), Snapshot.New(result));
        }

        [Fact]
        public void AsyncResolverMethodGenerator_GenerateWithSourceArgumentAndArgument()
        {
            // arrange
            var argumentDescriptor1 =
                new FieldResolverArgumentDescriptor("a", "b",
                    FieldResolverArgumentKind.Source,
                    typeof(GeneratorTestDummy));

            var argumentDescriptor2 =
                new FieldResolverArgumentDescriptor("b", "c",
                    FieldResolverArgumentKind.Argument,
                    typeof(string));

            var sourceType = typeof(GeneratorTestDummy);
            MethodInfo method = typeof(GeneratorTestDummyResolver).GetMethods()
                .Single(t => t.Name == "GetFooAsync" && t.GetParameters().Length == 2);
            var descriptor = FieldResolverDescriptor
                .CreateCollectionMethod(new FieldReference("Foo", "bar"),
                    method.ReflectedType, sourceType, method, true,
                    new[] { argumentDescriptor1, argumentDescriptor2 });

            // act
            var source = new StringBuilder();
            var generator = new AsyncResolverMethodGenerator();
            string result = generator.Generate("abc", descriptor);

            // assert
            Assert.Equal(Snapshot.Current(), Snapshot.New(result));
        }

        [Fact]
        public void AsyncResolverMethodGenerator_GenerateWithSourceArgumentAndTwoArguments()
        {
            // arrange
            var argumentDescriptor1 =
                new FieldResolverArgumentDescriptor("a", "b",
                    FieldResolverArgumentKind.Source,
                    typeof(GeneratorTestDummy));

            var argumentDescriptor2 =
                new FieldResolverArgumentDescriptor("b", "c",
                    FieldResolverArgumentKind.Argument,
                    typeof(string));

            var argumentDescriptor3 =
                new FieldResolverArgumentDescriptor("c", "d",
                    FieldResolverArgumentKind.Argument,
                    typeof(int));

            var sourceType = typeof(GeneratorTestDummy);
            MethodInfo method = typeof(GeneratorTestDummyResolver).GetMethods()
                .Single(t => t.Name == "GetFooAsync" && t.GetParameters().Length == 3);
            var descriptor = FieldResolverDescriptor
                .CreateCollectionMethod(new FieldReference("Foo", "bar"),
                    method.ReflectedType, sourceType, method, true,
                    new[] { argumentDescriptor1, argumentDescriptor2, argumentDescriptor3 });

            // act
            var source = new StringBuilder();
            var generator = new AsyncResolverMethodGenerator();
            string result = generator.Generate("abc", descriptor);

            // assert
            Assert.Equal(Snapshot.Current(), Snapshot.New(result));
        }

        [Fact]
        public void AsyncResolverMethodGenerator_GenerateWithCancellationToken()
        {
            // arrange
            var argumentDescriptor =
                new FieldResolverArgumentDescriptor("a", "b",
                    FieldResolverArgumentKind.CancellationToken,
                    typeof(GeneratorTestDummy));

            var sourceType = typeof(GeneratorTestDummy);
            MethodInfo method = typeof(GeneratorTestDummyResolver).GetMethods()
                .Single(t => t.Name == "GetFooAsync" && t.GetParameters().Length == 1);
            var descriptor = FieldResolverDescriptor
                .CreateCollectionMethod(new FieldReference("Foo", "bar"),
                    method.ReflectedType, sourceType, method, true,
                    new[] { argumentDescriptor });

            // act
            var source = new StringBuilder();
            var generator = new AsyncResolverMethodGenerator();
            string result = generator.Generate("abc", descriptor);

            // assert
            Assert.Equal(Snapshot.Current(), Snapshot.New(result));
        }

        [Fact]
        public void AsyncResolverMethodGenerator_GenerateWithContext()
        {
            // arrange
            var argumentDescriptor =
                new FieldResolverArgumentDescriptor("a", "b",
                    FieldResolverArgumentKind.Context,
                    typeof(GeneratorTestDummy));

            var sourceType = typeof(GeneratorTestDummy);
            MethodInfo method = typeof(GeneratorTestDummyResolver).GetMethods()
                .Single(t => t.Name == "GetFooAsync" && t.GetParameters().Length == 1);
            var descriptor = FieldResolverDescriptor
                .CreateCollectionMethod(new FieldReference("Foo", "bar"),
                    method.ReflectedType, sourceType, method, true,
                    new[] { argumentDescriptor });

            // act
            var source = new StringBuilder();
            var generator = new AsyncResolverMethodGenerator();
            string result = generator.Generate("abc", descriptor);

            // assert
            Assert.Equal(Snapshot.Current(), Snapshot.New(result));
        }

        [Fact]
        public void AsyncResolverMethodGenerator_GenerateWithField()
        {
            // arrange
            var argumentDescriptor =
                new FieldResolverArgumentDescriptor("a", "b",
                    FieldResolverArgumentKind.Field,
                    typeof(GeneratorTestDummy));

            var sourceType = typeof(GeneratorTestDummy);
            MethodInfo method = typeof(GeneratorTestDummyResolver).GetMethods()
                .Single(t => t.Name == "GetFooAsync" && t.GetParameters().Length == 1);
            var descriptor = FieldResolverDescriptor
                .CreateCollectionMethod(new FieldReference("Foo", "bar"),
                    method.ReflectedType, sourceType, method, true,
                    new[] { argumentDescriptor });

            // act
            var source = new StringBuilder();
            var generator = new AsyncResolverMethodGenerator();
            string result = generator.Generate("abc", descriptor);

            // assert
            Assert.Equal(Snapshot.Current(), Snapshot.New(result));
        }

        [Fact]
        public void AsyncResolverMethodGenerator_GenerateWithFieldSelection()
        {
            // arrange
            var argumentDescriptor =
                new FieldResolverArgumentDescriptor("a", "b",
                    FieldResolverArgumentKind.FieldSelection,
                    typeof(GeneratorTestDummy));

            var sourceType = typeof(GeneratorTestDummy);
            MethodInfo method = typeof(GeneratorTestDummyResolver).GetMethods()
                .Single(t => t.Name == "GetFooAsync" && t.GetParameters().Length == 1);
            var descriptor = FieldResolverDescriptor
                .CreateCollectionMethod(new FieldReference("Foo", "bar"),
                    method.ReflectedType, sourceType, method, true,
                    new[] { argumentDescriptor });

            // act
            var source = new StringBuilder();
            var generator = new AsyncResolverMethodGenerator();
            string result = generator.Generate("abc", descriptor);

            // assert
            Assert.Equal(Snapshot.Current(), Snapshot.New(result));
        }

        [Fact]
        public void AsyncResolverMethodGenerator_GenerateWithObjectType()
        {
            // arrange
            var argumentDescriptor =
                new FieldResolverArgumentDescriptor("a", "b",
                    FieldResolverArgumentKind.ObjectType,
                    typeof(GeneratorTestDummy));

            var sourceType = typeof(GeneratorTestDummy);
            MethodInfo method = typeof(GeneratorTestDummyResolver).GetMethods()
                .Single(t => t.Name == "GetFooAsync" && t.GetParameters().Length == 1);
            var descriptor = FieldResolverDescriptor
                .CreateCollectionMethod(new FieldReference("Foo", "bar"),
                    method.ReflectedType, sourceType, method, true,
                    new[] { argumentDescriptor });

            // act
            var source = new StringBuilder();
            var generator = new AsyncResolverMethodGenerator();
            string result = generator.Generate("abc", descriptor);

            // assert
            Assert.Equal(Snapshot.Current(), Snapshot.New(result));
        }

        [Fact]
        public void AsyncResolverMethodGenerator_GenerateWithOperationDefinition()
        {
            // arrange
            var argumentDescriptor =
                new FieldResolverArgumentDescriptor("a", "b",
                    FieldResolverArgumentKind.OperationDefinition,
                    typeof(GeneratorTestDummy));

            var sourceType = typeof(GeneratorTestDummy);
            MethodInfo method = typeof(GeneratorTestDummyResolver).GetMethods()
                .Single(t => t.Name == "GetFooAsync" && t.GetParameters().Length == 1);
            var descriptor = FieldResolverDescriptor
                .CreateCollectionMethod(new FieldReference("Foo", "bar"),
                    method.ReflectedType, sourceType, method, true,
                    new[] { argumentDescriptor });

            // act
            var source = new StringBuilder();
            var generator = new AsyncResolverMethodGenerator();
            string result = generator.Generate("abc", descriptor);

            // assert
            Assert.Equal(Snapshot.Current(), Snapshot.New(result));
        }

        [Fact]
        public void AsyncResolverMethodGenerator_GenerateWithQueryDocument()
        {
            // arrange
            var argumentDescriptor =
                new FieldResolverArgumentDescriptor("a", "b",
                    FieldResolverArgumentKind.QueryDocument,
                    typeof(GeneratorTestDummy));

            var sourceType = typeof(GeneratorTestDummy);
            MethodInfo method = typeof(GeneratorTestDummyResolver).GetMethods()
                .Single(t => t.Name == "GetFooAsync" && t.GetParameters().Length == 1);
            var descriptor = FieldResolverDescriptor
                .CreateCollectionMethod(new FieldReference("Foo", "bar"),
                    method.ReflectedType, sourceType, method, true,
                    new[] { argumentDescriptor });

            // act
            var source = new StringBuilder();
            var generator = new AsyncResolverMethodGenerator();
            string result = generator.Generate("abc", descriptor);

            // assert
            Assert.Equal(Snapshot.Current(), Snapshot.New(result));
        }

        [Fact]
        public void AsyncResolverMethodGenerator_GenerateWithQuerySchema()
        {
            // arrange
            var argumentDescriptor =
                new FieldResolverArgumentDescriptor("a", "b",
                    FieldResolverArgumentKind.Schema,
                    typeof(GeneratorTestDummy));

            var sourceType = typeof(GeneratorTestDummy);
            MethodInfo method = typeof(GeneratorTestDummyResolver).GetMethods()
                .Single(t => t.Name == "GetFooAsync" && t.GetParameters().Length == 1);
            var descriptor = FieldResolverDescriptor
                .CreateCollectionMethod(new FieldReference("Foo", "bar"),
                    method.ReflectedType, sourceType, method, true,
                    new[] { argumentDescriptor });

            // act
            var source = new StringBuilder();
            var generator = new AsyncResolverMethodGenerator();
            string result = generator.Generate("abc", descriptor);

            // assert
            Assert.Equal(Snapshot.Current(), Snapshot.New(result));
        }

        [Fact]
        public void AsyncResolverMethodGenerator_GenerateWithQueryService()
        {
            // arrange
            var argumentDescriptor =
                new FieldResolverArgumentDescriptor("a", "b",
                    FieldResolverArgumentKind.Service,
                    typeof(GeneratorTestDummy));

            var sourceType = typeof(GeneratorTestDummy);
            MethodInfo method = typeof(GeneratorTestDummyResolver).GetMethods()
                .Single(t => t.Name == "GetFooAsync" && t.GetParameters().Length == 1);
            var descriptor = FieldResolverDescriptor
                .CreateCollectionMethod(new FieldReference("Foo", "bar"),
                    method.ReflectedType, sourceType, method, true,
                    new[] { argumentDescriptor });

            // act
            var source = new StringBuilder();
            var generator = new AsyncResolverMethodGenerator();
            string result = generator.Generate("abc", descriptor);

            // assert
            Assert.Equal(Snapshot.Current(), Snapshot.New(result));
        }
    }
}

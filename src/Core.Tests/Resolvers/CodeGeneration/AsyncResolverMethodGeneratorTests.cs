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
            MethodInfo method = typeof(GeneratorTestDummyResolver)
                .GetMethods()
                .Single(t => t.Name == "GetFooAsync"
                    && t.GetParameters().Length == 0);

            var descriptor = new ResolverDescriptor() FieldResolverDescriptor
                .CreateCollectionMethod(new FieldReference("Foo", "bar"),
                    method.ReflectedType, sourceType, method, true,
                    Enumerable.Empty<ArgumentDescriptor>());

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
                new ArgumentDescriptor("a", "b",
                    ArgumentKind.Source,
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
                new ArgumentDescriptor("a", "b",
                    ArgumentKind.Source,
                    typeof(GeneratorTestDummy));

            var argumentDescriptor2 =
                new ArgumentDescriptor("b", "c",
                    ArgumentKind.Argument,
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
                new ArgumentDescriptor("a", "b",
                    ArgumentKind.Source,
                    typeof(GeneratorTestDummy));

            var argumentDescriptor2 =
                new ArgumentDescriptor("b", "c",
                    ArgumentKind.Argument,
                    typeof(string));

            var argumentDescriptor3 =
                new ArgumentDescriptor("c", "d",
                    ArgumentKind.Argument,
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
                new ArgumentDescriptor("a", "b",
                    ArgumentKind.CancellationToken,
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
                new ArgumentDescriptor("a", "b",
                    ArgumentKind.Context,
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
                new ArgumentDescriptor("a", "b",
                    ArgumentKind.Field,
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
                new ArgumentDescriptor("a", "b",
                    ArgumentKind.FieldSelection,
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
                new ArgumentDescriptor("a", "b",
                    ArgumentKind.ObjectType,
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
                new ArgumentDescriptor("a", "b",
                    ArgumentKind.OperationDefinition,
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
                new ArgumentDescriptor("a", "b",
                    ArgumentKind.QueryDocument,
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
                new ArgumentDescriptor("a", "b",
                    ArgumentKind.Schema,
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
                new ArgumentDescriptor("a", "b",
                    ArgumentKind.Service,
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

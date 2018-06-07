using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using HotChocolate.Resolvers;
using HotChocolate.Resolvers.CodeGeneration;
using HotChocolate.Types;
using Moq;
using Xunit;

namespace HotChocolate.Execution
{
    public class AsyncResolverMethodGeneratorTests
    {
        [Fact]
        public void AsyncResolverMethodGenerator_GenerateWithoutArguments()
        {
            // arrange
            Type sourceType = typeof(GeneratorTestDummy);
            MethodInfo method = typeof(GeneratorTestDummyResolver).GetMethods()
                .Single(t => t.Name == "GetFooAsync" && t.GetParameters().Length == 0);
            FieldResolverDescriptor descriptor = FieldResolverDescriptor
                .CreateCollectionMethod(new FieldReference("Foo", "bar"),
                    method.ReflectedType, sourceType, method, true,
                    Enumerable.Empty<FieldResolverArgumentDescriptor>());

            // act
            StringBuilder source = new StringBuilder();
            AsyncResolverMethodGenerator generator = new AsyncResolverMethodGenerator();
            string result = generator.Generate("abc", descriptor);

            // assert
            Assert.Equal(Snapshot.Current(), Snapshot.New(result));
        }


        [Fact]
        public void AsyncResolverMethodGenerator_GenerateWithSourceArgument()
        {
            // arrange
            FieldResolverArgumentDescriptor argumentDescriptor =
                FieldResolverArgumentDescriptor.Create("a",
                    FieldResolverArgumentKind.Source,
                    typeof(GeneratorTestDummy));

            Type sourceType = typeof(GeneratorTestDummy);
            MethodInfo method = typeof(GeneratorTestDummyResolver).GetMethods()
                .Single(t => t.Name == "GetFooAsync" && t.GetParameters().Length == 1);
            FieldResolverDescriptor descriptor = FieldResolverDescriptor
                .CreateCollectionMethod(new FieldReference("Foo", "bar"),
                    method.ReflectedType, sourceType, method, true,
                    new[] { argumentDescriptor });

            // act
            StringBuilder source = new StringBuilder();
            AsyncResolverMethodGenerator generator = new AsyncResolverMethodGenerator();
            string result = generator.Generate("abc", descriptor);

            // assert
            Assert.Equal(Snapshot.Current(), Snapshot.New(result));
        }

        [Fact]
        public void AsyncResolverMethodGenerator_GenerateWithSourceArgumentAndArgument()
        {
            // arrange
            FieldResolverArgumentDescriptor argumentDescriptor1 =
                FieldResolverArgumentDescriptor.Create("a",
                    FieldResolverArgumentKind.Source,
                    typeof(GeneratorTestDummy));

            FieldResolverArgumentDescriptor argumentDescriptor2 =
                FieldResolverArgumentDescriptor.Create("b",
                    FieldResolverArgumentKind.Argument,
                    typeof(string));

            Type sourceType = typeof(GeneratorTestDummy);
            MethodInfo method = typeof(GeneratorTestDummyResolver).GetMethods()
                .Single(t => t.Name == "GetFooAsync" && t.GetParameters().Length == 2);
            FieldResolverDescriptor descriptor = FieldResolverDescriptor
                .CreateCollectionMethod(new FieldReference("Foo", "bar"),
                    method.ReflectedType, sourceType, method, true,
                    new[] { argumentDescriptor1, argumentDescriptor2 });

            // act
            StringBuilder source = new StringBuilder();
            AsyncResolverMethodGenerator generator = new AsyncResolverMethodGenerator();
            string result = generator.Generate("abc", descriptor);

            // assert
            Assert.Equal(Snapshot.Current(), Snapshot.New(result));
        }

        [Fact]
        public void AsyncResolverMethodGenerator_GenerateWithSourceArgumentAndTwoArguments()
        {
            // arrange
            FieldResolverArgumentDescriptor argumentDescriptor1 =
                FieldResolverArgumentDescriptor.Create("a",
                    FieldResolverArgumentKind.Source,
                    typeof(GeneratorTestDummy));

            FieldResolverArgumentDescriptor argumentDescriptor2 =
                FieldResolverArgumentDescriptor.Create("b",
                    FieldResolverArgumentKind.Argument,
                    typeof(string));

            FieldResolverArgumentDescriptor argumentDescriptor3 =
                FieldResolverArgumentDescriptor.Create("c",
                    FieldResolverArgumentKind.Argument,
                    typeof(int));

            Type sourceType = typeof(GeneratorTestDummy);
            MethodInfo method = typeof(GeneratorTestDummyResolver).GetMethods()
                .Single(t => t.Name == "GetFooAsync" && t.GetParameters().Length == 3);
            FieldResolverDescriptor descriptor = FieldResolverDescriptor
                .CreateCollectionMethod(new FieldReference("Foo", "bar"),
                    method.ReflectedType, sourceType, method, true,
                    new[] { argumentDescriptor1, argumentDescriptor2, argumentDescriptor3 });

            // act
            StringBuilder source = new StringBuilder();
            AsyncResolverMethodGenerator generator = new AsyncResolverMethodGenerator();
            string result = generator.Generate("abc", descriptor);

            // assert
            Assert.Equal(Snapshot.Current(), Snapshot.New(result));
        }
    }

    public class GeneratorTestDummy
    {
        public Task<string> GetFooAsync()
        {
            throw new NotImplementedException();
        }

        public Task<string> GetFooAsync(string a)
        {
            throw new NotImplementedException();
        }

        public Task<string> GetFooAsync(string a, int b)
        {
            throw new NotImplementedException();
        }
    }

    public class GeneratorTestDummyResolver
    {
        public Task<string> GetFooAsync()
        {
            throw new NotImplementedException();
        }

        public Task<string> GetFooAsync(GeneratorTestDummy a)
        {
            throw new NotImplementedException();
        }

        public Task<string> GetFooAsync(GeneratorTestDummy a, string b)
        {
            throw new NotImplementedException();
        }

        public Task<string> GetFooAsync(GeneratorTestDummy a, string b, int c)
        {
            throw new NotImplementedException();
        }
    }
}

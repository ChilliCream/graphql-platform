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
    public class AsyncSourceMethodGeneratorTests
    {
        [Fact]
        public void GenerateWithoutArguments()
        {
            // arrange
            MethodInfo method = typeof(GeneratorTestDummy).GetMethods()
                .Single(t => t.Name == "GetFooAsync" && t.GetParameters().Length == 0);
            FieldResolverDescriptor descriptor = FieldResolverDescriptor
                .CreateCollectionMethod(new FieldReference("Foo", "bar"),
                    method.ReflectedType, method.ReflectedType, method, true,
                    Enumerable.Empty<FieldResolverArgumentDescriptor>());

            // act
            StringBuilder source = new StringBuilder();
            AsyncSourceMethodGenerator generator = new AsyncSourceMethodGenerator();
            string result = generator.Generate("abc", descriptor);

            // assert
            Assert.Equal(Snapshot.Current(), Snapshot.New(result));
        }


        [Fact]
        public void GenerateWithOneArgument()
        {
            // arrange
            FieldResolverArgumentDescriptor argumentDescriptor =
                FieldResolverArgumentDescriptor.Create("a",
                    FieldResolverArgumentKind.Argument,
                    typeof(string));

            MethodInfo method = typeof(GeneratorTestDummy).GetMethods()
                .Single(t => t.Name == "GetFooAsync" && t.GetParameters().Length == 1);
            FieldResolverDescriptor descriptor = FieldResolverDescriptor
                .CreateCollectionMethod(new FieldReference("Foo", "bar"),
                    method.ReflectedType, method.ReflectedType, method, true,
                    new[] { argumentDescriptor });

            // act
            StringBuilder source = new StringBuilder();
            AsyncSourceMethodGenerator generator = new AsyncSourceMethodGenerator();
            string result = generator.Generate("abc", descriptor);

            // assert
            Assert.Equal(Snapshot.Current(), Snapshot.New(result));
        }

        [Fact]
        public void GenerateWithTwoArgument()
        {
            // arrange
            FieldResolverArgumentDescriptor argumentDescriptor1 =
                FieldResolverArgumentDescriptor.Create("a",
                    FieldResolverArgumentKind.Argument,
                    typeof(string));

            FieldResolverArgumentDescriptor argumentDescriptor2 =
                FieldResolverArgumentDescriptor.Create("b",
                    FieldResolverArgumentKind.Argument,
                    typeof(int));

            MethodInfo method = typeof(GeneratorTestDummy).GetMethods()
                .Single(t => t.Name == "GetFooAsync" && t.GetParameters().Length == 2);
            FieldResolverDescriptor descriptor = FieldResolverDescriptor
                .CreateCollectionMethod(new FieldReference("Foo", "bar"),
                    method.ReflectedType, method.ReflectedType, method, true,
                    new[] { argumentDescriptor1, argumentDescriptor2 });

            // act
            StringBuilder source = new StringBuilder();
            AsyncSourceMethodGenerator generator = new AsyncSourceMethodGenerator();
            string result = generator.Generate("abc", descriptor);

            // assert
            Assert.Equal(Snapshot.Current(), Snapshot.New(result));
        }
    }
}

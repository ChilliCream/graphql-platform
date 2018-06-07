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

namespace HotChocolate.Resolvers
{
    public class SyncSourceMethodGeneratorTests
    {
        [Fact]
        public void SyncSourceMethodGenerator_GenerateWithoutArguments()
        {
            // arrange
            MethodInfo method = typeof(GeneratorTestDummy).GetMethods()
                .Single(t => t.Name == "GetFoo" && t.GetParameters().Length == 0);
            FieldResolverDescriptor descriptor = FieldResolverDescriptor
                .CreateSourceMethod(new FieldReference("Foo", "bar"),
                    method.ReflectedType, method, false,
                    Enumerable.Empty<FieldResolverArgumentDescriptor>());

            // act
            StringBuilder source = new StringBuilder();
            SyncSourceMethodGenerator generator = new SyncSourceMethodGenerator();
            string result = generator.Generate("abc", descriptor);

            // assert
            Assert.Equal(Snapshot.Current(), Snapshot.New(result));
        }


        [Fact]
        public void SyncSourceMethodGenerator_GenerateWithOneArgument()
        {
            // arrange
            FieldResolverArgumentDescriptor argumentDescriptor =
                FieldResolverArgumentDescriptor.Create("a",
                    FieldResolverArgumentKind.Argument,
                    typeof(string));

            MethodInfo method = typeof(GeneratorTestDummy).GetMethods()
                .Single(t => t.Name == "GetFoo" && t.GetParameters().Length == 1);
            FieldResolverDescriptor descriptor = FieldResolverDescriptor
                .CreateSourceMethod(new FieldReference("Foo", "bar"),
                    method.ReflectedType, method, false,
                    new[] { argumentDescriptor });

            // act
            StringBuilder source = new StringBuilder();
            SyncSourceMethodGenerator generator = new SyncSourceMethodGenerator();
            string result = generator.Generate("abc", descriptor);

            // assert
            Assert.Equal(Snapshot.Current(), Snapshot.New(result));
        }

        [Fact]
        public void SyncSourceMethodGenerator_GenerateWithTwoArgument()
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
                .Single(t => t.Name == "GetFoo" && t.GetParameters().Length == 2);
            FieldResolverDescriptor descriptor = FieldResolverDescriptor
                .CreateSourceMethod(new FieldReference("Foo", "bar"),
                    method.ReflectedType, method, false,
                    new[] { argumentDescriptor1, argumentDescriptor2 });

            // act
            StringBuilder source = new StringBuilder();
            SyncSourceMethodGenerator generator = new SyncSourceMethodGenerator();
            string result = generator.Generate("abc", descriptor);

            // assert
            Assert.Equal(Snapshot.Current(), Snapshot.New(result));
        }
    }
}

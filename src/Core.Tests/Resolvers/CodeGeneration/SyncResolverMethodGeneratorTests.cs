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
    public class SyncResolverMethodGeneratorTests
    {
        [Fact]
        public void SyncResolverMethodGenerator_GenerateWithoutArguments()
        {
            // arrange
            Type sourceType = typeof(GeneratorTestDummy);
            MethodInfo method = typeof(GeneratorTestDummyResolver).GetMethods()
                .Single(t => t.Name == "GetFoo" && t.GetParameters().Length == 0);
            FieldResolverDescriptor descriptor = FieldResolverDescriptor
                .CreateCollectionMethod(new FieldReference("Foo", "bar"),
                    method.ReflectedType, sourceType, method, false,
                    Enumerable.Empty<Types.ArgumentDescriptor>());

            // act
            StringBuilder source = new StringBuilder();
            SyncResolverMethodGenerator generator = new SyncResolverMethodGenerator();
            string result = generator.Generate("abc", descriptor);

            // assert
            Assert.Equal(Snapshot.Current(), Snapshot.New(result));
        }


        [Fact]
        public void SyncResolverMethodGenerator_GenerateWithSourceArgument()
        {
            // arrange
            Types.ArgumentDescriptor argumentDescriptor =
                new Types.ArgumentDescriptor("a", "b",
                    ArgumentKind.Source,
                    typeof(GeneratorTestDummy));

            Type sourceType = typeof(GeneratorTestDummy);
            MethodInfo method = typeof(GeneratorTestDummyResolver).GetMethods()
                .Single(t => t.Name == "GetFoo" && t.GetParameters().Length == 1);
            FieldResolverDescriptor descriptor = FieldResolverDescriptor
                .CreateCollectionMethod(new FieldReference("Foo", "bar"),
                    method.ReflectedType, sourceType, method, false,
                    new[] { argumentDescriptor });

            // act
            StringBuilder source = new StringBuilder();
            SyncResolverMethodGenerator generator = new SyncResolverMethodGenerator();
            string result = generator.Generate("abc", descriptor);

            // assert
            Assert.Equal(Snapshot.Current(), Snapshot.New(result));
        }

        [Fact]
        public void SyncResolverMethodGenerator_GenerateWithSourceArgumentAndArgument()
        {
            // arrange
            Types.ArgumentDescriptor argumentDescriptor1 =
                new Types.ArgumentDescriptor("a", "b",
                    ArgumentKind.Source,
                    typeof(GeneratorTestDummy));

            Types.ArgumentDescriptor argumentDescriptor2 =
                new Types.ArgumentDescriptor("b", "c",
                    ArgumentKind.Argument,
                    typeof(string));

            Type sourceType = typeof(GeneratorTestDummy);
            MethodInfo method = typeof(GeneratorTestDummyResolver).GetMethods()
                .Single(t => t.Name == "GetFoo" && t.GetParameters().Length == 2);
            FieldResolverDescriptor descriptor = FieldResolverDescriptor
                .CreateCollectionMethod(new FieldReference("Foo", "bar"),
                    method.ReflectedType, sourceType, method, false,
                    new[] { argumentDescriptor1, argumentDescriptor2 });

            // act
            StringBuilder source = new StringBuilder();
            SyncResolverMethodGenerator generator = new SyncResolverMethodGenerator();
            string result = generator.Generate("abc", descriptor);

            // assert
            Assert.Equal(Snapshot.Current(), Snapshot.New(result));
        }

        [Fact]
        public void SyncResolverMethodGenerator_GenerateWithSourceArgumentAndTwoArguments()
        {
            // arrange
            Types.ArgumentDescriptor argumentDescriptor1 =
                new Types.ArgumentDescriptor("a", "b",
                    ArgumentKind.Source,
                    typeof(GeneratorTestDummy));

            Types.ArgumentDescriptor argumentDescriptor2 =
                new Types.ArgumentDescriptor("b", "c",
                    ArgumentKind.Argument,
                    typeof(string));

            Types.ArgumentDescriptor argumentDescriptor3 =
                new Types.ArgumentDescriptor("c", "d",
                    ArgumentKind.Argument,
                    typeof(int));

            Type sourceType = typeof(GeneratorTestDummy);
            MethodInfo method = typeof(GeneratorTestDummyResolver).GetMethods()
                .Single(t => t.Name == "GetFoo" && t.GetParameters().Length == 3);
            FieldResolverDescriptor descriptor = FieldResolverDescriptor
                .CreateCollectionMethod(new FieldReference("Foo", "bar"),
                    method.ReflectedType, sourceType, method, false,
                    new[] { argumentDescriptor1, argumentDescriptor2, argumentDescriptor3 });

            // act
            StringBuilder source = new StringBuilder();
            SyncResolverMethodGenerator generator = new SyncResolverMethodGenerator();
            string result = generator.Generate("abc", descriptor);

            // assert
            Assert.Equal(Snapshot.Current(), Snapshot.New(result));
        }
    }
}

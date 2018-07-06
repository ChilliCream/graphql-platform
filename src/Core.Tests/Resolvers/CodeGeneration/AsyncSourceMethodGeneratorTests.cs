using System.Linq;
using System.Reflection;
using System.Text;
using HotChocolate.Resolvers.CodeGeneration;
using Xunit;

namespace HotChocolate.Resolvers
{
    public class AsyncSourceMethodGeneratorTests
    {
        [Fact]
        public void AsyncSourceMethodGenerator_GenerateWithoutArguments()
        {
            // arrange
            MethodInfo method = typeof(GeneratorTestDummy).GetMethods()
                .Single(t => t.Name == "GetFooAsync" && t.GetParameters().Length == 0);
            var descriptor = FieldResolverDescriptor
                .CreateSourceMethod(new FieldReference("Foo", "bar"),
                    method.ReflectedType, method, true,
                    Enumerable.Empty<FieldResolverArgumentDescriptor>());

            // act
            var source = new StringBuilder();
            var generator = new AsyncSourceMethodGenerator();
            string result = generator.Generate("abc", descriptor);

            // assert
            Assert.Equal(Snapshot.Current(), Snapshot.New(result));
        }


        [Fact]
        public void AsyncSourceMethodGenerator_GenerateWithOneArgument()
        {
            // arrange
            FieldResolverArgumentDescriptor argumentDescriptor =
                new FieldResolverArgumentDescriptor("a", "b",
                    FieldResolverArgumentKind.Argument,
                    typeof(string));

            MethodInfo method = typeof(GeneratorTestDummy).GetMethods()
                .Single(t => t.Name == "GetFooAsync" && t.GetParameters().Length == 1);
            var descriptor = FieldResolverDescriptor
                .CreateSourceMethod(new FieldReference("Foo", "bar"),
                    method.ReflectedType, method, true,
                    new[] { argumentDescriptor });

            // act
            var source = new StringBuilder();
            var generator = new AsyncSourceMethodGenerator();
            string result = generator.Generate("abc", descriptor);

            // assert
            Assert.Equal(Snapshot.Current(), Snapshot.New(result));
        }

        [Fact]
        public void AsyncSourceMethodGenerator_GenerateWithTwoArgument()
        {
            // arrange
            var argumentDescriptor1 =
                new FieldResolverArgumentDescriptor("a", "b",
                    FieldResolverArgumentKind.Argument,
                    typeof(string));

            var argumentDescriptor2 =
                new FieldResolverArgumentDescriptor("b", "c",
                    FieldResolverArgumentKind.Argument,
                    typeof(int));

            MethodInfo method = typeof(GeneratorTestDummy).GetMethods()
                .Single(t => t.Name == "GetFooAsync" && t.GetParameters().Length == 2);
            var descriptor = FieldResolverDescriptor
                .CreateSourceMethod(new FieldReference("Foo", "bar"),
                    method.ReflectedType, method, true,
                    new[] { argumentDescriptor1, argumentDescriptor2 });

            // act
            var source = new StringBuilder();
            var generator = new AsyncSourceMethodGenerator();
            string result = generator.Generate("abc", descriptor);

            // assert
            Assert.Equal(Snapshot.Current(), Snapshot.New(result));
        }
    }
}

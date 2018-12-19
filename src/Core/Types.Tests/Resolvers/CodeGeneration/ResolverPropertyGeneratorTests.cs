using System;
using System.Linq;
using System.Reflection;
using System.Text;
using ChilliCream.Testing;
using HotChocolate.Resolvers.CodeGeneration;
using Xunit;

namespace HotChocolate.Resolvers
{
    public class ResolverPropertyGeneratorTests
    {
        [Fact]
        public void ResolverPropertyGenerator_Generate()
        {
            // arrange
            Type sourceType = typeof(GeneratorTestDummy);

            var fieldMember = new FieldMember(
                "Foo", "bar",
                GetProperty());

            var descriptor = new ResolverDescriptor(sourceType, fieldMember);

            // act
            var source = new StringBuilder();
            var generator = new ResolverPropertyGenerator();
            string result = generator.Generate("abc", descriptor);

            // assert
            result.Snapshot();
        }

        private PropertyInfo GetProperty()
        {
            return typeof(GeneratorTestDummyResolver)
                .GetProperties().Single();
        }
    }
}

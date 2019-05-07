using System;
using System.Linq;
using System.Reflection;
using System.Text;
using HotChocolate.Resolvers.CodeGeneration;
using Snapshooter.Xunit;
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
            result.MatchSnapshot();
        }

        private PropertyInfo GetProperty()
        {
            return typeof(GeneratorTestDummyResolver)
                .GetProperties().Single();
        }
    }
}

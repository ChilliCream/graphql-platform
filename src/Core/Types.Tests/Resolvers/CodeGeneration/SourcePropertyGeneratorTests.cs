using System.Linq;
using System.Reflection;
using System.Text;
using HotChocolate.Resolvers.CodeGeneration;
using Snapshooter.Xunit;
using Xunit;

namespace HotChocolate.Resolvers
{
    public class SourcePropertyGeneratorTests
    {
        [Fact]
        public void SourcePropertyGenerator_Generate()
        {
            // arrange
            var fieldMember = new FieldMember(
                "Foo", "bar",
                GetProperty());

            var descriptor = new SourceResolverDescriptor(fieldMember);

            // act
            var source = new StringBuilder();
            var generator = new SourcePropertyGenerator();
            string result = generator.Generate("abc", descriptor);

            // assert
            result.MatchSnapshot();
        }

        private PropertyInfo GetProperty()
        {
            return typeof(GeneratorTestDummy)
                .GetProperties().Single();
        }
    }
}

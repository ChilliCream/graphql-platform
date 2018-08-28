using System.Linq;
using System.Reflection;
using System.Text;
using HotChocolate.Resolvers.CodeGeneration;
using Xunit;

namespace HotChocolate.Resolvers
{
    public class SourcePropertyGeneratorTests
    {
        [Fact]
        public void SourcePropertyGenerator_Generate()
        {
            // arrange
            PropertyInfo property = typeof(GeneratorTestDummy).GetProperties().Single();
            var descriptor = FieldResolverDescriptor
                .CreateSourceProperty(new FieldReference("Foo", "bar"),
                    property.ReflectedType, property);

            // act
            var source = new StringBuilder();
            var generator = new SourcePropertyGenerator();
            string result = generator.Generate("abc", descriptor);

            // assert
            Assert.Equal(Snapshot.Current(), Snapshot.New(result));
        }
    }
}

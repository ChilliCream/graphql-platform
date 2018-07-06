using System.Linq;
using System.Reflection;
using System.Text;
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
            PropertyInfo property = typeof(GeneratorTestDummyResolver)
                .GetProperties().Single();
            
            var descriptor = FieldResolverDescriptor
                .CreateCollectionProperty(new FieldReference("Foo", "bar"),
                    property.ReflectedType, typeof(GeneratorTestDummy), property);

            // act
            var source = new StringBuilder();
            var generator = new ResolverPropertyGenerator();
            string result = generator.Generate("abc", descriptor);

            // assert
            Assert.Equal(Snapshot.Current(), Snapshot.New(result));
        }
    }
}

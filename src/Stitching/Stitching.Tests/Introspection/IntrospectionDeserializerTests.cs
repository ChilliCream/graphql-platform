using ChilliCream.Testing;
using HotChocolate.Language;
using Xunit;

namespace HotChocolate.Stitching.Introspection
{
    public class IntrospectionDeserializerTests
    {
        [Fact]
        public void foo()
        {
            string json = FileResource.Open("StarWarsIntrospectionResult.json");

            DocumentNode schema = IntrospectionDeserializer.Deserialize(json);

            SchemaSyntaxSerializer.Serialize(schema).Snapshot();
        }


    }
}

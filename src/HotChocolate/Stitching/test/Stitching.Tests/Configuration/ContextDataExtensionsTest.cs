using System.Collections.Generic;
using Xunit;

namespace HotChocolate.Stitching.Configuration
{
    public class ContextDataExtensionsTest
    {
        [Fact]
        public void AddNameLookup_Single()
        {
            // arrange
            ISchemaBuilder schemaBuilder = SchemaBuilder.New().AddQueryType<Query>();

            // act
            schemaBuilder.AddNameLookup("OriginalType1", "NewType1", "Schema1");
            schemaBuilder.AddNameLookup("OriginalType2", "NewType2", "Schema2");

            // assert
            IReadOnlyDictionary<(NameString, NameString), NameString> lookup =
                schemaBuilder.Create().GetNameLookup();
            Assert.Equal("OriginalType1", lookup[("NewType1", "Schema1")]);
            Assert.Equal("OriginalType2", lookup[("NewType2", "Schema2")]);
        }

        [Fact]
        public void AddNameLookup_Multiple()
        {
            // arrange
            ISchemaBuilder schemaBuilder = SchemaBuilder.New().AddQueryType<Query>();
            var dict = new Dictionary<(NameString, NameString), NameString>
            {
                { ("OriginalType1", "Schema1"), "NewType1" },
                { ("OriginalType2", "Schema2"), "NewType2" }
            };

            // act
            schemaBuilder.AddNameLookup(dict);

            // assert
            IReadOnlyDictionary<(NameString, NameString), NameString> lookup =
                schemaBuilder.Create().GetNameLookup();
            Assert.Equal("OriginalType1", lookup[("NewType1", "Schema1")]);
            Assert.Equal("OriginalType2", lookup[("NewType2", "Schema2")]);
        }

        public class Query
        {
            public string Foo { get; }
        }
    }
}

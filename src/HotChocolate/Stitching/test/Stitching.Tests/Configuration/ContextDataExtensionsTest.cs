using System.Collections.Generic;
using HotChocolate.Configuration;
using HotChocolate.Types;
using HotChocolate.Types.Descriptors;
using HotChocolate.Types.Descriptors.Definitions;
using Xunit;

namespace HotChocolate.Stitching.Configuration
{
    public class ContextDataExtensionsTest
    {
        [Fact]
        public void AddNameLookup_Single()
        {
            // arrange
            ISchemaBuilder schemaBuilder = SchemaBuilder.New().AddQueryType<CustomQueryType>();

            // act
            schemaBuilder.AddNameLookup("OriginalType1", "NewType1", "Schema1");
            schemaBuilder.AddNameLookup("OriginalType2", "NewType2", "Schema2");

            // assert
            IReadOnlyDictionary<(NameString, NameString), NameString> lookup =
                schemaBuilder
                    .Create()
                    .GetType<CustomQueryType>(nameof(CustomQueryType))
                    .Context
                    .GetNameLookup();
            Assert.Equal("OriginalType1", lookup[("NewType1", "Schema1")]);
            Assert.Equal("OriginalType2", lookup[("NewType2", "Schema2")]);
        }

        [Fact]
        public void AddNameLookup_Multiple()
        {
            // arrange
            ISchemaBuilder schemaBuilder = SchemaBuilder.New().AddQueryType<CustomQueryType>();
            var dict = new Dictionary<(NameString, NameString), NameString>
            {
                { ("NewType1", "Schema1"), "OriginalType1" },
                { ("NewType2", "Schema2"), "OriginalType2" }
            };

            // act
            schemaBuilder.AddNameLookup(dict);

            // assert
            IReadOnlyDictionary<(NameString, NameString), NameString> lookup =
                schemaBuilder
                    .Create()
                    .GetType<CustomQueryType>(nameof(CustomQueryType))
                    .Context
                    .GetNameLookup();
            Assert.Equal("OriginalType1", lookup[("NewType1", "Schema1")]);
            Assert.Equal("OriginalType2", lookup[("NewType2", "Schema2")]);
        }

        public class CustomQueryType : ObjectType
        {
            public IDescriptorContext Context { get; set; }

            protected override void Configure(IObjectTypeDescriptor descriptor)
            {
                descriptor.Field("foo").Resolve("bar");
            }

            protected override ObjectTypeDefinition CreateDefinition(ITypeDiscoveryContext context)
            {
                Context = context.DescriptorContext;

                return base.CreateDefinition(context);
            }
        }
    }
}

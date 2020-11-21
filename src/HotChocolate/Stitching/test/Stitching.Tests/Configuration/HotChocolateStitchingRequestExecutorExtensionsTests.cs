using System.Collections.Generic;
using System.Threading.Tasks;
using HotChocolate.Configuration;
using HotChocolate.Execution;
using HotChocolate.Execution.Configuration;
using HotChocolate.Types;
using HotChocolate.Types.Descriptors;
using HotChocolate.Types.Descriptors.Definitions;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace HotChocolate.Stitching.Configuration
{
    public class HotChocolateStitchingRequestExecutorExtensionsTests
    {
        [Fact]
        public async Task RewriteType()
        {
            // arrange
            IRequestExecutorBuilder executorBuilder =
                new ServiceCollection().AddGraphQL().AddQueryType<CustomQueryType>();

            // act
            executorBuilder.RewriteType("OriginalType1", "NewType1", "Schema1");
            executorBuilder.RewriteType("OriginalType2", "NewType2", "Schema2");

            // assert
            ISchema schema = await executorBuilder.BuildSchemaAsync();
            IReadOnlyDictionary<(NameString, NameString), NameString> lookup =
                schema
                    .GetType<CustomQueryType>(nameof(CustomQueryType))
                    .Context
                    .GetNameLookup();
            Assert.Equal("OriginalType1", lookup[("NewType1", "Schema1")]);
            Assert.Equal("OriginalType2", lookup[("NewType2", "Schema2")]);
        }
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

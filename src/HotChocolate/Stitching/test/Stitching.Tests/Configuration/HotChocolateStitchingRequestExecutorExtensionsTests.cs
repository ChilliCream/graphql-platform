using System.Collections.Generic;
using System.Threading.Tasks;
using HotChocolate.Execution;
using HotChocolate.Execution.Configuration;
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
                new ServiceCollection().AddGraphQL().AddQueryType<Query>();

            // act
            executorBuilder.RenameType("OriginalType1", "NewType1", "Schema1");
            executorBuilder.RenameType("OriginalType2", "NewType2", "Schema2");

            // assert
            ISchema schema = await executorBuilder.BuildSchemaAsync();
            IReadOnlyDictionary<(NameString, NameString), NameString> lookup =
                schema.GetNameLookup();
            Assert.Equal("OriginalType1", lookup[("NewType1", "Schema1")]);
            Assert.Equal("OriginalType2", lookup[("NewType2", "Schema2")]);
        }
    }

    public class Query
    {
        public string Foo { get; }
    }
}

using System.Linq;
using System.Collections.Generic;
using HotChocolate.Language;
using HotChocolate.Language.Utilities;
using Xunit;
using Snapshooter.Xunit;

namespace HotChocolate.Stitching.Merge.Handlers
{
    public class UnionTypeMergeHandlerTests
    {
        [Fact]
        public void MergeUnionTypes()
        {
            // arrange
            DocumentNode schema_a =
                Utf8GraphQLParser.Parse("union Foo = Bar | Baz");
            DocumentNode schema_b =
                Utf8GraphQLParser.Parse("union Foo = Bar | Baz");

            var types = new List<ITypeInfo>
            {
                TypeInfo.Create(
                    schema_a.Definitions.OfType<ITypeDefinitionNode>().First(),
                    new SchemaInfo("Schema_A", schema_a)),
                TypeInfo.Create(
                    schema_b.Definitions.OfType<ITypeDefinitionNode>().First(),
                    new SchemaInfo("Schema_B", schema_b))
            };

            var context = new SchemaMergeContext();

            // act
            var typeMerger = new UnionTypeMergeHandler((c, t) => { });
            typeMerger.Merge(context, types);

            // assert
            context
                .CreateSchema()
                .Print()
                .MatchSnapshot();
        }
    }
}

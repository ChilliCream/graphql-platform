using System.Collections.Generic;
using System.Linq;
using HotChocolate.Language;
using Snapshooter.Xunit;
using Xunit;

namespace HotChocolate.Stitching.Merge.Handlers
{
    public class InputUnionTypeMergeHandlerTests
    {
        [Fact]
        public void MergeInputUnionTypes()
        {
            // arrange
            DocumentNode schema_a =
                Utf8GraphQLParser.Parse("inputunion Foo = Bar | Baz");
            DocumentNode schema_b =
                Utf8GraphQLParser.Parse("inputunion Foo = Bar | Baz");

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
            var typeMerger = new InputUnionTypeMergeHandler((c, t) => { });
            typeMerger.Merge(context, types);

            // assert
            SchemaSyntaxSerializer.Serialize(context.CreateSchema())
                .MatchSnapshot();
        }
    }
}

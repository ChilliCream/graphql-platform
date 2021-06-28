using System.Linq;
using System.Collections.Generic;
using HotChocolate.Language;
using HotChocolate.Language.Utilities;
using Xunit;
using Snapshooter.Xunit;

namespace HotChocolate.Stitching.Merge.Handlers
{
    public class RootTypeMergeHandlerTests
    {
        [Fact]
        public void Merge_RootTypeWithNoCollisions_TypeMerges()
        {
            // arrange
            DocumentNode schema_a =
                Utf8GraphQLParser.Parse("type Query { a: String }");
            DocumentNode schema_b =
                Utf8GraphQLParser.Parse("type Query { b: String }");

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
            var typeMerger = new RootTypeMergeHandler((c, t) => { });
            typeMerger.Merge(context, types);

            // assert
            context
                .CreateSchema()
                .Print()
                .MatchSnapshot();
        }

        [Fact]
        public void Merge_RootTypeWithCollisions_CollidingFieldsAreRenamed()
        {
            // arrange
            DocumentNode schema_a =
                Utf8GraphQLParser.Parse("type Query { a: String }");
            DocumentNode schema_b =
                Utf8GraphQLParser.Parse("type Query { a: String }");

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
            var typeMerger = new RootTypeMergeHandler((c, t) => { });
            typeMerger.Merge(context, types);

            // assert
            context
                .CreateSchema()
                .Print()
                .MatchSnapshot();
        }
    }
}

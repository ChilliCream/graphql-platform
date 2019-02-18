using System.Linq;
using System.Collections.Generic;
using HotChocolate.Language;
using ChilliCream.Testing;
using Xunit;

namespace HotChocolate.Stitching
{
    public class UnionTypeMergeHandlerTests
    {
        [Fact]
        public void MergeUnionTypes()
        {
            // arrange
            DocumentNode schema_a =
                Parser.Default.Parse("union Foo = Bar | Baz");
            DocumentNode schema_b =
                Parser.Default.Parse("union Foo = Bar | Baz");

            var types = new List<ITypeInfo>
            {
                new TypeInfo(
                    schema_a.Definitions.OfType<ITypeDefinitionNode>().First(),
                    new SchemaInfo("Schema_A", schema_a)),
                new TypeInfo(
                    schema_b.Definitions.OfType<ITypeDefinitionNode>().First(),
                    new SchemaInfo("Schema_B", schema_b))
            };

            var context = new SchemaMergeContext();

            // act
            var typeMerger = new UnionTypeMergeHandler((c, t) => { });
            typeMerger.Merge(context, types);

            // assert
            SchemaSyntaxSerializer.Serialize(context.CreateSchema()).Snapshot();
        }
    }
}

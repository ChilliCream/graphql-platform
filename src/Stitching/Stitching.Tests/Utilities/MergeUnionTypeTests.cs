using System.Linq;
using System.Collections.Generic;
using HotChocolate.Language;
using ChilliCream.Testing;
using Xunit;

namespace HotChocolate.Stitching
{
    public class MergeUnionTypeTests
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
                    schema_a,
                    "Schema_A"),
                new TypeInfo(
                    schema_b.Definitions.OfType<ITypeDefinitionNode>().First(),
                    schema_b,
                    "Schema_B"),
            };

            var context = new MergeSchemaContext();

            // act
            var typeMerger = new MergeUnionType((c, t) => { });
            typeMerger.Merge(context, types);

            // assert
            SchemaSyntaxSerializer.Serialize(context.CreateSchema()).Snapshot();
        }
    }
}

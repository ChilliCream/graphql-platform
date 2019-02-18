using System.Linq;
using System.Collections.Generic;
using HotChocolate.Language;
using ChilliCream.Testing;
using Xunit;

namespace HotChocolate.Stitching.Merge.Handlers
{
    public class InputObjectTypeMergeHandlerTests
    {
        [Fact]
        public void Merge_SimpleIdenticalInputs_TypeMerges()
        {
            // arrange
            DocumentNode schema_a =
                Parser.Default.Parse("input A { b: String }");
            DocumentNode schema_b =
                Parser.Default.Parse("input A { b: String }");

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
            var typeMerger = new InputObjectTypeMergeHandler((c, t) => { });
            typeMerger.Merge(context, types);

            // assert
            SchemaSyntaxSerializer.Serialize(context.CreateSchema()).Snapshot();
        }

        [Fact]
        public void Merge_ThreeInputsWhereTwoAreIdentical_TwoTypesAfterMerge()
        {
            // arrange
            DocumentNode schema_a =
                Parser.Default.Parse("input A { b: String }");
            DocumentNode schema_b =
                Parser.Default.Parse("input A { b: String c: String }");
            DocumentNode schema_c =
                Parser.Default.Parse("input A { b: String }");

            var types = new List<ITypeInfo>
            {
                new TypeInfo(
                    schema_a.Definitions.OfType<ITypeDefinitionNode>().First(),
                    new SchemaInfo("Schema_A", schema_a)),
                new TypeInfo(
                    schema_b.Definitions.OfType<ITypeDefinitionNode>().First(),
                    new SchemaInfo("Schema_B", schema_b)),
                new TypeInfo(
                    schema_c.Definitions.OfType<ITypeDefinitionNode>().First(),
                    new SchemaInfo("Schema_C", schema_c))
            };

            var context = new SchemaMergeContext();

            // act
            var typeMerger = new InputObjectTypeMergeHandler((c, t) => { });
            typeMerger.Merge(context, types);

            // assert
            SchemaSyntaxSerializer.Serialize(context.CreateSchema()).Snapshot();
        }
    }
}

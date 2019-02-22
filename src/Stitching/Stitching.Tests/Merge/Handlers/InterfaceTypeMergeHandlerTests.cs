using System.Linq;
using System.Collections.Generic;
using HotChocolate.Language;
using Xunit;
using Snapshooter.Xunit;

namespace HotChocolate.Stitching.Merge.Handlers
{
    public class InterfaceTypeMergeHandlerTests
    {
        [Fact]
        public void Merge_SimpleIdenticalInterfaces_TypeMerges()
        {
            // arrange
            DocumentNode schema_a =
                Parser.Default.Parse("interface A { b: String }");
            DocumentNode schema_b =
                Parser.Default.Parse("interface A { b: String }");

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
            var typeMerger = new InterfaceTypeMergeHandler((c, t) => { });
            typeMerger.Merge(context, types);

            // assert
            SchemaSyntaxSerializer.Serialize(context.CreateSchema())
                .MatchSnapshot();
        }

        [Fact]
        public void Merge_ThreeInterfWhereTwoAreIdentical_TwoTypesAfterMerge()
        {
            // arrange
            DocumentNode schema_a =
                Parser.Default.Parse("interface A { b: String }");
            DocumentNode schema_b =
                Parser.Default.Parse("interface A { b(a: String): String }");
            DocumentNode schema_c =
                Parser.Default.Parse("interface A { b: String }");

            var types = new List<ITypeInfo>
            {
                TypeInfo.Create(
                    schema_a.Definitions.OfType<ITypeDefinitionNode>().First(),
                    new SchemaInfo("Schema_A", schema_a)),
                TypeInfo.Create(
                    schema_b.Definitions.OfType<ITypeDefinitionNode>().First(),
                    new SchemaInfo("Schema_B", schema_b)),
                TypeInfo.Create(
                    schema_c.Definitions.OfType<ITypeDefinitionNode>().First(),
                    new SchemaInfo("Schema_C", schema_c))
            };

            var context = new SchemaMergeContext();

            // act
            var typeMerger = new InterfaceTypeMergeHandler((c, t) => { });
            typeMerger.Merge(context, types);

            // assert
            SchemaSyntaxSerializer.Serialize(context.CreateSchema())
                .MatchSnapshot();
        }

        [Fact]
        public void Merge_DifferentTypes_InterfMergesLeftoversArePassed()
        {
            // arrange
            DocumentNode schema_a =
                Parser.Default.Parse("interface A { b: String }");
            DocumentNode schema_b =
                Parser.Default.Parse("enum A { B C }");

            var types = new List<ITypeInfo>
            {
                TypeInfo.Create(
                    schema_a.Definitions.OfType<ITypeDefinitionNode>().First(),
                    new SchemaInfo("Schema_A", schema_a)),
                TypeInfo.Create(
                    schema_b.Definitions.OfType<ITypeDefinitionNode>().First(),
                    new SchemaInfo("Schema_B", schema_b)),
            };

            var context = new SchemaMergeContext();

            var leftovers = new List<ITypeInfo>();

            // act
            var typeMerger = new InterfaceTypeMergeHandler(
                (c, t) => leftovers.AddRange(t));
            typeMerger.Merge(context, types);

            // assert
            Assert.Collection(leftovers,
                t => Assert.IsType<EnumTypeInfo>(t));

            Snapshot.Match(new List<object>
            {
                SchemaSyntaxSerializer.Serialize(context.CreateSchema()),
                leftovers
            });
        }
    }
}

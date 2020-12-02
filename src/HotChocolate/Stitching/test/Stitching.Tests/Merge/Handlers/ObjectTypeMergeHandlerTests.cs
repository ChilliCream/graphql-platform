using System.Linq;
using System.Collections.Generic;
using HotChocolate.Language;
using HotChocolate.Language.Utilities;
using Xunit;
using Snapshooter.Xunit;

namespace HotChocolate.Stitching.Merge.Handlers
{
    public class ObjectTypeMergeHandlerTests
    {
        [Fact]
        public void Merge_SimpleIdenticalObjects_TypeMerges()
        {
            // arrange
            DocumentNode schema_a =
                Utf8GraphQLParser.Parse("type A { b: String }");
            DocumentNode schema_b =
                Utf8GraphQLParser.Parse("type A { b: String }");

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
            var typeMerger = new ObjectTypeMergeHandler((c, t) => { });
            typeMerger.Merge(context, types);

            // assert
            context
                .CreateSchema()
                .Print()
                .MatchSnapshot();
        }

        [Fact]
        public void Merge_ThreeObjectsWhereTwoAreIdentical_TwoTypesAfterMerge()
        {
            // arrange
            DocumentNode schema_a =
                Utf8GraphQLParser.Parse("type A { b: String }");
            DocumentNode schema_b =
                Utf8GraphQLParser.Parse("type A { b(a: String): String }");
            DocumentNode schema_c =
                Utf8GraphQLParser.Parse("type A { b: String }");

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
            var typeMerger = new ObjectTypeMergeHandler((c, t) => { });
            typeMerger.Merge(context, types);

            // assert
            context
                .CreateSchema()
                .Print()
                .MatchSnapshot();
        }

        [Fact]
        public void Merge_ObjectWithDifferentInterfaces_TypesMerge()
        {
            // arrange
            DocumentNode schema_a =
                Utf8GraphQLParser.Parse("type A implements IA { b: String }");
            DocumentNode schema_b =
                Utf8GraphQLParser.Parse("type A implements IB { b : String }");
            DocumentNode schema_c =
                Utf8GraphQLParser.Parse("type A implements IC { b: String }");

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
            var typeMerger = new ObjectTypeMergeHandler((c, t) => { });
            typeMerger.Merge(context, types);

            // assert
            context
                .CreateSchema()
                .Print()
                .MatchSnapshot();
        }

        [Fact]
        public void Merge_DifferentTypes_ObjectMergesLeftoversArePassed()
        {
            // arrange
            DocumentNode schema_a =
                Utf8GraphQLParser.Parse("type A { b: String }");
            DocumentNode schema_b =
                Utf8GraphQLParser.Parse("enum A { B C }");

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
            var typeMerger = new ObjectTypeMergeHandler(
                (c, t) => leftovers.AddRange(t));
            typeMerger.Merge(context, types);

            // assert
            Assert.Collection(leftovers,
                t => Assert.IsType<EnumTypeInfo>(t));

            Snapshot.Match(new List<object>
            {
                context.CreateSchema().Print(),
                leftovers
            });
        }
    }
}

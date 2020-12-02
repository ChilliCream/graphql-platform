using System;
using System.Collections.Generic;
using System.Linq;
using HotChocolate.Language;
using HotChocolate.Language.Utilities;
using Snapshooter.Xunit;
using Xunit;

namespace HotChocolate.Stitching.Merge.Handlers
{
    public class DirectiveTypeMergeHandlerTests
    {
        [Fact]
        public void Merge_SimpleIdenticalDirectives_TypeMerges()
        {
            // arrange
            DocumentNode schema_a =
                Utf8GraphQLParser.Parse(
                    "directive @test(arg: String) on OBJECT");
            DocumentNode schema_b =
                Utf8GraphQLParser.Parse(
                    "directive @test(arg: String) on OBJECT");

            var types = new List<IDirectiveTypeInfo>
            {
                new DirectiveTypeInfo(schema_a.Definitions
                    .OfType<DirectiveDefinitionNode>().First(),
                    new SchemaInfo("Schema_A", schema_a)),
                new DirectiveTypeInfo(schema_b.Definitions
                    .OfType<DirectiveDefinitionNode>().First(),
                    new SchemaInfo("Schema_B", schema_b)),
            };

            var context = new SchemaMergeContext();

            // act
            var typeMerger = new DirectiveTypeMergeHandler((c, t) => { });
            typeMerger.Merge(context, types);

            // assert
            context
                .CreateSchema()
                .Print()
                .MatchSnapshot();
        }

        [Fact]
        public void Merge_DifferentArguments_ThrowsException()
        {
            // arrange
            DocumentNode schema_a =
                Utf8GraphQLParser.Parse("directive @test(arg: Int) on OBJECT");
            DocumentNode schema_b =
                Utf8GraphQLParser.Parse("directive @test(arg: String) on OBJECT");

            var types = new List<IDirectiveTypeInfo>
            {
                new DirectiveTypeInfo(schema_a.Definitions
                    .OfType<DirectiveDefinitionNode>().First(),
                    new SchemaInfo("Schema_A", schema_a)),
                new DirectiveTypeInfo(schema_b.Definitions
                    .OfType<DirectiveDefinitionNode>().First(),
                    new SchemaInfo("Schema_B", schema_b))
            };

            var context = new SchemaMergeContext();

            // act
            var typeMerger = new DirectiveTypeMergeHandler((c, t) => { });

            Assert.Throws<InvalidOperationException>(
                () => typeMerger.Merge(context, types));
        }

        [Fact]
        public void Merge_DifferentLocations_ThrowsException()
        {
            // arrange
            DocumentNode schema_a =
                Utf8GraphQLParser.Parse(
                    "directive @test(arg: String) on OBJECT | INTERFACE");
            DocumentNode schema_b =
                Utf8GraphQLParser.Parse(
                    "directive @test(arg: String) on OBJECT");

            var types = new List<IDirectiveTypeInfo>
            {
                new DirectiveTypeInfo(schema_a.Definitions
                    .OfType<DirectiveDefinitionNode>().First(),
                    new SchemaInfo("Schema_A", schema_a)),
                new DirectiveTypeInfo(schema_b.Definitions
                    .OfType<DirectiveDefinitionNode>().First(),
                    new SchemaInfo("Schema_B", schema_b))
            };

            var context = new SchemaMergeContext();

            // act
            var typeMerger = new DirectiveTypeMergeHandler((c, t) => { });

            Assert.Throws<InvalidOperationException>(
                () => typeMerger.Merge(context, types));
        }

        [Fact]
        public void Merge_DifferentRepeatable_ThrowsException()
        {
            // arrange
            DocumentNode schema_a =
                Utf8GraphQLParser.Parse(
                    "directive @test(arg: String) repeatable on OBJECT");
            DocumentNode schema_b =
                Utf8GraphQLParser.Parse(
                    "directive @test(arg: String) on OBJECT");

            var types = new List<IDirectiveTypeInfo>
            {
                new DirectiveTypeInfo(schema_a.Definitions
                    .OfType<DirectiveDefinitionNode>().First(),
                    new SchemaInfo("Schema_A", schema_a)),
                new DirectiveTypeInfo(schema_b.Definitions
                    .OfType<DirectiveDefinitionNode>().First(),
                    new SchemaInfo("Schema_B", schema_b))
            };

            var context = new SchemaMergeContext();

            // act
            var typeMerger = new DirectiveTypeMergeHandler((c, t) => { });

            Assert.Throws<InvalidOperationException>(
                () => typeMerger.Merge(context, types));
        }

        [Fact]
        public void Merge_ThreeDirectivessWhereTwoAreIdentical_TwoTypesAfterMerge()
        {
            // arrange
            DocumentNode schema_a =
                Utf8GraphQLParser.Parse(
                    "directive @test(arg: String) on OBJECT");
            DocumentNode schema_b =
                Utf8GraphQLParser.Parse(
                    "directive @test1(arg: String) on OBJECT");
            DocumentNode schema_c =
                Utf8GraphQLParser.Parse(
                    "directive @test(arg: String) on OBJECT");

            var types = new List<IDirectiveTypeInfo>
            {
                new DirectiveTypeInfo(schema_a.Definitions
                    .OfType<DirectiveDefinitionNode>().First(),
                    new SchemaInfo("Schema_A", schema_a)),
                new DirectiveTypeInfo(schema_b.Definitions
                    .OfType<DirectiveDefinitionNode>().First(),
                    new SchemaInfo("Schema_B", schema_b)),
                new DirectiveTypeInfo(schema_c.Definitions
                    .OfType<DirectiveDefinitionNode>().First(),
                    new SchemaInfo("Schema_C", schema_c))
            };

            var context = new SchemaMergeContext();

            // act
            var typeMerger = new DirectiveTypeMergeHandler((c, t) => { });
            typeMerger.Merge(context, types);

            // assert
            context
                .CreateSchema()
                .Print()
                .MatchSnapshot();
        }
    }
}

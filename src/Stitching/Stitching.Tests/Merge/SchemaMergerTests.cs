using ChilliCream.Testing;
using Snapshooter.Xunit;
using Xunit;
using HotChocolate.Language;

namespace HotChocolate.Stitching.Merge
{
    public class SchemaMergerTests
    {
        [Fact]
        public void MergeSimpleSchemaWithDefaultHandler()
        {
            // arrange
            DocumentNode schema_a =
                Parser.Default.Parse("union Foo = Bar | Baz union A = B | C");
            DocumentNode schema_b =
                Parser.Default.Parse("union Foo = Bar | Baz");

            // act
            DocumentNode schema = SchemaMerger.New()
                .AddSchema("A", schema_a)
                .AddSchema("B", schema_b)
                .Merge();

            // assert
            SchemaSyntaxSerializer.Serialize(schema).MatchSnapshot();
        }

        [Fact]
        public void MergeDemoSchemaWithDefaultHandler()
        {
            // arrange
            DocumentNode schema_a =
                Parser.Default.Parse(
                    FileResource.Open("Contract.graphql"));
            DocumentNode schema_b =
                Parser.Default.Parse(
                    FileResource.Open("Customer.graphql"));

            // act
            DocumentNode schema = SchemaMerger.New()
                .AddSchema("A", schema_a)
                .AddSchema("B", schema_b)
                .Merge();

            // assert
            SchemaSyntaxSerializer.Serialize(schema).MatchSnapshot();
        }

        [Fact]
        public void MergeDemoSchemaAndRemoveRootTypes()
        {
            // arrange
            DocumentNode schema_a =
                Parser.Default.Parse(
                    FileResource.Open("Contract.graphql"));
            DocumentNode schema_b =
                Parser.Default.Parse(
                    FileResource.Open("Customer.graphql"));

            // act
            DocumentNode schema = SchemaMerger.New()
                .AddSchema("A", schema_a)
                .AddSchema("B", schema_b)
                .IgnoreRootTypes()
                .Merge();

            // assert
            SchemaSyntaxSerializer.Serialize(schema).MatchSnapshot();
        }

        [Fact]
        public void MergeDemoSchemaAndRemoveRootTypesFromSchemaA()
        {
            // arrange
            DocumentNode schema_a =
                Parser.Default.Parse(
                    FileResource.Open("Contract.graphql"));
            DocumentNode schema_b =
                Parser.Default.Parse(
                    FileResource.Open("Customer.graphql"));

            // act
            DocumentNode schema = SchemaMerger.New()
                .AddSchema("A", schema_a)
                .AddSchema("B", schema_b)
                .IgnoreRootTypes("A")
                .Merge();

            // assert
            SchemaSyntaxSerializer.Serialize(schema).MatchSnapshot();
        }

        [Fact]
        public void MergeSchemaAndRemoveTypeAFromAllSchemas()
        {
            // arrange
            DocumentNode schema_a =
                Parser.Default.Parse(
                    "type A { b1: String } type B { c: String }");
            DocumentNode schema_b =
                Parser.Default.Parse(
                    "type A { b2: String } type B { c: String }");

            // act
            DocumentNode schema = SchemaMerger.New()
                .AddSchema("A", schema_a)
                .AddSchema("B", schema_b)
                .IgnoreType("A")
                .Merge();

            // assert
            SchemaSyntaxSerializer.Serialize(schema).MatchSnapshot();
        }


        [Fact]
        public void MergeSchemaAndRemoveTypeAFromSchemaA()
        {
            // arrange
            DocumentNode schema_a =
                Parser.Default.Parse(
                    "type A { b1: String } type B { c: String }");
            DocumentNode schema_b =
                Parser.Default.Parse(
                    "type A { b2: String } type B { c: String }");

            // act
            DocumentNode schema = SchemaMerger.New()
                .AddSchema("A", schema_a)
                .AddSchema("B", schema_b)
                .IgnoreType("A", "A")
                .Merge();

            // assert
            SchemaSyntaxSerializer.Serialize(schema).MatchSnapshot();
        }
    }
}

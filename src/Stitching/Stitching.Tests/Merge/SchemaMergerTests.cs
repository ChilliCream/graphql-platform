using ChilliCream.Testing;
using Snapshooter.Xunit;
using Xunit;
using HotChocolate.Language;
using HotChocolate.Resolvers;
using Snapshooter;
using HotChocolate.Stitching.Introspection;

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
        public void MergeDemoSchemaAndRemoveRootTypesOnSchemaA()
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
        public void MergeSchemaAndRenameTypeAtoXyzOnAllSchemas()
        {
            // arrange
            DocumentNode schema_a =
                Parser.Default.Parse(
                    "type A { b1: String b2: String } type B { c: String }");
            DocumentNode schema_b =
                Parser.Default.Parse(
                    "type A { b1: String b3: String } type B { c: String }");

            // act
            DocumentNode schema = SchemaMerger.New()
                .AddSchema("A", schema_a)
                .AddSchema("B", schema_b)
                .RenameType("A", "Xyz")
                .Merge();

            // assert
            SchemaSyntaxSerializer.Serialize(schema).MatchSnapshot();
        }


        [Fact]
        public void MergeSchemaAndRenameTypeAtoXyzOnSchemaA()
        {
            // arrange
            DocumentNode schema_a =
                Parser.Default.Parse(
                    "type A { b1: String b2: String } type B { c: String }");
            DocumentNode schema_b =
                Parser.Default.Parse(
                    "type A { b1: String b3: String } type B { c: String }");

            // act
            DocumentNode schema = SchemaMerger.New()
                .AddSchema("A", schema_a)
                .AddSchema("B", schema_b)
                .RenameType("A", "A", "Xyz")
                .Merge();

            // assert
            SchemaSyntaxSerializer.Serialize(schema).MatchSnapshot();
        }

        [Fact]
        public void MergeSchemaAndRemoveTypeAOnAllSchemas()
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
        public void MergeSchemaAndRemoveTypeAOnSchemaA()
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

        [Fact]
        public void MergeSchemaAndRemoveFieldB1OnAllSchemas()
        {
            // arrange
            DocumentNode schema_a =
                Parser.Default.Parse(
                    "type A { b1: String b2: String } type B { c: String }");
            DocumentNode schema_b =
                Parser.Default.Parse(
                    "type A { b1: String b3: String } type B { c: String }");

            // act
            DocumentNode schema = SchemaMerger.New()
                .AddSchema("A", schema_a)
                .AddSchema("B", schema_b)
                .IgnoreField(new FieldReference("A", "b1"))
                .Merge();

            // assert
            SchemaSyntaxSerializer.Serialize(schema).MatchSnapshot();
        }


        [Fact]
        public void MergeSchemaAndRemoveFieldB1OnSchemaA()
        {
            // arrange
            DocumentNode schema_a =
                Parser.Default.Parse(
                    "type A { b1: String b2: String } type B { c: String }");
            DocumentNode schema_b =
                Parser.Default.Parse(
                    "type A { b1: String b3: String } type B { c: String }");

            // act
            DocumentNode schema = SchemaMerger.New()
                .AddSchema("A", schema_a)
                .AddSchema("B", schema_b)
                .IgnoreField("A", new FieldReference("A", "b1"))
                .Merge();

            // assert
            SchemaSyntaxSerializer.Serialize(schema).MatchSnapshot();
        }

        [Fact]
        public void MergeSchemaAndRenameFieldB1toB11OnAllSchemas()
        {
            // arrange
            DocumentNode schema_a =
                Parser.Default.Parse(
                    "type A { b1: String b2: String } type B { c: String }");
            DocumentNode schema_b =
                Parser.Default.Parse(
                    "type A { b1: String b3: String } type B { c: String }");

            // act
            DocumentNode schema = SchemaMerger.New()
                .AddSchema("A", schema_a)
                .AddSchema("B", schema_b)
                .RenameField(new FieldReference("A", "b1"), "b11")
                .Merge();

            // assert
            SchemaSyntaxSerializer.Serialize(schema).MatchSnapshot();
        }


        [Fact]
        public void MergeSchemaAndRenameFieldB1toB11OnSchemaA()
        {
            // arrange
            DocumentNode schema_a =
                Parser.Default.Parse(
                    "type A { b1: String b2: String } type B { c: String }");
            DocumentNode schema_b =
                Parser.Default.Parse(
                    "type A { b1: String b3: String } type B { c: String }");

            // act
            DocumentNode schema = SchemaMerger.New()
                .AddSchema("A", schema_a)
                .AddSchema("B", schema_b)
                .RenameField("A", new FieldReference("A", "b1"), "b11")
                .Merge();

            // assert
            SchemaSyntaxSerializer.Serialize(schema).MatchSnapshot();
        }

        [Fact]
        public void RenameReferencingType()
        {
            // arrange
            DocumentNode schema_a =
                Parser.Default.Parse(
                    "type A { b1: B } " +
                    "type B implements C { c: String } " +
                    "interface C { c: String }");

            DocumentNode schema_b =
                Parser.Default.Parse(
                    "type B { b1: String b3: String } type C { c: String }");

            // act
            DocumentNode a = SchemaMerger.New()
                .AddSchema("A", schema_a)
                .AddSchema("B", schema_b)
                .RenameType("A", "B", "Foo")
                .Merge();

            DocumentNode b = SchemaMerger.New()
                .AddSchema("B", schema_b)
                .AddSchema("A", schema_a)
                .RenameType("A", "B", "Foo")
                .Merge();

            // assert
            SchemaSyntaxSerializer.Serialize(a).MatchSnapshot(
                SnapshotNameExtension.Create("A"));
            SchemaSyntaxSerializer.Serialize(b).MatchSnapshot(
                SnapshotNameExtension.Create("B"));

        }

        [Fact]
        public void FieldDefinitionDoesNotHaveSameTypeShape()
        {
            // arrange
            DocumentNode schema_a =
                Parser.Default.Parse(
                    "type A { b1: String b2: String } type B { c: String! }");
            DocumentNode schema_b =
                Parser.Default.Parse(
                    "type A { b1: String b3: String } type B { c: String }");

            // act
            DocumentNode merged = SchemaMerger.New()
                .AddSchema("A", schema_a)
                .AddSchema("B", schema_b)
                .Merge();

            // assert
            SchemaSyntaxSerializer.Serialize(merged).MatchSnapshot();
        }
    }
}

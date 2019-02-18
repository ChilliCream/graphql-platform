using System;
using ChilliCream.Testing;
using HotChocolate.Language;
using Xunit;

namespace HotChocolate.Stitching.Merge
{
    public class AddSchemaExtensionRewriterTests
    {
        [Fact]
        public void ObjectType_AddScalarField()
        {
            // arrange
            const string schema = "type Foo { bar: String }";
            const string extensions = "extend type Foo { baz: Int }";

            // act
            var rewriter = new AddSchemaExtensionRewriter();
            DocumentNode merged = rewriter.AddExtensions(
                Parser.Default.Parse(schema),
                Parser.Default.Parse(extensions));

            // assert
            SchemaSyntaxSerializer.Serialize(merged).Snapshot();
        }

        [Fact]
        public void ObjectType_AddObjectField()
        {
            // arrange
            const string schema = "type Foo { bar: String }";
            const string extensions = "extend type Foo { baz: Bar } " +
                "type Bar { baz: String }";

            // act
            var rewriter = new AddSchemaExtensionRewriter();
            DocumentNode merged = rewriter.AddExtensions(
                Parser.Default.Parse(schema),
                Parser.Default.Parse(extensions));

            // assert
            SchemaSyntaxSerializer.Serialize(merged).Snapshot();
        }

        [Fact]
        public void ObjectType_AddDirectives()
        {
            // arrange
            const string schema = "type Foo { bar: String } " +
                "directive @foo on OBJECT";
            const string extensions = "extend type Foo @foo";

            // act
            var rewriter = new AddSchemaExtensionRewriter();
            DocumentNode merged = rewriter.AddExtensions(
                Parser.Default.Parse(schema),
                Parser.Default.Parse(extensions));

            // assert
            SchemaSyntaxSerializer.Serialize(merged).Snapshot();
        }

        [Fact]
        public void ObjectType_AddDuplicateDirectives()
        {
            // arrange
            const string schema = "type Foo @foo { bar: String } " +
                "directive @foo on OBJECT";
            const string extensions = "extend type Foo @foo";

            // act
            var rewriter = new AddSchemaExtensionRewriter();
            Action action = () => rewriter.AddExtensions(
                  Parser.Default.Parse(schema),
                  Parser.Default.Parse(extensions));

            // assert
            Assert.Throws<SchemaMergeException>(action).Snapshot();
        }

        [Fact]
        public void ObjectType_AddUndeclaredDirectives()
        {
            // arrange
            const string schema = "type Foo @foo { bar: String }";
            const string extensions = "extend type Foo @foo";

            // act
            var rewriter = new AddSchemaExtensionRewriter();
            Action action = () => rewriter.AddExtensions(
                  Parser.Default.Parse(schema),
                  Parser.Default.Parse(extensions));

            // assert
            Assert.Throws<SchemaMergeException>(action).Snapshot();
        }

        [Fact]
        public void InterfaceType_AddScalarField()
        {
            // arrange
            const string schema = "interface Foo { bar: String }";
            const string extensions = "extend interface Foo { baz: Int }";

            // act
            var rewriter = new AddSchemaExtensionRewriter();
            DocumentNode merged = rewriter.AddExtensions(
                Parser.Default.Parse(schema),
                Parser.Default.Parse(extensions));

            // assert
            SchemaSyntaxSerializer.Serialize(merged).Snapshot();
        }

        [Fact]
        public void InterfaceType_AddObjectField()
        {
            // arrange
            const string schema = "interface Foo { bar: String }";
            const string extensions = "extend interface Foo { baz: Bar } " +
                "interface Bar { baz: String }";

            // act
            var rewriter = new AddSchemaExtensionRewriter();
            DocumentNode merged = rewriter.AddExtensions(
                Parser.Default.Parse(schema),
                Parser.Default.Parse(extensions));

            // assert
            SchemaSyntaxSerializer.Serialize(merged).Snapshot();
        }

        [Fact]
        public void InterfaceType_AddDirectives()
        {
            // arrange
            const string schema = "interface Foo { bar: String } " +
                "directive @foo on INTERFACE";
            const string extensions = "extend interface Foo @foo";

            // act
            var rewriter = new AddSchemaExtensionRewriter();
            DocumentNode merged = rewriter.AddExtensions(
                Parser.Default.Parse(schema),
                Parser.Default.Parse(extensions));

            // assert
            SchemaSyntaxSerializer.Serialize(merged).Snapshot();
        }

        [Fact]
        public void InterfaceType_AddDuplicateDirectives()
        {
            // arrange
            const string schema = "interface Foo @foo { bar: String } " +
                "directive @foo on INTERFACE";
            const string extensions = "extend interface Foo @foo";

            // act
            var rewriter = new AddSchemaExtensionRewriter();
            Action action = () => rewriter.AddExtensions(
                  Parser.Default.Parse(schema),
                  Parser.Default.Parse(extensions));

            // assert
            Assert.Throws<SchemaMergeException>(action).Snapshot();
        }

        [Fact]
        public void InterfaceType_AddUndeclaredDirectives()
        {
            // arrange
            const string schema = "interface Foo @foo { bar: String }";
            const string extensions = "extend interface Foo @foo";

            // act
            var rewriter = new AddSchemaExtensionRewriter();
            Action action = () => rewriter.AddExtensions(
                  Parser.Default.Parse(schema),
                  Parser.Default.Parse(extensions));

            // assert
            Assert.Throws<SchemaMergeException>(action).Snapshot();
        }

        [Fact]
        public void UnionType_AddType()
        {
            // arrange
            const string schema = "union Foo = A | B "
                + "type A { a: String } "
                + "type B { b: String }";
            const string extensions = "extend union Foo = C "
                + "type C { c: String }";

            // act
            var rewriter = new AddSchemaExtensionRewriter();
            DocumentNode merged = rewriter.AddExtensions(
                Parser.Default.Parse(schema),
                Parser.Default.Parse(extensions));

            // assert
            SchemaSyntaxSerializer.Serialize(merged).Snapshot();
        }

        [Fact]
        public void UnionType_AddDirectives()
        {
            // arrange
            const string schema = "union Foo = A | B "
                + "type A { a: String } "
                + "type B { b: String } "
                + "directive @foo on INTERFACE";
            const string extensions = "extend union Foo @foo";

            // act
            var rewriter = new AddSchemaExtensionRewriter();
            DocumentNode merged = rewriter.AddExtensions(
                Parser.Default.Parse(schema),
                Parser.Default.Parse(extensions));

            // assert
            SchemaSyntaxSerializer.Serialize(merged).Snapshot();
        }

        [Fact]
        public void UnionType_AddDuplicateDirectives()
        {
            // arrange
            const string schema = "union Foo @foo = A | B "
                + "type A { a: String } "
                + "type B { b: String } "
                + "directive @foo on INTERFACE";
            const string extensions = "extend union Foo @foo";

            // act
            var rewriter = new AddSchemaExtensionRewriter();
            Action action = () => rewriter.AddExtensions(
                  Parser.Default.Parse(schema),
                  Parser.Default.Parse(extensions));

            // assert
            Assert.Throws<SchemaMergeException>(action).Snapshot();
        }

        [Fact]
        public void UnionType_AddUndeclaredDirectives()
        {
            // arrange
            const string schema = "union Foo = A | B "
                + "type A { a: String } "
                + "type B { b: String }";
            const string extensions = "extend union Foo @foo";

            // act
            var rewriter = new AddSchemaExtensionRewriter();
            Action action = () => rewriter.AddExtensions(
                  Parser.Default.Parse(schema),
                  Parser.Default.Parse(extensions));

            // assert
            Assert.Throws<SchemaMergeException>(action).Snapshot();
        }
    }
}

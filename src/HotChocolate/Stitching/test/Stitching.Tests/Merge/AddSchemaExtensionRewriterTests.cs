using System;
using Snapshooter.Xunit;
using Xunit;
using HotChocolate.Language;

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
                Utf8GraphQLParser.Parse(schema),
                Utf8GraphQLParser.Parse(extensions));

            // assert
            merged.ToString().MatchSnapshot();
        }

        [Fact]
        public void ObjectType_AddScalarField_2()
        {
            // arrange
            const string schema = "type Foo { bar: String }";
            const string extensions = "extend type Foo { baz: Int } extend type Baz { a: String }";

            // act
            var rewriter = new AddSchemaExtensionRewriter();
            DocumentNode merged = rewriter.AddExtensions(
                Utf8GraphQLParser.Parse(schema),
                Utf8GraphQLParser.Parse(extensions));

            // assert
            merged.ToString().MatchSnapshot();
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
                Utf8GraphQLParser.Parse(schema),
                Utf8GraphQLParser.Parse(extensions));

            // assert
            merged.ToString().MatchSnapshot();
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
                Utf8GraphQLParser.Parse(schema),
                Utf8GraphQLParser.Parse(extensions));

            // assert
            merged.ToString().MatchSnapshot();
        }

        [Fact]
        public void ObjectType_AddDirectivesToField()
        {
            // arrange
            const string schema = "type Foo { bar: String } " +
                "directive @foo on FIELD";
            const string extensions = "extend type Foo { bar: String @foo }";

            // act
            var rewriter = new AddSchemaExtensionRewriter();
            DocumentNode merged = rewriter.AddExtensions(
                Utf8GraphQLParser.Parse(schema),
                Utf8GraphQLParser.Parse(extensions));

            // assert
            merged.ToString().MatchSnapshot();
        }

        [Fact]
        public void ObjectType_DirectiveDeclaredInExtensionDoc()
        {
            // arrange
            const string schema = "type Foo { bar: String }";
            const string extensions = "extend type Foo @foo { bar: String }"
                + "directive @foo on OBJECT";

            // act
            var rewriter = new AddSchemaExtensionRewriter();
            DocumentNode merged = rewriter.AddExtensions(
                Utf8GraphQLParser.Parse(schema),
                Utf8GraphQLParser.Parse(extensions));

            // assert
            merged.ToString().MatchSnapshot();
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
                  Utf8GraphQLParser.Parse(schema),
                  Utf8GraphQLParser.Parse(extensions));

            // assert
            Assert.Throws<SchemaMergeException>(action).Message.MatchSnapshot();
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
                  Utf8GraphQLParser.Parse(schema),
                  Utf8GraphQLParser.Parse(extensions));

            // assert
            Assert.Throws<SchemaMergeException>(action).Message.MatchSnapshot();
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
                Utf8GraphQLParser.Parse(schema),
                Utf8GraphQLParser.Parse(extensions));

            // assert
            merged.ToString().MatchSnapshot();
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
                Utf8GraphQLParser.Parse(schema),
                Utf8GraphQLParser.Parse(extensions));

            // assert
            merged.ToString().MatchSnapshot();
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
                Utf8GraphQLParser.Parse(schema),
                Utf8GraphQLParser.Parse(extensions));

            // assert
            merged.ToString().MatchSnapshot();
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
                  Utf8GraphQLParser.Parse(schema),
                  Utf8GraphQLParser.Parse(extensions));

            // assert
            Assert.Throws<SchemaMergeException>(action).Message.MatchSnapshot();
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
                  Utf8GraphQLParser.Parse(schema),
                  Utf8GraphQLParser.Parse(extensions));

            // assert
            Assert.Throws<SchemaMergeException>(action).Message.MatchSnapshot();
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
                Utf8GraphQLParser.Parse(schema),
                Utf8GraphQLParser.Parse(extensions));

            // assert
            merged.ToString().MatchSnapshot();
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
                Utf8GraphQLParser.Parse(schema),
                Utf8GraphQLParser.Parse(extensions));

            // assert
            merged.ToString().MatchSnapshot();
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
                  Utf8GraphQLParser.Parse(schema),
                  Utf8GraphQLParser.Parse(extensions));

            // assert
            Assert.Throws<SchemaMergeException>(action).Message.MatchSnapshot();
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
                  Utf8GraphQLParser.Parse(schema),
                  Utf8GraphQLParser.Parse(extensions));

            // assert
            Assert.Throws<SchemaMergeException>(action).Message.MatchSnapshot();
        }

        [Fact]
        public void InputObjectType_AddScalarField()
        {
            // arrange
            const string schema = "input Foo { bar: String }";
            const string extensions = "extend input Foo { baz: Int }";

            // act
            var rewriter = new AddSchemaExtensionRewriter();
            DocumentNode merged = rewriter.AddExtensions(
                Utf8GraphQLParser.Parse(schema),
                Utf8GraphQLParser.Parse(extensions));

            // assert
            merged.ToString().MatchSnapshot();
        }

        [Fact]
        public void InputObjectType_AddObjectField()
        {
            // arrange
            const string schema = "input Foo { bar: String }";
            const string extensions = "extend input Foo { baz: Bar } " +
                "input Bar { baz: String }";

            // act
            var rewriter = new AddSchemaExtensionRewriter();
            DocumentNode merged = rewriter.AddExtensions(
                Utf8GraphQLParser.Parse(schema),
                Utf8GraphQLParser.Parse(extensions));

            // assert
            merged.ToString().MatchSnapshot();
        }

        [Fact]
        public void InputObjectType_AddDirectives()
        {
            // arrange
            const string schema = "input Foo { bar: String } " +
                "directive @foo on INPUT_OBJECT";
            const string extensions = "extend input Foo @foo";

            // act
            var rewriter = new AddSchemaExtensionRewriter();
            DocumentNode merged = rewriter.AddExtensions(
                Utf8GraphQLParser.Parse(schema),
                Utf8GraphQLParser.Parse(extensions));

            // assert
            merged.ToString().MatchSnapshot();
        }

        [Fact]
        public void InputObjectType_AddDuplicateDirectives()
        {
            // arrange
            const string schema = "input Foo @foo { bar: String } " +
                "directive @foo on INPUT_OBJECT";
            const string extensions = "extend input Foo @foo";

            // act
            var rewriter = new AddSchemaExtensionRewriter();
            Action action = () => rewriter.AddExtensions(
                  Utf8GraphQLParser.Parse(schema),
                  Utf8GraphQLParser.Parse(extensions));

            // assert
            Assert.Throws<SchemaMergeException>(action).Message.MatchSnapshot();
        }

        [Fact]
        public void InputObjectType_AddUndeclaredDirectives()
        {
            // arrange
            const string schema = "input Foo @foo { bar: String }";
            const string extensions = "extend input Foo @foo";

            // act
            var rewriter = new AddSchemaExtensionRewriter();
            Action action = () => rewriter.AddExtensions(
                  Utf8GraphQLParser.Parse(schema),
                  Utf8GraphQLParser.Parse(extensions));

            // assert
            Assert.Throws<SchemaMergeException>(action).Message.MatchSnapshot();
        }

        [Fact]
        public void EnumType_AddValue()
        {
            // arrange
            const string schema = "enum Foo { BAR BAZ }";
            const string extensions = "extend enum Foo { QUX }";

            // act
            var rewriter = new AddSchemaExtensionRewriter();
            DocumentNode merged = rewriter.AddExtensions(
                Utf8GraphQLParser.Parse(schema),
                Utf8GraphQLParser.Parse(extensions));

            // assert
            SchemaSyntaxSerializer.Serialize(merged).MatchSnapshot();
        }

        [Fact]
        public void EnumType_AddDirectives()
        {
            // arrange
            const string schema = "enum Foo { BAR BAZ } " +
                "directive @foo on ENUM";
            const string extensions = "extend enum Foo @foo";

            // act
            var rewriter = new AddSchemaExtensionRewriter();
            DocumentNode merged = rewriter.AddExtensions(
                Utf8GraphQLParser.Parse(schema),
                Utf8GraphQLParser.Parse(extensions));

            // assert
            SchemaSyntaxSerializer.Serialize(merged).MatchSnapshot();
        }

        [Fact]
        public void EnumType_TypeMismatch()
        {
            // arrange
            const string schema = "enum Foo @foo { BAR BAZ } " +
                "directive @foo on ENUM";
            const string extensions = "extend input Foo @foo";

            // act
            var rewriter = new AddSchemaExtensionRewriter();
            Action action = () => rewriter.AddExtensions(
                  Utf8GraphQLParser.Parse(schema),
                  Utf8GraphQLParser.Parse(extensions));

            // assert
            Assert.Throws<SchemaMergeException>(action).Message.MatchSnapshot();
        }

        [Fact]
        public void EnumType_AddDuplicateDirectives()
        {
            // arrange
            const string schema = "enum Foo @foo { BAR BAZ } " +
                "directive @foo on ENUM";
            const string extensions = "extend enum Foo @foo";

            // act
            var rewriter = new AddSchemaExtensionRewriter();
            Action action = () => rewriter.AddExtensions(
                  Utf8GraphQLParser.Parse(schema),
                  Utf8GraphQLParser.Parse(extensions));

            // assert
            Assert.Throws<SchemaMergeException>(action).Message.MatchSnapshot();
        }

        [Fact]
        public void EnumType_AddUndeclaredDirectives()
        {
            // arrange
            const string schema = "enum Foo { BAR BAZ }";
            const string extensions = "extend enum Foo @foo";

            // act
            var rewriter = new AddSchemaExtensionRewriter();
            Action action = () => rewriter.AddExtensions(
                  Utf8GraphQLParser.Parse(schema),
                  Utf8GraphQLParser.Parse(extensions));

            // assert
            Assert.Throws<SchemaMergeException>(action).Message.MatchSnapshot();
        }
    }
}

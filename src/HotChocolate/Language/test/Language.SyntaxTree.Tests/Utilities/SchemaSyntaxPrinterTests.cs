using Xunit;

namespace HotChocolate.Language.SyntaxTree.Utilities;

public class SchemaSyntaxPrinterTests
{
    [Fact]
    public void Serialize_ObjectTypeDefNoIndent_InOutShouldBeTheSame()
    {
        // arrange
        var schema = "type Foo { bar: String baz: [Int] }";
        var document = Utf8GraphQLParser.Parse(schema);

        // act
        var result = document.ToString(false);

        // assert
        Assert.Equal(schema, result);
    }

    [Fact]
    public void Serialize_ObjectTypeDefWithIndent_OutHasIndentation()
    {
        // arrange
        var schema = "type Foo { bar: String baz: [Int] }";
        var document = Utf8GraphQLParser.Parse(schema);

        // act
        var result = document.ToString();

        // assert
        result.MatchSnapshot();
    }

    [Fact]
    public void Serialize_ObjectTypeDefWithArgsNoIndent_InOutShouldBeTheSame()
    {
        // arrange
        var schema = "type Foo { bar(a: Int = 1 b: Int): String }";
        var document = Utf8GraphQLParser.Parse(schema);

        // act
        var result = document.ToString(false);

        // assert
        Assert.Equal(schema, result);
    }

    [Fact]
    public void Serialize_ObjectTypeDefWithArgsWithIndent_OutHasIndentation()
    {
        // arrange
        var schema = "type Foo { bar(a: Int = 1 b: Int): String }";
        var document = Utf8GraphQLParser.Parse(schema);

        // act
        var result = document.ToString();

        // assert
        result.MatchSnapshot();
    }

    [Fact]
    public void Serialize_ObjectTypeDefWithDirectivesNoIndent_InOutShouldBeTheSame()
    {
        // arrange
        var schema = "type Foo @a(x: \"y\") { bar: String baz: [Int] } " +
            "type Foo @a @b { bar: String @foo " +
            "baz(a: String = \"abc\"): [Int] @foo @bar }";
        var document = Utf8GraphQLParser.Parse(schema);

        // act
        var result = document.ToString(false);

        // assert
        Assert.Equal(schema, result);
    }

    [Fact]
    public void Serialize_ObjectTypeDefWithDirectivesWithIndent_OutHasIndentation()
    {
        // arrange
        var schema = "type Foo @a(x: \"y\") { bar: String baz: [Int] } " +
            "type Foo @a @b { bar: String @foo " +
            "baz(a: String = \"abc\"): [Int] @foo @bar }";
        var document = Utf8GraphQLParser.Parse(schema);

        // act
        var result = document.ToString();

        // assert
        result.MatchSnapshot();
    }

    [Fact]
    public void Serialize_ObjectTypeDefWithDescriptionNoIndent_InOutShouldBeTheSame()
    {
        // arrange
        var schema = "\"abc\" type Foo @a { \"abc\" bar: String " +
            "\"abc\" baz: [Int] } " +
            "\"abc\" type Foo @a @b { \"abc\" bar: String @foo " +
            "\"abc\" baz(\"abc\" a: String = \"abc\"): [Int] @foo @bar }";
        var document = Utf8GraphQLParser.Parse(schema);

        // act
        var result = document.ToString(false);

        // assert
        Assert.Equal(schema, result);
    }

    [Fact]
    public void Serialize_ObjectTypeDefWithDescriptionWithIndent_OutHasIndentation()
    {
        // arrange
        var schema = "\"abc\" type Foo @a { \"abc\" bar: String " +
            "\"abc\" baz: [Int] } " +
            "\"abc\" type Foo @a @b { \"abc\" bar: String @foo " +
            "\"abc\" baz(\"abc\" a: String = \"abc\"): [Int] @foo @bar }";
        var document = Utf8GraphQLParser.Parse(schema);

        // act
        var result = document.ToString();

        // assert
        result.MatchSnapshot();
    }

    [Fact]
    public void Serialize_ObjectTypeImplementsXYZ_InOutShouldBeTheSame()
    {
        // arrange
        var schema = "type Foo implements X & Y & Z " +
            "{ bar: String baz: [Int] }";
        var document = Utf8GraphQLParser.Parse(schema);

        // act
        var result = document.ToString(false);

        // assert
        Assert.Equal(schema, result);
    }

    [Fact]
    public void Serialize_ObjectTypeImplementsXYZWithIndent_OutHasIndentation()
    {
        // arrange
        var schema = "type Foo implements X & Y & Z " +
            "{ bar: String baz: [Int] }";
        var document = Utf8GraphQLParser.Parse(schema);

        // act
        var result = document.ToString();

        // assert
        result.MatchSnapshot();
    }

    [Fact]
    public void Serialize_ObjectTypeExtensionDef_InOutShouldBeTheSame()
    {
        // arrange
        var schema = "extend type Foo { bar: String baz: [Int] }";
        var document = Utf8GraphQLParser.Parse(schema);

        // act
        var result = document.ToString(false);

        // assert
        Assert.Equal(schema, result);
    }

    [Fact]
    public void Serialize_InterfaceTypeDefNoIndent_InOutShouldBeTheSame()
    {
        // arrange
        var schema = "interface Foo { bar: String baz: [Int] }";
        var document = Utf8GraphQLParser.Parse(schema);

        // act
        var result = document.ToString(false);

        // assert
        Assert.Equal(schema, result);
    }

    [Fact]
    public void Serialize_InterfaceTypeDefWithIndent_OutHasIndentation()
    {
        // arrange
        var schema = "interface Foo { bar: String baz: [Int] }";
        var document = Utf8GraphQLParser.Parse(schema);

        // act
        var result = document.ToString();

        // assert
        result.MatchSnapshot();
    }

    [Fact]
    public void Serialize_InterfaceTypeDefWithArgsNoIndent_InOutShouldBeTheSame()
    {
        // arrange
        var schema = "interface Foo { bar(a: Int = 1 b: Int): String }";
        var document = Utf8GraphQLParser.Parse(schema);

        // act
        var result = document.ToString(false);

        // assert
        Assert.Equal(schema, result);
    }

    [Fact]
    public void Serialize_InterfaceTypeDefWithArgsWithIndent_OutHasIndentation()
    {
        // arrange
        var schema = "interface Foo { bar(a: Int = 1 b: Int): String }";
        var document = Utf8GraphQLParser.Parse(schema);

        // act
        var result = document.ToString();

        // assert
        result.MatchSnapshot();
    }

    [Fact]
    public void Serialize_InterfaceTypeDefWithDirectivesNoIndent_InOutShouldBeTheSame()
    {
        // arrange
        var schema = "interface Foo @a(x: \"y\") { bar: String baz: [Int] } " +
            "interface Foo @a @b { bar: String @foo " +
            "baz(a: String = \"abc\"): [Int] @foo @bar }";
        var document = Utf8GraphQLParser.Parse(schema);

        // act
        var result = document.ToString(false);

        // assert
        Assert.Equal(schema, result);
    }

    [Fact]
    public void Serialize_InterfaceTypeDefWithDirectivesWithIndent_OutHasIndentation()
    {
        // arrange
        var schema = "interface Foo @a(x: \"y\") { bar: String baz: [Int] } " +
            "interface Foo @a @b { bar: String @foo " +
            "baz(a: String = \"abc\"): [Int] @foo @bar }";
        var document = Utf8GraphQLParser.Parse(schema);

        // act
        var result = document.ToString();

        // assert
        result.MatchSnapshot();
    }

    [Fact]
    public void Serialize_InterfaceTypeDefWithDescriptionNoIndent_InOutShouldBeTheSame()
    {
        // arrange
        var schema = "\"abc\" interface Foo @a { \"abc\" bar: String " +
            "\"abc\" baz: [Int] } " +
            "\"abc\" interface Foo @a @b { \"abc\" bar: String @foo " +
            "\"abc\" baz(\"abc\" a: String = \"abc\"): [Int] @foo @bar }";
        var document = Utf8GraphQLParser.Parse(schema);

        // act
        var result = document.ToString(false);

        // assert
        Assert.Equal(schema, result);
    }

    [Fact]
    public void Serialize_InterfaceTypeDefWithDescriptionWithIndent_OutHasIndentation()
    {
        // arrange
        var schema = "\"abc\" interface Foo @a { \"abc\" bar: String " +
            "\"abc\" baz: [Int] } " +
            "\"abc\" interface Foo @a @b { \"abc\" bar: String @foo " +
            "\"abc\" baz(\"abc\" a: String = \"abc\"): [Int] @foo @bar }";
        var document = Utf8GraphQLParser.Parse(schema);

        // act
        var result = document.ToString();

        // assert
        result.MatchSnapshot();
    }

    [Fact]
    public void Serialize_InterfaceTypeImplementsXYZ_InOutShouldBeTheSame()
    {
        // arrange
        var schema = "interface Foo implements X & Y & Z " +
            "{ bar: String baz: [Int] }";
        var document = Utf8GraphQLParser.Parse(schema);

        // act
        var result = document.ToString(false);

        // assert
        Assert.Equal(schema, result);
    }

    [Fact]
    public void Serialize_InterfaceTypeImplementsXYZWithIndent_OutHasIndentation()
    {
        // arrange
        var schema = "interface Foo implements X & Y & Z " +
            "{ bar: String baz: [Int] }";
        var document = Utf8GraphQLParser.Parse(schema);

        // act
        var result = document.ToString();

        // assert
        result.MatchSnapshot();
    }

    [Fact]
    public void Serialize_InterfaceTypeExtensionDef_InOutShouldBeTheSame()
    {
        // arrange
        var schema = "extend interface Foo { bar: String baz: [Int] }";
        var document = Utf8GraphQLParser.Parse(schema);

        // act
        var result = document.ToString(false);

        // assert
        Assert.Equal(schema, result);
    }

    [Fact]
    public void Serialize_UnionTypeDefNoIndent_InOutShouldBeTheSame()
    {
        // arrange
        var schema = "union A = B | C";
        var document = Utf8GraphQLParser.Parse(schema);

        // act
        var result = document.ToString(false);

        // assert
        Assert.Equal(schema, result);
    }

    [Fact]
    public void Serialize_UnionTypeDefNoIndent_OutHasIndentation()
    {
        // arrange
        var schema = "union A = B | C";
        var document = Utf8GraphQLParser.Parse(schema);

        // act
        var result = document.ToString(false);

        // assert
        result.MatchSnapshot();
    }

    [Fact]
    public void Serialize_UnionTypeDefWithDirectiveNoIndent_InOutShouldBeTheSame()
    {
        // arrange
        var schema = "union A @a = B | C union A @a @b = B | C";
        var document = Utf8GraphQLParser.Parse(schema);

        // act
        var result = document.ToString(false);

        // assert
        Assert.Equal(schema, result);
    }

    [Fact]
    public void Serialize_UnionTypeWithDirectiveDefNoIndent_OutHasIndentation()
    {
        // arrange
        var schema = "union A @a = B | C union A @a @b = B | C";
        var document = Utf8GraphQLParser.Parse(schema);

        // act
        var result = document.ToString(false);

        // assert
        result.MatchSnapshot();
    }

    [Fact]
    public void Serialize_UnionTypeDefWithDescriptionNoIndent_InOutShouldBeTheSame()
    {
        // arrange
        var schema = "\"abc\" union A = B | C";
        var document = Utf8GraphQLParser.Parse(schema);

        // act
        var result = document.ToString(false);

        // assert
        Assert.Equal(schema, result);
    }

    [Fact]
    public void Serialize_UnionTypeDefWithDescriptionNoIndented_OutHasIndentation()
    {
        // arrange
        var schema = "\"abc\"union A = B | C";
        var document = Utf8GraphQLParser.Parse(schema);

        // act
        var result = document.ToString(false);

        // assert
        result.MatchSnapshot();
    }

    [Fact]
    public void Serialize_UnionTypeExtensionDef_InOutShouldBeTheSame()
    {
        // arrange
        var schema = "extend union A = B | C";
        var document = Utf8GraphQLParser.Parse(schema);

        // act
        var result = document.ToString(false);

        // assert
        Assert.Equal(schema, result);
    }

    [Fact]
    public void Serialize_EnumTypeDefNoIndent_InOutShouldBeTheSame()
    {
        // arrange
        var schema = "enum A { B C }";
        var document = Utf8GraphQLParser.Parse(schema);

        // act
        var result = document.ToString(false);

        // assert
        Assert.Equal(schema, result);
    }

    [Fact]
    public void Serialize_EnumTypeDefNoIndent_OutHasIndentation()
    {
        // arrange
        var schema = "enum A { B C }";
        var document = Utf8GraphQLParser.Parse(schema);

        // act
        var result = document.ToString(false);

        // assert
        result.MatchSnapshot();
    }

    [Fact]
    public void Serialize_EnumTypeDefWithDirectiveNoIndent_InOutShouldBeTheSame()
    {
        // arrange
        var schema = "enum A @a @b(c: 1) { B @a @b(c: 1) C @a @b(c: 1) }";
        var document = Utf8GraphQLParser.Parse(schema);

        // act
        var result = document.ToString(false);

        // assert
        Assert.Equal(schema, result);
    }

    [Fact]
    public void Serialize_EnumTypeWithDirectiveDefNoIndent_OutHasIndentation()
    {
        // arrange
        var schema = "enum A @a @b(c: 1) { B @a @b(c: 1) C @a @b(c: 1) }";
        var document = Utf8GraphQLParser.Parse(schema);

        // act
        var result = document.ToString(false);

        // assert
        result.MatchSnapshot();
    }

    [Fact]
    public void Serialize_EnumTypeDefWithDescriptionNoIndent_InOutShouldBeTheSame()
    {
        // arrange
        var schema = "\"abc\" enum A { \"def\" B \"ghi\" C }";
        var document = Utf8GraphQLParser.Parse(schema);

        // act
        var result = document.ToString(false);

        // assert
        Assert.Equal(schema, result);
    }

    [Fact]
    public void Serialize_EnumTypeDefWithDescriptionNoIndented_OutHasIndentation()
    {
        // arrange
        var schema = "\"abc\" enum A { \"def\" B \"ghi\" C }";
        var document = Utf8GraphQLParser.Parse(schema);

        // act
        var result = document.ToString(false);

        // assert
        result.MatchSnapshot();
    }

    [Fact]
    public void Serialize_EnumTypeExtensionDef_InOutShouldBeTheSame()
    {
        // arrange
        var schema = "extend enum A { B C }";
        var document = Utf8GraphQLParser.Parse(schema);

        // act
        var result = document.ToString(false);

        // assert
        Assert.Equal(schema, result);
    }

    [Fact]
    public void Serialize_InputObjectTypeDefNoIndent_InOutShouldBeTheSame()
    {
        // arrange
        var schema = "input A { b: String c: [String!]! d: Int = 1 }";
        var document = Utf8GraphQLParser.Parse(schema);

        // act
        var result = document.ToString(false);

        // assert
        Assert.Equal(schema, result);
    }

    [Fact]
    public void Serialize_InputObjectTypeDefNoIndent_OutHasIndentation()
    {
        // arrange
        var schema = "input A { b: String c: [String!]! d: Int = 1 }";
        var document = Utf8GraphQLParser.Parse(schema);

        // act
        var result = document.ToString(false);

        // assert
        result.MatchSnapshot();
    }

    [Fact]
    public void Serialize_InputObjectTypeDefWithDirectiveNoIndent_InOutShouldBeTheSame()
    {
        // arrange
        var schema = "input A @a @b(c: 1) { b: String @a @b(c: 1) " +
            "c: [String!]! @a @b(c: 1) d: Int = 1 @a @b(c: 1) }";
        var document = Utf8GraphQLParser.Parse(schema);

        // act
        var result = document.ToString(false);

        // assert
        Assert.Equal(schema, result);
    }

    [Fact]
    public void Serialize_InputObjectTypeWithDirectiveDefNoIndent_OutHasIndentation()
    {
        // arrange
        var schema = "input A @a @b(c: 1) { b: String @a @b(c: 1) " +
            "c: [String!]! @a @b(c: 1) d: Int = 1 @a @b(c: 1) }";
        var document = Utf8GraphQLParser.Parse(schema);

        // act
        var result = document.ToString(false);

        // assert
        result.MatchSnapshot();
    }

    [Fact]
    public void Serialize_InputObjectTypeDefWithDescriptionNoIndent_InOutShouldBeTheSame()
    {
        // arrange
        var schema = "\"abc\" input A { \"abc\" b: String }";
        var document = Utf8GraphQLParser.Parse(schema);

        // act
        var result = document.ToString(false);

        // assert
        Assert.Equal(schema, result);
    }

    [Fact]
    public void Serialize_InputObjectTypeDefWithDescriptionNoIndentt_OutHasIndentation()
    {
        // arrange
        var schema = "\"abc\" input A { \"abc\" b: String }";
        var document = Utf8GraphQLParser.Parse(schema);

        // act
        var result = document.ToString(false);

        // assert
        result.MatchSnapshot();
    }

    [Fact]
    public void Serialize_InputObjectTypeExtensionDef_InOutShouldBeTheSame()
    {
        // arrange
        var schema = "extend input A { b: String }";
        var document = Utf8GraphQLParser.Parse(schema);

        // act
        var result = document.ToString(false);

        // assert
        Assert.Equal(schema, result);
    }

    [Fact]
    public void Serialize_ScalarTypeDefNoIndent_InOutShouldBeTheSame()
    {
        // arrange
        var schema = "scalar A";
        var document = Utf8GraphQLParser.Parse(schema);

        // act
        var result = document.ToString(false);

        // assert
        Assert.Equal(schema, result);
    }

    [Fact]
    public void Serialize_ScalarTypeDefWithDirectiveNoIndent_InOutShouldBeTheSame()
    {
        // arrange
        var schema = "scalar A @a @b(c: 1)";
        var document = Utf8GraphQLParser.Parse(schema);

        // act
        var result = document.ToString(false);

        // assert
        Assert.Equal(schema, result);
    }

    [Fact]
    public void Serialize_ScalarTypeDefWithDescNoIndent_InOutShouldBeTheSame()
    {
        // arrange
        var schema = "\"abc\" scalar A @a @b(c: 1)";
        var document = Utf8GraphQLParser.Parse(schema);

        // act
        var result = document.ToString(false);

        // assert
        Assert.Equal(schema, result);
    }

    [Fact]
    public void Serialize_ScalarTypeDefWithDescIndent_OutHasIndentation()
    {
        // arrange
        var schema = "\"abc\" scalar A @a @b(c: 1)";
        var document = Utf8GraphQLParser.Parse(schema);

        // act
        var result = document.ToString();

        // assert
        result.MatchSnapshot();
    }

    [Fact]
    public void Serialize_ScalarTypeExtensionDef_InOutShouldBeTheSame()
    {
        // arrange
        var schema = "extend scalar A @a @b(c: 1)";
        var document = Utf8GraphQLParser.Parse(schema);

        // act
        var result = document.ToString(false);

        // assert
        Assert.Equal(schema, result);
    }

    [Fact]
    public void Serialize_SchemaDefWithOpNoIndent_InOutShouldBeTheSame()
    {
        // arrange
        var schema = "schema { query: A }";
        var document = Utf8GraphQLParser.Parse(schema);

        // act
        var result = document.ToString(false);

        // assert
        Assert.Equal(schema, result);
    }

    [Fact]
    public void Serialize_SchemaDefWithOpNoIndent_OutHasIndentation()
    {
        // arrange
        var schema = "schema { query: A }";
        var document = Utf8GraphQLParser.Parse(schema);

        // act
        var result = document.ToString(false);

        // assert
        result.MatchSnapshot();
    }

    [Fact]
    public void Serialize_SchemaDefWithOpAndDirecNoIndent_InOutShouldBeTheSame()
    {
        // arrange
        var schema = "schema @a @b(c: 1) { query: A }";
        var document = Utf8GraphQLParser.Parse(schema);

        // act
        var result = document.ToString(false);

        // assert
        Assert.Equal(schema, result);
    }

    [Fact]
    public void Serialize_SchemaDefWithOpAndDirecNoIndent_OutHasIndentation()
    {
        // arrange
        var schema = "schema @a @b(c: 1) { query: A }";
        var document = Utf8GraphQLParser.Parse(schema);

        // act
        var result = document.ToString(false);

        // assert
        result.MatchSnapshot();
    }

    [Fact]
    public void Serialize_SchemaTypeExtensionDef_InOutShouldBeTheSame()
    {
        // arrange
        var schema = "extend schema { query: A }";
        var document = Utf8GraphQLParser.Parse(schema);

        // act
        var result = document.ToString(false);

        // assert
        Assert.Equal(schema, result);
    }
}

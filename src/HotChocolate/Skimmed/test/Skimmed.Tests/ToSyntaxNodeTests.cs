using System.Text;
using HotChocolate.Language;
using HotChocolate.Skimmed.Serialization;

namespace HotChocolate.Skimmed;

public class ToSyntaxNodeTests
{
    [Fact]
    public void ObjectType_ToSyntaxNode()
    {
        // arrange
        const string sdl =
            """
            type Foo @example {
                field(argument: Int @example): String @example
            }
            """;

        // act
        var schema = SchemaParser.Parse(Encoding.UTF8.GetBytes(sdl));
        var syntaxNode = ((ObjectTypeDefinition)schema.Types["Foo"]).ToSyntaxNode();

        // assert
        Assert.IsType<ObjectTypeDefinitionNode>(syntaxNode);
        syntaxNode.ToString().MatchInlineSnapshot(
            """
            type Foo @example {
                field(argument: Int @example): String @example
            }
            """);
    }

    [Fact]
    public void InterfaceType_ToSyntaxNode()
    {
        // arrange
        const string sdl =
            """
            interface Foo @example {
                field(argument: Int @example): String @example
            }
            """;

        // act
        var schema = SchemaParser.Parse(Encoding.UTF8.GetBytes(sdl));
        var syntaxNode = ((InterfaceTypeDefinition)schema.Types["Foo"]).ToSyntaxNode();

        // assert
        Assert.IsType<InterfaceTypeDefinitionNode>(syntaxNode);
        syntaxNode.ToString().MatchInlineSnapshot(
            """
            interface Foo @example {
                field(argument: Int @example): String @example
            }
            """);
    }

    [Fact]
    public void InputObjectType_ToSyntaxNode()
    {
        // arrange
        const string sdl =
            """
            input Foo @example {
                field: String @example
            }
            """;

        // act
        var schema = SchemaParser.Parse(Encoding.UTF8.GetBytes(sdl));
        var syntaxNode = ((InputObjectTypeDefinition)schema.Types["Foo"]).ToSyntaxNode();

        // assert
        Assert.IsType<InputObjectTypeDefinitionNode>(syntaxNode);
        syntaxNode.ToString().MatchInlineSnapshot(
            """
            input Foo @example {
                field: String @example
            }
            """);
    }

    [Fact]
    public void ScalarType_ToSyntaxNode()
    {
        // arrange
        const string sdl = "scalar String @example";

        // act
        var schema = SchemaParser.Parse(Encoding.UTF8.GetBytes(sdl));
        var syntaxNode = ((ScalarTypeDefinition)schema.Types["String"]).ToSyntaxNode();

        // assert
        Assert.IsType<ScalarTypeDefinitionNode>(syntaxNode);
        syntaxNode.ToString().MatchInlineSnapshot("scalar String @example");
    }

    [Fact]
    public void UnionType_ToSyntaxNode()
    {
        // arrange
        const string sdl =
            """
            union Example @example = A | B

            type A { }
            type B { }
            """;

        // act
        var schema = SchemaParser.Parse(Encoding.UTF8.GetBytes(sdl));
        var syntaxNode = ((UnionTypeDefinition)schema.Types["Example"]).ToSyntaxNode();

        // assert
        Assert.IsType<UnionTypeDefinitionNode>(syntaxNode);
        syntaxNode.ToString().MatchInlineSnapshot("union Example @example = A | B");
    }

    [Fact]
    public void EnumType_ToSyntaxNode()
    {
        // arrange
        const string sdl =
            """
            enum Example @example {
                A @example
            }
            """;

        // act
        var schema = SchemaParser.Parse(Encoding.UTF8.GetBytes(sdl));
        var syntaxNode = ((EnumTypeDefinition)schema.Types["Example"]).ToSyntaxNode();

        // assert
        Assert.IsType<EnumTypeDefinitionNode>(syntaxNode);
        syntaxNode.ToString().MatchInlineSnapshot(
            """
            enum Example @example {
                A @example
            }
            """);
    }

    [Fact]
    public void EnumValue_ToSyntaxNode()
    {
        // arrange
        const string sdl =
            """
            enum Example {
                A @example
            }
            """;

        // act
        var schema = SchemaParser.Parse(Encoding.UTF8.GetBytes(sdl));
        var syntaxNode = ((EnumTypeDefinition)schema.Types["Example"]).Values["A"].ToSyntaxNode();

        // assert
        Assert.IsType<EnumValueDefinitionNode>(syntaxNode);
        syntaxNode.ToString().MatchInlineSnapshot("A @example");
    }

    [Fact]
    public void DirectiveType_ToSyntaxNode()
    {
        // arrange
        const string sdl = "directive @example(argument: Int) on FIELD";

        // act
        var schema = SchemaParser.Parse(Encoding.UTF8.GetBytes(sdl));
        var syntaxNode = schema.DirectiveDefinitions["example"].ToSyntaxNode();

        // assert
        Assert.IsType<DirectiveDefinitionNode>(syntaxNode);
        syntaxNode.ToString().MatchInlineSnapshot("directive @example(argument: Int) on FIELD");
    }

    [Fact]
    public void ArgumentAssignment_ToSyntaxNode()
    {
        // arrange
        const string sdl =
            """
            type Foo {
                field: String @example(argument: 123)
            }

            directive @example(argument: Int) on FIELD
            """;

        // act
        var schema = SchemaParser.Parse(Encoding.UTF8.GetBytes(sdl));
        var syntaxNode =
            ((ObjectTypeDefinition)schema.Types["Foo"])
            .Fields["field"].Directives["example"].First().Arguments[0].ToSyntaxNode();

        // assert
        Assert.IsType<ArgumentNode>(syntaxNode);
        syntaxNode.ToString().MatchInlineSnapshot("argument: 123");
    }

    [Fact]
    public void OutputField_ToSyntaxNode()
    {
        // arrange
        const string sdl =
            """
            type Foo {
                field(argument: Int @example): String @example
            }
            """;

        // act
        var schema = SchemaParser.Parse(Encoding.UTF8.GetBytes(sdl));
        var syntaxNode = ((ObjectTypeDefinition)schema.Types["Foo"]).Fields["field"].ToSyntaxNode();

        // assert
        Assert.IsType<FieldDefinitionNode>(syntaxNode);
        syntaxNode.ToString().MatchInlineSnapshot("field(argument: Int @example): String @example");
    }

    [Fact]
    public void InputField_ToSyntaxNode()
    {
        // arrange
        const string sdl =
            """
            input Foo {
                field: String @example
            }
            """;

        // act
        var schema = SchemaParser.Parse(Encoding.UTF8.GetBytes(sdl));
        var syntaxNode =
            ((InputObjectTypeDefinition)schema.Types["Foo"]).Fields["field"].ToSyntaxNode();

        // assert
        Assert.IsType<InputValueDefinitionNode>(syntaxNode);
        syntaxNode.ToString().MatchInlineSnapshot("field: String @example");
    }
}

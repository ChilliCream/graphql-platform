using System.Text;
using Xunit;

namespace HotChocolate.Language;

public class InterfaceTypeParserTests
{
    [Fact]
    public void Parser_Simple()
    {
        // arrange
        var sourceText = "interface a { b: String } ";
        var parser = new Utf8GraphQLParser(
            Encoding.UTF8.GetBytes(sourceText));

        // act
        var document = parser.Parse();

        // assert
        var def = (document.Definitions[0] as InterfaceTypeDefinitionNode);
        Assert.NotNull(def);
        Assert.Equal("a", def.Name.Value);
        Assert.Null(def.Description);
        Assert.Single(def.Fields);
        Assert.Equal("b", def.Fields[0].Name.Value);
        Assert.Empty(def.Directives);
        Assert.Empty(def.Interfaces);
        Assert.Equal(SyntaxKind.InterfaceTypeDefinition, def.Kind);
    }

    [Fact]
    public void Parser_Description()
    {
        // arrange
        var sourceText = "\"\"\"test\"\"\"interface a { b: String } ";
        var parser = new Utf8GraphQLParser(
            Encoding.UTF8.GetBytes(sourceText));

        // act
        var document = parser.Parse();

        // assert
        var def = (document.Definitions[0] as InterfaceTypeDefinitionNode);
        Assert.NotNull(def);
        Assert.Equal("a", def.Name.Value);
        Assert.Equal("test", def.Description?.Value);
        Assert.Single(def.Fields);
        Assert.Equal("b", def.Fields[0].Name.Value);
        Assert.Empty(def.Directives);
        Assert.Empty(def.Interfaces);
        Assert.Equal(SyntaxKind.InterfaceTypeDefinition, def.Kind);
    }

    [Fact]
    public void Parser_Directive()
    {
        // arrange
        var sourceText = "interface a @foo(a: \"123\") { b: String } ";
        var parser = new Utf8GraphQLParser(
            Encoding.UTF8.GetBytes(sourceText));

        // act
        var document = parser.Parse();

        // assert
        var def = (document.Definitions[0] as InterfaceTypeDefinitionNode);
        Assert.NotNull(def);
        Assert.Equal("a", def.Name.Value);
        Assert.Null(def.Description);
        Assert.Single(def.Fields);
        Assert.Equal("b", def.Fields[0].Name.Value);
        Assert.Single(def.Directives);
        Assert.Equal("foo", def.Directives[0].Name.Value);
        Assert.Equal("a", def.Directives[0].Arguments[0].Name.Value);
        Assert.Equal("123", def.Directives[0].Arguments[0].Value.Value);
        Assert.Empty(def.Interfaces);
        Assert.Equal(SyntaxKind.InterfaceTypeDefinition, def.Kind);
    }

    [Fact]
    public void Parser_Directive_Multiple()
    {
        // arrange
        var sourceText = "interface a @foo(a: \"123\") @foo(b: \"321\") { b: String } ";
        var parser = new Utf8GraphQLParser(
            Encoding.UTF8.GetBytes(sourceText));

        // act
        var document = parser.Parse();

        // assert
        var def = (document.Definitions[0] as InterfaceTypeDefinitionNode);
        Assert.NotNull(def);
        Assert.Equal("a", def.Name.Value);
        Assert.Null(def.Description);
        Assert.Single(def.Fields);
        Assert.Equal("b", def.Fields[0].Name.Value);
        Assert.Collection(def.Directives,
            d =>
            {
                Assert.Equal("foo", d.Name.Value);
                Assert.Equal("a", d.Arguments[0].Name.Value);
                Assert.Equal("123", d.Arguments[0].Value.Value);
            },
            d =>
            {
                Assert.Equal("foo", d.Name.Value);
                Assert.Equal("b", d.Arguments[0].Name.Value);
                Assert.Equal("321", d.Arguments[0].Value.Value);
            });
        Assert.Empty(def.Interfaces);
        Assert.Equal(SyntaxKind.InterfaceTypeDefinition, def.Kind);
    }

    [Fact]
    public void Parser_ImplementsInterfaces()
    {
        // arrange
        var sourceText = "interface a implements e { b: String } ";
        var parser = new Utf8GraphQLParser(
            Encoding.UTF8.GetBytes(sourceText));

        // act
        var document = parser.Parse();

        // assert
        var def = (document.Definitions[0] as InterfaceTypeDefinitionNode);
        Assert.NotNull(def);
        Assert.Equal("a", def.Name.Value);
        Assert.Null(def.Description);
        Assert.Single(def.Fields);
        Assert.Equal("b", def.Fields[0].Name.Value);
        Assert.Empty(def.Directives);
        Assert.Single(def.Interfaces);
        Assert.Equal("e", def.Interfaces[0].Name.Value);
        Assert.Equal(SyntaxKind.InterfaceTypeDefinition, def.Kind);
    }

    [Fact]
    public void Parser_ImplementsInterfaces_Multiple()
    {
        // arrange
        var sourceText = "interface a implements e & f { b: String } ";
        var parser = new Utf8GraphQLParser(
            Encoding.UTF8.GetBytes(sourceText));

        // act
        var document = parser.Parse();

        // assert
        var def = (document.Definitions[0] as InterfaceTypeDefinitionNode);
        Assert.NotNull(def);
        Assert.Equal("a", def.Name.Value);
        Assert.Null(def.Description);
        Assert.Single(def.Fields);
        Assert.Equal("b", def.Fields[0].Name.Value);
        Assert.Empty(def.Directives);
        Assert.Collection(def.Interfaces,
            i =>
            {
                Assert.Equal("e", i.Name.Value);
            },
            i =>
            {
                Assert.Equal("f", i.Name.Value);
            });
        Assert.Equal(SyntaxKind.InterfaceTypeDefinition, def.Kind);
    }

    [Fact]
    public void Parser_ImplementsInterfacesAndDirectives()
    {
        // arrange
        var sourceText = "interface a implements e & f" +
            "@foo(a: \"123\") @foo(b: \"321\") { b: String } ";

        var parser = new Utf8GraphQLParser(
            Encoding.UTF8.GetBytes(sourceText));

        // act
        var document = parser.Parse();

        // assert
        var def = (document.Definitions[0] as InterfaceTypeDefinitionNode);
        Assert.NotNull(def);
        Assert.Equal("a", def.Name.Value);
        Assert.Null(def.Description);
        Assert.Single(def.Fields);
        Assert.Equal("b", def.Fields[0].Name.Value);
        Assert.Collection(def.Interfaces,
            i =>
            {
                Assert.Equal("e", i.Name.Value);
            },
            i =>
            {
                Assert.Equal("f", i.Name.Value);
            });
        Assert.Collection(def.Directives,
            d =>
            {
                Assert.Equal("foo", d.Name.Value);
                Assert.Equal("a", d.Arguments[0].Name.Value);
                Assert.Equal("123", d.Arguments[0].Value.Value);
            },
            d =>
            {
                Assert.Equal("foo", d.Name.Value);
                Assert.Equal("b", d.Arguments[0].Name.Value);
                Assert.Equal("321", d.Arguments[0].Value.Value);
            });
        Assert.Equal(SyntaxKind.InterfaceTypeDefinition, def.Kind);
    }

    [Fact]
    public void Parser__Should_Fail_WhenDirectivesBeforeInterface()
    {
        // arrange
        var sourceText = "interface a @foo(a: \"123\") implements e & f" +
            " @foo(b: \"321\") { b: String } ";

        // act & assert
        Assert.Throws<SyntaxException>(() =>
        {
            var parser = new Utf8GraphQLParser(
                Encoding.UTF8.GetBytes(sourceText));
            parser.Parse();
        });
    }
}

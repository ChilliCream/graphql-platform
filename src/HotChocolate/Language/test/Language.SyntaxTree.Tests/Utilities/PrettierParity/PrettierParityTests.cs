using CookieCrumble;

namespace HotChocolate.Language.SyntaxTree.Utilities.PrettierParity;

public class PrettierParityTests
{
    [Theory]
    [InlineData("lists/lists.graphql")]
    [InlineData("objects/objects.graphql")]
    [InlineData("arguments/hello.graphql")]
    [InlineData("directive-decl/directive_decl.graphql")] // note: blank lines added between every top-level decl, AST does not preserve input grouping trivia
    [InlineData("directives/directives.graphql")] // note: blank line between selections removed, AST does not preserve trivia
    [InlineData("kitchen-sink/kitchen-sink.graphql")] // note: copyright comment removed, AST does not model comments
    [InlineData("object-type-def/arguments.graphql")]
    [InlineData("object-type-def/directives.graphql")]
    [InlineData("object-type-def/extend.graphql")]
    [InlineData("object-type-def/implements.graphql")]
    [InlineData("object-type-def/input.graphql")]
    [InlineData("object-type-def/object_type_def.graphql")]
    [InlineData("interface/interface.graphql")] // note: copyright/section comments removed, AST does not model comments
    [InlineData("interface/many-interfaces.graphql")]
    [InlineData("enum/enum.graphql")]
    [InlineData("scalar/scalar.graphql")]
    [InlineData("union-types/union_types.graphql")] // note: trailing comment lines removed, AST does not model comments
    [InlineData("fields/fields.graphql")]
    [InlineData("fragments/fragments.graphql")]
    [InlineData("variable-definitions/variable_definitions.graphql")]
    [InlineData("hello/hello.graphql")]
    [InlineData("deprecation/directives.graphql")] // note: header comment removed and blank lines added between every top-level decl, AST does not preserve trivia
    [InlineData("type-extension-definition/type-extendsion-syntax.graphql")]
    public void Format_Should_Match_Prettier(string fixture)
    {
        // arrange
        var input = FileResource.Open($"PrettierParity/{fixture}");
        var expected = FileResource.Open(
            $"PrettierParity/{fixture.Replace(".graphql", ".expected.graphql")}");

        // act
        var document = Utf8GraphQLParser.Parse(input);
        var actual = document.ToString(indented: true) + Environment.NewLine;

        // assert
        Assert.Equal(expected, actual, ignoreLineEndingDifferences: true);
    }
}

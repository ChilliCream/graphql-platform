using System.Text;
using Xunit;

namespace HotChocolate.Language;

public class SemanticNonNullParserTests
{
    [Fact]
    public void Parse_SemanticNonNull_Field()
    {
        // arrange
        var text = """
                   type MyObject {
                     field: String @semanticNonNull
                   }
                   """;
        var parser = new Utf8GraphQLParser(Encoding.UTF8.GetBytes(text));

        // assert
        var document = parser.Parse();

        // assert
        var fieldDefinition = document.Definitions
            .OfType<ObjectTypeDefinitionNode>()
            .FirstOrDefault()?
            .Fields.FirstOrDefault(f => f.Name.Value == "field");
        Assert.NotNull(fieldDefinition);
        Assert.Empty(fieldDefinition.Directives);
        var fieldReturnType = Assert.IsType<SemanticNonNullTypeNode>(fieldDefinition.Type);
        Assert.IsType<NamedTypeNode>(fieldReturnType.Type);
    }

    [Fact]
    public void Parse_SemanticNonNull_List_And_Nullable_ListItem()
    {
        // arrange
        var text = """
                   type MyObject {
                     field: [String] @semanticNonNull
                   }
                   """;
        var parser = new Utf8GraphQLParser(Encoding.UTF8.GetBytes(text));

        // assert
        var document = parser.Parse();

        // assert
        var fieldDefinition = document.Definitions
            .OfType<ObjectTypeDefinitionNode>()
            .FirstOrDefault()?
            .Fields.FirstOrDefault(f => f.Name.Value == "field");
        Assert.NotNull(fieldDefinition);
        Assert.Empty(fieldDefinition.Directives);
        var fieldReturnType = Assert.IsType<SemanticNonNullTypeNode>(fieldDefinition.Type);
        var innerListType = Assert.IsType<ListTypeNode>(fieldReturnType.Type);
        Assert.IsType<NamedTypeNode>(innerListType.Type);
    }

    [Fact]
    public void Parse_Nullable_List_And_SemanticNonNull_List_Item()
    {
        // arrange
        var text = """
                   type MyObject {
                     field: [String] @semanticNonNull(levels: [ 1 ])
                   }
                   """;
        var parser = new Utf8GraphQLParser(Encoding.UTF8.GetBytes(text));

        // assert
        var document = parser.Parse();

        // assert
        var fieldDefinition = document.Definitions
            .OfType<ObjectTypeDefinitionNode>()
            .FirstOrDefault()?
            .Fields.FirstOrDefault(f => f.Name.Value == "field");
        Assert.NotNull(fieldDefinition);
        Assert.Empty(fieldDefinition.Directives);
        var fieldReturnType = Assert.IsType<ListTypeNode>(fieldDefinition.Type);
        var innerListType = Assert.IsType<SemanticNonNullTypeNode>(fieldReturnType.Type);
        Assert.IsType<NamedTypeNode>(innerListType.Type);
    }

    [Fact]
    public void Parse_SemanticNonNull_List_And_SemanticNonNull_ListItem()
    {
        // arrange
        var text = """
                   type MyObject {
                     field: [String] @semanticNonNull(levels: [ 0, 1 ])
                   }
                   """;
        var parser = new Utf8GraphQLParser(Encoding.UTF8.GetBytes(text));

        // assert
        var document = parser.Parse();

        // assert
        var fieldDefinition = document.Definitions
            .OfType<ObjectTypeDefinitionNode>()
            .FirstOrDefault()?
            .Fields.FirstOrDefault(f => f.Name.Value == "field");
        Assert.NotNull(fieldDefinition);
        Assert.Empty(fieldDefinition.Directives);
        var fieldReturnType = Assert.IsType<SemanticNonNullTypeNode>(fieldDefinition.Type);
        var listType = Assert.IsType<ListTypeNode>(fieldReturnType.Type);
        var innerListType = Assert.IsType<SemanticNonNullTypeNode>(listType.Type);
        Assert.IsType<NamedTypeNode>(innerListType.Type);
    }

    [Fact]
    public void Parse_SemanticNonNull_List_And_Nested_Nullable_List_And_SemanticNonNull_ListItem()
    {
        // arrange
        var text = """
                   type MyObject {
                     field: [[String]] @semanticNonNull(levels: [ 0, 2 ])
                   }
                   """;
        var parser = new Utf8GraphQLParser(Encoding.UTF8.GetBytes(text));

        // assert
        var document = parser.Parse();

        // assert
        var fieldDefinition = document.Definitions
            .OfType<ObjectTypeDefinitionNode>()
            .FirstOrDefault()?
            .Fields.FirstOrDefault(f => f.Name.Value == "field");
        Assert.NotNull(fieldDefinition);
        Assert.Empty(fieldDefinition.Directives);
        var fieldReturnType = Assert.IsType<SemanticNonNullTypeNode>(fieldDefinition.Type);
        var listType = Assert.IsType<ListTypeNode>(fieldReturnType.Type);
        var innerListType = Assert.IsType<ListTypeNode>(listType.Type);
        var innermostListType = Assert.IsType<SemanticNonNullTypeNode>(innerListType.Type);
        Assert.IsType<NamedTypeNode>(innermostListType.Type);
    }
}

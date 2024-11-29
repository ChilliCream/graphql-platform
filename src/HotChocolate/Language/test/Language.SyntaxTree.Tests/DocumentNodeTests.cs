using Xunit;

namespace HotChocolate.Language.SyntaxTree;

public class DocumentNodeTests
{
    [Fact]
    public void CreateDocumentWithLocation()
    {
        // arrange
        var location = new Location(0, 0, 0, 0);

        var fragment = new FragmentDefinitionNode(
            null, new NameNode("foo"),
            Array.Empty<VariableDefinitionNode>(),
            new NamedTypeNode("foo"),
            Array.Empty<DirectiveNode>(),
            new SelectionSetNode(Array.Empty<ISelectionNode>()));

        // act
        var document = new DocumentNode(location, new IDefinitionNode[] { fragment, });

        // assert
        Assert.Equal(SyntaxKind.Document, document.Kind);
        Assert.Equal(location, document.Location);
        Assert.Collection(document.Definitions, d => Assert.Equal(fragment, d));
    }

    [Fact]
    public void CreateDocument()
    {
        // arrange
        var fragment = new FragmentDefinitionNode(
            null, new NameNode("foo"),
            Array.Empty<VariableDefinitionNode>(),
            new NamedTypeNode("foo"),
            Array.Empty<DirectiveNode>(),
            new SelectionSetNode(Array.Empty<ISelectionNode>()));

        // act
        var document = new DocumentNode(new IDefinitionNode[] { fragment, });

        // assert
        Assert.Equal(SyntaxKind.Document, document.Kind);
        Assert.Null(document.Location);
        Assert.Collection(document.Definitions, d => Assert.Equal(fragment, d));
    }

    [Fact]
    public void Document_With_Location()
    {
        // arrange
        var location = new Location(0, 0, 0, 0);

        var fragment = new FragmentDefinitionNode(
            null, new NameNode("foo"),
            Array.Empty<VariableDefinitionNode>(),
            new NamedTypeNode("foo"),
            Array.Empty<DirectiveNode>(),
            new SelectionSetNode(Array.Empty<ISelectionNode>()));

        var document = new DocumentNode(new IDefinitionNode[] { fragment, });

        // act
        document = document.WithLocation(location);

        // assert
        Assert.Equal(location, document.Location);
    }

    [Fact]
    public void Document_With_Location_Null()
    {
        // arrange
        var location = new Location(0, 0, 0, 0);

        var fragment = new FragmentDefinitionNode(
            null, new NameNode("foo"),
            Array.Empty<VariableDefinitionNode>(),
            new NamedTypeNode("foo"),
            Array.Empty<DirectiveNode>(),
            new SelectionSetNode(Array.Empty<ISelectionNode>()));

        var document = new DocumentNode(location, new IDefinitionNode[] { fragment, });

        // act
        document = document.WithLocation(null);

        // assert
        Assert.Null(document.Location);
    }

    [Fact]
    public void Document_With_Definitions()
    {
        // arrange
        var location = new Location(0, 0, 0, 0);

        var fragment = new FragmentDefinitionNode(
            null, new NameNode("foo"),
            Array.Empty<VariableDefinitionNode>(),
            new NamedTypeNode("foo"),
            Array.Empty<DirectiveNode>(),
            new SelectionSetNode(Array.Empty<ISelectionNode>()));

        var document = new DocumentNode(location, new IDefinitionNode[] { });

        // act
        document = document.WithDefinitions(new IDefinitionNode[] { fragment, });

        // assert
        Assert.Collection(document.Definitions, d => Assert.Equal(fragment, d));
    }

    [Fact]
    public void Document_With_Definitions_Null()
    {
        // arrange
        var fragment = new FragmentDefinitionNode(
            null, new NameNode("foo"),
            Array.Empty<VariableDefinitionNode>(),
            new NamedTypeNode("foo"),
            Array.Empty<DirectiveNode>(),
            new SelectionSetNode(Array.Empty<ISelectionNode>()));

        var document = new DocumentNode(new IDefinitionNode[] { });

        // act
        void Action() => document.WithDefinitions(null!);

        // assert
        Assert.Throws<ArgumentNullException>(Action);
    }

    [Fact]
    public void Equals_With_Same_Location()
    {
        // arrange
        var definitions1 = new List<IDefinitionNode>
        {
            new EnumTypeDefinitionNode(
                null,
                new("Abc"),
                null,
                Array.Empty<DirectiveNode>(),
                new[]
                {
                    new EnumValueDefinitionNode(
                        null,
                        new("DEF"),
                        null,
                        Array.Empty<DirectiveNode>()),
                }),
        };

        var definitions2 = new List<IDefinitionNode>
        {
            new EnumTypeDefinitionNode(
                null,
                new("Def"),
                null,
                Array.Empty<DirectiveNode>(),
                new[]
                {
                    new EnumValueDefinitionNode(
                        null,
                        new("DEF"),
                        null,
                        Array.Empty<DirectiveNode>()),
                }),
        };

        var a = new DocumentNode(
            TestLocations.Location1,
            definitions1);
        var b = new DocumentNode(
            TestLocations.Location1,
            definitions1);
        var c = new DocumentNode(
            TestLocations.Location1,
            definitions2);

        // act
        var abResult = SyntaxComparer.BySyntax.Equals(a, b);
        var aaResult = SyntaxComparer.BySyntax.Equals(a, a);
        var acResult = SyntaxComparer.BySyntax.Equals(a, c);
        var aNullResult = SyntaxComparer.BySyntax.Equals(a, default);

        // assert
        Assert.True(abResult);
        Assert.True(aaResult);
        Assert.False(acResult);
        Assert.False(aNullResult);
    }

    [Fact]
    public void Equals_With_Different_Location()
    {
        // arrange
        var definitions1 = new List<IDefinitionNode>
        {
            new EnumTypeDefinitionNode(
                null,
                new("Abc"),
                null,
                Array.Empty<DirectiveNode>(),
                new[]
                {
                    new EnumValueDefinitionNode(
                        null,
                        new("DEF"),
                        null,
                        Array.Empty<DirectiveNode>()),
                }),
        };

        var definitions2 = new List<IDefinitionNode>
        {
            new EnumTypeDefinitionNode(
                null,
                new("Def"),
                null,
                Array.Empty<DirectiveNode>(),
                new[]
                {
                    new EnumValueDefinitionNode(
                        null,
                        new("DEF"),
                        null,
                        Array.Empty<DirectiveNode>()),
                }),
        };

        var a = new DocumentNode(
            TestLocations.Location1,
            definitions1);
        var b = new DocumentNode(
            TestLocations.Location2,
            definitions1);
        var c = new DocumentNode(
            TestLocations.Location1,
            definitions2);

        // act
        var abResult = SyntaxComparer.BySyntax.Equals(a, b);
        var aaResult = SyntaxComparer.BySyntax.Equals(a, a);
        var acResult = SyntaxComparer.BySyntax.Equals(a, c);
        var aNullResult = SyntaxComparer.BySyntax.Equals(a, default);

        // assert
        Assert.True(abResult);
        Assert.True(aaResult);
        Assert.False(acResult);
        Assert.False(aNullResult);
    }

    [Fact]
    public void GetHashCode_With_Location()
    {
        // arrange
        // arrange
        var definitions1 = new List<IDefinitionNode>
        {
            new EnumTypeDefinitionNode(
                null,
                new("Abc"),
                null,
                Array.Empty<DirectiveNode>(),
                new[]
                {
                    new EnumValueDefinitionNode(
                        null,
                        new("DEF"),
                        null,
                        Array.Empty<DirectiveNode>()),
                }),
        };

        var definitions2 = new List<IDefinitionNode>
        {
            new EnumTypeDefinitionNode(
                null,
                new("Def"),
                null,
                Array.Empty<DirectiveNode>(),
                new[]
                {
                    new EnumValueDefinitionNode(
                        null,
                        new("DEF"),
                        null,
                        Array.Empty<DirectiveNode>()),
                }),
        };

        var a = new DocumentNode(
            TestLocations.Location1,
            definitions1);
        var b = new DocumentNode(
            TestLocations.Location2,
            definitions1);
        var c = new DocumentNode(
            TestLocations.Location1,
            definitions2);
        var d = new DocumentNode(
            TestLocations.Location2,
            definitions2);

        // act
        var aHash = SyntaxComparer.BySyntax.GetHashCode(a);
        var bHash = SyntaxComparer.BySyntax.GetHashCode(b);
        var cHash = SyntaxComparer.BySyntax.GetHashCode(c);
        var dHash = SyntaxComparer.BySyntax.GetHashCode(d);

        // assert
        Assert.Equal(aHash, bHash);
        Assert.NotEqual(aHash, cHash);
        Assert.Equal(cHash, dHash);
        Assert.NotEqual(aHash, dHash);
    }
}

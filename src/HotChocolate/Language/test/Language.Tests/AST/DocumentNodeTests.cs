using System;
using Xunit;

namespace HotChocolate.Language
{
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
            var document = new DocumentNode(location, new IDefinitionNode[] { fragment });

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
            var document = new DocumentNode(new IDefinitionNode[] { fragment });

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

            var document = new DocumentNode(new IDefinitionNode[] { fragment });

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

            var document = new DocumentNode(location, new IDefinitionNode[] { fragment });

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
            document = document.WithDefinitions(new IDefinitionNode[] { fragment });

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
            void Action() => document.WithDefinitions(null);

            // assert
            Assert.Throws<ArgumentNullException>(Action);
        }
    }
}

using System.Collections.Generic;
using Xunit;

namespace HotChocolate.Language
{
    public class DirectiveDefinitionNodeTests
    {
        [InlineData(true)]
        [InlineData(false)]
        [Theory]
        public void CreateDirectiveDefinitionWithLocation(bool isRepeatable)
        {
            // arrange
            var source = new Source("foo");
            var start = new SyntaxToken(
                TokenKind.StartOfFile, 0, 0, 1, 1, null);
            var location = new Location(source, start, start);
            var name = new NameNode("foo");
            var description = new StringValueNode("bar");
            var arguments = new List<InputValueDefinitionNode>();
            var locations = new List<NameNode>();

            // act
            var directiveDefinition = new DirectiveDefinitionNode(
                location, name, description, isRepeatable,
                arguments, locations);

            // assert
            Assert.Equal(NodeKind.DirectiveDefinition,
                directiveDefinition.Kind);
            Assert.Equal(location, directiveDefinition.Location);
            Assert.Equal(name, directiveDefinition.Name);
            Assert.Equal(description, directiveDefinition.Description);
            Assert.Equal(isRepeatable, directiveDefinition.IsRepeatable);
            Assert.Equal(arguments, directiveDefinition.Arguments);
            Assert.Equal(locations, directiveDefinition.Locations);
        }

        [Fact]
        public void CreateDirectiveDefinition()
        {
            // arrange
            var name = new NameNode("foo");
            var description = new StringValueNode("bar");
            var arguments = new List<InputValueDefinitionNode>();
            var locations = new List<NameNode>();

            // act
            var directiveDefinition = new DirectiveDefinitionNode(
                null, name, description, true,
                arguments, locations);

            // assert
            Assert.Equal(NodeKind.DirectiveDefinition,
                directiveDefinition.Kind);
            Assert.Null(directiveDefinition.Location);
            Assert.Equal(name, directiveDefinition.Name);
            Assert.Equal(description, directiveDefinition.Description);
            Assert.Equal(arguments, directiveDefinition.Arguments);
            Assert.Equal(locations, directiveDefinition.Locations);
        }
    }
}

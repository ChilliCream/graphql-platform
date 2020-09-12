using System;
using System.Collections.Generic;
using Xunit;

namespace HotChocolate.Language
{
    public class EnumTypeDefinitionNodeTests
    {
        [Fact]
        public void EnumTypeDefinitionWithLocation()
        {
            // arrange
            var location = new Location(0, 0, 0, 0);
            var name = new NameNode("foo");
            var description = new StringValueNode("bar");
            var directives = new List<DirectiveNode>();
            var values = new List<EnumValueDefinitionNode>();


            // act
            var type = new EnumTypeDefinitionNode(
                location,
                name,
                description,
                directives,
                values);

            // assert
            Assert.Equal(SyntaxKind.EnumTypeDefinition, type.Kind);
            Assert.Equal(location, type.Location);
            Assert.Equal(name, type.Name);
            Assert.Equal(description, type.Description);
            Assert.Equal(directives, type.Directives);
            Assert.Equal(values, type.Values);
        }

        [Fact]
        public void EnumTypeDefinitionWithoutLocation()
        {
            // arrange
            var name = new NameNode("foo");
            var description = new StringValueNode("bar");
            var directives = new List<DirectiveNode>();
            var values = new List<EnumValueDefinitionNode>();

            // act
            var type = new EnumTypeDefinitionNode(
                null,
                name,
                description,
                directives,
                values);

            // assert
            Assert.Equal(SyntaxKind.EnumTypeDefinition, type.Kind);
            Assert.Null(type.Location);
            Assert.Equal(name, type.Name);
            Assert.Equal(description, type.Description);
            Assert.Equal(directives, type.Directives);
            Assert.Equal(values, type.Values);
        }

        [Fact]
        public void EnumTypeDefinitionWithoutName_ArgumentNullException()
        {
            // arrange
            var location = new Location(0, 0, 0, 0);
            var description = new StringValueNode("bar");
            var directives = new List<DirectiveNode>();
            var values = new List<EnumValueDefinitionNode>();


            // act
            Action a = () => new EnumTypeDefinitionNode(
                 location,
                 null,
                 description,
                 directives,
                 values);

            // assert
            Assert.Throws<ArgumentNullException>(a);
        }

        [Fact]
        public void EnumTypeDefinitionWithoutDirectives_ArgumentNullException()
        {
            // arrange
            var location = new Location(0, 0, 0, 0);
            var name = new NameNode("foo");
            var description = new StringValueNode("bar");
            var values = new List<EnumValueDefinitionNode>();


            // act
            Action a = () => new EnumTypeDefinitionNode(
                 location,
                 name,
                 description,
                 null,
                 values);

            // assert
            Assert.Throws<ArgumentNullException>(a);
        }

        [Fact]
        public void EnumTypeDefinitionWithoutValues_ArgumentNullException()
        {
            // arrange
            var location = new Location(0, 0, 0, 0);
            var name = new NameNode("foo");
            var description = new StringValueNode("bar");
            var directives = new List<DirectiveNode>();

            // act
            Action a = () => new EnumTypeDefinitionNode(
                 location,
                 name,
                 description,
                 directives,
                 null);

            // assert
            Assert.Throws<ArgumentNullException>(a);
        }

        [Fact]
        public void WithName()
        {
            // arrange
            var location = new Location(0, 0, 0, 0);
            var name = new NameNode("foo");
            var description = new StringValueNode("bar");
            var directives = new List<DirectiveNode>();
            var values = new List<EnumValueDefinitionNode>();

            var type = new EnumTypeDefinitionNode(
               location,
               name,
               description,
               directives,
               values);

            // act
            type = type.WithName(new NameNode("baz"));


            // assert
            Assert.Equal("baz", type.Name.Value);
        }

        [Fact]
        public void WithDescription()
        {
            // arrange
            var location = new Location(0, 0, 0, 0);
            var name = new NameNode("foo");
            var description = new StringValueNode("bar");
            var directives = new List<DirectiveNode>();
            var values = new List<EnumValueDefinitionNode>();

            var type = new EnumTypeDefinitionNode(
               location,
               name,
               description,
               directives,
               values);

            // act
            type = type.WithDescription(new StringValueNode("baz"));


            // assert
            Assert.Equal("baz", type.Description.Value);
        }

        [Fact]
        public void WithDirectives()
        {
            // arrange
            var location = new Location(0, 0, 0, 0);
            var name = new NameNode("foo");
            var description = new StringValueNode("bar");
            var directives = new List<DirectiveNode>();
            var values = new List<EnumValueDefinitionNode>();

            var type = new EnumTypeDefinitionNode(
               location,
               name,
               description,
               new List<DirectiveNode>(),
               values);

            // act
            type = type.WithDirectives(directives);


            // assert
            Assert.Equal(directives, type.Directives);
        }

        [Fact]
        public void WithValues()
        {
            // arrange
            var location = new Location(0, 0, 0, 0);
            var name = new NameNode("foo");
            var description = new StringValueNode("bar");
            var directives = new List<DirectiveNode>();
            var values = new List<EnumValueDefinitionNode>();

            var type = new EnumTypeDefinitionNode(
               location,
               name,
               description,
               directives,
               new List<EnumValueDefinitionNode>());

            // act
            type = type.WithValues(values);


            // assert
            Assert.Equal(values, type.Values);
        }

        [Fact]
        public void WithLocation()
        {
            // arrange
            var location = new Location(0, 0, 0, 0);
            var name = new NameNode("foo");
            var description = new StringValueNode("bar");
            var directives = new List<DirectiveNode>();
            var values = new List<EnumValueDefinitionNode>();

            var type = new EnumTypeDefinitionNode(
               null,
               name,
               description,
               directives,
               values);

            // act
            type = type.WithLocation(location);


            // assert
            Assert.Equal(location, type.Location);
        }
    }
}

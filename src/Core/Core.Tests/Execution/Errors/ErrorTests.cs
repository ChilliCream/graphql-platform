using System.Collections.Generic;
using HotChocolate.Execution;
using HotChocolate.Language;
using Xunit;

namespace HotChocolate.Execution
{
    public class ErrorTests
    {
        [Fact]
        public void ArgumentError_CreateWithoutLocation()
        {
            // arrange
            var fieldSelection = new FieldNode(
                null,
                new NameNode("foo"),
                null,
                new List<DirectiveNode>(),
                new List<ArgumentNode>(),
                null);

            // act
            var error = new ArgumentError("a", "b", fieldSelection);

            // assert
            Assert.Equal("a", error.Message);
            Assert.Equal("b", error.Extensions["argumentName"]);
            Assert.Equal("foo", error.Extensions["fieldName"]);
            Assert.Null(error.Locations);
        }

        [Fact]
        public void FieldError_CreateWithoutLocation()
        {
            // arrange
            var fieldSelection = new FieldNode(
                null,
                new NameNode("foo"),
                null,
                new List<DirectiveNode>(),
                new List<ArgumentNode>(),
                null);

            // act
            var error = new FieldError("a", fieldSelection);

            // assert
            Assert.Equal("a", error.Message);
            Assert.Equal("foo", error.Extensions["fieldName"]);
            Assert.Null(error.Locations);
        }

        [Fact]
        public void VariableError_CreateWithoutLocation()
        {
            // arrange
            // act
            var error = new VariableError("foo", "bar");

            // assert
            Assert.Equal("foo", error.Message);
            Assert.Equal("bar", error.Extensions["variableName"]);
            Assert.Null(error.Locations);
            Assert.Null(error.Path);
        }
    }
}

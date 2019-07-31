using System;
using HotChocolate.Language;
using Snapshooter.Xunit;
using Xunit;

namespace HotChocolate.Execution.Tests
{
    public class QuerySourceTextTests
    {
        [Fact]
        public void Create_Document_IsNull()
        {
            // arrange
            // act
            Action action = () => new QuerySourceText(null);

            // assert
            Assert.Throws<ArgumentNullException>(action);
        }

        [Fact]
        public void Create_Document()
        {
            // arrange
            // act
            var query = new QuerySourceText("{ a }");

            // assert
            Assert.Equal("{ a }", query.Text);
        }

        [Fact]
        public void QueryDocument_ToString()
        {
            // arrange
            // act
            var query = new QuerySourceText("{ a }");

            // assert
            query.ToString().MatchSnapshot();
        }

        [Fact]
        public void QueryDocument_ToSource()
        {
            // arrange
            // act
            var query = new QuerySourceText("{ a }");

            // assert
            QuerySyntaxSerializer.Serialize(
                Utf8GraphQLParser.Parse(query.ToSpan()))
                .ToString().MatchSnapshot();
        }
    }
}

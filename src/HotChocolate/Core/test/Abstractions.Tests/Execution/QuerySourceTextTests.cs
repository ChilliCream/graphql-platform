using System;
using System.IO;
using System.Threading.Tasks;
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
                Utf8GraphQLParser.Parse(query.AsSpan()))
                .ToString().MatchSnapshot();
        }

        [Fact]
        public async Task QuerySourceText_WriteToAsync()
        {
            // arrange
            DocumentNode document = Utf8GraphQLParser.Parse("{ a }");
            var query = new QuerySourceText("{ a }");
            byte[] buffer;

            // act
            using (var stream = new MemoryStream())
            {
                await query.WriteToAsync(stream);
                buffer = stream.ToArray();
            }

            // assert
            QuerySyntaxSerializer.Serialize(
                Utf8GraphQLParser.Parse(buffer))
                .ToString().MatchSnapshot();
        }

        [Fact]
        public void QuerySourceText_WriteTo()
        {
            // arrange
            DocumentNode document = Utf8GraphQLParser.Parse("{ a }");
            var query = new QuerySourceText("{ a }");
            byte[] buffer;

            // act
            using (var stream = new MemoryStream())
            {
                query.WriteTo(stream);
                buffer = stream.ToArray();
            }

            // assert
            QuerySyntaxSerializer.Serialize(
                Utf8GraphQLParser.Parse(buffer))
                .ToString().MatchSnapshot();
        }
    }
}

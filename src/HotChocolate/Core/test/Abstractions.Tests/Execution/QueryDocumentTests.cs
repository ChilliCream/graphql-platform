using System;
using System.IO;
using System.Threading.Tasks;
using HotChocolate.Language;
using HotChocolate.Language.Utilities;
using Snapshooter.Xunit;
using Xunit;

namespace HotChocolate.Execution
{
    public class QueryDocumentTests
    {
        [Fact]
        public void Create_Document_IsNull()
        {
            // arrange
            // act
            void Action() => new QueryDocument(null);

            // assert
            Assert.Throws<ArgumentNullException>(Action);
        }

        [Fact]
        public void Create_Document()
        {
            // arrange
            DocumentNode document = Utf8GraphQLParser.Parse("{ a }");

            // act
            var query = new QueryDocument(document);

            // assert
            Assert.Equal(document, query.Document);
        }

        [Fact]
        public void QueryDocument_ToString()
        {
            // arrange
            DocumentNode document = Utf8GraphQLParser.Parse("{ a }");

            // act
            var query = new QueryDocument(document);

            // assert
            query.ToString().MatchSnapshot();
        }

        [Fact]
        public void QueryDocument_ToSource()
        {
            // arrange
            DocumentNode document = Utf8GraphQLParser.Parse("{ a }");

            // act
            var query = new QueryDocument(document);

            // assert
            Utf8GraphQLParser
                .Parse(query.AsSpan())
                .Print(true)
                .MatchSnapshot();
        }

        [Fact]
        public async Task QueryDocument_WriteToAsync()
        {
            // arrange
            DocumentNode document = Utf8GraphQLParser.Parse("{ a }");
            var query = new QueryDocument(document);
            byte[] buffer;

            // act
            using (var stream = new MemoryStream())
            {
                await query.WriteToAsync(stream);
                buffer = stream.ToArray();
            }

            // assert
            Utf8GraphQLParser
                .Parse(buffer)
                .Print(true)
                .MatchSnapshot();
        }

        [Fact]
        public void QueryDocument_WriteTo()
        {
            // arrange
            DocumentNode document = Utf8GraphQLParser.Parse("{ a }");
            var query = new QueryDocument(document);
            byte[] buffer;

            // act
            using (var stream = new MemoryStream())
            {
                query.WriteTo(stream);
                buffer = stream.ToArray();
            }

            // assert
            Utf8GraphQLParser
                .Parse(buffer)
                .Print(true)
                .MatchSnapshot();
        }
    }
}

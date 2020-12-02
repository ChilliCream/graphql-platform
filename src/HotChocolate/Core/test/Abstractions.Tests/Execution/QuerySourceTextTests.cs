using System;
using System.IO;
using System.Threading.Tasks;
using HotChocolate.Language;
using HotChocolate.Language.Utilities;
using Snapshooter.Xunit;
using Xunit;

namespace HotChocolate.Execution
{
    public class QuerySourceTextTests
    {
        [Fact]
        public void Create_Document_IsNull()
        {
            // arrange
            // act
            void Action() => new QuerySourceText(null);

            // assert
            Assert.Throws<ArgumentNullException>(Action);
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
            Utf8GraphQLParser
                .Parse(query.AsSpan())
                .Print(true)
                .MatchSnapshot();
        }

        [Fact]
        public async Task QuerySourceText_WriteToAsync()
        {
            // arrange
            var query = new QuerySourceText("{ a }");
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
        public void QuerySourceText_WriteTo()
        {
            // arrange
            var query = new QuerySourceText("{ a }");
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

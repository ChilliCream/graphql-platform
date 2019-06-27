using System.Collections.Generic;
using System.Text;
using ChilliCream.Testing;
using Newtonsoft.Json;
using Snapshooter.Xunit;
using Xunit;

namespace HotChocolate.Language
{
    public class Utf8GraphQLRequestParserTests
    {
        [Fact]
        public void Parse_Kitchen_Sink_Query_No_Cache()
        {
            // arrange
            byte[] source = Encoding.UTF8.GetBytes(
                JsonConvert.SerializeObject(
                    new GraphQLRequestDto
                    {
                        Query = FileResource.Open("kitchen-sink.graphql")
                    }));

            // act
            var parserOptions = new ParserOptions();
            var requestParser = new Utf8GraphQLRequestParser(
                source, parserOptions);
            IReadOnlyList<GraphQLRequest> batch = requestParser.Parse();

            // assert
            Assert.Collection(batch,
                r =>
                {
                    Assert.Null(r.OperationName);
                    Assert.Null(r.NamedQuery);
                    Assert.Null(r.Variables);
                    Assert.Null(r.Extensions);

                    QuerySyntaxSerializer.Serialize(r.Query, true)
                        .MatchSnapshot();
                });
        }
    }

    public class GraphQLRequestDto
    {
        [JsonProperty("operationName")]
        public string OperationName { get; set; }

        [JsonProperty("namedQuery")]
        public string NamedQuery { get; set; }

        [JsonProperty("query")]
        public string Query { get; set; }

        [JsonProperty("variables")]
        public IReadOnlyDictionary<string, object> Variables { get; set; }

        [JsonProperty("extensions")]
        public IReadOnlyDictionary<string, object> Extensions { get; set; }
    }
}

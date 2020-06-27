using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using HotChocolate.Execution;
using HotChocolate.Language;
using Moq;
using Newtonsoft.Json;
using Xunit;

namespace HotChocolate.Stitching.Utilities
{
    public class HttpQueryClientTests
    {
        private readonly HttpQueryClient sut;

        public HttpQueryClientTests()
        {
            sut = new HttpQueryClient();
        }

        [Fact]
        public async Task FetchAsync_Sends_Valid_Json_Request()
        {
            // arrange
            var messageHandler = new FakeMessageHandler();

            var httpClient = new HttpClient(messageHandler)
            {
                BaseAddress = new Uri("http://some.url")
            };

            IReadOnlyQueryRequest query = QueryRequestBuilder.New()
                .SetQuery(
                    @"query aQuery {
                        foo
                        bar
                    }")
                .AddVariableValue("strVar", new StringValueNode("some-string"))
                .AddVariableValue("intVal", new IntValueNode(42))
                .AddVariableValue("floatVal", new FloatValueNode(1.23m))
                .AddVariableValue("boolVal", new BooleanValueNode(true))
                .AddVariableValue("listVal",
                    new ListValueNode(
                        new ReadOnlyCollection<FloatValueNode>(
                            new List<FloatValueNode>
                            {
                                new FloatValueNode(1.23m),
                                new FloatValueNode(1.80m),
                                new FloatValueNode(2.80m)
                            }) ))
                .AddVariableValue("listStringVal",
                    new ListValueNode(
                        new ReadOnlyCollection<StringValueNode>(
                            new List<StringValueNode>
                            {
                                new StringValueNode("a"),
                                new StringValueNode("b"),
                                new StringValueNode("c")
                            })))
                .AddVariableValue("enumVal", new EnumValueNode(System.Net.HttpStatusCode.OK))
                .AddVariableValue("otherStrVar", new StringValueNode("some-other-string"))
                .Create();

            // act
            await sut.FetchAsync(
                query,
                httpClient);

            // assert
            try
            {
                dynamic requestObj = JsonConvert.DeserializeObject(messageHandler.requestBody);
                Assert.NotNull(requestObj);
                Assert.NotNull(requestObj.query);
                Assert.NotNull(requestObj.variables);
            }
            catch (Exception e)
            {
                Assert.True(false, $"Unable to parse request as json: {e.Message}");
            }
        }

        private class FakeMessageHandler : HttpMessageHandler
        {
            public string requestBody;

            protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            {
                requestBody = await request.Content.ReadAsStringAsync();

                var response = new HttpResponseMessage(System.Net.HttpStatusCode.OK)
                {
                    Content = new StringContent("{}")
                };
                return response;
            }
        }
    }
}

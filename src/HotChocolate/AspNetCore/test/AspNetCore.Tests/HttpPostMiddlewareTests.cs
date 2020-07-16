using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using HotChocolate.StarWars;
using HotChocolate.AspNetCore.Utilities;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using Xunit;
using HotChocolate.Language;
using Snapshooter.Xunit;

namespace HotChocolate.AspNetCore.Utilities
{
    public class HttpPostMiddlewareTests : ServerTestBase
    {
        public HttpPostMiddlewareTests(TestServerFactory serverFactory)
            : base(serverFactory)
        {
        }

        [Fact]
        public async Task Simple_IsAlive_Test()
        {
            // arrange
            TestServer server = CreateStarWarsServer();

            // act
            ClientQueryResult result = await server.PostAsync(
                new ClientQueryRequest { Query = "{ __typename }" })
                .ConfigureAwait(false);

            // assert
            result.MatchSnapshot();
        }
    }

    public class ServerTestBase
        : IClassFixture<TestServerFactory>
    {
        public ServerTestBase(TestServerFactory serverFactory)
        {
            ServerFactory = serverFactory;
        }

        protected TestServerFactory ServerFactory { get; set; }

        protected TestServer CreateStarWarsServer(string pattern = "/graphql")
        {
            return ServerFactory.Create(
                services => services
                    .AddRouting()
                    .AddGraphQLServer()
                    .AddStarWarsTypes()
                    .AddStarWarsRepositories(),
                app =>
                {
                    app.UseRouting();

                    app.UseEndpoints(endpoints =>
                    {
                        endpoints.MapGraphQL(pattern);
                    });
                });
        }

        protected async Task<ClientQueryResult> DeserializeAsync(
            HttpResponseMessage message)
        {
            Assert.Equal(HttpStatusCode.OK, message.StatusCode);
            string json = await message.Content.ReadAsStringAsync();
            return JsonConvert.DeserializeObject<ClientQueryResult>(json);
        }

        protected async Task<List<ClientQueryResult>> DeserializeBatchAsync(
            HttpResponseMessage message)
        {
            Assert.Equal(HttpStatusCode.OK, message.StatusCode);
            string json = await message.Content.ReadAsStringAsync();
            return JsonConvert.DeserializeObject<List<ClientQueryResult>>(json);
        }
    }

    public class TestServerFactory
        : IDisposable
    {
        public List<TestServer> _instances = new List<TestServer>();

        public TestServer Create(
            Action<IServiceCollection> configureServices,
            Action<IApplicationBuilder> configureApplication)
        {
            IWebHostBuilder builder = new WebHostBuilder()
                .Configure(configureApplication)
                .ConfigureServices(services =>
                {
                    services.AddHttpContextAccessor();
                    configureServices?.Invoke(services);
                });

            var server = new TestServer(builder);
            _instances.Add(server);
            return server;
        }

        public void Dispose()
        {
            foreach (TestServer testServer in _instances)
            {
                testServer.Dispose();
            }
        }
    }

    public static class TestServerExtensions
    {
        public static async Task<ClientQueryResult> PostAsync(
            this TestServer testServer,
            ClientQueryRequest request,
            string path = "/graphql")
        {
            HttpResponseMessage response =
                await SendPostRequestAsync(
                    testServer,
                    JsonConvert.SerializeObject(request),
                    path);
            response.EnsureSuccessStatusCode();
            return JsonConvert.DeserializeObject<ClientQueryResult>(
                await response.Content.ReadAsStringAsync());
        }


        public static Task<HttpResponseMessage> SendPostRequestAsync<TObject>(
            this TestServer testServer, TObject requestBody, string path = "/graphql")
        {
            return SendPostRequestAsync(
                testServer,
                JsonConvert.SerializeObject(requestBody),
                path);
        }

        public static Task<HttpResponseMessage> SendPostRequestAsync(
            this TestServer testServer, string requestBody, string path = null)
        {
            return SendPostRequestAsync(
                testServer, requestBody,
                "application/json", path);
        }

        public static Task<HttpResponseMessage> SendPostRequestAsync(
            this TestServer testServer, string requestBody,
            string contentType, string path)
        {
            return testServer.CreateClient()
                .PostAsync(CreateUrl(path),
                    new StringContent(requestBody,
                    Encoding.UTF8, contentType));
        }

        public static Task<HttpResponseMessage> SendGetRequestAsync(
            this TestServer testServer, string query, string path = null)
        {
            string normalizedQuery = query
                .Replace("\r", string.Empty)
                .Replace("\n", string.Empty);

            return testServer.CreateClient()
                .GetAsync($"{CreateUrl(path)}?query={normalizedQuery}");
        }

        public static string CreateUrl(string path)
        {
            string url = "http://localhost:5000";
            if (path != null)
            {
                url += "/" + path.TrimStart('/');
            }
            return url;
        }
    }

    public class ClientQueryRequest
    {
        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("operationName")]
        public string OperationName { get; set; }

        [JsonProperty("query")]
        public string Query { get; set; }

        [JsonProperty("variables")]
        public Dictionary<string, object> Variables { get; set; }
    }

    public class ClientQueryResult
    {
        public Dictionary<string, object> Data { get; set; }
        public List<Dictionary<string, object>> Errors { get; set; }
        public Dictionary<string, object> Extensions { get; set; }
    }
}
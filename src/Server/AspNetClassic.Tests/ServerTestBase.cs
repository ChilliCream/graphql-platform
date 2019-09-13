using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Owin;
using Microsoft.Owin.Testing;
using Newtonsoft.Json;
using Xunit;


namespace HotChocolate.AspNetClassic
{
    public class ServerTestBase
        : IClassFixture<TestServerFactory>
    {
        public ServerTestBase(TestServerFactory serverFactory)
        {
            ServerFactory = serverFactory;
        }

        protected TestServerFactory ServerFactory { get; set; }

        protected TestServer CreateStarWarsServer(string path = null)
        {
            return ServerFactory.Create(
                services => services.AddStarWars(),
                (app, sp) => app.UseGraphQL(sp, new PathString(path)));
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
}

using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using HotChocolate.AspNetCore.Tests.Utilities;
using Microsoft.AspNetCore.TestHost;
using Newtonsoft.Json;
using Xunit;

namespace HotChocolate.AspNetCore.Subscriptions
{
    public class SubscriptionTestBase
        : IClassFixture<TestServerFactory>
    {
        public SubscriptionTestBase(TestServerFactory serverFactory)
        {
            ServerFactory = serverFactory;
        }

        protected TestServerFactory ServerFactory { get; set; }

        protected TestServer CreateStarWarsServer(string path = null)
        {
            return ServerFactory.Create(
                services => services.AddStarWars(),
                app => app.UseGraphQL(path));
        }

        protected async Task<ClientQueryResult> DeserializeAsync(
            HttpResponseMessage message)
        {
            Assert.Equal(HttpStatusCode.OK, message.StatusCode);
            string json = await message.Content.ReadAsStringAsync();
            return JsonConvert.DeserializeObject<ClientQueryResult>(json);
        }
    }
}

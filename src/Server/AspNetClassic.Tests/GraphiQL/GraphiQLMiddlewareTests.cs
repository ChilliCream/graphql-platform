using System.Net.Http;
using System.Threading.Tasks;
using Snapshooter.Xunit;
using Xunit;
using HotChocolate.AspNetClassic.GraphiQL;
using Microsoft.Owin.Testing;
using Microsoft.Owin;

namespace HotChocolate.AspNetClassic
{
    public class GraphiQLMiddlewareTests
        : ServerTestBase
    {
        public GraphiQLMiddlewareTests(TestServerFactory serverFactory)
            : base(serverFactory)
        {
        }

        [Fact]
        public async Task Default_Values()
        {
            // arrange
            var options = new GraphiQLOptions();

            TestServer server = CreateServer(options);
            string settingsUri = "/graphiql/settings.js";

            // act
            string settings_js = await GetSettingsAsync(server, settingsUri);

            // act
            settings_js.MatchSnapshot();
        }

        [Fact]
        public async Task Disable_Subscriptions()
        {
            // arrange
            var options = new GraphiQLOptions();
            options.EnableSubscription = false;

            TestServer server = CreateServer(options);
            string settingsUri = "/graphiql/settings.js";

            // act
            string settings_js = await GetSettingsAsync(server, settingsUri);

            // act
            settings_js.MatchSnapshot();
        }

        [Fact]
        public async Task SetPath()
        {
            // arrange
            var options = new GraphiQLOptions();
            options.Path = new PathString("/foo");

            TestServer server = CreateServer(options);
            string settingsUri = "/foo/settings.js";

            // act
            string settings_js = await GetSettingsAsync(server, settingsUri);

            // act
            settings_js.MatchSnapshot();
        }

        [Fact]
        public async Task SetPath_Then_SetQueryPath()
        {
            // arrange
            var options = new GraphiQLOptions();
            options.Path = new PathString("/foo");
            options.QueryPath = new PathString("/bar");

            TestServer server = CreateServer(options);
            string settingsUri = "/foo/settings.js";

            // act
            string settings_js = await GetSettingsAsync(server, settingsUri);

            // act
            settings_js.MatchSnapshot();
        }

        [Fact]
        public async Task SetQueryPath()
        {
            // arrange
            var options = new GraphiQLOptions();
            options.QueryPath = new PathString("/foo");

            TestServer server = CreateServer(options);
            string settingsUri = "/foo/graphiql/settings.js";

            // act
            string settings_js = await GetSettingsAsync(server, settingsUri);

            // act
            settings_js.MatchSnapshot();
        }

        [Fact]
        public async Task SetQueryPath_Then_SetPath()
        {
            // arrange
            var options = new GraphiQLOptions();
            options.QueryPath = new PathString("/foo");
            options.Path = new PathString("/bar");

            TestServer server = CreateServer(options);
            string settingsUri = "/bar/settings.js";

            // act
            string settings_js = await GetSettingsAsync(server, settingsUri);

            // act
            settings_js.MatchSnapshot();
        }

        [Fact]
        public async Task SetQueryPath_Then_SetSubscriptionPath()
        {
            // arrange
            var options = new GraphiQLOptions();
            options.QueryPath = new PathString("/foo");
            options.SubscriptionPath = new PathString("/bar");

            TestServer server = CreateServer(options);
            string settingsUri = "/foo/graphiql/settings.js";

            // act
            string settings_js = await GetSettingsAsync(server, settingsUri);

            // act
            settings_js.MatchSnapshot();
        }

        [Fact]
        public async Task SetSubscriptionPath()
        {
            // arrange
            var options = new GraphiQLOptions();
            options.SubscriptionPath = new PathString("/foo");

            TestServer server = CreateServer(options);
            string settingsUri = "/graphiql/settings.js";

            // act
            string settings_js = await GetSettingsAsync(server, settingsUri);

            // act
            settings_js.MatchSnapshot();
        }

        [Fact]
        public async Task SetSubscriptionPath_Then_SetQueryPath()
        {
            // arrange
            var options = new GraphiQLOptions();
            options.SubscriptionPath = new PathString("/foo");
            options.QueryPath = new PathString("/bar");

            TestServer server = CreateServer(options);
            string settingsUri = "/bar/graphiql/settings.js";

            // act
            string settings_js = await GetSettingsAsync(server, settingsUri);

            // act
            settings_js.MatchSnapshot();
        }

        private TestServer CreateServer(GraphiQLOptions options)
        {
            return ServerFactory.Create(
                services => services.AddStarWars(),
                (app, sp) => app.UseGraphQL(sp).UseGraphiQL(options));
        }

        private async Task<string> GetSettingsAsync(
            TestServer server,
            string path)
        {
            HttpResponseMessage response =
                await server.HttpClient.GetAsync(
                    TestServerExtensions.CreateUrl(path));
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadAsStringAsync();
        }
    }
}

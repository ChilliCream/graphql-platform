using System;
using System.Net.Http;
using System.Threading.Tasks;
using HotChocolate.AspNetCore.Tests.Utilities;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.TestHost;
using Moq;
using Snapshooter.Xunit;
using Xunit;

namespace HotChocolate.AspNetCore
{
    public class ApplicationBuilderTests
        : ServerTestBase
    {
        public ApplicationBuilderTests(TestServerFactory serverFactory)
            : base(serverFactory)
        {
        }

        [Fact]
        public void UseGraphQLHttpGet_Builder_BuilderIsNull()
        {
            // arrange
            // act
            Action action =
                () => HttpGetApplicationBuilderExtensions
                    .UseGraphQLHttpGet(null);

            // assert
            Assert.Throws<ArgumentNullException>(action);
        }

        [Fact]
        public async Task UseGraphQLHttpGet_Builder()
        {
            // arrange
            TestServer server = ServerFactory.Create(
                services => services.AddStarWars(),
                app => app.UseGraphQLHttpGet());

            HttpClient client = server.CreateClient();

            // act
            HttpResponseMessage message =
                await server.SendGetRequestAsync("{ hero { name } }");

            // assert
            ClientQueryResult result = await DeserializeAsync(message);
            result.MatchSnapshot();
        }

        [Fact]
        public void UseGraphQLHttpGet_BuilderOptions_BuilderIsNull()
        {
            // arrange
            var options = new HttpGetMiddlewareOptions();

            // act
            Action action =
                () => HttpGetApplicationBuilderExtensions
                    .UseGraphQLHttpGet(null, options);

            // assert
            Assert.Throws<ArgumentNullException>(action);
        }

        [Fact]
        public void UseGraphQLHttpGet_BuilderOptions_OptionsIsNull()
        {
            // arrange
            IApplicationBuilder builder = Mock.Of<IApplicationBuilder>();

            // act
            Action action =
                () => HttpGetApplicationBuilderExtensions
                    .UseGraphQLHttpGet(builder, null);

            // assert
            Assert.Throws<ArgumentNullException>(action);
        }

        [Fact]
        public async Task UseGraphQLHttpGet_BuilderOptions()
        {
            // arrange
            TestServer server = ServerFactory.Create(
                services => services.AddStarWars(),
                app => app.UseGraphQLHttpGet(
                    new HttpGetMiddlewareOptions
                    {
                        Path = "/foo"
                    }));

            HttpClient client = server.CreateClient();

            // act
            HttpResponseMessage message =
                await server.SendGetRequestAsync(
                    "{ hero { name } }", "foo");

            // assert
            ClientQueryResult result = await DeserializeAsync(message);
            result.MatchSnapshot();
        }

        [Fact]
        public void UseGraphQLHttpPost_Builder_BuilderIsNull()
        {
            // arrange
            // act
            Action action =
                () => HttpPostApplicationBuilderExtensions
                    .UseGraphQLHttpPost(null);

            // assert
            Assert.Throws<ArgumentNullException>(action);
        }

        [Fact]
        public async Task UseGraphQLHttpPost_Builder()
        {
            // arrange
            TestServer server = ServerFactory.Create(
                services => services.AddStarWars(),
                app => app.UseGraphQLHttpPost());

            HttpClient client = server.CreateClient();

            var request = new ClientQueryRequest
            {
                Query =
                @"
                    {
                        hero {
                            name
                        }
                    }
                "
            };

            // act
            HttpResponseMessage message =
                await server.SendPostRequestAsync(request);

            // assert
            ClientQueryResult result = await DeserializeAsync(message);
            result.MatchSnapshot();
        }

        [Fact]
        public void UseGraphQLHttpPost_BuilderOptions_BuilderIsNull()
        {
            // arrange
            var options = new HttpPostMiddlewareOptions();

            // act
            Action action =
                () => HttpPostApplicationBuilderExtensions
                    .UseGraphQLHttpPost(null, options);

            // assert
            Assert.Throws<ArgumentNullException>(action);
        }

        [Fact]
        public void UseGraphQLHttpPost_BuilderOptions_OptionsIsNull()
        {
            // arrange
            IApplicationBuilder builder = Mock.Of<IApplicationBuilder>();

            // act
            Action action =
                () => HttpPostApplicationBuilderExtensions
                    .UseGraphQLHttpPost(builder, null);

            // assert
            Assert.Throws<ArgumentNullException>(action);
        }

        [Fact]
        public async Task UseGraphQLHttpPost_BuilderOptions()
        {
            // arrange
            TestServer server = ServerFactory.Create(
                services => services.AddStarWars(),
                app => app.UseGraphQLHttpPost(
                    new HttpPostMiddlewareOptions
                    {
                        Path = "/foo"
                    }));

            HttpClient client = server.CreateClient();

            var request = new ClientQueryRequest
            {
                Query =
                @"
                    {
                        hero {
                            name
                        }
                    }
                "
            };

            // act
            HttpResponseMessage message =
                await server.SendPostRequestAsync(request, "foo");

            // assert
            ClientQueryResult result = await DeserializeAsync(message);
            result.MatchSnapshot();
        }

        [Fact]
        public void UseGraphQLHttpGetSchema_Builder_BuilderIsNull()
        {
            // arrange
            // act
            Action action =
                () => HttpGetSchemaApplicationBuilderExtensions
                    .UseGraphQLHttpGetSchema(null);

            // assert
            Assert.Throws<ArgumentNullException>(action);
        }

        [Fact]
        public async Task UseGraphQLHttpGetSchema_Builder()
        {
            // arrange
            TestServer server = ServerFactory.Create(
                services => services.AddStarWars(),
                app => app.UseGraphQLHttpGetSchema());

            HttpClient client = server.CreateClient();

            string uri = TestServerExtensions.CreateUrl(null);

            // act
            HttpResponseMessage message = await client.GetAsync(uri);

            // assert
            string s = await message.Content.ReadAsStringAsync();
            s.MatchSnapshot();
        }

        [Fact]
        public void UseGraphQLHttpGetSchema_BuilderOptions_BuilderIsNull()
        {
            // arrange
            var options = new HttpGetSchemaMiddlewareOptions();

            // act
            Action action =
                () => HttpGetSchemaApplicationBuilderExtensions
                    .UseGraphQLHttpGetSchema(null, options);

            // assert
            Assert.Throws<ArgumentNullException>(action);
        }

        [Fact]
        public void UseGraphQLHttpGetSchema_BuilderOptions_OptionsIsNull()
        {
            // arrange
            IApplicationBuilder builder = Mock.Of<IApplicationBuilder>();

            // act
            Action action =
                () => HttpGetSchemaApplicationBuilderExtensions
                    .UseGraphQLHttpGetSchema(builder, null);

            // assert
            Assert.Throws<ArgumentNullException>(action);
        }

        [Fact]
        public async Task UseGraphQLHttpGetSchema_BuilderOptions()
        {
            // arrange
            TestServer server = ServerFactory.Create(
                services => services.AddStarWars(),
                app => app.UseGraphQLHttpGetSchema(
                    new HttpGetSchemaMiddlewareOptions
                    {
                        Path = "/foo"
                    }));

            HttpClient client = server.CreateClient();

            string uri = TestServerExtensions.CreateUrl("foo");

            // act
            HttpResponseMessage message = await client.GetAsync(uri);

            // assert
            string s = await message.Content.ReadAsStringAsync();
            s.MatchSnapshot();
        }
    }
}

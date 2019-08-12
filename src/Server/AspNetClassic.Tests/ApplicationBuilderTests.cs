using System;
using System.Net.Http;
using System.Threading.Tasks;
using HotChocolate.Utilities;
using Moq;
using Owin;
using Snapshooter.Xunit;
using Xunit;
using Microsoft.Owin.Testing;
using Microsoft.Owin;

namespace HotChocolate.AspNetClassic
{
    public class ApplicationBuilderTests
        : ServerTestBase
    {
        public ApplicationBuilderTests(TestServerFactory serverFactory)
            : base(serverFactory)
        {
        }

        [Fact]
        public void UseGraphQLHttpGet_BuilderServices_BuilderIsNull()
        {
            // arrange
            IServiceProvider service = new EmptyServiceProvider();

            // act
            Action action =
                () => HttpGetApplicationBuilderExtensions
                    .UseGraphQLHttpGet(null, service);

            // assert
            Assert.Throws<ArgumentNullException>(action);
        }


        [Fact]
        public void UseGraphQLHttpGet_BuilderServices_ServicesIsNull()
        {
            // arrange
            IAppBuilder builder = Mock.Of<IAppBuilder>();

            // act
            Action action =
                () => HttpGetApplicationBuilderExtensions
                    .UseGraphQLHttpGet(builder, null);

            // assert
            Assert.Throws<ArgumentNullException>(action);
        }

        [Fact]
        public async Task UseGraphQLHttpGet_BuilderServices()
        {
            // arrange
            TestServer server = ServerFactory.Create(
                services => services.AddStarWars(),
                (app, sp) => app.UseGraphQLHttpGet(sp));

            // act
            HttpResponseMessage message =
                await server.SendGetRequestAsync("{ hero { name } }");

            // assert
            ClientQueryResult result = await DeserializeAsync(message);
            result.MatchSnapshot();
        }

        [Fact]
        public void UseGraphQLHttpGet_BuilderServicesOptions_BuilderIsNull()
        {
            // arrange
            IServiceProvider services = new EmptyServiceProvider();
            var options = new HttpGetMiddlewareOptions();

            // act
            Action action =
                () => HttpGetApplicationBuilderExtensions
                    .UseGraphQLHttpGet(null, services, options);

            // assert
            Assert.Throws<ArgumentNullException>(action);
        }

        [Fact]
        public void UseGraphQLHttpGet_BuilderServicesOptions_ServicesIsNull()
        {
            // arrange
            IAppBuilder builder = Mock.Of<IAppBuilder>();
            var options = new HttpGetMiddlewareOptions();

            // act
            Action action =
                () => HttpGetApplicationBuilderExtensions
                    .UseGraphQLHttpGet(builder, null, options);

            // assert
            Assert.Throws<ArgumentNullException>(action);
        }

        [Fact]
        public void UseGraphQLHttpGet_BuilderServicesOptions_OptionsIsNull()
        {
            // arrange
            IAppBuilder builder = Mock.Of<IAppBuilder>();
            IServiceProvider services = new EmptyServiceProvider();

            // act
            Action action =
                () => HttpGetApplicationBuilderExtensions
                    .UseGraphQLHttpGet(builder, services, null);

            // assert
            Assert.Throws<ArgumentNullException>(action);
        }



        [Fact]
        public async Task UseGraphQLHttpGet_BuilderServicesOptions()
        {
            // arrange
            TestServer server = ServerFactory.Create(
                services => services.AddStarWars(),
                (app, sp) => app.UseGraphQLHttpGet(
                    sp,
                    new HttpGetMiddlewareOptions
                    {
                        Path = new PathString("/foo")
                    }));

            // act
            HttpResponseMessage message =
                await server.SendGetRequestAsync(
                    "{ hero { name } }", "foo");

            // assert
            ClientQueryResult result = await DeserializeAsync(message);
            result.MatchSnapshot();
        }

        [Fact]
        public void UseGraphQLHttpPost_BuilderServices_BuilderIsNull()
        {
            // arrange
            IServiceProvider services = new EmptyServiceProvider();

            // act
            Action action =
                () => HttpPostApplicationBuilderExtensions
                    .UseGraphQLHttpPost(null, services);

            // assert
            Assert.Throws<ArgumentNullException>(action);
        }

        [Fact]
        public void UseGraphQLHttpPost_BuilderServices_ServicesIsNull()
        {
            // arrange
            IAppBuilder builder = Mock.Of<IAppBuilder>();

            // act
            Action action =
                () => HttpPostApplicationBuilderExtensions
                    .UseGraphQLHttpPost(builder, null);

            // assert
            Assert.Throws<ArgumentNullException>(action);
        }

        [Fact]
        public async Task UseGraphQLHttpPost_BuilderServices()
        {
            // arrange
            TestServer server = ServerFactory.Create(
                services => services.AddStarWars(),
                (app, sp) => app.UseGraphQLHttpPost(sp));

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
        public void UseGraphQLHttpPost_BuilderServicesOptions_BuilderIsNull()
        {
            // arrange
            IServiceProvider services = new EmptyServiceProvider();
            var options = new HttpPostMiddlewareOptions();

            // act
            Action action =
                () => HttpPostApplicationBuilderExtensions
                    .UseGraphQLHttpPost(null, services, options);

            // assert
            Assert.Throws<ArgumentNullException>(action);
        }

        [Fact]
        public void UseGraphQLHttpPost_BuilderServicesOptions_ServicesIsNull()
        {
            // arrange
            IAppBuilder builder = Mock.Of<IAppBuilder>();
            var options = new HttpPostMiddlewareOptions();

            // act
            Action action =
                () => HttpPostApplicationBuilderExtensions
                    .UseGraphQLHttpPost(builder, null, options);

            // assert
            Assert.Throws<ArgumentNullException>(action);
        }

        [Fact]
        public void UseGraphQLHttpPost_BuilderServicesOptions_OptionsIsNull()
        {
            // arrange
            IAppBuilder builder = Mock.Of<IAppBuilder>();
            IServiceProvider services = new EmptyServiceProvider();

            // act
            Action action =
                () => HttpPostApplicationBuilderExtensions
                    .UseGraphQLHttpPost(builder, services, null);

            // assert
            Assert.Throws<ArgumentNullException>(action);
        }

        [Fact]
        public async Task UseGraphQLHttpPost_BuilderServicesOptions()
        {
            // arrange
            TestServer server = ServerFactory.Create(
                services => services.AddStarWars(),
                (app, sp) => app.UseGraphQLHttpPost(
                    sp,
                    new HttpPostMiddlewareOptions
                    {
                        Path = new PathString("/foo")
                    }));

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
        public void UseGraphQLHttpGetSchema_BuilderServices_BuilderIsNull()
        {
            // arrange
            IServiceProvider services = new EmptyServiceProvider();

            // act
            Action action =
                () => HttpGetSchemaApplicationBuilderExtensions
                    .UseGraphQLHttpGetSchema(null, services);

            // assert
            Assert.Throws<ArgumentNullException>(action);
        }

        [Fact]
        public void UseGraphQLHttpGetSchema_BuilderServices_ServicesIsNull()
        {
            // arrange
            IAppBuilder builder = Mock.Of<IAppBuilder>();

            // act
            Action action =
                () => HttpGetSchemaApplicationBuilderExtensions
                    .UseGraphQLHttpGetSchema(builder, null);

            // assert
            Assert.Throws<ArgumentNullException>(action);
        }

        [Fact]
        public async Task UseGraphQLHttpGetSchema_BuilderServices()
        {
            // arrange
            TestServer server = ServerFactory.Create(
                services => services.AddStarWars(),
                (app, sp) => app.UseGraphQLHttpGetSchema(sp));

            HttpClient client = server.HttpClient;

            string uri = TestServerExtensions.CreateUrl(null);

            // act
            HttpResponseMessage message = await client.GetAsync(uri);

            // assert
            string s = await message.Content.ReadAsStringAsync();
            s.MatchSnapshot();
        }

        [Fact]
        public void UseGraphQLHttpGetSchema_BuilderServicesOptions_BuilderIsNull()
        {
            // arrange
            IServiceProvider services = new EmptyServiceProvider();
            var options = new HttpGetSchemaMiddlewareOptions();

            // act
            Action action =
                () => HttpGetSchemaApplicationBuilderExtensions
                    .UseGraphQLHttpGetSchema(null, services, options);

            // assert
            Assert.Throws<ArgumentNullException>(action);
        }

        [Fact]
        public void UseGraphQLHttpGetSchema_BuilderServicesOptions_ServicesIsNull()
        {
            // arrange
            IAppBuilder builder = Mock.Of<IAppBuilder>();
            var options = new HttpGetSchemaMiddlewareOptions();

            // act
            Action action =
                () => HttpGetSchemaApplicationBuilderExtensions
                    .UseGraphQLHttpGetSchema(builder, null, options);

            // assert
            Assert.Throws<ArgumentNullException>(action);
        }

        [Fact]
        public void UseGraphQLHttpGetSchema_BuilderServicesOptions_OptionsIsNull()
        {
            // arrange
            IAppBuilder builder = Mock.Of<IAppBuilder>();
            IServiceProvider services = new EmptyServiceProvider();

            // act
            Action action =
                () => HttpGetSchemaApplicationBuilderExtensions
                    .UseGraphQLHttpGetSchema(builder, services, null);

            // assert
            Assert.Throws<ArgumentNullException>(action);
        }

        [Fact]
        public async Task UseGraphQLHttpGetSchema_BuilderServicesOptions()
        {
            // arrange
            TestServer server = ServerFactory.Create(
                services => services.AddStarWars(),
                (app, sp) => app.UseGraphQLHttpGetSchema(
                    sp,
                    new HttpGetSchemaMiddlewareOptions
                    {
                        Path = new PathString("/foo")
                    }));

            HttpClient client = server.HttpClient;

            string uri = TestServerExtensions.CreateUrl("foo");

            // act
            HttpResponseMessage message = await client.GetAsync(uri);

            // assert
            string s = await message.Content.ReadAsStringAsync();
            s.MatchSnapshot();
        }
    }
}

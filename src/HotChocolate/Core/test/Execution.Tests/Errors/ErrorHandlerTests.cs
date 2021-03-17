using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using HotChocolate.Execution.Configuration;
using HotChocolate.Tests;
using Snapshooter.Xunit;
using Xunit;

namespace HotChocolate.Execution.Errors
{
    public class ErrorHandlerTests
    {
        [Fact]
        public async Task AddFuncErrorFilter()
        {
            Snapshot.FullName();
            await ExpectError(
                "{ foo }",
                b => b
                    .AddDocumentFromString("type Query { foo: String }")
                    .UseField(next => context => throw new Exception("Foo"))
                    .Services
                    .AddErrorFilter(error => error.WithCode("Foo123")));
        }

        [Fact]
        public async Task FilterOnlyNullRefExceptions()
        {
            Snapshot.FullName();
            await ExpectError(
                "{ foo bar }",
                b => b
                    .AddDocumentFromString("type Query { foo: String bar: String }")
                    .AddResolver("Query", "foo", ctx => throw new Exception("Foo"))
                    .AddResolver("Query", "bar", ctx => throw new NullReferenceException("Foo"))
                    .AddErrorFilter(error =>
                    {
                        if (error.Exception is NullReferenceException)
                        {
                            return error.WithCode("NullRef");
                        }
                        return error;
                    }),

                expectedErrorCount: 2);
        }

        [Fact]
        public async Task AddClassErrorFilter()
        {
            Snapshot.FullName();
            await ExpectError(
                "{ foo }",
                b => b
                    .AddDocumentFromString("type Query { foo: String }")
                    .AddResolver("Query", "foo", ctx => throw new Exception("Foo"))
                    .Services
                    .AddErrorFilter<DummyErrorFilter>());
        }

        [Fact]
        public async Task AddClassErrorFilter_SchemaBuiltViaServiceExtensions_ErrorFilterWorks()
        {
            // arrange
            var serviceCollection = new ServiceCollection();
            var schema = await serviceCollection
                .AddGraphQLServer()
                .AddErrorFilter<DummyErrorFilter>()
                .AddQueryType<Query>()
                .BuildRequestExecutorAsync();

            // act
            var resp = await schema.ExecuteAsync("{ foo }");

            // assert
            resp.MatchSnapshot();
        }

        [Fact]
        public async Task AddClassErrorFilterUsingFactory_SchemaBuiltViaServiceExtensions_ErrorFilterWorks()
        {
            // arrange
            var serviceCollection = new ServiceCollection();
            var schema = await serviceCollection
                .AddGraphQLServer()
                .AddErrorFilter(f => new DummyErrorFilter())
                .AddQueryType<Query>()
                .BuildRequestExecutorAsync();

            // act
            var resp = await schema.ExecuteAsync("{ foo }");

            // assert
            resp.MatchSnapshot();
        }

        [Fact]
        public async Task AddClassErrorFilterWithFactory()
        {
            Snapshot.FullName();
            await ExpectError(
                "{ foo }",
                b => b
                    .AddDocumentFromString("type Query { foo: String }")
                    .AddResolver("Query", "foo", ctx => throw new Exception("Foo"))
                    .Services
                    .AddErrorFilter(s => new DummyErrorFilter()));
        }

        private async Task ExpectError(
            string query,
            Action<IRequestExecutorBuilder> configure,
            int expectedErrorCount = 1)
        {
            int errors = 0;

            await TestHelper.ExpectError(
                query,
                b =>
                {
                    configure(b);
                    b.AddErrorFilter(error =>
                    {
                        errors++;
                        return error;
                    });
                });

            Assert.Equal(expectedErrorCount, errors);
        }

        public class DummyErrorFilter
            : IErrorFilter
        {
            public IError OnError(IError error)
            {
                return error.WithCode("Foo123");
            }
        }

        public class Query
        {
            public string GetFoo() => throw new Exception("FooError");
        }
    }
}

using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using HotChocolate.Execution.Configuration;
using HotChocolate.Tests;
using Xunit;
using Snapshooter.Xunit;

namespace HotChocolate.Execution.Errors
{
    public class ErrorHandlerTests
    {
        [Fact]
        public async Task AddFuncErrorFilter()
        {
            Snapshot.FullName();
            await ExpectError(
                b => b
                    .AddDocumentFromString("type Query { foo: String }")
                    .UseField(next => context => throw new Exception("Foo")),
                s => s.AddErrorFilter(error => error.WithCode("Foo123")),
                "{ foo }");
        }

        [Fact]
        public async Task FilterOnlyNullRefExceptions()
        {
            Snapshot.FullName();
            await ExpectError(
                b => b
                    .AddDocumentFromString("type Query { foo: String bar: String }")
                    .AddResolver("Query", "foo", ctx => throw new Exception("Foo"))
                    .AddResolver("Query", "bar", ctx => throw new NullReferenceException("Foo")),
                s => s.AddErrorFilter(error =>
                {
                    if (error.Exception is NullReferenceException)
                    {
                        return error.WithCode("NullRef");
                    }
                    return error;
                }),
                "{ foo bar }",
                expectedErrorCount: 2);
        }

        [Fact]
        public async Task AddClassErrorFilter()
        {
            Snapshot.FullName();
            await ExpectError(
                b => b
                    .AddDocumentFromString("type Query { foo: String }")
                    .AddResolver("Query", "foo", ctx => throw new Exception("Foo")),
                s => s.AddErrorFilter<DummyErrorFilter>(),
                "{ foo }");
        }

        [Fact]
        public async Task AddClassErrorFilterWithFactory()
        {
            Snapshot.FullName();
            await ExpectError(
                b => b
                    .AddDocumentFromString("type Query { foo: String }")
                    .AddResolver("Query", "foo", ctx => throw new Exception("Foo")),
                s => s.AddErrorFilter(s => new DummyErrorFilter()),
                "{ foo }");
        }

        private async Task ExpectError(
            Func<IRequestExecutorBuilder, IRequestExecutorBuilder> build,
            Func<IServiceCollection, IServiceCollection> services,
            string query,
            int expectedErrorCount = 1)
        {
            int errors = 0;

            await TestHelper.ExpectError(
                new TestConfiguration
                {
                    CreateExecutor = s => build(services(new ServiceCollection())
                        .AddGraphQL())
                        .AddErrorFilter(error =>
                        {
                            errors++;
                            return error;
                        })
                        .Services
                        .BuildServiceProvider()
                        .GetRequiredService<IRequestExecutorResolver>()
                        .GetRequestExecutorAsync()
                        .Result,
                },
                query);

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
    }
}

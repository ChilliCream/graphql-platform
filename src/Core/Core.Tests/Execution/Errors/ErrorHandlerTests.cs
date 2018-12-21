using System;
using System.Threading.Tasks;
using ChilliCream.Testing;
using HotChocolate;
using HotChocolate.Execution;
using HotChocolate.Execution.Configuration;
using Xunit;

namespace Core.Tests.Execution.Errors
{
    public class ErrorHandlerTests
    {
        [Fact]
        public async Task AddFuncErrorFilter()
        {
            // arrange
            ISchema schema = Schema.Create("type Query { foo: String }",
                c => c.BindResolver(
                    ctx =>
                    {
                        throw new Exception("Foo");
                    }).To("Query", "foo"));

            var options = new QueryExecutionOptions
            {
                IncludeExceptionDetails = true
            };

            IQueryExecuter executer = schema.MakeExecutable(builder =>
                builder.UseDefaultPipeline(options)
                    .AddErrorFilter((error, exception) =>
                        error.WithCode("Foo123")));

            // act
            IExecutionResult result = await executer.ExecuteAsync("{ foo }");

            // assert
            result.Snapshot();
        }

        [Fact]
        public async Task FilterOnlyNullRefExceptions()
        {
            // arrange
            ISchema schema = Schema.Create(
                "type Query { foo: String bar: String }",
                c =>
                {
                    // will be handled by the default filter logic
                    c.BindResolver(
                        ctx =>
                        {
                            throw new Exception("Foo");
                        }).To("Query", "foo");

                    // will be handled by the custom filter logic
                    c.BindResolver(
                        ctx =>
                        {
                            throw new NullReferenceException("Foo");
                        }).To("Query", "bar");
                });

            var options = new QueryExecutionOptions
            {
                IncludeExceptionDetails = false,
                ExecutionTimeout = TimeSpan.FromMinutes(10)
            };

            IQueryExecuter executer = schema.MakeExecutable(builder =>
                builder.UseDefaultPipeline(options)
                    .AddErrorFilter((error, exception) =>
                    {
                        if (exception is NullReferenceException)
                        {
                            return error.WithCode("NullRef");
                        }
                        return error;
                    }));

            // act
            IExecutionResult result =
                await executer.ExecuteAsync("{ foo bar }");

            // assert
            result.Snapshot();
        }

        [Fact]
        public async Task AddClassErrorFilter()
        {
            // arrange
            ISchema schema = Schema.Create("type Query { foo: String }",
                c => c.BindResolver(
                    ctx =>
                    {
                        throw new Exception("Foo");
                    }).To("Query", "foo"));

            var options = new QueryExecutionOptions
            {
                IncludeExceptionDetails = true
            };

            IQueryExecuter executer = schema.MakeExecutable(builder =>
                builder.UseDefaultPipeline(options)
                    .AddErrorFilter<DummyErrorFilter>());

            // act
            IExecutionResult result = await executer.ExecuteAsync("{ foo }");

            // assert
            result.Snapshot();
        }

        [Fact]
        public async Task AddClassErrorFilterWithFactory()
        {
            // arrange
            ISchema schema = Schema.Create("type Query { foo: String }",
                c => c.BindResolver(
                    ctx =>
                    {
                        throw new Exception("Foo");
                    }).To("Query", "foo"));

            var options = new QueryExecutionOptions
            {
                IncludeExceptionDetails = true
            };

            IQueryExecuter executer = schema.MakeExecutable(builder =>
                builder.UseDefaultPipeline(options)
                    .AddErrorFilter(s => new DummyErrorFilter()));

            // act
            IExecutionResult result = await executer.ExecuteAsync("{ foo }");

            // assert
            result.Snapshot();
        }

        public class DummyErrorFilter
            : IErrorFilter
        {
            public IError OnError(IError error, Exception exception)
            {
                return error.WithCode("Foo123");
            }
        }
    }
}

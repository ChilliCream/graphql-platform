using System;
using System.Threading.Tasks;
using HotChocolate.Execution.Configuration;
using Snapshooter.Xunit;
using Xunit;

namespace HotChocolate.Execution.Errors
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
                IncludeExceptionDetails = false
            };

            IQueryExecutor executor = schema.MakeExecutable(builder =>
                builder.UseDefaultPipeline(options)
                    .AddErrorFilter(error =>
                        error.WithCode("Foo123")));

            // act
            IExecutionResult result = await executor.ExecuteAsync("{ foo }");

            // assert
            result.MatchSnapshot(o =>
                o.IgnoreField("Errors[0].Exception"));
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

            IQueryExecutor executor = schema.MakeExecutable(builder =>
                builder.UseDefaultPipeline(options)
                    .AddErrorFilter(error =>
                    {
                        if (error.Exception is NullReferenceException)
                        {
                            return error.WithCode("NullRef");
                        }
                        return error;
                    }));

            // act
            IExecutionResult result =
                await executor.ExecuteAsync("{ foo bar }");

            // assert
            result.MatchSnapshot(o =>
                o.IgnoreField("Errors[0].Exception")
                .IgnoreField("Errors[1].Exception"));
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
                IncludeExceptionDetails = false
            };

            IQueryExecutor executor = schema.MakeExecutable(builder =>
                builder.UseDefaultPipeline(options)
                    .AddErrorFilter<DummyErrorFilter>());

            // act
            IExecutionResult result = await executor.ExecuteAsync("{ foo }");

            // assert
            result.MatchSnapshot(o =>
                o.IgnoreField("Errors[0].Exception"));
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
                IncludeExceptionDetails = false
            };

            IQueryExecutor executor = schema.MakeExecutable(builder =>
                builder.UseDefaultPipeline(options)
                    .AddErrorFilter(s => new DummyErrorFilter()));

            // act
            IExecutionResult result = await executor.ExecuteAsync("{ foo }");

            // assert
            result.MatchSnapshot(o =>
                o.IgnoreField("Errors[0].Exception"));
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

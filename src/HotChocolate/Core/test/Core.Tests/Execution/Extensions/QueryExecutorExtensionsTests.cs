using System.Threading;
using Xunit;
using HotChocolate.Types;
using Snapshooter.Xunit;
using System;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace HotChocolate.Execution
{
    public class QueryExecutorExtensionsTests
    {
        [Fact]
        public void Execute_ExecutorRequest_Execute()
        {
            // arrange
            IQueryExecutor executor = Create();

            IReadOnlyQueryRequest request =
                QueryRequestBuilder.New()
                    .SetQuery("{ foo }")
                    .Create();

            // act
            IExecutionResult result =
                QueryExecutorExtensions.Execute(executor, request);

            // assert
            result.MatchSnapshot();
        }

        [Fact]
        public void Execute_ExecutorRequest_ExecutorNull()
        {
            // arrange
            IReadOnlyQueryRequest request =
                QueryRequestBuilder.New()
                    .SetQuery("{ foo }")
                    .Create();

            // act
            Action action = () =>
                QueryExecutorExtensions.Execute(null, request);

            // assert
            Assert.Throws<ArgumentNullException>(action);
        }

        [Fact]
        public void Execute_ExecutorRequest_RequestNull()
        {
            // arrange
            IQueryExecutor executor = Create();

            // act
            Action action = () =>
                QueryExecutorExtensions.Execute(
                    executor, (IReadOnlyQueryRequest)null);

            // assert
            Assert.Throws<ArgumentNullException>(action);
        }

        [Fact]
        public async Task ExecuteAsync_ExecutorRequest_Execute()
        {
            // arrange
            IQueryExecutor executor = Create();

            IReadOnlyQueryRequest request =
                QueryRequestBuilder.New()
                    .SetQuery("{ foo }")
                    .Create();

            // act
            IExecutionResult result =
                await QueryExecutorExtensions.ExecuteAsync(executor, request);

            // assert
            result.MatchSnapshot();
        }

        [Fact]
        public async Task ExecuteAsync_ExecutorRequest_ExecutorNull()
        {
            // arrange
            IReadOnlyQueryRequest request =
                QueryRequestBuilder.New()
                    .SetQuery("{ foo }")
                    .Create();

            // act
            Func<Task> action = () =>
                QueryExecutorExtensions.ExecuteAsync(null, request);

            // assert
            await Assert.ThrowsAsync<ArgumentNullException>(action);
        }

        [Fact]
        public async Task ExecuteAsync_ExecutorRequest_RequestNull()
        {
            // arrange
            IQueryExecutor executor = Create();

            // act
            Func<Task> action = () =>
                QueryExecutorExtensions.ExecuteAsync(
                    executor, (IReadOnlyQueryRequest)null);

            // assert
            await Assert.ThrowsAsync<ArgumentNullException>(action);
        }

        [Fact]
        public void Execute_ExecutorQuery_Execute()
        {
            // arrange
            IQueryExecutor executor = Create();

            var request = "{ foo }";

            // act
            IExecutionResult result =
                QueryExecutorExtensions.Execute(executor, request);

            // assert
            result.MatchSnapshot();
        }

        [Fact]
        public void Execute_ExecutorQuery_ExecutorNull()
        {
            // arrange
            var request = "{ foo }";

            // act
            Action action = () =>
                QueryExecutorExtensions.Execute(null, request);

            // assert
            Assert.Throws<ArgumentNullException>(action);
        }

        [Fact]
        public void Execute_ExecutorQuery_RequestNull()
        {
            // arrange
            IQueryExecutor executor = Create();

            // act
            Action action = () =>
                QueryExecutorExtensions.Execute(
                    executor, (string)null);

            // assert
            Assert.Throws<ArgumentException>(action);
        }

        [Fact]
        public async Task ExecuteAsync_ExecutorQuery_Execute()
        {
            // arrange
            IQueryExecutor executor = Create();

            var request = "{ foo }";

            // act
            IExecutionResult result =
                await QueryExecutorExtensions.ExecuteAsync(executor, request);

            // assert
            result.MatchSnapshot();
        }

        [Fact]
        public async Task ExecuteAsync_ExecutorQuery_ExecutorNull()
        {
            // arrange
            var request = "{ foo }";

            // act
            Func<Task> action = () =>
                QueryExecutorExtensions.ExecuteAsync(null, request);

            // assert
            await Assert.ThrowsAsync<ArgumentNullException>(action);
        }

        [Fact]
        public async Task ExecuteAsync_ExecutorQuery_RequestNull()
        {
            // arrange
            IQueryExecutor executor = Create();

            // act
            Func<Task> action = () =>
                QueryExecutorExtensions.ExecuteAsync(
                    executor, (string)null);

            // assert
            await Assert.ThrowsAsync<ArgumentException>(action);
        }

        [Fact]
        public async Task ExecuteAsync_ExecutorQueryCancellationToken_Execute()
        {
            // arrange
            IQueryExecutor executor = Create();

            var request = "{ foo }";

            // act
            IExecutionResult result =
                await QueryExecutorExtensions.ExecuteAsync(
                    executor, request, CancellationToken.None);

            // assert
            result.MatchSnapshot();
        }

        [Fact]
        public async Task ExecuteAsync_ExecutorQueryCancellation_ExecutorNull()
        {
            // arrange
            var request = "{ foo }";

            // act
            Func<Task> action = () =>
                QueryExecutorExtensions.ExecuteAsync(
                    null, request, CancellationToken.None);

            // assert
            await Assert.ThrowsAsync<ArgumentNullException>(action);
        }

        [Fact]
        public async Task ExecuteAsync_ExecutorQueryCancellation_RequestNull()
        {
            // arrange
            IQueryExecutor executor = Create();

            // act
            Func<Task> action = () =>
                QueryExecutorExtensions.ExecuteAsync(
                    executor, (string)null, CancellationToken.None);

            // assert
            await Assert.ThrowsAsync<ArgumentException>(action);
        }

        [Fact]
        public void Execute_ExecutorQueryVariables_Execute()
        {
            // arrange
            IQueryExecutor executor = Create();

            var request = "query a($a : String) { foo(a: $a) }";

            var variables = new Dictionary<string, object>
            {
                { "a", "_baz" }
            };

            // act
            IExecutionResult result =
                QueryExecutorExtensions.Execute(
                    executor, request, variables);

            // assert
            result.MatchSnapshot();
        }

        [Fact]
        public void Execute_ExecutorQueryVariables_ExecutorNull()
        {
            // arrange
            var request = "query a($a : String) { foo(a: $a) }";

            var variables = new Dictionary<string, object>
            {
                { "a", "_baz" }
            };

            // act
            Action action = () =>
                QueryExecutorExtensions.Execute(null, request, variables);

            // assert
            Assert.Throws<ArgumentNullException>(action);
        }

        [Fact]
        public void Execute_ExecutorQueryVariables_RequestNull()
        {
            // arrange
            IQueryExecutor executor = Create();

            var variables = new Dictionary<string, object>
            {
                { "a", "_baz" }
            };

            // act
            Action action = () =>
                QueryExecutorExtensions.Execute(
                    executor, (string)null, variables);

            // assert
            Assert.Throws<ArgumentException>(action);
        }

        [Fact]
        public void Execute_ExecutorQueryVariables_VariablesNull()
        {
            // arrange
            IQueryExecutor executor = Create();

            var request = "query a($a : String) { foo(a: $a) }";

            // act
            Action action = () =>
                QueryExecutorExtensions.Execute(
                    executor, request, null);

            // assert
            Assert.Throws<ArgumentNullException>(action);
        }

        [Fact]
        public async Task ExecuteAsync_ExecutorQueryVariables_Execute()
        {
            // arrange
            IQueryExecutor executor = Create();

            var request = "query a($a : String) { foo(a: $a) }";

            var variables = new Dictionary<string, object>
            {
                { "a", "_baz" }
            };

            // act
            IExecutionResult result =
                await QueryExecutorExtensions.ExecuteAsync(
                    executor, request, variables);

            // assert
            result.MatchSnapshot();
        }

        [Fact]
        public async Task ExecuteAsync_ExecutorQueryVariables_ExecutorNull()
        {
            // arrange
            var request = "query a($a : String) { foo(a: $a) }";

            var variables = new Dictionary<string, object>
            {
                { "a", "_baz" }
            };

            // act
            Func<Task> action = () =>
                QueryExecutorExtensions.ExecuteAsync(
                    null, request, variables);

            // assert
            await Assert.ThrowsAsync<ArgumentNullException>(action);
        }

        [Fact]
        public async Task ExecuteAsync_ExecutorQueryVariables_RequestNull()
        {
            // arrange
            IQueryExecutor executor = Create();

            var variables = new Dictionary<string, object>
            {
                { "a", "_baz" }
            };

            // act
            Func<Task> action = () =>
                QueryExecutorExtensions.ExecuteAsync(
                    executor, (string)null, variables);

            // assert
            await Assert.ThrowsAsync<ArgumentException>(action);
        }

        [Fact]
        public async Task ExecuteAsync_ExecutorQueryVariables_VariablesNull()
        {
            // arrange
            IQueryExecutor executor = Create();

            var request = "query a($a : String) { foo(a: $a) }";

            // act
            Func<Task> action = () =>
                QueryExecutorExtensions.ExecuteAsync(
                    executor, request, null);

            // assert
            await Assert.ThrowsAsync<ArgumentNullException>(action);
        }

        [Fact]
        public async Task ExecuteAsync_ExecutorQueryVariablesCT_Execute()
        {
            // arrange
            IQueryExecutor executor = Create();

            var request = "query a($a : String) { foo(a: $a) }";

            var variables = new Dictionary<string, object>
            {
                { "a", "_baz" }
            };

            // act
            IExecutionResult result =
                await QueryExecutorExtensions.ExecuteAsync(
                    executor, request, variables, CancellationToken.None);

            // assert
            result.MatchSnapshot();
        }

        [Fact]
        public async Task ExecuteAsync_ExecutorQueryVariablesCT_ExecutorNull()
        {
            // arrange
            var request = "query a($a : String) { foo(a: $a) }";

            var variables = new Dictionary<string, object>
            {
                { "a", "_baz" }
            };

            // act
            Func<Task> action = () =>
                QueryExecutorExtensions.ExecuteAsync(
                    null, request, variables, CancellationToken.None);

            // assert
            await Assert.ThrowsAsync<ArgumentNullException>(action);
        }

        [Fact]
        public async Task ExecuteAsync_ExecutorQueryVariablesCT_RequestNull()
        {
            // arrange
            IQueryExecutor executor = Create();

            var variables = new Dictionary<string, object>
            {
                { "a", "_baz" }
            };

            // act
            Func<Task> action = () =>
                QueryExecutorExtensions.ExecuteAsync(
                    executor, (string)null, variables, CancellationToken.None);

            // assert
            await Assert.ThrowsAsync<ArgumentException>(action);
        }

        [Fact]
        public async Task ExecuteAsync_ExecutorQueryVariablesCT_VariablesNull()
        {
            // arrange
            IQueryExecutor executor = Create();

            var request = "query a($a : String) { foo(a: $a) }";

            // act
            Func<Task> action = () =>
                QueryExecutorExtensions.ExecuteAsync(
                    executor, request, null, CancellationToken.None);

            // assert
            await Assert.ThrowsAsync<ArgumentNullException>(action);
        }

        private IQueryExecutor Create()
        {
            return SchemaBuilder.New()
                .AddQueryType(d => d
                    .Name("Query")
                    .Field("foo")
                    .Argument("a", a => a.Type<StringType>())
                    .Resolver(c =>
                    {
                        string a = c.Argument<string>("a");
                        if (a != null)
                        {
                            return "bar" + a;
                        }
                        return "bar";
                    }))
                .Create()
                .MakeExecutable();
        }
    }
}

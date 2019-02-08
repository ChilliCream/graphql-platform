using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ChilliCream.Testing;
using HotChocolate.Execution;
using HotChocolate.Resolvers;
using HotChocolate.Utilities;
using Moq;
using Xunit;

namespace HotChocolate.Stitching
{
    public class RemoteQueryClientTests
    {
        [Fact]
        public async Task DispatchSingleQuery()
        {
            // arrange
            string query = null;

            var executor = new Mock<IQueryExecutor>();
            executor.Setup(t => t.ExecuteAsync(
                It.IsAny<IReadOnlyQueryRequest>(),
                It.IsAny<CancellationToken>()))
                .Returns(new Func<IReadOnlyQueryRequest,
                    CancellationToken, Task<IExecutionResult>>((r, ct) =>
                {
                    query = r.Query;
                    return Task.FromResult<IExecutionResult>(null);
                }));

            var client = new RemoteQueryClient(
                new EmptyServiceProvider(),
                executor.Object);

            // act
            Task result = client.ExecuteAsync(
                new QueryRequest("{ a }"));
            await client.DispatchAsync(CancellationToken.None);
            await result;


            query.Snapshot();
        }

        [Fact]
        public async Task DispatchMultipleQueries()
        {
            // arrange
            string query = null;
            int count = 0;

            var result = new QueryResult();
            result.Data["__0__a"] = "a";
            result.Data["__1__a"] = "b";
            result.Data["__1__b"] = "c";

            var executor = new Mock<IQueryExecutor>();
            executor.Setup(t => t.ExecuteAsync(
                It.IsAny<IReadOnlyQueryRequest>(),
                It.IsAny<CancellationToken>()))
                .Returns(new Func<IReadOnlyQueryRequest,
                    CancellationToken, Task<IExecutionResult>>((r, ct) =>
                {
                    count++;
                    query = r.Query;
                    return Task.FromResult<IExecutionResult>(result);
                }));

            var request_a = new QueryRequest("query a { a }");
            var request_b = new QueryRequest("query b { a b }");

            var client = new RemoteQueryClient(
                new EmptyServiceProvider(),
                executor.Object);

            // act
            Task<IExecutionResult> task_a = client.ExecuteAsync(request_a);
            Task<IExecutionResult> task_b = client.ExecuteAsync(request_b);
            await client.DispatchAsync(CancellationToken.None);

            // assert
            Assert.Equal(1, count);
            query.Snapshot("DispatchMultipleQueries_MergedQuery");

            IExecutionResult result_a = await task_a;
            result_a.Snapshot("DispatchMultipleQueries_Result_A");

            IExecutionResult result_b = await task_b;
            result_b.Snapshot("DispatchMultipleQueries_Result_B");
        }

        [Fact]
        public async Task DispatchMultipleQueriesAndRewriteErrors()
        {
            // arrange
            string query = null;
            int count = 0;

            var result = new QueryResult();
            result.Data["__0__a"] = "a";
            result.Data["__1__a"] = "b";
            result.Data["__1__b"] = "c";
            result.Errors.Add(ErrorBuilder.New()
                .SetMessage("foo")
                .SetPath(Path.New("__1__b"))
                .Build());

            var executor = new Mock<IQueryExecutor>();
            executor.Setup(t => t.ExecuteAsync(
                It.IsAny<IReadOnlyQueryRequest>(),
                It.IsAny<CancellationToken>()))
                .Returns(new Func<IReadOnlyQueryRequest,
                    CancellationToken, Task<IExecutionResult>>((r, ct) =>
                {
                    count++;
                    query = r.Query;
                    return Task.FromResult<IExecutionResult>(result);
                }));

            var request_a = new QueryRequest("query a { a }");
            var request_b = new QueryRequest("query b { a b }");

            var client = new RemoteQueryClient(
                new EmptyServiceProvider(),
                executor.Object);

            // act
            Task<IExecutionResult> task_a = client.ExecuteAsync(request_a);
            Task<IExecutionResult> task_b = client.ExecuteAsync(request_b);
            await client.DispatchAsync(CancellationToken.None);

            // assert
            Assert.Equal(1, count);

            IExecutionResult result_a = await task_a;
            result_a.Snapshot("DispatchMultipleQueriesAndRewriteErrors_A");

            IExecutionResult result_b = await task_b;
            result_b.Snapshot("DispatchMultipleQueriesAndRewriteErrors_B");
        }

        [Fact]
        public async Task DispatchMultipleQueriesWithGlobalError()
        {
            // arrange
            string query = null;
            int count = 0;

            var result = new QueryResult();
            result.Data["__0__a"] = "a";
            result.Data["__1__a"] = "b";
            result.Data["__1__b"] = "c";
            result.Errors.Add(ErrorBuilder.New()
                .SetMessage("foo")
                .Build());

            var executor = new Mock<IQueryExecutor>();
            executor.Setup(t => t.ExecuteAsync(
                It.IsAny<IReadOnlyQueryRequest>(),
                It.IsAny<CancellationToken>()))
                .Returns(new Func<IReadOnlyQueryRequest,
                    CancellationToken, Task<IExecutionResult>>((r, ct) =>
                {
                    count++;
                    query = r.Query;
                    return Task.FromResult<IExecutionResult>(result);
                }));

            var request_a = new QueryRequest("query a { a }");
            var request_b = new QueryRequest("query b { a b }");

            var client = new RemoteQueryClient(
                new EmptyServiceProvider(),
                executor.Object);

            // act
            Task<IExecutionResult> task_a = client.ExecuteAsync(request_a);
            Task<IExecutionResult> task_b = client.ExecuteAsync(request_b);
            await client.DispatchAsync(CancellationToken.None);

            // assert
            Assert.Equal(1, count);

            IExecutionResult result_a = await task_a;
            result_a.Snapshot("DispatchMultipleQueriesWithGlobalError_A");

            IExecutionResult result_b = await task_b;
            result_b.Snapshot("DispatchMultipleQueriesWithGlobalError_B");
        }

        [Fact]
        public async Task DispatchMultipleQueriesWithGlobalException()
        {
            // arrange
            var executor = new Mock<IQueryExecutor>();
            executor.Setup(t => t.ExecuteAsync(
                It.IsAny<IReadOnlyQueryRequest>(),
                It.IsAny<CancellationToken>()))
                .Returns(new Func<IReadOnlyQueryRequest,
                    CancellationToken, Task<IExecutionResult>>((r, ct) =>
                {
                    return Task.FromException<IExecutionResult>(
                        new Exception("foo"));
                }));

            var request_a = new QueryRequest("query a { a }");
            var request_b = new QueryRequest("query b { a b }");

            var client = new RemoteQueryClient(
                new EmptyServiceProvider(),
                executor.Object);

            // act
            Task<IExecutionResult> task_a = client.ExecuteAsync(request_a);
            Task<IExecutionResult> task_b = client.ExecuteAsync(request_b);
            await client.DispatchAsync(CancellationToken.None);

            // assert
            Assert.Equal("foo",
                (await Assert.ThrowsAsync<Exception>(() => task_a)).Message);
            Assert.Equal("foo",
                (await Assert.ThrowsAsync<Exception>(() => task_b)).Message);
        }

        [Fact]
        public async Task DispatchMultipleQueriesWithVariables()
        {
            // arrange
            IReadOnlyQueryRequest mergedRequest = null;
            int count = 0;

            var result = new QueryResult();
            result.Data["__0__a"] = "a";
            result.Data["__1__a"] = "b";
            result.Data["__1__b"] = "c";

            var executor = new Mock<IQueryExecutor>();
            executor.Setup(t => t.ExecuteAsync(
                It.IsAny<IReadOnlyQueryRequest>(),
                It.IsAny<CancellationToken>()))
                .Returns(new Func<IReadOnlyQueryRequest,
                    CancellationToken, Task<IExecutionResult>>((r, ct) =>
                {
                    count++;
                    mergedRequest = r;
                    return Task.FromResult<IExecutionResult>(result);
                }));

            var request_a = new QueryRequest(
                "query a($a: String) { a(b: $a) }")
            {
                VariableValues = new Dictionary<string, object>
                {
                    { "a", "foo" }
                }
            };

            var request_b = new QueryRequest(
                "query b { a b }");

            var client = new RemoteQueryClient(
                new EmptyServiceProvider(),
                executor.Object);

            // act
            Task<IExecutionResult> task_a = client.ExecuteAsync(request_a);
            Task<IExecutionResult> task_b = client.ExecuteAsync(request_b);
            await client.DispatchAsync(CancellationToken.None);

            // assert
            await task_a;
            await task_b;

            Assert.Equal(1, count);
            mergedRequest.Snapshot();
        }
    }
}

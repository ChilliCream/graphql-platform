using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using HotChocolate.Execution;
using HotChocolate.Stitching.Client;
using HotChocolate.Utilities;
using Moq;
using Snapshooter.Xunit;
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
                    query = r.Query.ToString();
                    return Task.FromResult<IExecutionResult>(null);
                }));

            var client = new RemoteQueryClient(
                new EmptyServiceProvider(),
                executor.Object);

            // act
            Task result = client.ExecuteAsync(
                QueryRequestBuilder.New()
                    .SetQuery("{ a }")
                    .Create());
            await client.DispatchAsync(CancellationToken.None);
            await result;


            query.MatchSnapshot();
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
                    query = r.Query.ToString();
                    return Task.FromResult<IExecutionResult>(result);
                }));

            var request_a = QueryRequestBuilder.Create("query a { a }");
            var request_b = QueryRequestBuilder.Create("query b { a b }");

            var client = new RemoteQueryClient(
                new EmptyServiceProvider(),
                executor.Object);

            // act
            Task<IExecutionResult> task_a = client.ExecuteAsync(request_a);
            Task<IExecutionResult> task_b = client.ExecuteAsync(request_b);
            await client.DispatchAsync(CancellationToken.None);

            // assert
            Assert.Equal(1, count);
            query.MatchSnapshot($"{nameof(DispatchMultipleQueries)}_MergedQuery");

            IExecutionResult result_a = await task_a;
            result_a.MatchSnapshot($"{nameof(DispatchMultipleQueries)}_Result_A");

            IExecutionResult result_b = await task_b;
            result_b.MatchSnapshot($"{nameof(DispatchMultipleQueries)}_Result_B");
        }

        [Fact]
        public async Task DispatchMultipleQueriesDistinctOperationName()
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
                        query = r.Query.ToString();
                        return Task.FromResult<IExecutionResult>(result);
                    }));

            var request_a = QueryRequestBuilder.New().SetQuery("query a { a }").SetOperation("a").Create();
            var request_b = QueryRequestBuilder.New().SetQuery("query b { a b }").SetOperation("a").Create();

            var client = new RemoteQueryClient(
                new EmptyServiceProvider(),
                executor.Object);

            // act
            Task<IExecutionResult> task_a = client.ExecuteAsync(request_a);
            Task<IExecutionResult> task_b = client.ExecuteAsync(request_b);
            await client.DispatchAsync(CancellationToken.None);

            // assert
            Assert.Equal(1, count);
            query.MatchSnapshot($"{nameof(DispatchMultipleQueriesDistinctOperationName)}_MergedQuery");

            IExecutionResult result_a = await task_a;
            result_a.MatchSnapshot($"{nameof(DispatchMultipleQueriesDistinctOperationName)}_Result_A");

            IExecutionResult result_b = await task_b;
            result_b.MatchSnapshot($"{nameof(DispatchMultipleQueriesDistinctOperationName)}_Result_B");
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
                    query = r.Query.ToString();
                    return Task.FromResult<IExecutionResult>(result);
                }));

            var request_a = QueryRequestBuilder.Create("query a { a }");
            var request_b = QueryRequestBuilder.Create("query b { a b }");

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
            result_a.MatchSnapshot("DispatchMultipleQueriesAndRewriteErrors_A");

            IExecutionResult result_b = await task_b;
            result_b.MatchSnapshot("DispatchMultipleQueriesAndRewriteErrors_B");
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
                    query = r.Query.ToString();
                    return Task.FromResult<IExecutionResult>(result);
                }));

            var request_a = QueryRequestBuilder.Create("query a { a }");
            var request_b = QueryRequestBuilder.Create("query b { a b }");

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
            result_a.MatchSnapshot("DispatchMultipleQueriesWithGlobalError_A");

            IExecutionResult result_b = await task_b;
            result_b.MatchSnapshot("DispatchMultipleQueriesWithGlobalError_B");
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

            var request_a = QueryRequestBuilder.Create("query a { a }");
            var request_b = QueryRequestBuilder.Create("query b { a b }");

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

            var request_a = QueryRequestBuilder.New()
                .SetQuery("query a($a: String) { a(b: $a) }")
                .SetVariableValue("a", "foo")
                .Create();

            var request_b = QueryRequestBuilder.Create(
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
            mergedRequest.MatchSnapshot();
        }
    }
}

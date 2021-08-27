using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using HotChocolate.Language;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Snapshooter.Xunit;
using Xunit;

#nullable enable

namespace HotChocolate.Execution.Pipeline
{
    public class PersistedQueryPipelineTests
    {
        [Fact]
        public async Task PersistedQuery_CorrectId_ExecuteQuery()
        {
            // arrange
            const string queryId = nameof(queryId);
            var readStore = new Mock<IReadStoredQueries>();
            readStore
                .Setup(x => x.TryReadQueryAsync(queryId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new QueryDocument(Utf8GraphQLParser.Parse("{ foo }")));
            IRequestExecutor schema = await new ServiceCollection()
                .AddGraphQL()
                .ConfigureSchemaServices(x => x.AddSingleton(readStore.Object))
                .AddQueryType<Query>()
                .UsePersistedQueryPipeline()
                .BuildRequestExecutorAsync();

            // act
            IReadOnlyQueryRequest query = QueryRequestBuilder
                .New()
                .SetQueryId(queryId)
                .Create();
            IExecutionResult result = await schema.ExecuteAsync(query, CancellationToken.None);

            // assert
            result.ToJson().MatchSnapshot();
        }

        [Fact]
        public async Task PersistedQuery_Document_ExecuteQuery()
        {
            // arrange
            var readStore = new Mock<IReadStoredQueries>();
            IRequestExecutor schema = await new ServiceCollection()
                .AddGraphQL()
                .ConfigureSchemaServices(x => x.AddSingleton(readStore.Object))
                .AddQueryType<Query>()
                .UsePersistedQueryPipeline()
                .BuildRequestExecutorAsync();

            // act
            IReadOnlyQueryRequest query = QueryRequestBuilder
                .New()
                .SetQuery("{ foo }")
                .Create();

            IExecutionResult result = await schema.ExecuteAsync(query, CancellationToken.None);

            // assert
            result.ToJson().MatchSnapshot();
        }

        [Fact]
        public async Task PersistedQuery_AllowOnlyPersistedQueriesDocument_Errors()
        {
            // arrange
            var readStore = new Mock<IReadStoredQueries>();
            IRequestExecutor schema = await new ServiceCollection()
                .AddGraphQL()
                .ConfigureSchemaServices(x => x.AddSingleton(readStore.Object))
                .AddQueryType<Query>()
                .UseOnlyPersistedQueryPipeline()
                .BuildRequestExecutorAsync();

            // act
            IReadOnlyQueryRequest query = QueryRequestBuilder
                .New()
                .SetQuery("{ foo }")
                .Create();

            IExecutionResult result = await schema.ExecuteAsync(query, CancellationToken.None);

            // assert
            result.ToJson().MatchSnapshot();
        }

        [Fact]
        public async Task PersistedQuery_AllowOnlyPersistedQueryIdNotFound_Errors()
        {
            // arrange
            const string queryId = nameof(queryId);
            var readStore = new Mock<IReadStoredQueries>();
            readStore
                .Setup(x => x.TryReadQueryAsync(queryId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(() => null);
            IRequestExecutor schema = await new ServiceCollection()
                .AddGraphQL()
                .ConfigureSchemaServices(x => x.AddSingleton(readStore.Object))
                .AddQueryType<Query>()
                .UseOnlyPersistedQueryPipeline()
                .BuildRequestExecutorAsync();

            // act
            IReadOnlyQueryRequest query = QueryRequestBuilder
                .New()
                .SetQueryId(queryId)
                .Create();
            IExecutionResult result = await schema.ExecuteAsync(query, CancellationToken.None);

            // assert
            result.ToJson().MatchSnapshot();
        }

        [Fact]
        public async Task PersistedQuery_AllowOnlyPersistedQueryIdNotProvided_Errors()
        {
            // arrange
            var readStore = new Mock<IReadStoredQueries>();
            IRequestExecutor schema = await new ServiceCollection()
                .AddGraphQL()
                .ConfigureSchemaServices(x => x.AddSingleton(readStore.Object))
                .AddQueryType<Query>()
                .UseOnlyPersistedQueryPipeline()
                .BuildRequestExecutorAsync();

            // act
            IExecutionResult result =
                await schema.ExecuteAsync(new MockRequest(), CancellationToken.None);

            // assert
            result.ToJson().MatchSnapshot();
        }

        [Fact]
        public async Task PersistedQuery_AllowOnlyPersistedQuery_ExecuteQuery()
        {
            // arrange
            const string queryId = nameof(queryId);
            var readStore = new Mock<IReadStoredQueries>();
            readStore
                .Setup(x => x.TryReadQueryAsync(queryId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new QueryDocument(Utf8GraphQLParser.Parse("{ foo }")));
            IRequestExecutor schema = await new ServiceCollection()
                .AddGraphQL()
                .ConfigureSchemaServices(x => x.AddSingleton(readStore.Object))
                .AddQueryType<Query>()
                .UseOnlyPersistedQueryPipeline()
                .BuildRequestExecutorAsync();

            // act
            IReadOnlyQueryRequest query = QueryRequestBuilder
                .New()
                .SetQueryId(queryId)
                .Create();
            IExecutionResult result = await schema.ExecuteAsync(query, CancellationToken.None);

            // assert
            result.ToJson().MatchSnapshot();
        }

        public class Query
        {
            public string Foo => "foo";
        }

        public class MockRequest : IReadOnlyQueryRequest
        {
            public IQuery? Query { get; set; }
            public string? QueryId { get; set; }
            public string? QueryHash { get; set; }
            public string? OperationName { get; set; }
            public IReadOnlyDictionary<string, object?>? VariableValues { get; set; }
            public object? InitialValue { get; set; }
            public IReadOnlyDictionary<string, object?>? ContextData { get; set; }
            public IReadOnlyDictionary<string, object?>? Extensions { get; set; }
            public IServiceProvider? Services { get; set; }
            public OperationType[]? AllowedOperations { get; set; }
        }
    }
}

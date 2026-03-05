using System.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using HotChocolate.Execution;
using HotChocolate.Language;
using HotChocolate.PersistedOperations;
using static HotChocolate.Diagnostics.ActivityTestHelper;

namespace HotChocolate.Diagnostics;

[Collection("Instrumentation")]
public partial class QueryInstrumentationTests
{
    [Fact]
    public async Task Track_events_of_a_simple_query_default()
    {
        using (CaptureActivities(out var activities))
        {
            // arrange & act
            await new ServiceCollection()
                .AddGraphQL()
                .AddInstrumentation()
                .AddQueryType<SimpleQuery>()
                .ExecuteRequestAsync("{ sayHello }");

            // assert
            activities.MatchSnapshot();
        }
    }

    [Fact(Skip = "This test is flaky with the new DL batching.")]
    public async Task Track_data_loader_events()
    {
        using (CaptureActivities(out var activities))
        {
            // arrange & act
            await new ServiceCollection()
                .AddGraphQL()
                .AddInstrumentation()
                .AddQueryType<SimpleQuery>()
                .ExecuteRequestAsync("{ dataLoader(key: \"abc\") }");

            // assert
            activities.MatchSnapshot();
        }
    }

    [Fact(Skip = "This test is flaky with the new DL batching.")]
    public async Task Track_data_loader_events_with_keys()
    {
        using (CaptureActivities(out var activities))
        {
            // arrange & act
            await new ServiceCollection()
                .AddGraphQL()
                .AddInstrumentation(o => o.IncludeDataLoaderKeys = true)
                .AddQueryType<SimpleQuery>()
                .ExecuteRequestAsync("{ dataLoader(key: \"abc\") }");

            // assert
            activities.MatchSnapshot();
        }
    }

    [Fact]
    public async Task Track_events_of_a_simple_query_default_rename_root()
    {
        using (CaptureActivities(out _))
        {
            // arrange & act
            await new ServiceCollection()
                .AddGraphQL()
                .AddInstrumentation(o => o.Scopes = ActivityScopes.All)
                .AddQueryType<SimpleQuery>()
                .ExecuteRequestAsync("{ sayHello }");

            // assert
            Assert.Equal("CaptureActivities", Activity.Current!.DisplayName);
        }
    }

    [Fact]
    public async Task Parsing_error_when_rename_root_is_activated()
    {
        using (CaptureActivities(out _))
        {
            // arrange & act
            await new ServiceCollection()
                .AddGraphQL()
                .AddInstrumentation(o => o.Scopes = ActivityScopes.All)
                .AddQueryType<SimpleQuery>()
                .ExecuteRequestAsync("{ sayHello");

            // assert
            Assert.Equal("CaptureActivities", Activity.Current!.DisplayName);
        }
    }

    [Fact]
    public async Task Validation_error_when_rename_root_is_activated()
    {
        using (CaptureActivities(out _))
        {
            // arrange & act
            await new ServiceCollection()
                .AddGraphQL()
                .AddInstrumentation(o => o.Scopes = ActivityScopes.All)
                .AddQueryType<SimpleQuery>()
                .ExecuteRequestAsync("{ abc123 }");

            // assert
            Assert.Equal("CaptureActivities", Activity.Current!.DisplayName);
        }
    }

    [Fact]
    public async Task Track_events_of_a_simple_query_detailed()
    {
        using (CaptureActivities(out var activities))
        {
            // arrange & act
            await new ServiceCollection()
                .AddGraphQL()
                .AddInstrumentation(o => o.Scopes = ActivityScopes.All)
                .AddQueryType<SimpleQuery>()
                .ExecuteRequestAsync("{ sayHello }");

            // assert
            activities.MatchSnapshot();
        }
    }

    [Fact]
    public async Task Ensure_operation_name_is_used_as_request_name()
    {
        using (CaptureActivities(out var activities))
        {
            // arrange & act
            await new ServiceCollection()
                .AddGraphQL()
                .AddInstrumentation(o => o.Scopes = ActivityScopes.All)
                .AddQueryType<SimpleQuery>()
                .ExecuteRequestAsync("query SayHelloOperation { sayHello }");

            // assert
            activities.MatchSnapshot();
        }
    }

    [Fact]
    public async Task Allow_document_to_be_captured()
    {
        using (CaptureActivities(out var activities))
        {
            // arrange & act
            await new ServiceCollection()
                .AddGraphQL()
                .AddInstrumentation(o =>
                {
                    o.Scopes = ActivityScopes.All;
                    o.IncludeDocument = true;
                })
                .AddQueryType<SimpleQuery>()
                .ExecuteRequestAsync("query SayHelloOperation { sayHello }");

            // assert
            activities.MatchSnapshot();
        }
    }

    [Fact]
    public async Task Ensure_that_the_validation_activity_has_an_error_status()
    {
        using (CaptureActivities(out var activities))
        {
            // arrange & act
            await new ServiceCollection()
                .AddGraphQL()
                .AddInstrumentation(o =>
                {
                    o.Scopes = ActivityScopes.All;
                    o.IncludeDocument = true;
                })
                .AddQueryType<SimpleQuery>()
                .ExecuteRequestAsync("query SayHelloOperation { sayHello_ }");

            // assert
            activities.MatchSnapshot();
        }
    }

    [Fact]
    public async Task Cause_a_resolver_error_that_deletes_the_whole_result()
    {
        using (CaptureActivities(out var activities))
        {
            // arrange & act
            await new ServiceCollection()
                .AddGraphQL()
                .AddInstrumentation(o =>
                {
                    o.Scopes = ActivityScopes.All;
                    o.IncludeDocument = true;
                })
                .AddQueryType<SimpleQuery>()
                .ExecuteRequestAsync("query SayHelloOperation { causeFatalError }");

            // assert
            activities.MatchSnapshot();
        }
    }

    [Fact]
    public async Task Cause_a_resolver_error_that_deletes_the_whole_result_deep()
    {
        using (CaptureActivities(out var activities))
        {
            // arrange & act
            await new ServiceCollection()
                .AddGraphQL()
                .AddInstrumentation(o =>
                {
                    o.Scopes = ActivityScopes.All;
                    o.IncludeDocument = true;
                })
                .AddQueryType<SimpleQuery>()
                .ExecuteRequestAsync(
                    """
                    query SayHelloOperation {
                        deep {
                            deeper {
                                deeps {
                                    deeper {
                                        causeFatalError
                                    }
                                }
                            }
                        }
                    }
                    """);

            // assert
            activities.MatchSnapshot();
        }
    }

    [Fact]
    public async Task PersistedOperation_LoadsFromStorage_DefaultScopes()
    {
        using (CaptureActivities(out var activities))
        {
            // arrange
            var storage = new InMemoryOperationDocumentStorage();
            storage.Add("sayHelloOp", "{ sayHello }");

            var services = new ServiceCollection()
                .AddGraphQL()
                .AddInstrumentation()
                .AddQueryType<SimpleQuery>()
                .UsePersistedOperationPipeline()
                .ConfigureSchemaServices(
                    s => s.AddSingleton<IOperationDocumentStorage>(storage))
                .Services
                .BuildServiceProvider();

            var executor = await services.GetRequestExecutorAsync();

            // act
            await executor.ExecuteAsync(OperationRequest.FromId("sayHelloOp"));

            // assert
            activities.MatchSnapshot();
        }
    }

    [Fact]
    public async Task ParsingError_InvalidGraphQLDocument_ReportsErrorStatus()
    {
        using (CaptureActivities(out var activities))
        {
            // arrange & act
            await new ServiceCollection()
                .AddGraphQL()
                .AddInstrumentation(o => o.Scopes = ActivityScopes.All)
                .AddQueryType<SimpleQuery>()
                .ExecuteRequestAsync("{ sayHello");

            // assert
            activities.MatchSnapshot();
        }
    }

    [Fact]
    public async Task ValidationError_UnknownField_ReportsErrorStatus()
    {
        using (CaptureActivities(out var activities))
        {
            // arrange & act
            await new ServiceCollection()
                .AddGraphQL()
                .AddInstrumentation(o => o.Scopes = ActivityScopes.All)
                .AddQueryType<SimpleQuery>()
                .ExecuteRequestAsync("{ unknownField123 }");

            // assert
            activities.MatchSnapshot();
        }
    }

    // TODO: Not sure if we want this
    [Fact]
    public async Task DefaultScopes_ExcludesExecuteRequestAndParseDocumentSpans()
    {
        using (CaptureActivities(out var activities))
        {
            // arrange & act
            await new ServiceCollection()
                .AddGraphQL()
                .AddInstrumentation()
                .AddQueryType<SimpleQuery>()
                .ExecuteRequestAsync("{ sayHello }");

            // assert
            activities.MatchSnapshot();
        }
    }

    [Fact]
    public async Task AllScopes_IncludesExecuteRequestAndParseDocumentSpans()
    {
        using (CaptureActivities(out var activities))
        {
            // arrange & act
            await new ServiceCollection()
                .AddGraphQL()
                .AddInstrumentation(o => o.Scopes = ActivityScopes.All)
                .AddQueryType<SimpleQuery>()
                .ExecuteRequestAsync("{ sayHello }");

            // assert
            activities.MatchSnapshot();
        }
    }

    [Fact]
    public async Task CustomScopes_OnlyValidateAndCompile_LimitsSpans()
    {
        using (CaptureActivities(out var activities))
        {
            // arrange & act
            await new ServiceCollection()
                .AddGraphQL()
                .AddInstrumentation(o =>
                    o.Scopes = ActivityScopes.ValidateDocument | ActivityScopes.CompileOperation)
                .AddQueryType<SimpleQuery>()
                .ExecuteRequestAsync("{ sayHello }");

            // assert
            activities.MatchSnapshot();
        }
    }

    [Fact]
    public async Task OperationNameInRequest_UsedAsActivityDisplayName()
    {
        using (CaptureActivities(out var activities))
        {
            // arrange & act
            await new ServiceCollection()
                .AddGraphQL()
                .AddInstrumentation(o => o.Scopes = ActivityScopes.All)
                .AddQueryType<SimpleQuery>()
                .ExecuteRequestAsync("query MyOp { sayHello }");

            // assert
            activities.MatchSnapshot();
        }
    }

    [Fact]
    public async Task NoOperationName_UsesAnonymousDisplayName()
    {
        using (CaptureActivities(out var activities))
        {
            // arrange & act
            await new ServiceCollection()
                .AddGraphQL()
                .AddInstrumentation(o => o.Scopes = ActivityScopes.All)
                .AddQueryType<SimpleQuery>()
                .ExecuteRequestAsync("{ sayHello }");

            // assert
            activities.MatchSnapshot();
        }
    }

    [Fact]
    public async Task ResolverError_AtRootLevel_MarksOperationAsError()
    {
        using (CaptureActivities(out var activities))
        {
            // arrange & act
            await new ServiceCollection()
                .AddGraphQL()
                .AddInstrumentation(o =>
                {
                    o.Scopes = ActivityScopes.All;
                    o.IncludeDocument = true;
                })
                .AddQueryType<SimpleQuery>()
                .ExecuteRequestAsync("{ causeFatalError }");

            // assert
            activities.MatchSnapshot();
        }
    }

    [Fact]
    public async Task ResolverError_DeepInTree_MarksNestedFieldAsError()
    {
        using (CaptureActivities(out var activities))
        {
            // arrange & act
            await new ServiceCollection()
                .AddGraphQL()
                .AddInstrumentation(o =>
                {
                    o.Scopes = ActivityScopes.All;
                    o.IncludeDocument = true;
                })
                .AddQueryType<SimpleQuery>()
                .ExecuteRequestAsync(
                    """
                    {
                        deep {
                            deeper {
                                deeps {
                                    deeper {
                                        causeFatalError
                                    }
                                }
                            }
                        }
                    }
                    """);

            // assert
            activities.MatchSnapshot();
        }
    }

    [Fact]
    public async Task ComplexityAnalysis_Enabled_RecordsCostInSpan()
    {
        using (CaptureActivities(out var activities))
        {
            // arrange & act
            await new ServiceCollection()
                .AddGraphQL()
                .AddInstrumentation(o =>
                    o.Scopes = ActivityScopes.All)
                .AddCostAnalyzer()
                .AddQueryType<SimpleQuery>()
                .ExecuteRequestAsync("{ sayHello }");

            // assert
            activities.MatchSnapshot();
        }
    }

    [Fact]
    public async Task DataLoader_BatchExecution_RecordsBatchSpan()
    {
        using (CaptureActivities(out var activities))
        {
            // arrange & act
            await new ServiceCollection()
                .AddGraphQL()
                .AddInstrumentation(o => o.Scopes = ActivityScopes.All)
                .AddQueryType<SimpleQuery>()
                .ExecuteRequestAsync("{ dataLoader(key: \"abc\") }");

            // assert
            activities.MatchSnapshot();
        }
    }

    [Fact]
    public async Task VariableCoercion_WithAllScopes_RecordsCoercionSpan()
    {
        using (CaptureActivities(out var activities))
        {
            // arrange & act
            await new ServiceCollection()
                .AddGraphQL()
                .AddInstrumentation(o => o.Scopes = ActivityScopes.All)
                .AddQueryType<SimpleQuery>()
                .ExecuteRequestAsync(
                    OperationRequestBuilder.New()
                        .SetDocument("query($name: String!) { greeting(name: $name) }")
                        .SetVariableValues(
                            new Dictionary<string, object?> { { "name", "World" } })
                        .Build());

            // assert
            activities.MatchSnapshot();
        }
    }

    [Fact]
    public async Task DocumentCache_SecondExecution_RecordsCacheHitEvent()
    {
        using (CaptureActivities(out var activities))
        {
            // arrange
            var services = new ServiceCollection()
                .AddGraphQL()
                .AddInstrumentation(o => o.Scopes = ActivityScopes.All)
                .AddQueryType<SimpleQuery>()
                .Services
                .BuildServiceProvider();

            var executor = await services.GetRequestExecutorAsync();

            // act - execute twice so second uses cached document
            await executor.ExecuteAsync("{ sayHello }");
            await executor.ExecuteAsync("{ sayHello }");

            // assert
            activities.MatchSnapshot();
        }
    }

    public class SimpleQuery
    {
        public string SayHello() => "hello";

        public string Greeting(string name) => $"Hello, {name}!";

        public string CauseFatalError() => throw new GraphQLException("fail");

        public Deep Deep() => new();

        public Task<string?> DataLoader(CustomDataLoader dataLoader, string key)
            => dataLoader.LoadAsync(key);
    }

    public class Deep
    {
        public Deeper Deeper() => new();

        public string CauseFatalError() => throw new GraphQLException("fail");
    }

    public class Deeper
    {
        public Deep[] Deeps() => [new Deep()];
    }

    private sealed class InMemoryOperationDocumentStorage : IOperationDocumentStorage
    {
        private readonly Dictionary<string, DocumentNode> _cache = [];

        public void Add(string id, string document)
            => _cache[id] = Utf8GraphQLParser.Parse(document);

        public ValueTask<IOperationDocument?> TryReadAsync(
            OperationDocumentId documentId,
            CancellationToken cancellationToken = default)
        {
            if (_cache.TryGetValue(documentId.Value, out var document))
            {
                return new ValueTask<IOperationDocument?>(new OperationDocument(document));
            }

            return new ValueTask<IOperationDocument?>(default(IOperationDocument));
        }

        public ValueTask SaveAsync(
            OperationDocumentId documentId,
            IOperationDocument document,
            CancellationToken cancellationToken = default)
        {
            _cache[documentId.Value] = Utf8GraphQLParser.Parse(document.AsSpan());
            return default;
        }
    }
}

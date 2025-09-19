using System.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using HotChocolate.Execution;
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

    [Fact]
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

    [Fact]
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
        using (CaptureActivities(out var activities))
        {
            // arrange & act
            await new ServiceCollection()
                .AddGraphQL()
                .AddInstrumentation(o =>
                {
                    o.RenameRootActivity = true;
                    o.Scopes = ActivityScopes.All;
                })
                .AddQueryType<SimpleQuery>()
                .ExecuteRequestAsync("{ sayHello }");

            // assert
            Assert.Equal("CaptureActivities: query { sayHello }", Activity.Current!.DisplayName);
        }
    }

    [Fact]
    public async Task Parsing_error_when_rename_root_is_activated()
    {
        using (CaptureActivities(out var activities))
        {
            // arrange & act
            await new ServiceCollection()
                .AddGraphQL()
                .AddInstrumentation(o =>
                {
                    o.RenameRootActivity = true;
                    o.Scopes = ActivityScopes.All;
                })
                .AddQueryType<SimpleQuery>()
                .ExecuteRequestAsync("{ sayHello");

            // assert
            Assert.Equal("CaptureActivities: Begin Parse Document", Activity.Current!.DisplayName);
        }
    }

    [Fact]
    public async Task Validation_error_when_rename_root_is_activated()
    {
        using (CaptureActivities(out var activities))
        {
            // arrange & act
            await new ServiceCollection()
                .AddGraphQL()
                .AddInstrumentation(o =>
                {
                    o.RenameRootActivity = true;
                    o.Scopes = ActivityScopes.All;
                })
                .AddQueryType<SimpleQuery>()
                .ExecuteRequestAsync("{ abc123 }");

            // assert
            Assert.Equal("CaptureActivities: Begin Validate Document",
                Activity.Current!.DisplayName);
        }
    }

    [Fact]
    public async Task Create_operation_display_name_with_1_field()
    {
        using (CaptureActivities(out var activities))
        {
            // arrange & act
            await new ServiceCollection()
                .AddGraphQL()
                .AddInstrumentation(o =>
                {
                    o.RenameRootActivity = true;
                    o.Scopes = ActivityScopes.All;
                })
                .AddQueryType<SimpleQuery>()
                .ExecuteRequestAsync("{ a: sayHello }");

            // assert
            activities.MatchSnapshot();
        }
    }

    [Fact]
    public async Task Create_operation_display_name_with_1_field_and_op()
    {
        using (CaptureActivities(out var activities))
        {
            // arrange & act
            await new ServiceCollection()
                .AddGraphQL()
                .AddInstrumentation(o =>
                {
                    o.RenameRootActivity = true;
                    o.Scopes = ActivityScopes.All;
                })
                .AddQueryType<SimpleQuery>()
                .ExecuteRequestAsync("query GetA { a: sayHello }");

            // assert
            activities.MatchSnapshot();
        }
    }

    [Fact]
    public async Task Create_operation_display_name_with_3_field()
    {
        using (CaptureActivities(out var activities))
        {
            // arrange & act
            await new ServiceCollection()
                .AddGraphQL()
                .AddInstrumentation(o =>
                {
                    o.RenameRootActivity = true;
                    o.Scopes = ActivityScopes.All;
                })
                .AddQueryType<SimpleQuery>()
                .ExecuteRequestAsync("{ a: sayHello b: sayHello c: sayHello }");

            // assert
            activities.MatchSnapshot();
        }
    }

    [Fact]
    public async Task Create_operation_display_name_with_4_field()
    {
        using (CaptureActivities(out var activities))
        {
            // arrange & act
            await new ServiceCollection()
                .AddGraphQL()
                .AddInstrumentation(o =>
                {
                    o.RenameRootActivity = true;
                    o.Scopes = ActivityScopes.All;
                })
                .AddQueryType<SimpleQuery>()
                .ExecuteRequestAsync("{ a: sayHello b: sayHello c: sayHello d: sayHello }");

            // assert
            activities.MatchSnapshot();
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

    public class SimpleQuery
    {
        public string SayHello() => "hello";

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
}

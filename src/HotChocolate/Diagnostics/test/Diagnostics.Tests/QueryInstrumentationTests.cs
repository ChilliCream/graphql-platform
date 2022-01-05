using System.Threading.Tasks;
using ChilliCream.Testing;
using HotChocolate.Execution;
using Microsoft.Extensions.DependencyInjection;
using Snapshooter.Xunit;
using Xunit;
using static HotChocolate.Diagnostics.ActivityTestHelper;

namespace HotChocolate.Diagnostics;

[Collection("Instrumentation")]
public class QueryInstrumentationTests
{
    [Fact]
    public async Task Track_events_of_a_simple_query()
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
    public async Task Ensure_operation_name_is_used_as_request_name()
    {
        using (CaptureActivities(out var activities))
        {
            // arrange & act
            await new ServiceCollection()
                .AddGraphQL()
                .AddInstrumentation()
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
                    o.IncludeDocument = true;
                })
                .AddQueryType<SimpleQuery>()
                .ExecuteRequestAsync("query SayHelloOperation { causeFatalError }");

            // assert
            activities.MatchSnapshot();
        }
    }

    [Fact]
    public async Task MaxComplexity_Not_Reached()
    {
        using (CaptureActivities(out var activities))
        {
            // arrange & act
            await new ServiceCollection()
                .AddGraphQL()
                .AddInstrumentation(o =>
                {
                    o.IncludeDocument = true;
                })
                .AddDocumentFromString(FileResource.Open("CostSchema.graphql"))
                .UseField(_ => _ => default)
                .ConfigureSchema(s => s.AddCostDirectiveType())
                .ModifyRequestOptions(o =>
                {
                    o.Complexity.Enable = true;
                    o.Complexity.MaximumAllowed = 9;
                })
                .ExecuteRequestAsync(@"
                    {
                        foo {
                            ... on Foo {
                                ... on Foo {
                                    field
                                    ... on Bar {
                                        baz {
                                            foo {
                                                field
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }");

            // assert
            activities.MatchSnapshot();
        }
    }

    [Fact]
    public async Task MaxComplexity_Reached()
    {
        using (CaptureActivities(out var activities))
        {
            // arrange & act
            await new ServiceCollection()
                .AddGraphQL()
                .AddInstrumentation(o =>
                {
                    o.IncludeDocument = true;
                })
                .AddDocumentFromString(FileResource.Open("CostSchema.graphql"))
                .UseField(_ => _ => default)
                .ConfigureSchema(s => s.AddCostDirectiveType())
                .ModifyRequestOptions(o =>
                {
                    o.Complexity.Enable = true;
                    o.Complexity.MaximumAllowed = 2;
                })
                .ExecuteRequestAsync(@"
                    {
                        foo {
                            ... on Foo {
                                ... on Foo {
                                    field
                                    ... on Bar {
                                        baz {
                                            foo {
                                                field
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }");

            // assert
            activities.MatchSnapshot();
        }
    }

    public class SimpleQuery
    {
        public string SayHello() => "hello";

        public string CauseFatalError() => throw new GraphQLException("fail");
    }

}

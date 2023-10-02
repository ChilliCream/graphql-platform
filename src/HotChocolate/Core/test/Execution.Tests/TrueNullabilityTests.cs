#nullable enable
using CookieCrumble;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Execution;

public class TrueNullabilityTests
{
    [Fact]
    public async Task Schema_Without_TrueNullability()
    {
        var schema =
            await new ServiceCollection()
                .AddGraphQLServer()
                .AddQueryType<Query>()
                .ModifyOptions(o => o.EnableTrueNullability = false)
                .BuildSchemaAsync();
        
        schema.MatchSnapshot();
    }
    
    [Fact]
    public async Task Schema_With_TrueNullability()
    {
        var schema =
            await new ServiceCollection()
                .AddGraphQLServer()
                .AddQueryType<Query>()
                .ModifyOptions(o => o.EnableTrueNullability = true)
                .BuildSchemaAsync();
        
        schema.MatchSnapshot();
    }
    
    [Fact]
    public async Task Error_Query_With_TrueNullability_And_NullBubbling_Enabled_By_Default()
    {
        var response =
            await new ServiceCollection()
                .AddGraphQLServer()
                .AddQueryType<Query>()
                .ModifyOptions(o => o.EnableTrueNullability = true)
                .ExecuteRequestAsync(
                    """
                    query {
                        book {
                            name
                            author {
                                name
                            }
                        }
                    }
                    """);
        
        response.MatchSnapshot();
    }
    
    [Fact]
    public async Task Error_Query_With_TrueNullability_And_NullBubbling_Enabled()
    {
        var response =
            await new ServiceCollection()
                .AddGraphQLServer()
                .AddQueryType<Query>()
                .ModifyOptions(o => o.EnableTrueNullability = true)
                .ExecuteRequestAsync(
                    """
                    query @nullBubbling {
                        book {
                            name
                            author {
                                name
                            }
                        }
                    }
                    """);
        
        response.MatchSnapshot();
    }
    
    [Fact]
    public async Task Error_Query_With_TrueNullability_And_NullBubbling_Disabled()
    {
        var response =
            await new ServiceCollection()
                .AddGraphQLServer()
                .AddQueryType<Query>()
                .ModifyOptions(o => o.EnableTrueNullability = true)
                .ExecuteRequestAsync(
                    """
                    query @nullBubbling(enable: false) {
                        book {
                            name
                            author {
                                name
                            }
                        }
                    }
                    """);
        
        response.MatchSnapshot();
    }
    
    [Fact]
    public async Task Error_Query_With_TrueNullability_And_NullBubbling_Disabled_With_Variable()
    {
        var response =
            await new ServiceCollection()
                .AddGraphQLServer()
                .AddQueryType<Query>()
                .ModifyOptions(o => o.EnableTrueNullability = true)
                .ExecuteRequestAsync(
                    QueryRequestBuilder.New()
                        .SetQuery(
                        """
                        query($enable: Boolean!) @nullBubbling(enable: $enable) {
                            book {
                                name
                                author {
                                    name
                                }
                            }
                        }
                        """)
                        .SetVariableValue("enable", false)
                        .Create());
        
        response.MatchSnapshot();
    }
    
    public class Query
    {
        public Book? GetBook() => new();
    }
    
    public class Book
    {
        public string Name => "Some book!";

        public Author Author => new();
    }
    
    public class Author
    {
        public string Name => throw new Exception();
    } 
}
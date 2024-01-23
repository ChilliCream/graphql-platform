using System.Collections.Generic;
using System.Threading.Tasks;
using HotChocolate.Execution;
using HotChocolate.Types.Pagination;
using Microsoft.Extensions.DependencyInjection;
using Snapshooter.Xunit;
using Xunit;
using static HotChocolate.Tests.TestHelper;

#nullable enable

namespace HotChocolate.Types;

public class PaginationTests
{
    [Fact(Skip = "Test is flaky")]
    public async Task Execute_NestedOffsetPaging_NoCyclicDependencies()
    {
        await TryTest(async ct =>
        {
            var executor =
                await new ServiceCollection()
                    .AddGraphQL()
                    .AddQueryType<QueryType>()
                    .SetPagingOptions(new PagingOptions { DefaultPageSize = 50, })
                    .Services
                    .BuildServiceProvider()
                    .GetRequestExecutorAsync(cancellationToken: ct);

            var executionResult = await executor
                .ExecuteAsync(@"
                        {
                            users {
                                items {
                                    parents {
                                        items {
                                            firstName
                                        }
                                    }
                               }
                            }
                        }",
                    ct);

            executionResult.ToJson().MatchSnapshot();
        });
    }

    [Fact(Skip = "Flaky test.")]
    public async Task Execute_NestedOffsetPaging_With_Indirect_Cycles()
    {
        await TryTest(async ct =>
        {
            var executor =
                await new ServiceCollection()
                    .AddGraphQL()
                    .AddQueryType<QueryType>()
                    .SetPagingOptions(new PagingOptions { DefaultPageSize = 50, })
                    .Services
                    .BuildServiceProvider()
                    .GetRequestExecutorAsync(cancellationToken: ct);

            var executionResult = await executor
                .ExecuteAsync(@"
                    {
                        users {
                            items {
                                groups {
                                    items {
                                        members {
                                            items {
                                                firstName
                                            }
                                        }
                                    }
                                }
                           }
                        }
                    }",
                    ct);

            executionResult.ToJson().MatchSnapshot();
        });
    }

    public class User
    {
        public string? FirstName { get; set; }

        public List<User> Parents { get; set; } = default!;

        public List<Group> Groups { get; set; } = default!;
    }

    public class Group
    {
        public string? FirstName { get; set; }

        public List<User> Members { get; set; } = default!;
    }

    public class UserType : ObjectType<User>
    {
        protected override void Configure(IObjectTypeDescriptor<User> descriptor)
        {
            descriptor
                .Field(i => i.Parents)
                .UseOffsetPaging<UserType>()
                .Resolve(() => new[]
                {
                    new User { FirstName = "Mother", },
                    new User { FirstName = "Father", },
                });

            descriptor
                .Field(i => i.Groups)
                .UseOffsetPaging<GroupType>()
                .Resolve(() => new[]
                {
                    new Group { FirstName = "Admin", },
                });
        }
    }

    public class GroupType : ObjectType<Group>
    {
        protected override void Configure(IObjectTypeDescriptor<Group> descriptor)
        {
            descriptor
                .Field(i => i.Members)
                .UseOffsetPaging<UserType>()
                .Resolve(() => new[]
                {
                    new User { FirstName = "Mother", },
                    new User { FirstName = "Father", },
                });
        }
    }

    public class Query
    {
        public List<User> Users => [new User(),];
    }

    public class QueryType : ObjectType<Query>
    {
        protected override void Configure(IObjectTypeDescriptor<Query> descriptor)
        {
            descriptor
                .Field(t => t.Users)
                .UseOffsetPaging<UserType>();
        }
    }
}

// ReSharper disable UnusedAutoPropertyAccessor.Global
// ReSharper disable AutoPropertyCanBeMadeGetOnly.Global
// ReSharper disable ClassNeverInstantiated.Global
// ReSharper disable MemberCanBePrivate.Global

#nullable enable

using HotChocolate.Execution;
using HotChocolate.Tests;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Types;

public class PaginationTests
{
    [Fact]
    public async Task Execute_NestedOffsetPaging_NoCyclicDependencies()
        => await SnapshotTest
            .Create(
                async (snapshot, ct) =>
                {
                    var executor =
                        await new ServiceCollection()
                            .AddGraphQL()
                            .AddQueryType<QueryType>()
                            .ModifyPagingOptions(o => o.DefaultPageSize = 50)
                            .BuildRequestExecutorAsync(cancellationToken: ct);

                    snapshot.Add(
                        await executor.ExecuteAsync(
                            """
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
                            }
                            """,
                            ct));
                })
            .MatchAsync();

    [Fact]
    public async Task Execute_NestedOffsetPaging_With_Indirect_Cycles()
        => await SnapshotTest
            .Create(
                async (snapshot, ct) =>
                {
                    var executor =
                        await new ServiceCollection()
                            .AddGraphQL()
                            .AddQueryType<QueryType>()
                            .ModifyPagingOptions(o => o.DefaultPageSize = 50)
                            .BuildRequestExecutorAsync(cancellationToken: ct);

                    snapshot.Add(await executor
                        .ExecuteAsync(
                            """
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
                            }
                            """,
                            ct));
                })
            .MatchAsync();

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
                .Resolve(
                    () => new[]
                    {
                        new User { FirstName = "Mother", },
                        new User { FirstName = "Father", },
                    });

            descriptor
                .Field(i => i.Groups)
                .UseOffsetPaging<GroupType>()
                .Resolve(
                    () => new[]
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
                .Resolve(
                    () => new[]
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

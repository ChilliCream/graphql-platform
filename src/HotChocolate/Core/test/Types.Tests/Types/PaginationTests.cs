using System.Collections.Generic;
using System.Threading.Tasks;
using HotChocolate.Execution;
using HotChocolate.Types.Pagination;
using Microsoft.Extensions.DependencyInjection;
using Snapshooter.Xunit;
using Xunit;

namespace HotChocolate.Types
{
    public class PaginationTests
    {
        [Fact]
        public async Task Execute_NestedOffsetPaging_NoCyclicDependencies()
        {
            IRequestExecutor executor =
                await new ServiceCollection()
                    .AddGraphQL()
                    .AddQueryType<QueryType>()
                    .SetPagingOptions(new PagingOptions { DefaultPageSize = 50 })
                    .Services
                    .BuildServiceProvider()
                    .GetRequestExecutorAsync();

            IExecutionResult executionResult = await executor
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
                    }");

            executionResult.ToJson().MatchSnapshot();
        }

        [Fact]
        public async Task Execute_NestedOffsetPaging_With_Indirect_Cycles()
        {
            IRequestExecutor executor =
                await new ServiceCollection()
                    .AddGraphQL()
                    .AddQueryType<QueryType>()
                    .SetPagingOptions(new PagingOptions { DefaultPageSize = 50 })
                    .Services
                    .BuildServiceProvider()
                    .GetRequestExecutorAsync();

            IExecutionResult executionResult = await executor
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
                    }");

            executionResult.ToJson().MatchSnapshot();
        }

        public class User
        {
            public string? FirstName { get; set; }

            public List<User> Parents { get; set; }

            public List<Group> Groups { get; set; }
        }

        public class Group
        {
            public string? FirstName { get; set; }

            public List<User> Members { get; set; }
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
                        new User { FirstName = "Mother" },
                        new User { FirstName = "Father" }
                    });

                descriptor
                    .Field(i => i.Groups)
                    .UseOffsetPaging<GroupType>()
                    .Resolve(() => new[]
                    {
                        new Group { FirstName = "Admin" }
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
                        new User { FirstName = "Mother" },
                        new User { FirstName = "Father" }
                    });
            }
        }

        public class Query
        {
            public List<User> Users => new() { new User() };
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
}

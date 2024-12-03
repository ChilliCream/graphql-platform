using HotChocolate.Execution;
using HotChocolate.Types;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Resolvers;

public class IsSelectedTests
{
    [Fact]
    public async Task IsSelected_Attribute_1_Selected()
    {
        var result =
            await new ServiceCollection()
                .AddGraphQL()
                .AddQueryType<Query>()
                .ExecuteRequestAsync(
                    """
                    query {
                        user_Attribute_1 {
                            name
                            email
                        }
                    }
                    """);

        result.MatchMarkdownSnapshot();
    }

    [Fact]
    public async Task IsSelected_Attribute_1_Not_Selected()
    {
        var result =
            await new ServiceCollection()
                .AddGraphQL()
                .AddQueryType<Query>()
                .ExecuteRequestAsync(
                    """
                    query {
                        user_Attribute_1 {
                            name
                        }
                    }
                    """);

        result.MatchMarkdownSnapshot();
    }

    [Fact]
    public async Task IsSelected_Attribute_2_Selected_1()
    {
        var result =
            await new ServiceCollection()
                .AddGraphQL()
                .AddQueryType<Query>()
                .ExecuteRequestAsync(
                    """
                    query {
                        user_Attribute_2 {
                            name
                            email
                        }
                    }
                    """);

        result.MatchMarkdownSnapshot();
    }

    [Fact]
    public async Task IsSelected_Attribute_2_Selected_2()
    {
        var result =
            await new ServiceCollection()
                .AddGraphQL()
                .AddQueryType<Query>()
                .ExecuteRequestAsync(
                    """
                    query {
                        user_Attribute_2 {
                            name
                            password
                        }
                    }
                    """);

        result.MatchMarkdownSnapshot();
    }

    [Fact]
    public async Task IsSelected_Attribute_2_Not_Selected()
    {
        var result =
            await new ServiceCollection()
                .AddGraphQL()
                .AddQueryType<Query>()
                .ExecuteRequestAsync(
                    """
                    query {
                        user_Attribute_2 {
                            name
                        }
                    }
                    """);

        result.MatchMarkdownSnapshot();
    }

    [Fact]
    public async Task IsSelected_Attribute_3_Selected_1()
    {
        var result =
            await new ServiceCollection()
                .AddGraphQL()
                .AddQueryType<Query>()
                .ExecuteRequestAsync(
                    """
                    query {
                        user_Attribute_3 {
                            name
                            email
                        }
                    }
                    """);

        result.MatchMarkdownSnapshot();
    }

    [Fact]
    public async Task IsSelected_Attribute_3_Selected_2()
    {
        var result =
            await new ServiceCollection()
                .AddGraphQL()
                .AddQueryType<Query>()
                .ExecuteRequestAsync(
                    """
                    query {
                        user_Attribute_3 {
                            name
                            password
                        }
                    }
                    """);

        result.MatchMarkdownSnapshot();
    }

    [Fact]
    public async Task IsSelected_Attribute_3_Selected_3()
    {
        var result =
            await new ServiceCollection()
                .AddGraphQL()
                .AddQueryType<Query>()
                .ExecuteRequestAsync(
                    """
                    query {
                        user_Attribute_3 {
                            name
                            phoneNumber
                        }
                    }
                    """);

        result.MatchMarkdownSnapshot();
    }

    [Fact]
    public async Task IsSelected_Attribute_3_Not_Selected()
    {
        var result =
            await new ServiceCollection()
                .AddGraphQL()
                .AddQueryType<Query>()
                .ExecuteRequestAsync(
                    """
                    query {
                        user_Attribute_3 {
                            name
                        }
                    }
                    """);

        result.MatchMarkdownSnapshot();
    }

    [Fact]
    public async Task IsSelected_Attribute_4_Selected_1()
    {
        var result =
            await new ServiceCollection()
                .AddGraphQL()
                .AddQueryType<Query>()
                .ExecuteRequestAsync(
                    """
                    query {
                        user_Attribute_4 {
                            name
                            email
                        }
                    }
                    """);

        result.MatchMarkdownSnapshot();
    }

    [Fact]
    public async Task IsSelected_Attribute_4_Selected_2()
    {
        var result =
            await new ServiceCollection()
                .AddGraphQL()
                .AddQueryType<Query>()
                .ExecuteRequestAsync(
                    """
                    query {
                        user_Attribute_4 {
                            name
                            password
                        }
                    }
                    """);

        result.MatchMarkdownSnapshot();
    }

    [Fact]
    public async Task IsSelected_Attribute_4_Selected_3()
    {
        var result =
            await new ServiceCollection()
                .AddGraphQL()
                .AddQueryType<Query>()
                .ExecuteRequestAsync(
                    """
                    query {
                        user_Attribute_4 {
                            name
                            phoneNumber
                        }
                    }
                    """);

        result.MatchMarkdownSnapshot();
    }

    [Fact]
    public async Task IsSelected_Attribute_4_Selected_4()
    {
        var result =
            await new ServiceCollection()
                .AddGraphQL()
                .AddQueryType<Query>()
                .ExecuteRequestAsync(
                    """
                    query {
                        user_Attribute_4 {
                            name
                            address
                        }
                    }
                    """);

        result.MatchMarkdownSnapshot();
    }

    [Fact]
    public async Task IsSelected_Attribute_4_Not_Selected()
    {
        var result =
            await new ServiceCollection()
                .AddGraphQL()
                .AddQueryType<Query>()
                .ExecuteRequestAsync(
                    """
                    query {
                        user_Attribute_4 {
                            name
                        }
                    }
                    """);

        result.MatchMarkdownSnapshot();
    }

    [Fact]
    public async Task IsSelected_Attribute_5_Selected()
    {
        var result =
            await new ServiceCollection()
                .AddGraphQL()
                .AddQueryType<Query>()
                .ExecuteRequestAsync(
                    """
                    query {
                        user_Attribute_5 {
                            email
                            category {
                                next {
                                    name
                                }
                            }
                        }
                    }
                    """);

        result.MatchMarkdownSnapshot();
    }

    [Fact]
    public async Task IsSelected_Attribute_5_Not_Selected()
    {
        var result =
            await new ServiceCollection()
                .AddGraphQL()
                .AddQueryType<Query>()
                .ExecuteRequestAsync(
                    """
                    query {
                        user_Attribute_5 {
                            email
                            category {
                                name
                            }
                        }
                    }
                    """);

        result.MatchMarkdownSnapshot();
    }

    [Fact]
    public async Task IsSelected_Attribute_6_Selected()
    {
        var result =
            await new ServiceCollection()
                .AddGraphQL()
                .AddQueryType<Query>()
                .ExecuteRequestAsync(
                    """
                    query {
                        user_Attribute_6 {
                            email
                            category {
                                next {
                                    name
                                }
                            }
                        }
                    }
                    """);

        result.MatchMarkdownSnapshot();
    }

    [Fact]
    public async Task IsSelected_Attribute_6_Not_Selected()
    {
        var result =
            await new ServiceCollection()
                .AddGraphQL()
                .AddQueryType<Query>()
                .ExecuteRequestAsync(
                    """
                    query {
                        user_Attribute_6 {
                            email
                            category {
                                name
                            }
                        }
                    }
                    """);

        result.MatchMarkdownSnapshot();
    }

    [Fact]
    public async Task IsSelected_Pattern_Is_Invalid()
    {
        var snapshot = new Snapshot();

        async Task Broken() =>
            await new ServiceCollection()
                .AddGraphQL()
                .AddQueryType<BrokenQuery>()
                .ExecuteRequestAsync(
                    """
                    query {
                        user_Attribute_6 {
                            email
                            category {
                                name
                            }
                        }
                    }
                    """);

        var ex = await Assert.ThrowsAsync<SchemaException>(Broken);

        foreach (var error in ex.Errors)
        {
            snapshot.Add(error.Message);
        }

        await snapshot.MatchMarkdownAsync();
    }

    [Fact]
    public async Task IsSelected_Context_1_Selected()
    {
        var result =
            await new ServiceCollection()
                .AddGraphQL()
                .AddQueryType<Query>()
                .ExecuteRequestAsync(
                    """
                    query {
                        user_Context_1 {
                            name
                            email
                        }
                    }
                    """);

        result.MatchMarkdownSnapshot();
    }

    [Fact]
    public async Task IsSelected_Context_1_Not_Selected()
    {
        var result =
            await new ServiceCollection()
                .AddGraphQL()
                .AddQueryType<Query>()
                .ExecuteRequestAsync(
                    """
                    query {
                        user_Context_1 {
                            name
                        }
                    }
                    """);

        result.MatchMarkdownSnapshot();
    }

    [Fact]
    public async Task IsSelected_Context_2_Selected_1()
    {
        var result =
            await new ServiceCollection()
                .AddGraphQL()
                .AddQueryType<Query>()
                .ExecuteRequestAsync(
                    """
                    query {
                        user_Context_2 {
                            name
                            email
                        }
                    }
                    """);

        result.MatchMarkdownSnapshot();
    }

    [Fact]
    public async Task IsSelected_Context_2_Selected_2()
    {
        var result =
            await new ServiceCollection()
                .AddGraphQL()
                .AddQueryType<Query>()
                .ExecuteRequestAsync(
                    """
                    query {
                        user_Context_2 {
                            name
                            password
                        }
                    }
                    """);

        result.MatchMarkdownSnapshot();
    }

    [Fact]
    public async Task IsSelected_Context_2_Not_Selected()
    {
        var result =
            await new ServiceCollection()
                .AddGraphQL()
                .AddQueryType<Query>()
                .ExecuteRequestAsync(
                    """
                    query {
                        user_Context_2 {
                            name
                        }
                    }
                    """);

        result.MatchMarkdownSnapshot();
    }

    [Fact]
    public async Task IsSelected_Context_3_Selected_1()
    {
        var result =
            await new ServiceCollection()
                .AddGraphQL()
                .AddQueryType<Query>()
                .ExecuteRequestAsync(
                    """
                    query {
                        user_Context_3 {
                            name
                            email
                        }
                    }
                    """);

        result.MatchMarkdownSnapshot();
    }

    [Fact]
    public async Task IsSelected_Context_3_Selected_2()
    {
        var result =
            await new ServiceCollection()
                .AddGraphQL()
                .AddQueryType<Query>()
                .ExecuteRequestAsync(
                    """
                    query {
                        user_Context_3 {
                            name
                            password
                        }
                    }
                    """);

        result.MatchMarkdownSnapshot();
    }

    [Fact]
    public async Task IsSelected_Context_3_Selected_3()
    {
        var result =
            await new ServiceCollection()
                .AddGraphQL()
                .AddQueryType<Query>()
                .ExecuteRequestAsync(
                    """
                    query {
                        user_Context_3 {
                            name
                            phoneNumber
                        }
                    }
                    """);

        result.MatchMarkdownSnapshot();
    }

    [Fact]
    public async Task IsSelected_Context_3_Not_Selected()
    {
        var result =
            await new ServiceCollection()
                .AddGraphQL()
                .AddQueryType<Query>()
                .ExecuteRequestAsync(
                    """
                    query {
                        user_Context_3 {
                            name
                        }
                    }
                    """);

        result.MatchMarkdownSnapshot();
    }

    [Fact]
    public async Task IsSelected_Context_4_Selected_1()
    {
        var result =
            await new ServiceCollection()
                .AddGraphQL()
                .AddQueryType<Query>()
                .ExecuteRequestAsync(
                    """
                    query {
                        user_Context_4 {
                            name
                            email
                        }
                    }
                    """);

        result.MatchMarkdownSnapshot();
    }

    [Fact]
    public async Task IsSelected_Context_4_Selected_2()
    {
        var result =
            await new ServiceCollection()
                .AddGraphQL()
                .AddQueryType<Query>()
                .ExecuteRequestAsync(
                    """
                    query {
                        user_Context_4 {
                            name
                            password
                        }
                    }
                    """);

        result.MatchMarkdownSnapshot();
    }

    [Fact]
    public async Task IsSelected_Context_4_Selected_3()
    {
        var result =
            await new ServiceCollection()
                .AddGraphQL()
                .AddQueryType<Query>()
                .ExecuteRequestAsync(
                    """
                    query {
                        user_Context_4 {
                            name
                            phoneNumber
                        }
                    }
                    """);

        result.MatchMarkdownSnapshot();
    }

    [Fact]
    public async Task IsSelected_Context_4_Selected_4()
    {
        var result =
            await new ServiceCollection()
                .AddGraphQL()
                .AddQueryType<Query>()
                .ExecuteRequestAsync(
                    """
                    query {
                        user_Context_4 {
                            name
                            address
                        }
                    }
                    """);

        result.MatchMarkdownSnapshot();
    }

    [Fact]
    public async Task IsSelected_Context_4_Not_Selected()
    {
        var result =
            await new ServiceCollection()
                .AddGraphQL()
                .AddQueryType<Query>()
                .ExecuteRequestAsync(
                    """
                    query {
                        user_Context_4 {
                            name
                        }
                    }
                    """);

        result.MatchMarkdownSnapshot();
    }

    [Fact]
    public async Task Select_Category_Level_1()
    {
        var result =
            await new ServiceCollection()
                .AddGraphQL()
                .AddQueryType(
                    c =>
                    {
                        c.Name("Query");
                        c.Field("user")
                            .Resolve(
                                ctx =>
                                {
                                    ((IMiddlewareContext)ctx).OperationResult.SetExtension(
                                        "isSelected",
                                        ctx.Select("category").IsSelected("next"));
                                    return Query.DummyUser;
                                });
                    })
                .ExecuteRequestAsync(
                    """
                    query {
                        user {
                            name
                            category {
                                next {
                                    name
                                }
                            }
                        }
                    }
                    """);

        result.MatchMarkdownSnapshot();
    }

    [Fact]
    public async Task Traverse_With_Select()
    {
        var result =
            await new ServiceCollection()
                .AddGraphQL()
                .AddQueryType(
                    c =>
                    {
                        c.Name("Query");
                        c.Field("user")
                            .Resolve(
                                ctx =>
                                {
                                    var isTagsOnContext = ctx.IsSelected("tags");
                                    var collection = ctx.Select("tags");
                                    var isAuditOnTagsCollection = collection.IsSelected("audit");
                                    collection = collection.Select("audit");
                                    var isEditedByOnAudiCollection = collection.IsSelected("editedBy");
                                    var operationResult = ((IMiddlewareContext)ctx).OperationResult;

                                    operationResult.SetExtension(
                                        nameof(isTagsOnContext),
                                        isTagsOnContext);
                                    operationResult.SetExtension(
                                        nameof(isAuditOnTagsCollection),
                                        isAuditOnTagsCollection);
                                    operationResult.SetExtension(
                                        nameof(isEditedByOnAudiCollection),
                                        isEditedByOnAudiCollection);

                                    return Query.DummyUser;
                                });
                    })
                .ExecuteRequestAsync(
                    """
                    query {
                        user {
                            name
                            tags {
                                value
                                name
                                audit {
                                    editedBy
                                }
                            }
                        }
                    }
                    """);

        result.MatchMarkdownSnapshot();
    }

    [Fact]
    public async Task Select_Category_Level_2()
    {
        var result =
            await new ServiceCollection()
                .AddGraphQL()
                .AddQueryType(
                    c =>
                    {
                        c.Name("Query");
                        c.Field("user")
                            .Resolve(
                                ctx =>
                                {
                                    ((IMiddlewareContext)ctx).OperationResult.SetExtension(
                                        "isSelected",
                                        ctx.Select("category").Select("next").IsSelected("name"));
                                    return Query.DummyUser;
                                });
                    })
                .ExecuteRequestAsync(
                    """
                    query {
                        user {
                            name
                            category {
                                next {
                                    next {
                                        name
                                    }
                                }
                            }
                        }
                    }
                    """);

        result.MatchMarkdownSnapshot();
    }

    public class Query
    {
        public static User DummyUser { get; } =
            new()
            {
                Name = "a",
                Email = "b",
                Password = "c",
                PhoneNumber = "d",
                Address = "e",
                City = "f",
            };

        public User GetUser_Attribute_1([IsSelected("email")] bool isSelected, IResolverContext context)
        {
            ((IMiddlewareContext)context).OperationResult.SetExtension("isSelected", isSelected);
            return new User
            {
                Name = "a",
                Email = "b",
                Password = "c",
                PhoneNumber = "d",
                Address = "e",
                City = "f",
            };
        }

        public User GetUser_Attribute_2([IsSelected("email", "password")] bool isSelected, IResolverContext context)
        {
            ((IMiddlewareContext)context).OperationResult.SetExtension("isSelected", isSelected);
            return new User
            {
                Name = "a",
                Email = "b",
                Password = "c",
                PhoneNumber = "d",
                Address = "e",
                City = "f",
            };
        }

        public User GetUser_Attribute_3(
            [IsSelected("email", "password", "phoneNumber")]
            bool isSelected,
            IResolverContext context)
        {
            ((IMiddlewareContext)context).OperationResult.SetExtension("isSelected", isSelected);
            return new User
            {
                Name = "a",
                Email = "b",
                Password = "c",
                PhoneNumber = "d",
                Address = "e",
                City = "f",
            };
        }

        public User GetUser_Attribute_4(
            [IsSelected("email", "password", "phoneNumber", "address")]
            bool isSelected,
            IResolverContext context)
        {
            ((IMiddlewareContext)context).OperationResult.SetExtension("isSelected", isSelected);
            return new User
            {
                Name = "a",
                Email = "b",
                Password = "c",
                PhoneNumber = "d",
                Address = "e",
                City = "f",
            };
        }

        public User GetUser_Attribute_5(
            [IsSelected("email category { next { name } }")]
            bool isSelected,
            IResolverContext context)
        {
            ((IMiddlewareContext)context).OperationResult.SetExtension("isSelected", isSelected);
            return new User
            {
                Name = "a",
                Email = "b",
                Password = "c",
                PhoneNumber = "d",
                Address = "e",
                City = "f",
            };
        }

        public User GetUser_Attribute_6(
            [IsSelected("email category { ... on Category { next { name } } }")]
            bool isSelected,
            IResolverContext context)
        {
            ((IMiddlewareContext)context).OperationResult.SetExtension("isSelected", isSelected);
            return new User
            {
                Name = "a",
                Email = "b",
                Password = "c",
                PhoneNumber = "d",
                Address = "e",
                City = "f",
            };
        }

        public User GetUser_Context_1(IResolverContext context)
        {
            ((IMiddlewareContext)context).OperationResult.SetExtension("isSelected", context.IsSelected("email"));
            return new User
            {
                Name = "a",
                Email = "b",
                Password = "c",
                PhoneNumber = "d",
                Address = "e",
                City = "f",
            };
        }

        public User GetUser_Context_2(IResolverContext context)
        {
            ((IMiddlewareContext)context).OperationResult.SetExtension(
                "isSelected",
                context.IsSelected("email", "password"));
            return new User
            {
                Name = "a",
                Email = "b",
                Password = "c",
                PhoneNumber = "d",
                Address = "e",
                City = "f",
            };
        }

        public User GetUser_Context_3(IResolverContext context)
        {
            ((IMiddlewareContext)context).OperationResult.SetExtension(
                "isSelected",
                context.IsSelected("email", "password", "phoneNumber"));
            return new User
            {
                Name = "a",
                Email = "b",
                Password = "c",
                PhoneNumber = "d",
                Address = "e",
                City = "f",
            };
        }

        public User GetUser_Context_4(IResolverContext context)
        {
            ((IMiddlewareContext)context).OperationResult.SetExtension(
                "isSelected",
                context.IsSelected(new HashSet<string>
                {
                    "email", "password", "phoneNumber", "address",
                }));
            return new User
            {
                Name = "a",
                Email = "b",
                Password = "c",
                PhoneNumber = "d",
                Address = "e",
                City = "f",
            };
        }
    }

    public class BrokenQuery
    {
        public static User DummyUser { get; } =
            new()
            {
                Name = "a",
                Email = "b",
                Password = "c",
                PhoneNumber = "d",
                Address = "e",
                City = "f",
            };

        public User GetUser_1(
            [IsSelected("email category { next { bar } }")]
            bool isSelected,
            IResolverContext context)
        {
            ((IMiddlewareContext)context).OperationResult.SetExtension("isSelected", isSelected);
            return new User
            {
                Name = "a",
                Email = "b",
                Password = "c",
                PhoneNumber = "d",
                Address = "e",
                City = "f",
            };
        }

        public User GetUser_2(
            [IsSelected("email category { ... on String { next { name } } }")]
            bool isSelected,
            IResolverContext context)
        {
            ((IMiddlewareContext)context).OperationResult.SetExtension("isSelected", isSelected);
            return new User
            {
                Name = "a",
                Email = "b",
                Password = "c",
                PhoneNumber = "d",
                Address = "e",
                City = "f",
            };
        }
    }

    public class User
    {
        public string Name { get; set; }

        public string Email { get; set; }

        public string Password { get; set; }

        public string PhoneNumber { get; set; }

        public string Address { get; set; }

        public string City { get; set; }

        public Category Category { get; set; }
        public List<UserTag> Tags { get; set; }
    }

    public class Category
    {
        public string Name { get; set; }

        public Category Next { get; set; }
    }

    public class UserTag
    {
        public string Name { get; set; }
        public int Value { get; set; }
        public Audit Audit { get; set; }
    }

    public class Audit
    {
        public string EditedBy { get; set; }
        public string EditedAt { get; set; }
    }
}

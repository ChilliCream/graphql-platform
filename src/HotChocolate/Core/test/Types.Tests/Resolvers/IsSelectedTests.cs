using System.Collections.Generic;
using System.Threading.Tasks;
using CookieCrumble;
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

    public class Query
    {
        [UseIsSelected("email")]
        public User GetUser_Attribute_1([LocalState] bool isSelected, IResolverContext context)
        {
            ((IMiddlewareContext)context).OperationResult.SetExtension("isSelected", isSelected);
            return new User { Name = "a", Email = "b", Password = "c", PhoneNumber = "d", Address = "e", City = "f", };
        }

        [UseIsSelected("email", "password")]
        public User GetUser_Attribute_2([LocalState] bool isSelected, IResolverContext context)
        {
            ((IMiddlewareContext)context).OperationResult.SetExtension("isSelected", isSelected);
            return new User { Name = "a", Email = "b", Password = "c", PhoneNumber = "d", Address = "e", City = "f", };
        }

        [UseIsSelected("email", "password", "phoneNumber")]
        public User GetUser_Attribute_3([LocalState] bool isSelected, IResolverContext context)
        {
            ((IMiddlewareContext)context).OperationResult.SetExtension("isSelected", isSelected);
            return new User { Name = "a", Email = "b", Password = "c", PhoneNumber = "d", Address = "e", City = "f", };
        }

        [UseIsSelected("email", "password", "phoneNumber", "address")]
        public User GetUser_Attribute_4([LocalState] bool isSelected, IResolverContext context)
        {
            ((IMiddlewareContext)context).OperationResult.SetExtension("isSelected", isSelected);
            return new User { Name = "a", Email = "b", Password = "c", PhoneNumber = "d", Address = "e", City = "f", };
        }

        public User GetUser_Context_1(IResolverContext context)
        {
            ((IMiddlewareContext)context).OperationResult.SetExtension("isSelected", context.IsSelected("email"));
            return new User { Name = "a", Email = "b", Password = "c", PhoneNumber = "d", Address = "e", City = "f", };
        }
        
        public User GetUser_Context_2(IResolverContext context)
        {
            ((IMiddlewareContext)context).OperationResult.SetExtension(
                "isSelected",
                context.IsSelected("email", "password"));
            return new User { Name = "a", Email = "b", Password = "c", PhoneNumber = "d", Address = "e", City = "f", };
        }
        
        public User GetUser_Context_3(IResolverContext context)
        {
            ((IMiddlewareContext)context).OperationResult.SetExtension(
                "isSelected",
                context.IsSelected("email", "password", "phoneNumber"));
            return new User { Name = "a", Email = "b", Password = "c", PhoneNumber = "d", Address = "e", City = "f", };
        }
        
        public User GetUser_Context_4(IResolverContext context)
        {
            ((IMiddlewareContext)context).OperationResult.SetExtension(
                "isSelected",
                context.IsSelected(new HashSet<string> { "email", "password", "phoneNumber", "address", }));
            return new User { Name = "a", Email = "b", Password = "c", PhoneNumber = "d", Address = "e", City = "f", };
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
    }
}
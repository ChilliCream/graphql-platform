using System.Text.Json;
using HotChocolate.Execution;
using HotChocolate.Types;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Data;

public class Issue5072VerificationTests
{
    [Fact]
    public async Task Extended_Field_Resolver_Does_Not_Project_Original_Field()
    {
        var executor = await new ServiceCollection()
            .AddGraphQL()
            .AddProjections()
            .AddQueryType<Issue5072Query>()
            .AddTypeExtension<Issue5072UserExtensions>()
            .BuildRequestExecutorAsync();

        var result = await executor.ExecuteAsync(
            """
            {
              users {
                id
                profile {
                  id
                }
              }
            }
            """);

        var operationResult = result.ExpectOperationResult();
        Assert.Empty(operationResult.Errors ?? []);
    }

    [Fact]
    public async Task Extended_Field_Resolver_Uses_Parent_Requirements_For_Projection()
    {
        var executor = await new ServiceCollection()
            .AddGraphQL()
            .AddProjections()
            .AddQueryType<Issue5072Query>()
            .AddTypeExtension<Issue5072UserExtensionsWithRequirements>()
            .BuildRequestExecutorAsync();

        var result = await executor.ExecuteAsync(
            """
            {
              users {
                profile {
                  id
                }
              }
            }
            """);

        var operationResult = result.ExpectOperationResult();
        Assert.Empty(operationResult.Errors ?? []);

        using var document = JsonDocument.Parse(result.ToJson());

        var id = document
            .RootElement
            .GetProperty("data")
            .GetProperty("users")[0]
            .GetProperty("profile")
            .GetProperty("id")
            .GetInt32();

        Assert.Equal(10, id);
    }

    public class Issue5072Query
    {
        [UseProjection]
        public IQueryable<Issue5072User> GetUsers()
            => new[] { new Issue5072User { Id = 1 } }.AsQueryable();
    }

    [ExtendObjectType(typeof(Issue5072User))]
    public class Issue5072UserExtensions
    {
        public Issue5072Profile Profile([Parent] Issue5072User user)
            => new() { Id = user.Id * 10 };
    }

    [ExtendObjectType(typeof(Issue5072User))]
    public class Issue5072UserExtensionsWithRequirements
    {
        public Issue5072Profile Profile([Parent("Id")] Issue5072User user)
            => new() { Id = user.Id * 10 };
    }

    public class Issue5072User
    {
        public int Id { get; set; }

        public Issue5072Profile Profile
        {
            get => throw new InvalidOperationException(
                "The original Profile member should not be projected for extended resolver fields.");
            set { }
        }
    }

    public class Issue5072Profile
    {
        public int Id { get; set; }
    }
}

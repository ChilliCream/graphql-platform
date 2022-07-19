using System;
using System.Threading.Tasks;
using HotChocolate.Execution;
using Microsoft.Extensions.DependencyInjection;
using Snapshooter.Xunit;
using Xunit;

namespace HotChocolate.Types;

public class UseServiceScopeAttributeTests
{
    [Fact]
    public async Task UseServiceScope()
    {
        // arrange
        Snapshot.FullName();

        // assert
        var result = await new ServiceCollection()
            .AddScoped<Service>()
            .AddGraphQL()
            .AddQueryType<Query>()
            .ExecuteRequestAsync("{ a: scoped b: scoped }");

        // assert
        Assert.Null(result.ExpectQueryResult().Errors);
        var queryResult = Assert.IsAssignableFrom<IQueryResult>(result);
        Assert.NotEqual(queryResult.Data!["a"], queryResult.Data!["b"]);
    }

    [Fact]
    public void UseServiceScope_FieldDescriptor()
        => Assert.Throws<ArgumentNullException>(
            () => default(IObjectFieldDescriptor).UseServiceScope());

    public class Query
    {
        [UseServiceScope]
        public string GetScoped([Service] Service service)
            => service.Id;
    }

    public class Service
    {
        public string Id { get; } = Guid.NewGuid().ToString("N");
    }
}

using HotChocolate.Execution;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Data;

public class Issue5102VerificationTests
{
    [Fact]
    public async Task Can_Filter_Null_Object_Field_Through_Nested_NonNull_Id()
    {
        var executor = await new ServiceCollection()
            .AddGraphQL()
            .AddFiltering()
            .AddQueryType<Issue5102Query>()
            .BuildRequestExecutorAsync();

        var result = await executor.ExecuteAsync(
            """
            {
              devices(where: { site: { id: { eq: null } } }) {
                id
              }
            }
            """);

        var operationResult = result.ExpectOperationResult();
        Assert.Empty(operationResult.Errors ?? []);
        var json = result.ToJson();
        Assert.Contains("\"id\": 1", json);
        Assert.DoesNotContain("\"id\": 2", json);
    }

    public class Issue5102Query
    {
        [UseFiltering]
        public IQueryable<Issue5102Device> GetDevices()
            => new[]
            {
                new Issue5102Device { Id = 1, Site = null },
                new Issue5102Device { Id = 2, Site = new Issue5102Site { Id = 7 } }
            }.AsQueryable();
    }

    public class Issue5102Device
    {
        public int Id { get; set; }

        public Issue5102Site? Site { get; set; }
    }

    public class Issue5102Site
    {
        public int Id { get; set; }
    }
}

using System.Text.Json;
using HotChocolate.Execution;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Data;

public class Issue5893Tests
{
    [Fact]
    public async Task UseProjection_With_JsonDocument_Should_Not_Error()
    {
        var executor = await new ServiceCollection()
            .AddGraphQLServer()
            .AddProjections()
            .AddQueryType<Issue5893Query>()
            .BuildRequestExecutorAsync();

        var result = await executor.ExecuteAsync(
            """
            {
              tests {
                codigo
                data {
                  rootElement
                }
              }
            }
            """);

        var operationResult = result.ExpectOperationResult();
        Assert.Empty(operationResult.Errors ?? []);
    }

    public sealed class Issue5893Query
    {
        [UseProjection]
        public IQueryable<Issue5893Model> GetTests()
            => new[]
                {
                    new Issue5893Model
                    {
                        Codigo = "a",
                        Data = JsonDocument.Parse("""{"a":1}""")
                    }
                }
                .AsQueryable();
    }

    public sealed class Issue5893Model
    {
        public string Codigo { get; set; } = string.Empty;

        public JsonDocument? Data { get; set; }
    }
}

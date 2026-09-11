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
            // No @listSize on this schema, so pin the assumed list size ahead of
            // cost enforcement going live (R-DEFAULT-LIST-SIZE).
            .ModifyCostOptions(o => o.DefaultListSize = 1)
            .AddProjections()
            .AddQueryType<Issue5893Query>()
            .BuildRequestExecutorAsync(cancellationToken: TestContext.Current.CancellationToken);

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
            """,
            TestContext.Current.CancellationToken);

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

using HotChocolate.Execution;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Types.Relay;

public class Issue7346Tests
{
    [Fact]
    public async Task Invalid_Id_Does_Not_Erase_Sibling_Data()
    {
        var result =
            await new ServiceCollection()
                .AddGraphQLServer()
                .AddQueryType<ProbeQuery>()
                .AddGlobalObjectIdentification(false)
                .ExecuteRequestAsync(
                    """
                    {
                      byId(id: "invalid")
                      unrelated
                    }
                    """);

        result.ToJson().MatchInlineSnapshot(
            """
            {
              "errors": [
                {
                  "message": "The node ID string has an invalid format.",
                  "path": [
                    "byId"
                  ],
                  "extensions": {
                    "originalValue": "invalid"
                  }
                }
              ],
              "data": {
                "byId": null,
                "unrelated": "value"
              }
            }
            """);
    }

    [Fact]
    public async Task Invalid_Id_On_Skipped_Field_Does_Not_Error()
    {
        var result =
            await new ServiceCollection()
                .AddGraphQLServer()
                .AddQueryType<ProbeQuery>()
                .AddGlobalObjectIdentification(false)
                .ExecuteRequestAsync(
                    """
                    {
                      byId(id: "invalid") @skip(if: true)
                      unrelated
                    }
                    """);

        result.ToJson().MatchInlineSnapshot(
            """
            {
              "data": {
                "unrelated": "value"
              }
            }
            """);
    }

    public class ProbeQuery
    {
        public int? GetById([ID] int id) => id;

        public string GetUnrelated() => "value";
    }
}

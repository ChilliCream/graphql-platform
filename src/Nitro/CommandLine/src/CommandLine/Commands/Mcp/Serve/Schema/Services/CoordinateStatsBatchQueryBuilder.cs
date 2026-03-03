using HotChocolate.Transport;

namespace ChilliCream.Nitro.CommandLine.Commands.Mcp.Serve.Schema.Services;

/// <summary>
/// Builds a GraphQL request that fetches statistics for multiple coordinates
/// in a single request using a list variable.
/// </summary>
internal static class CoordinateStatsBatchQueryBuilder
{
    private const int MaxCoordinatesPerBatch = 50;

    private const string Query = """
        query($stageId: ID!, $coordinates: [String!]!, $from: DateTime!, $to: DateTime!) {
          node(id: $stageId) {
            ... on Stage {
              coordinates(coordinates: $coordinates) {
                coordinate
                isDeprecated
                usage(from: $from, to: $to) {
                  clientCount
                  operationCount
                  totalReferences
                  totalRequests
                  totalUsages
                  opm
                  errorRate
                  meanDuration
                  firstSeen
                  lastSeen
                }
                metrics {
                  clientUsages(from: $from, to: $to) {
                    name
                    totalRequests
                    totalOperations
                    totalVersions
                  }
                }
              }
            }
          }
        }
        """;

    public static OperationRequest Build(string stageId, string[] coordinates, DateTimeOffset from, DateTimeOffset to)
    {
        if (coordinates.Length > MaxCoordinatesPerBatch)
        {
            throw new ArgumentException(
                $"Maximum {MaxCoordinatesPerBatch} coordinates per batch.",
                nameof(coordinates));
        }

        var variables = new Dictionary<string, object?>
        {
            ["stageId"] = stageId,
            ["coordinates"] = coordinates,
            ["from"] = from.UtcDateTime.ToString("yyyy-MM-ddTHH:mm:ssZ"),
            ["to"] = to.UtcDateTime.ToString("yyyy-MM-ddTHH:mm:ssZ")
        };

        return new OperationRequest(Query, variables: variables);
    }
}

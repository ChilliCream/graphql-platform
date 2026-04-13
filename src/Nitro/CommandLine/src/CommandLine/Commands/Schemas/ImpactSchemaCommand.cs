using ChilliCream.Nitro.Client;
using ChilliCream.Nitro.Client.Coordinates;
using ChilliCream.Nitro.CommandLine.Commands.Schemas.Components;
using ChilliCream.Nitro.CommandLine.Helpers;
using ChilliCream.Nitro.CommandLine.Output;
using ChilliCream.Nitro.CommandLine.Services.Sessions;

namespace ChilliCream.Nitro.CommandLine.Commands.Schemas;

internal sealed class ImpactSchemaCommand : Command
{
    public ImpactSchemaCommand() : base("impact")
    {
        Description =
            "Show the removal impact (clients, operations, verdict) for a schema coordinate "
            + "on a stage.";

        Options.Add(Opt<AnalyticsApiIdOption>.Instance);
        Options.Add(Opt<AnalyticsStageNameOption>.Instance);
        Options.Add(Opt<CoordinateOption>.Instance);

        this.AddAnalyticsGlobalOptions();

        this.AddExamples(
            """
            schema impact \
              --coordinate "User.email" \
              --format markdown
            """);

        this.SetActionWithExceptionHandling(ExecuteAsync);
    }

    private static async Task<int> ExecuteAsync(
        ICommandServices services,
        ParseResult parseResult,
        CancellationToken cancellationToken)
    {
        var console = services.GetRequiredService<INitroConsole>();
        var sessionService = services.GetRequiredService<ISessionService>();
        var coordinatesClient = services.GetRequiredService<ICoordinatesClient>();

        parseResult.AssertHasAuthentication(sessionService);

        var context = OutputContextResolver.Resolve(parseResult, console, sessionService);
        var coordinates = parseResult.GetRequiredValue(Opt<CoordinateOption>.Instance);

        if (coordinates.Count != 1)
        {
            throw new ExitException(
                "The 'impact' command accepts exactly one '--coordinate' argument.");
        }

        var coordinate = coordinates[0];
        var formatter = new CoordinateImpactFormatter(context.Format);

        var stage = await coordinatesClient.GetCoordinateImpactAsync(
            context.ApiId,
            context.Stage,
            coordinate,
            context.From,
            context.To,
            cancellationToken);

        if (stage?.Coordinate is not { } coordinateData)
        {
            var failureEnvelope = OutputEnvelope<CoordinateImpactResult>.Failure(
                context.ApiId,
                context.Stage,
                context.ToWindow(),
                new OutputEnvelopeError(
                    ErrorCodes.CoordinateNotFound,
                    $"Coordinate '{coordinate}' was not found on stage '{context.Stage}'."));
            formatter.WriteError(console, failureEnvelope);
            return ExitCodes.Error;
        }

        var usage = new CoordinateUsageResult
        {
            Coordinate = coordinateData.Coordinate,
            IsDeprecated = coordinateData.IsDeprecated,
            TotalRequests = coordinateData.Usage.TotalRequests ?? 0L,
            ClientCount = coordinateData.Usage.ClientCount,
            OperationCount = coordinateData.Usage.OperationCount,
            FirstSeen = coordinateData.Usage.FirstSeen,
            LastSeen = coordinateData.Usage.LastSeen,
            ErrorRate = coordinateData.Usage.ErrorRate,
            MeanDuration = coordinateData.Usage.MeanDuration
        };

        var clients = new List<CoordinateClientsResultEntry>();
        var operations = new List<CoordinateOperationsResultEntry>();
        if (coordinateData.Metrics?.ClientUsages is { } clientUsages)
        {
            foreach (var clientUsage in clientUsages.OrderByDescending(x => x.TotalRequests))
            {
                var clientName = clientUsage.Name ?? clientUsage.Client?.Name ?? "unknown";

                clients.Add(new CoordinateClientsResultEntry
                {
                    Name = clientName,
                    ClientId = clientUsage.Client?.Id,
                    TotalVersions = clientUsage.TotalVersions,
                    TotalOperations = clientUsage.TotalOperations,
                    TotalRequests = clientUsage.TotalRequests
                });

                var operationNodes = clientUsage.Metrics?.Operations?.Nodes;
                if (operationNodes is null)
                {
                    continue;
                }

                foreach (var operation in operationNodes)
                {
                    operations.Add(new CoordinateOperationsResultEntry
                    {
                        OperationName = operation.OperationName,
                        Hash = operation.Hash,
                        Kind = operation.Kind?.ToString().ToUpperInvariant(),
                        ClientName = clientName,
                        OperationsPerMinute = operation.Opm,
                        TotalCount = operation.TotalCount,
                        ErrorRate = operation.ErrorRate,
                        AverageLatency = operation.AverageLatency
                    });
                }
            }
        }

        operations.Sort((a, b) => b.TotalCount.CompareTo(a.TotalCount));

        var verdict = VerdictCalculator.Compute(usage.IsDeprecated, usage.TotalRequests);

        var successEnvelope = OutputEnvelope<CoordinateImpactResult>.Success(
            context.ApiId,
            context.Stage,
            context.ToWindow(),
            new CoordinateImpactResult
            {
                Coordinate = coordinateData.Coordinate,
                IsDeprecated = coordinateData.IsDeprecated,
                Verdict = VerdictCalculator.ToSerializedString(verdict),
                Usage = usage,
                Clients = clients,
                Operations = operations
            });

        formatter.Write(console, successEnvelope);

        return ExitCodes.Success;
    }
}

using ChilliCream.Nitro.Client;
using ChilliCream.Nitro.Client.Coordinates;
using ChilliCream.Nitro.CommandLine.Commands.Schemas.Components;
using ChilliCream.Nitro.CommandLine.Helpers;
using ChilliCream.Nitro.CommandLine.Output;
using ChilliCream.Nitro.CommandLine.Services.Sessions;

namespace ChilliCream.Nitro.CommandLine.Commands.Schemas;

internal sealed class OperationsSchemaCommand : Command
{
    public OperationsSchemaCommand() : base("operations")
    {
        Description =
            "Show the per-operation usage breakdown for a schema coordinate on a stage.";

        Options.Add(Opt<AnalyticsApiIdOption>.Instance);
        Options.Add(Opt<AnalyticsStageNameOption>.Instance);
        Options.Add(Opt<CoordinateOption>.Instance);

        this.AddAnalyticsGlobalOptions();

        this.AddExamples(
            """
            schema operations \
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
                "The 'operations' command accepts exactly one '--coordinate' argument.");
        }

        var coordinate = coordinates[0];
        var formatter = new CoordinateOperationsFormatter(context.Format);

        var stage = await coordinatesClient.GetCoordinateOperationsAsync(
            context.ApiId,
            context.Stage,
            coordinate,
            context.From,
            context.To,
            cancellationToken);

        if (stage?.Coordinate is not { } coordinateData)
        {
            var failureEnvelope = OutputEnvelope<CoordinateOperationsResult>.Failure(
                context.ApiId,
                context.Stage,
                context.ToWindow(),
                new OutputEnvelopeError(
                    ErrorCodes.CoordinateNotFound,
                    $"Coordinate '{coordinate}' was not found on stage '{context.Stage}'."));
            formatter.WriteError(console, failureEnvelope);
            return ExitCodes.Error;
        }

        var entries = new List<CoordinateOperationsResultEntry>();
        if (coordinateData.Metrics?.ClientUsages is { } clientUsages)
        {
            foreach (var clientUsage in clientUsages)
            {
                var clientName = clientUsage.Name ?? clientUsage.Client?.Name ?? "unknown";
                var operations = clientUsage.Metrics?.Operations?.Nodes;
                if (operations is null)
                {
                    continue;
                }

                foreach (var operation in operations)
                {
                    entries.Add(new CoordinateOperationsResultEntry
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

        entries.Sort((a, b) => b.TotalCount.CompareTo(a.TotalCount));

        var successEnvelope = OutputEnvelope<CoordinateOperationsResult>.Success(
            context.ApiId,
            context.Stage,
            context.ToWindow(),
            new CoordinateOperationsResult
            {
                Coordinate = coordinateData.Coordinate,
                IsDeprecated = coordinateData.IsDeprecated,
                Operations = entries
            });

        formatter.Write(console, successEnvelope);

        return ExitCodes.Success;
    }
}

using ChilliCream.Nitro.Client;
using ChilliCream.Nitro.Client.Coordinates;
using ChilliCream.Nitro.CommandLine.Commands.Schemas.Components;
using ChilliCream.Nitro.CommandLine.Helpers;
using ChilliCream.Nitro.CommandLine.Output;
using ChilliCream.Nitro.CommandLine.Services.Sessions;

namespace ChilliCream.Nitro.CommandLine.Commands.Schemas;

internal sealed class ClientsSchemaCommand : Command
{
    public ClientsSchemaCommand() : base("clients")
    {
        Description =
            "Show the per-client usage breakdown for a schema coordinate on a stage.";

        Options.Add(Opt<AnalyticsApiIdOption>.Instance);
        Options.Add(Opt<AnalyticsStageNameOption>.Instance);
        Options.Add(Opt<CoordinateOption>.Instance);

        this.AddAnalyticsGlobalOptions();

        this.AddExamples(
            """
            schema clients \
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
                "The 'clients' command accepts exactly one '--coordinate' argument.");
        }

        var coordinate = coordinates[0];
        var formatter = new CoordinateClientsFormatter(context.Format);

        var stage = await coordinatesClient.GetCoordinateClientsAsync(
            context.ApiId,
            context.Stage,
            coordinate,
            context.From,
            context.To,
            cancellationToken);

        if (stage?.Coordinate is not { } coordinateData)
        {
            var failureEnvelope = OutputEnvelope<CoordinateClientsResult>.Failure(
                context.ApiId,
                context.Stage,
                context.ToWindow(),
                new OutputEnvelopeError(
                    ErrorCodes.CoordinateNotFound,
                    $"Coordinate '{coordinate}' was not found on stage '{context.Stage}'."));
            formatter.WriteError(console, failureEnvelope);
            return ExitCodes.Error;
        }

        var entries = new List<CoordinateClientsResultEntry>();
        var metrics = coordinateData.Metrics;
        if (metrics?.ClientUsages is { } clientUsages)
        {
            foreach (var usage in clientUsages.OrderByDescending(x => x.TotalRequests))
            {
                entries.Add(new CoordinateClientsResultEntry
                {
                    Name = usage.Name ?? usage.Client?.Name ?? "unknown",
                    ClientId = usage.Client?.Id,
                    TotalVersions = usage.TotalVersions,
                    TotalOperations = usage.TotalOperations,
                    TotalRequests = usage.TotalRequests
                });
            }
        }

        var successEnvelope = OutputEnvelope<CoordinateClientsResult>.Success(
            context.ApiId,
            context.Stage,
            context.ToWindow(),
            new CoordinateClientsResult
            {
                Coordinate = coordinateData.Coordinate,
                IsDeprecated = coordinateData.IsDeprecated,
                Clients = entries
            });

        formatter.Write(console, successEnvelope);

        return ExitCodes.Success;
    }
}

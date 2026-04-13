using ChilliCream.Nitro.Client;
using ChilliCream.Nitro.Client.Coordinates;
using ChilliCream.Nitro.CommandLine.Commands.Schemas.Components;
using ChilliCream.Nitro.CommandLine.Helpers;
using ChilliCream.Nitro.CommandLine.Output;
using ChilliCream.Nitro.CommandLine.Services.Sessions;

namespace ChilliCream.Nitro.CommandLine.Commands.Schemas;

internal sealed class UnusedSchemaCommand : Command
{
    private const int Limit = 100;

    public UnusedSchemaCommand() : base("unused")
    {
        Description =
            "List schema coordinates with zero usage in the requested window "
            + $"(capped at {Limit} entries).";

        Options.Add(Opt<AnalyticsApiIdOption>.Instance);
        Options.Add(Opt<AnalyticsStageNameOption>.Instance);
        Options.Add(Opt<CoordinateKindsOption>.Instance);
        Options.Add(Opt<DeprecatedOnlyOption>.Instance);

        this.AddAnalyticsGlobalOptions();

        this.AddExamples(
            """
            schema unused \
              --deprecated-only \
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
        var deprecatedOnly = parseResult.GetValue(Opt<DeprecatedOnlyOption>.Instance);
        var rawKinds = parseResult.GetValue(Opt<CoordinateKindsOption>.Instance);

        var kinds = ParseKinds(rawKinds);
        var formatter = new UnusedCoordinatesFormatter(context.Format);

        var stage = await coordinatesClient.GetUnusedCoordinatesAsync(
            context.ApiId,
            context.Stage,
            context.From,
            context.To,
            kinds,
            isDeprecated: deprecatedOnly ? true : null,
            Limit,
            cancellationToken);

        if (stage is null)
        {
            var failureEnvelope = OutputEnvelope<UnusedCoordinatesResult>.Failure(
                context.ApiId,
                context.Stage,
                context.ToWindow(),
                new OutputEnvelopeError(
                    ErrorCodes.StageNotFound,
                    $"Stage '{context.Stage}' was not found on API '{context.ApiId}'."));
            formatter.WriteError(console, failureEnvelope);
            return ExitCodes.Error;
        }

        var entries = new List<UnusedCoordinatesResultEntry>();
        var nodes = stage.Coordinates?.Nodes;
        if (nodes is not null)
        {
            foreach (var node in nodes)
            {
                var totalRequests = node.Usage.TotalRequests ?? 0L;
                if (totalRequests != 0L)
                {
                    continue;
                }

                entries.Add(new UnusedCoordinatesResultEntry
                {
                    Coordinate = node.Coordinate,
                    IsDeprecated = node.IsDeprecated,
                    TotalRequests = totalRequests,
                    ClientCount = node.Usage.ClientCount,
                    OperationCount = node.Usage.OperationCount,
                    LastSeen = node.Usage.LastSeen
                });
            }
        }

        var successEnvelope = OutputEnvelope<UnusedCoordinatesResult>.Success(
            context.ApiId,
            context.Stage,
            context.ToWindow(),
            new UnusedCoordinatesResult
            {
                Coordinates = entries,
                Limit = Limit
            });

        formatter.Write(console, successEnvelope);

        return ExitCodes.Success;
    }

    private static IReadOnlyList<CoordinateKind> ParseKinds(IReadOnlyList<string>? rawKinds)
    {
        if (rawKinds is null || rawKinds.Count == 0)
        {
            return [CoordinateKind.ObjectField, CoordinateKind.InterfaceField];
        }

        var parsed = new List<CoordinateKind>(rawKinds.Count);
        foreach (var raw in rawKinds)
        {
            if (!Enum.TryParse<CoordinateKind>(raw, ignoreCase: true, out var kind))
            {
                throw new ExitException(
                    $"Unknown coordinate kind '{raw}'. Expected one of: "
                    + string.Join(", ", Enum.GetNames<CoordinateKind>()));
            }

            parsed.Add(kind);
        }

        return parsed;
    }
}

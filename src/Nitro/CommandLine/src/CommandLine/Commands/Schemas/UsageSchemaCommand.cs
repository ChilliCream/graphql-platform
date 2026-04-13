using ChilliCream.Nitro.Client;
using ChilliCream.Nitro.Client.Coordinates;
using ChilliCream.Nitro.CommandLine.Commands.Schemas.Components;
using ChilliCream.Nitro.CommandLine.Helpers;
using ChilliCream.Nitro.CommandLine.Output;
using ChilliCream.Nitro.CommandLine.Services.Sessions;

namespace ChilliCream.Nitro.CommandLine.Commands.Schemas;

internal sealed class UsageSchemaCommand : Command
{
    public UsageSchemaCommand() : base("usage")
    {
        Description =
            "Show aggregate usage metrics for one or more schema coordinates on a stage.";

        Options.Add(Opt<AnalyticsApiIdOption>.Instance);
        Options.Add(Opt<AnalyticsStageNameOption>.Instance);
        Options.Add(Opt<CoordinateOption>.Instance);

        this.AddAnalyticsGlobalOptions();

        this.AddExamples(
            """
            schema usage \
              --coordinate "User.email" \
              --coordinate "Query.users" \
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

        if (coordinates.Count == 0)
        {
            throw new ExitException("At least one '--coordinate' is required.");
        }

        var formatter = new CoordinateUsageFormatter(context.Format);

        var tasks = new List<Task<(string Coordinate, CoordinateUsageResult? Result)>>(coordinates.Count);
        foreach (var coordinate in coordinates)
        {
            tasks.Add(LoadAsync(
                coordinatesClient,
                context,
                coordinate,
                cancellationToken));
        }

        var results = await Task.WhenAll(tasks);

        var payload = new Dictionary<string, CoordinateUsageResult>(results.Length);
        foreach (var (coordinate, result) in results)
        {
            if (result is null)
            {
                var envelope = OutputEnvelope<CoordinateUsageResultSet>.Failure(
                    context.ApiId,
                    context.Stage,
                    context.ToWindow(),
                    new OutputEnvelopeError(
                        ErrorCodes.CoordinateNotFound,
                        $"Coordinate '{coordinate}' was not found on stage '{context.Stage}'."));
                formatter.WriteError(console, envelope);
                return ExitCodes.Error;
            }

            payload[coordinate] = result;
        }

        var successEnvelope = OutputEnvelope<CoordinateUsageResultSet>.Success(
            context.ApiId,
            context.Stage,
            context.ToWindow(),
            new CoordinateUsageResultSet { Coordinates = payload });

        formatter.Write(console, successEnvelope);

        return ExitCodes.Success;
    }

    private static async Task<(string Coordinate, CoordinateUsageResult? Result)> LoadAsync(
        ICoordinatesClient client,
        OutputContext context,
        string coordinate,
        CancellationToken cancellationToken)
    {
        var stage = await client.GetCoordinateUsageAsync(
            context.ApiId,
            context.Stage,
            coordinate,
            context.From,
            context.To,
            cancellationToken);

        if (stage?.Coordinate is not { } coordinateData)
        {
            return (coordinate, null);
        }

        return (coordinate, new CoordinateUsageResult
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
        });
    }
}

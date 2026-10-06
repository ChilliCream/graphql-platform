using ChilliCream.Nitro.Client.Telemetry;
using ChilliCream.Nitro.Client.Telemetry.Models;
using ChilliCream.Nitro.CommandLine.Commands.Telemetry.Arguments;
using ChilliCream.Nitro.CommandLine.Commands.Telemetry.Options;
using ChilliCream.Nitro.CommandLine.Helpers;
using ChilliCream.Nitro.CommandLine.Results;
using ChilliCream.Nitro.CommandLine.Services.Sessions;

namespace ChilliCream.Nitro.CommandLine.Commands.Telemetry.Services;

internal sealed class ShowServiceCommand : Command
{
    public ShowServiceCommand() : base("show")
    {
        Description = "Show a telemetry service.";

        Arguments.Add(Opt<ServiceNameArgument>.Instance);
        Options.Add(Opt<TelemetryEnvironmentOption>.Instance);
        Options.Add(Opt<TelemetrySinceOption>.Instance);
        Options.Add(Opt<TelemetryUntilOption>.Instance);

        TelemetryCommandOptions.AddOptions(this);

        TelemetryCommandOptions.AddTimeRangeValidator(this);

        this.AddExamples("telemetry services show \"<name>\"");

        this.SetActionWithExceptionHandling(ExecuteAsync);
    }

    private static async Task<int> ExecuteAsync(
        ICommandServices services,
        ParseResult parseResult,
        CancellationToken cancellationToken)
    {
        var client = services.GetRequiredService<ITelemetryClient>();
        var sessionService = services.GetRequiredService<ISessionService>();
        var resultHolder = services.GetRequiredService<IResultHolder>();

        parseResult.AssertHasAuthentication(sessionService);

        var workspaceId = parseResult.GetWorkspaceId(sessionService);

        var name = parseResult.GetRequiredValue(Opt<ServiceNameArgument>.Instance);
        var environments = parseResult.GetValue(Opt<TelemetryEnvironmentOption>.Instance);
        var since = parseResult.GetValue(Opt<TelemetrySinceOption>.Instance);
        var until = parseResult.GetValue(Opt<TelemetryUntilOption>.Instance);

        var service = await client.GetServiceAsync(workspaceId, name, environments, since, until, cancellationToken);

        if (service is null)
        {
            throw ThrowHelper.Exit($"The service '{name.EscapeMarkup()}' was not found.");
        }

        var detail = ServiceDetail.From(service);

        resultHolder.SetResult(new ObjectResult(detail));

        return ExitCodes.Success;
    }

    internal sealed record ServiceDetail(
        string Name,
        IReadOnlyList<string> Environments,
        IReadOnlyList<ServiceVersionMarkerDetail> VersionMarkers)
    {
        public static ServiceDetail From(ServiceRow service)
            => new(
                service.Name,
                service.EnvironmentNames,
                service
                    .VersionMarkers.Select(static marker => new ServiceVersionMarkerDetail(
                        marker.Version,
                        marker.FirstSeenAt))
                    .ToArray());
    }

    internal sealed record ServiceVersionMarkerDetail(string Version, DateTimeOffset FirstSeenAt);
}

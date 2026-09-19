using ChilliCream.Nitro.CommandLine.Commands.Agent.Hooks.Options;
using ChilliCream.Nitro.CommandLine.Helpers;
using ChilliCream.Nitro.CommandLine.Results;
using ChilliCream.Nitro.CommandLine.Services;
using ChilliCream.Nitro.CommandLine.Services.Hook;
using ChilliCream.Nitro.CommandLine.Services.Workspace;

namespace ChilliCream.Nitro.CommandLine.Commands.Agent.Hooks.Opencode;

internal sealed class StatusOpencodeHooksCommand : Command
{
    public StatusOpencodeHooksCommand() : base("status")
    {
        Description = "Show whether Nitro's Opencode plugin is missing, current, or outdated, and report "
            + "what is known about each session's push endpoint and last ping.";

        Options.Add(Opt<OpencodeHookInstallScopeOption>.Instance);
        Options.Add(Opt<OptionalOutputFormatOption>.Instance);

        this.AddExamples("agent hooks opencode status", "agent hooks opencode status --scope project");

        this.SetActionWithExceptionHandling(ExecuteAsync);
    }

    private static async Task<int> ExecuteAsync(
        ICommandServices services,
        ParseResult parseResult,
        CancellationToken cancellationToken)
    {
        var console = services.GetRequiredService<INitroConsole>();
        var installer = services.GetRequiredService<IOpencodeHooksInstallerService>();
        var versionResolver = services.GetRequiredService<IOpencodeVersionResolver>();
        var resultHolder = services.GetRequiredService<IResultHolder>();
        var scope = parseResult.GetRequiredValue(Opt<OpencodeHookInstallScopeOption>.Instance);

        await OpencodeVersionWarning.WriteAsync(console, versionResolver, cancellationToken);
        var report = await installer.StatusAsync(scope, cancellationToken);
        var current = report.Outcome == HookStatusOutcome.Installed;

        var sessions = await ResolveOpencodeSessionsAsync(services, cancellationToken);

        if (!console.IsHumanReadable)
        {
            resultHolder.SetResult(new ObjectResult(new OpencodeHooksStatusResult(
                report.Path,
                report.Outcome.ToString(),
                current,
                sessions)));
            return current ? ExitCodes.Success : ExitCodes.Error;
        }

        console.WriteLine($"Nitro's Opencode plugin in '{report.Path.EscapeMarkup()}': {Describe(report.Outcome)}.");

        foreach (var session in sessions)
        {
            console.WriteLine(session.FormatLine());

            if (session.EndpointKind == AgentSessionEndpointKind.None)
            {
                console.MarkupLine(OpencodeEndpointGuidance.SessionRemedy.AsWarning());
            }
        }

        return current ? ExitCodes.Success : ExitCodes.Error;
    }

    /// <summary>
    /// Returns every opencode session this Nitro instance knows about, paired with its
    /// registered endpoint, reachability, and last recorded ping result and detail.
    /// Excludes sessions recorded by a different Nitro instance, and returns empty when
    /// no workspace exists yet.
    /// </summary>
    private static async Task<IReadOnlyList<OpencodeSessionStatus>> ResolveOpencodeSessionsAsync(
        ICommandServices services, CancellationToken cancellationToken)
    {
        var fileSystem = services.GetRequiredService<IFileSystem>();

        if (AgentWorkspace.Find(fileSystem, fileSystem.GetCurrentDirectory()) is null)
        {
            return [];
        }

        var sessionRegistry = services.GetRequiredService<IAgentSessionRegistry>();
        var participants = await sessionRegistry.ListParticipantsAsync(cancellationToken);

        return participants
            .Where(participant => participant.Session.Harness == AgentSessionHarness.Opencode
                && participant.State != AgentSessionState.Remote)
            .Select(OpencodeSessionStatus.From)
            .ToArray();
    }

    private static string Describe(HookStatusOutcome outcome) => outcome switch
    {
        HookStatusOutcome.Missing => "missing",
        HookStatusOutcome.Installed => "installed",
        HookStatusOutcome.Outdated => "outdated",
        _ => outcome.ToString()
    };

    public sealed record OpencodeHooksStatusResult(
        string Path, string Outcome, bool Current, IReadOnlyList<OpencodeSessionStatus> Sessions);

    /// <summary>
    /// What is known about one opencode session's push path: whether an endpoint is
    /// registered, its reachability, and the last recorded ping result and detail,
    /// verbatim. Never collapses these into a "healthy" or "working" verdict.
    /// </summary>
    public sealed record OpencodeSessionStatus(
        string SessionId,
        string? Actor,
        string EndpointKind,
        string EndpointAddress,
        string Reachability,
        string? LastPingResult,
        string? LastPingDetail)
    {
        public static OpencodeSessionStatus From(AgentSessionParticipant participant)
        {
            var session = participant.Session;

            return new OpencodeSessionStatus(
                session.SessionId,
                session.AgentName,
                session.EndpointKind,
                session.EndpointAddr,
                ComputeReachability(session),
                session.LastPingResult,
                session.LastPingDetail);
        }

        public string FormatLine()
        {
            var actor = Actor is { Length: > 0 } name ? name : "unbound";
            var endpoint = EndpointKind == AgentSessionEndpointKind.None
                ? "no endpoint registered"
                : $"{EndpointKind} {EndpointAddress}".EscapeMarkup();

            return $"  session {SessionId.EscapeMarkup()} (actor {actor.EscapeMarkup()}): "
                + $"{endpoint}; {Reachability}; {DescribeLastPing()}";
        }

        private string DescribeLastPing()
        {
            if (LastPingResult is null)
            {
                return "never pinged";
            }

            var phrase = LastPingResult switch
            {
                AgentPingResult.Ok => "last ping accepted",
                AgentPingResult.SpawnFailed => "last ping: spawn failed",
                AgentPingResult.EndpointGone => "last ping: endpoint gone",
                AgentPingResult.Timeout => "last ping: timed out",
                AgentPingResult.CapacityDropped => "last ping: capacity dropped",
                AgentPingResult.Unsupported => "last ping: unsupported",
                _ => "last ping: error"
            };

            return LastPingDetail is { Length: > 0 } detail
                ? $"{phrase} ({detail.EscapeMarkup()})"
                : phrase;
        }

        /// <summary>
        /// Returns <see cref="OpencodeSessionReachability.NoEndpoint"/> when no trusted
        /// endpoint was ever registered, otherwise maps the last recorded ping result to
        /// <see cref="OpencodeSessionReachability.ReachableAtLastPing"/>,
        /// <see cref="OpencodeSessionReachability.EndpointGoneAtLastPing"/>, or
        /// <see cref="OpencodeSessionReachability.Unknown"/>. Performs no live network
        /// probe of its own.
        /// </summary>
        private static string ComputeReachability(AgentSessionRecord session)
        {
            if (session.EndpointKind == AgentSessionEndpointKind.None)
            {
                return OpencodeSessionReachability.NoEndpoint;
            }

            return session.LastPingResult switch
            {
                AgentPingResult.Ok => OpencodeSessionReachability.ReachableAtLastPing,
                AgentPingResult.EndpointGone => OpencodeSessionReachability.EndpointGoneAtLastPing,
                _ => OpencodeSessionReachability.Unknown
            };
        }
    }

    /// <summary>
    /// The values <see cref="OpencodeSessionStatus.Reachability"/> can hold. None of them
    /// is a "working" or "healthy" verdict.
    /// </summary>
    internal static class OpencodeSessionReachability
    {
        /// <summary>No endpoint was ever registered; never pinged.</summary>
        public const string NoEndpoint = "unreachable";

        /// <summary>The last recorded ping to a registered endpoint was accepted.</summary>
        public const string ReachableAtLastPing = "reachable at last ping";

        /// <summary>
        /// The last recorded ping to a registered endpoint reported
        /// <see cref="AgentPingResult.EndpointGone"/>. This is the client's recorded
        /// verdict, not proof the endpoint itself is unreachable.
        /// </summary>
        public const string EndpointGoneAtLastPing = "endpoint gone at last ping";

        /// <summary>
        /// A registered endpoint whose last recorded ping outcome does not
        /// say whether it is reachable: never pinged, a timeout, a
        /// transport error, a capacity drop, or an unsupported endpoint
        /// kind.
        /// </summary>
        public const string Unknown = "unknown";
    }
}

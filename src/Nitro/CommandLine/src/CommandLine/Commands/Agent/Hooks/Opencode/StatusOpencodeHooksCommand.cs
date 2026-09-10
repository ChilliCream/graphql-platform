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
        Description = "Show whether Nitro's Opencode plugin is missing, current, or outdated.";

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

            if (session.Reachability == OpencodeSessionReachability.Unreachable)
            {
                console.MarkupLine(OpencodeEndpointGuidance.SessionRemedy.AsWarning());
            }
        }

        return current ? ExitCodes.Success : ExitCodes.Error;
    }

    /// <summary>
    /// Reports every opencode session this Nitro instance knows about, each
    /// paired with what is actually known about its push path: the
    /// registered endpoint, whether it is reachable (derived from the last
    /// recorded ping, never freshly probed here), and the raw last-ping
    /// result/detail. A session recorded by a different Nitro instance is
    /// excluded - this instance never pings it (see
    /// <see cref="IAgentSessionRegistry.FindLiveClaimedByAgentNameAsync"/>),
    /// so nothing here could be verified. No workspace yet (hooks can be
    /// installed and their status checked before `agent init` ever runs) is
    /// not an error: it simply has no sessions to report.
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
            .Select(participant => OpencodeSessionStatus.From(participant))
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
    /// What is actually known about one opencode session's push path, no
    /// more: whether an endpoint is registered at all, whether it is
    /// reachable, and the last recorded ping result/detail verbatim. Never
    /// collapses those into a "healthy" or "working" verdict, and never
    /// treats <see cref="AgentPingResult.Ok"/> as proof the agent received
    /// or acted on anything - the async push route this reflects only
    /// confirms the opencode server accepted and forked the run.
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
        /// <see cref="OpencodeSessionReachability.Unreachable"/> when no
        /// trusted endpoint was ever registered (the unproven placeholder,
        /// see hc-10-w61.1) or the last recorded ping failed;
        /// <see cref="OpencodeSessionReachability.Reachable"/> only when the
        /// last recorded ping succeeded; <see
        /// cref="OpencodeSessionReachability.Unknown"/> when an endpoint is
        /// registered but has never been pinged yet. Derived entirely from
        /// what was already recorded - this performs no live network probe
        /// of its own.
        /// </summary>
        private static string ComputeReachability(AgentSessionRecord session)
        {
            if (session.EndpointKind == AgentSessionEndpointKind.None)
            {
                return OpencodeSessionReachability.Unreachable;
            }

            return session.LastPingResult switch
            {
                null => OpencodeSessionReachability.Unknown,
                AgentPingResult.Ok => OpencodeSessionReachability.Reachable,
                _ => OpencodeSessionReachability.Unreachable
            };
        }
    }

    internal static class OpencodeSessionReachability
    {
        public const string Reachable = "reachable";
        public const string Unreachable = "unreachable";
        public const string Unknown = "unknown";
    }
}

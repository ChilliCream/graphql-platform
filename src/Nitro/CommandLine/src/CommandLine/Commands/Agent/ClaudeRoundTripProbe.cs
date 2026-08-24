using ChilliCream.Nitro.CommandLine.Services.Mail;
using ChilliCream.Nitro.CommandLine.Services.Memory;
using ChilliCream.Nitro.CommandLine.Services.Notify;
using ChilliCream.Nitro.CommandLine.Services.Workspace;

namespace ChilliCream.Nitro.CommandLine.Commands.Agent;

/// <summary>
/// The live round-trip probe behind <c>doctor --probe claude</c>: registers a
/// scratch actor, self-claims this process's own live Claude Code ancestor
/// session under it, sends that actor a synthetic message, and verifies the
/// delivery ledger actually reserves it on both the digest and gate channels
/// (the same reservation the Claude Code hook adapter makes for a real
/// turn). The scratch claim is always torn down afterward, whatever the
/// outcome, so a probe run leaves no session behind. Never spawns a process
/// or touches a socket: every step is a local database or file operation, so
/// it runs under normal sandboxing.
/// </summary>
internal sealed class ClaudeRoundTripProbe(
    IAgentRegistry agentRegistry,
    IAgentSessionRegistry sessionRegistry,
    IMailStore mailStore,
    ISessionDeliveryLedger ledger,
    TimeProvider timeProvider)
{
    /// <summary>
    /// Runs the full probe. Throws <see cref="ExitException"/> (with an
    /// actionable message, never a raw stack trace) when no live Claude Code
    /// ancestor session can be found for this process, or when one exists
    /// but is already explicitly claimed by a different actor - the same
    /// failure modes <see cref="IAgentSessionRegistry.SelfClaimAsync"/>
    /// reports for <c>agent session claim</c>.
    /// </summary>
    public async Task<ClaudeProbeResult> RunAsync(CancellationToken cancellationToken)
    {
        var now = timeProvider.GetUtcNow();
        var scratchActor = $"doctor-probe-{MemoryId.New(now)}";

        await agentRegistry.RegisterAsync(scratchActor, role: "", client: "doctor-probe", cancellationToken);

        var claim = await sessionRegistry.SelfClaimAsync(scratchActor, forceRebind: false, cancellationToken);
        var session = claim.Session;
        var generation = new AgentSessionGeneration(
            session.Harness, session.SessionId, session.Host, session.Pid, session.ProcStart);

        try
        {
            var message = await mailStore.SendMessageAsync(
                new MailMessageCreation
                {
                    Sender = scratchActor,
                    Subject = "nitro doctor round-trip probe",
                    Body = "Synthetic probe message from `nitro agent doctor --probe claude`. "
                        + "Safe to ignore; already archived by the probe.",
                    To = [scratchActor]
                },
                cancellationToken);

            // Mirrors the exact reservation ClaudeHookHandler makes on
            // UserPromptSubmit (digest) and Stop (gate): the delivery ledger
            // is at-most-once PER CHANNEL, so both independently claim the
            // same message id.
            var digestReserved = await ledger.ReserveAsync(
                generation.Harness, generation.SessionId, [message.Id],
                AgentSessionChannel.Digest, now, cancellationToken);
            var gateReserved = await ledger.ReserveAsync(
                generation.Harness, generation.SessionId, [message.Id],
                AgentSessionChannel.Gate, now, cancellationToken);

            var pingResult = await PingAsync(session, generation, cancellationToken);

            // Cleans up the scratch actor's own mailbox: the probe message
            // is real mail (it went through the same SendMessageAsync path
            // and ledger reservations a production turn would), so leaving
            // it unread would misreport this scratch actor's own unread
            // count on a later run.
            await mailStore.ArchiveAsync([message.Id], scratchActor, cancellationToken);

            var digestClaimed = digestReserved.Contains(message.Id);
            var gateClaimed = gateReserved.Contains(message.Id);

            return new ClaudeProbeResult(
                scratchActor,
                generation.Harness,
                generation.SessionId,
                session.EndpointKind,
                message.Id,
                digestClaimed,
                gateClaimed,
                pingResult,
                digestClaimed && gateClaimed);
        }
        finally
        {
            // Always undo the claim this probe made: a real hook's next
            // SessionStart recreates the row from scratch, exactly as if
            // this probe had never run.
            await sessionRegistry.EndAsync(generation, cancellationToken);
        }
    }

    /// <summary>
    /// Fires the same best-effort ping <c>nitro agent ping</c> does for a
    /// single session, minus the lease machinery a <c>codex-thread</c>
    /// attempt would need (this probe's session is always
    /// <c>claude-code</c>, so that branch never applies). Its outcome is
    /// informational only - never part of <see cref="ClaudeProbeResult.Success"/> -
    /// because the delivery ledger claim, not the ping, is the round trip
    /// this probe's binding ruling requires.
    /// </summary>
    private async Task<string> PingAsync(
        AgentSessionRecord session, AgentSessionGeneration generation, CancellationToken cancellationToken)
    {
        if (session.EndpointKind == AgentSessionEndpointKind.None)
        {
            return "skipped-no-endpoint";
        }

        try
        {
            var now = timeProvider.GetUtcNow();
            var attemptId = MemoryId.New(now);

            var cooldownClaimed = await sessionRegistry.TryClaimPingCooldownAsync(
                session, attemptId, now, PingPolicy.Cooldown, cancellationToken);

            if (!cooldownClaimed)
            {
                return "skipped-cooldown";
            }

            if (session.EndpointKind != AgentSessionEndpointKind.CodexThread)
            {
                // Every claude-peer endpoint lands here: there is no socket
                // transport yet (spike perles-net-k3j.19 is on hold), so the
                // notifier itself has no transport for this endpoint kind -
                // reported as 'unsupported', matching `agent ping`'s own
                // behavior for any non-codex-thread endpoint, never a failure.
                await sessionRegistry.WritePingResultAsync(
                    generation.Harness, generation.SessionId, attemptId,
                    AgentPingResult.Unsupported, null, cancellationToken);
                return AgentPingResult.Unsupported;
            }

            // A live codex-thread transport call needs IPingSessionExecutor;
            // out of scope for `--probe claude` (this session is never
            // codex-thread), kept only so this switch stays exhaustive.
            return "not-attempted";
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            // The ping leg is informational only (see the summary above): a
            // failure here - e.g. a pre-existing workspace database whose
            // last_ping_result CHECK constraint predates a since-added
            // result value - must never sink the digest/gate claim this
            // probe actually exists to verify, and must never surface as a
            // raw database exception either.
            return $"error ({exception.GetType().Name})";
        }
    }
}

/// <summary>
/// The outcome of <see cref="ClaudeRoundTripProbe.RunAsync"/>. <see cref="Success"/>
/// reflects only the digest and gate delivery-ledger claims; <see cref="PingResult"/>
/// is reported separately and never fails the probe on its own.
/// </summary>
internal sealed record ClaudeProbeResult(
    string ScratchActor,
    string Harness,
    string SessionId,
    string EndpointKind,
    string MessageId,
    bool DigestLedgerClaimed,
    bool GateLedgerClaimed,
    string PingResult,
    bool Success);

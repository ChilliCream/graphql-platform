namespace ChilliCream.Nitro.CommandLine.Commands.Agent.Options;

/// <summary>
/// Opts into the explicit, live round-trip probe for one harness: register a
/// scratch actor, claim this process's live session, send it mail, verify
/// the digest and gate delivery-ledger claims, fire the best-effort ping,
/// then clean up the scratch claim. Unlike every other doctor check, this
/// one is not free: it requires a live claimed session for the given
/// harness. Only "claude" is wired today.
/// </summary>
internal sealed class ProbeHarnessDoctorOption : Option<string?>
{
    public ProbeHarnessDoctorOption() : base("--probe")
    {
        Description = "Run the live round-trip probe for a harness (register a scratch actor, "
            + "send mail, verify the digest/gate ledger claims, fire the ping): 'claude'. "
            + "Requires a live claimed session; not part of the default, free checks.";
        Required = false;
        AcceptOnlyFromAmong("claude");
    }
}

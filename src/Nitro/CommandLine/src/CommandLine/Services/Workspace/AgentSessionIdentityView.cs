namespace ChilliCream.Nitro.CommandLine.Services.Workspace;

internal sealed record AgentSessionIdentityView(
    AgentSessionIdentityRecord Identity,
    AgentSessionParticipant? Participant)
{
    public string State => Participant?.State ?? "offline";

    public bool Online => Participant?.State == AgentSessionState.Online;

    public DateTimeOffset LastSeenAt
        => Participant?.Session.LastBeatAt ?? DateTimeOffset.Parse(Identity.LastSeenAt);
}

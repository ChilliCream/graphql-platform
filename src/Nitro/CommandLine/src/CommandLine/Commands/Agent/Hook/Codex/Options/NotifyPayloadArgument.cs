namespace ChilliCream.Nitro.CommandLine.Commands.Agent.Hook.Codex.Options;

/// <summary>
/// The single JSON argument Codex CLI's <c>notify</c> mechanism passes as
/// argv[1], not stdin like a <c>hooks.json</c> event.
/// </summary>
internal sealed class NotifyPayloadArgument : Argument<string>
{
    public NotifyPayloadArgument() : base("payload")
    {
        Description = "The notify event's JSON payload, exactly as Codex CLI passes it as a single argument.";
    }
}

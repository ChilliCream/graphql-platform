namespace ChilliCream.Nitro.CommandLine.Commands.Agent.Options;

internal sealed class ForceActorTakeoverOption : Option<bool>
{
    public ForceActorTakeoverOption() : base("--force")
    {
        Description = "Take the actor from another session and remove that session";
    }
}

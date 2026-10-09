namespace ChilliCream.Nitro.CommandLine.Commands.Agent.Options;

internal sealed class TakeoverReasonOption : Option<string>
{
    public TakeoverReasonOption() : base("--reason")
    {
        Description = "The reason recorded for the takeover";
    }
}

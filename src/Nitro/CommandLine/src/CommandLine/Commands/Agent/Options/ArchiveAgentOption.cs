namespace ChilliCream.Nitro.CommandLine.Commands.Agent.Options;

internal sealed class ArchiveAgentOption : Option<string>
{
    public ArchiveAgentOption() : base("--archive")
    {
        Description = "The path of the workspace archive (.zip), relative to the current directory";
        Required = true;
    }
}

namespace ChilliCream.Nitro.CommandLine.Commands.PersonalAccessTokens;

internal sealed class PersonalAccessTokenCommand : Command
{
    public PersonalAccessTokenCommand() : base("pat")
    {
        Description = "Manage personal access tokens";

        this.AddNitroCloudDefaultOptions();

        AddCommand(new CreatePersonalAccessTokenCommand());
        AddCommand(new RevokePersonalAccessTokenCommand());
        AddCommand(new ListPersonalAccessTokenCommand());
    }
}

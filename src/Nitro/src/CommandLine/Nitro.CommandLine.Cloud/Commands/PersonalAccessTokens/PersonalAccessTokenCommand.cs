namespace ChilliCream.Nitro.CLI.Commands.PersonalAccessToken;

internal sealed class PersonalAccessTokenCommand : Command
{
    public PersonalAccessTokenCommand() : base("pat")
    {
        Description = "Use this command to manage personal access tokens";

        AddCommand(new CreatePersonalAccessTokenCommand());
        AddCommand(new RevokePersonalAccessTokenCommand());
        AddCommand(new ListPersonalAccessTokenCommand());
    }
}

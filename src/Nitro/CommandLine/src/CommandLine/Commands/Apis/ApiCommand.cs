namespace ChilliCream.Nitro.CommandLine.Commands.Apis;

internal sealed class ApiCommand : Command
{
    public ApiCommand() : base("api")
    {
        Description = "Manage APIs";

        this.AddNitroCloudDefaultOptions();

        AddCommand(new CreateApiCommand());
        AddCommand(new DeleteApiCommand());
        AddCommand(new ListApiCommand());
        AddCommand(new ShowApiCommand());
        AddCommand(new SetApiSettingsApiCommand());
    }
}

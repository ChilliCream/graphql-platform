namespace ChilliCream.Nitro.CommandLine.Commands.Mocks;

internal sealed class MockCommand : Command
{
    public MockCommand() : base("mock")
    {
        Description = "Create, Update and Delete Mocks";
        IsHidden = true;

        this.AddNitroCloudDefaultOptions();

        AddCommand(new CreateMockCommand());
        AddCommand(new ListMockCommand());
        AddCommand(new UpdateMockCommand());
    }
}

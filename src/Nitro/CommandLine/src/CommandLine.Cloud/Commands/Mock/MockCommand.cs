namespace ChilliCream.Nitro.CLI.Commands.Mock;

internal sealed class MockCommand : Command
{
    public MockCommand() : base("mock")
    {
        Description = "Create, Update and Delete Mocks";
        IsHidden = true;

        AddCommand(new CreateMockCommand());
        AddCommand(new ListMockCommand());
        AddCommand(new UpdateMockCommand());
    }
}

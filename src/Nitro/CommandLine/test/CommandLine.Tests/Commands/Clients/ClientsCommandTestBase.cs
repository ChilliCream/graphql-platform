namespace ChilliCream.Nitro.CommandLine.Tests.Commands.Clients;

public abstract class ClientsCommandTestBase(NitroCommandFixture fixture) : CommandTestBase(fixture)
{
    protected const string ClientId = "client-1";
    protected const string OperationsFile = "operations.json";

    protected void SetupOperationsFile()
    {
        SetupFile(OperationsFile, "{}");
    }
}

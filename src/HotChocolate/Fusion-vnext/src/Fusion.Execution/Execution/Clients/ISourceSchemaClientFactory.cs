namespace HotChocolate.Fusion.Execution.Clients;

public interface ISourceSchemaClientFactory
{
    ISourceSchemaClient CreateClient(string name);
}

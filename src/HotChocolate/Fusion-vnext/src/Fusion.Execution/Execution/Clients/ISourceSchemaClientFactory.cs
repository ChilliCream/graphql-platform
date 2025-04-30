namespace HotChocolate.Fusion.Execution.Clients;

public interface ISourceSchemaClientScope
{
    ISourceSchemaClient GetClient(string name);
}

using HotChocolate.Fusion.Types;

namespace HotChocolate.Fusion.Execution.Clients;

internal sealed class DefaultSourceSchemaClientScopeFactory : ISourceSchemaClientScopeFactory
{
    private readonly ISourceSchemaClientFactory[] _clientFactories;

    public DefaultSourceSchemaClientScopeFactory(ISourceSchemaClientFactory[] clientFactories)
    {
        ArgumentNullException.ThrowIfNull(clientFactories);

        _clientFactories = clientFactories;
    }

    public ISourceSchemaClientScope CreateScope(ISchemaDefinition schemaDefinition)
    {
        ArgumentNullException.ThrowIfNull(schemaDefinition);

        if (schemaDefinition is not FusionSchemaDefinition schema)
        {
            throw new ArgumentException(
                "The schema definition must be a FusionSchemaDefinition.",
                nameof(schemaDefinition));
        }

        return new DefaultSourceSchemaClientScope(schema, _clientFactories);
    }
}

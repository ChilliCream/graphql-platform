using Microsoft.Extensions.Options;

namespace HotChocolate.AspNetCore;

/// <summary>
/// Automatically enables batching for schemas that have been configured as source schemas.
/// </summary>
internal sealed class SourceSchemaServerOptionsPostConfigure(
    IEnumerable<SourceSchemaRegistration> registrations)
    : IPostConfigureOptions<GraphQLServerOptions>
{
    private readonly HashSet<string> _sourceSchemaNames =
        new(registrations.Select(r => r.SchemaName), StringComparer.Ordinal);

    public void PostConfigure(string? name, GraphQLServerOptions options)
    {
        if (name is not null && _sourceSchemaNames.Contains(name))
        {
            options.Batching |= AllowedBatching.VariableBatching | AllowedBatching.RequestBatching;
        }
    }
}

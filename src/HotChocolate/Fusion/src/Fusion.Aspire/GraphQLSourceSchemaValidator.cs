using HotChocolate.Fusion.Logging;

namespace HotChocolate.Fusion.Aspire;

internal static class GraphQLSourceSchemaValidator
{
    public static void Validate(
        string resourceName,
        SchemaEndpointConfiguration configuration,
        string sourceText,
        string? extensionsSourceText = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(resourceName);

        if (string.IsNullOrWhiteSpace(sourceText))
        {
            throw new InvalidOperationException(
                $"Schema export for resource '{resourceName}' produced empty GraphQL SDL.");
        }

        if (extensionsSourceText is not null
            && string.IsNullOrWhiteSpace(extensionsSourceText))
        {
            throw new InvalidOperationException(
                $"Schema extensions for resource '{resourceName}' contain empty GraphQL SDL.");
        }

        var log = new CompositionLog();
        var result = new SourceSchemaParser(
            new SourceSchemaText(
                configuration.SourceSchemaName,
                sourceText,
                extensionsSourceText),
            log,
            isApolloFederationV1:
                configuration.ApolloFederationVersion is ApolloFederationVersion.Version1)
            .Parse();

        if (result.IsFailure)
        {
            throw new InvalidOperationException(
                $"Schema for resource '{resourceName}' is not valid GraphQL SDL.");
        }
    }
}

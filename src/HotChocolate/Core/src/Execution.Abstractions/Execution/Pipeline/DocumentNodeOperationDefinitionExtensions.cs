using System.Diagnostics.CodeAnalysis;
using HotChocolate.Language;

namespace HotChocolate.Execution.Pipeline;

/// <summary>
/// Provides a lookup for the operation definition a request selects out of a parsed operation
/// document, shared by diagnostics spans and listeners that need the operation type and name
/// before the document is compiled, planned, or normalized.
/// </summary>
public static class DocumentNodeOperationDefinitionExtensions
{
    /// <summary>
    /// Tries to find the operation definition that <paramref name="operationName"/> selects in
    /// <paramref name="document"/>. When <paramref name="operationName"/> is
    /// <see langword="null"/> or empty, the document's single anonymous operation is returned;
    /// more than one anonymous candidate makes the selection ambiguous and returns
    /// <see langword="false"/>.
    /// </summary>
    /// <param name="document">
    /// The parsed operation document to search.
    /// </param>
    /// <param name="operationName">
    /// The name of the operation to select, or <see langword="null"/> to select the document's
    /// single anonymous operation.
    /// </param>
    /// <param name="operationDefinition">
    /// The selected operation definition.
    /// </param>
    /// <returns>
    /// <see langword="true"/> if an operation definition was selected; otherwise,
    /// <see langword="false"/>.
    /// </returns>
    public static bool TryGetOperationDefinition(
        this DocumentNode document,
        string? operationName,
        [NotNullWhen(true)] out OperationDefinitionNode? operationDefinition)
    {
        ArgumentNullException.ThrowIfNull(document);

        OperationDefinitionNode? match = null;

        foreach (var definition in document.Definitions)
        {
            if (definition is not OperationDefinitionNode operation)
            {
                continue;
            }

            if (string.IsNullOrEmpty(operationName))
            {
                // More than one anonymous candidate makes the request itself ambiguous;
                // there is no single operation left to report.
                if (match is not null)
                {
                    operationDefinition = null;
                    return false;
                }

                match = operation;
                continue;
            }

            if (operation.Name is { } name && name.Value.Equals(operationName, StringComparison.Ordinal))
            {
                operationDefinition = operation;
                return true;
            }
        }

        operationDefinition = match;
        return match is not null;
    }
}

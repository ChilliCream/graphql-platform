using HotChocolate.Language;

namespace HotChocolate.Fusion.Execution.Nodes;

/// <summary>
/// Represents a GraphQL operation definition source text that needs parsing before it can be executed.
/// </summary>
/// <param name="Name">
/// Gets the type of the operation name.
/// </param>
/// <param name="Type">
/// Gets the type of the operation.
/// </param>
/// <param name="SourceText">
/// Gets the GraphQL operation document source text.
/// </param>
/// <param name="Hash">
/// Gets the SHA256 hash of the operation source text as hex string.
/// </param>
public readonly record struct OperationSourceText(
    string Name,
    OperationType Type,
    string SourceText,
    string Hash);

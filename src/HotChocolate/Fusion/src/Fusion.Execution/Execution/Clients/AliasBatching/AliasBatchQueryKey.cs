using HotChocolate.Language;

namespace HotChocolate.Fusion.Execution.Clients.AliasBatching;

/// <summary>
/// The inputs a merged operation of a batched request is built from.
/// </summary>
internal readonly ref struct AliasBatchQueryKey(
    ReadOnlySpan<AliasBatchItem> items,
    List<Utf8VariableDefinitionNode> sharedVariables,
    string? operationName,
    string nameSafeShortHash,
    ulong compositionHash)
{
    public ReadOnlySpan<AliasBatchItem> Items { get; } = items;

    public List<Utf8VariableDefinitionNode> SharedVariables { get; } = sharedVariables;

    public string? OperationName { get; } = operationName;

    public string NameSafeShortHash { get; } = nameSafeShortHash;

    public ulong CompositionHash { get; } = compositionHash;
}

namespace ChilliCream.Nitro.Client;

/// <summary>
/// Represents a validation task update.
/// </summary>
public sealed record ValidationUpdate(
    ValidationUpdateKind Kind,
    IReadOnlyList<MutationError>? Errors = null);

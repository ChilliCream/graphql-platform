namespace Mocha.Analyzers;

/// <summary>
/// Represents a discovered <c>Saga&lt;TState&gt;</c> subclass for source-generated registration.
/// </summary>
/// <param name="SagaTypeName">The fully qualified type name of the saga class.</param>
/// <param name="SagaNamespace">The namespace containing the saga class.</param>
/// <param name="StateTypeName">The fully qualified type name of the saga state type (<c>TState</c>).</param>
public sealed record SagaInfo(
    string SagaTypeName,
    string SagaNamespace,
    string StateTypeName) : SyntaxInfo
{
    /// <inheritdoc />
    public override string OrderByKey => $"Saga:{SagaTypeName}";
}

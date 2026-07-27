namespace Mocha.Analyzers;

/// <summary>
/// Represents a discovered <c>Saga&lt;TState&gt;</c> subclass for source-generated registration.
/// </summary>
/// <param name="SagaTypeName">The fully qualified type name of the saga class.</param>
/// <param name="SagaNamespace">The namespace containing the saga class.</param>
/// <param name="StateTypeName">The fully qualified type name of the saga state type (<c>TState</c>).</param>
/// <param name="XmlDocumentation">The XML documentation captured from the saga declaration.</param>
/// <param name="Location">
/// The equatable source location of the saga type declaration, or <see langword="null"/> if unavailable.
/// </param>
public sealed record SagaInfo(
    string SagaTypeName,
    string SagaNamespace,
    string StateTypeName,
    string? XmlDocumentation,
    LocationInfo? Location) : SyntaxInfo
{
    /// <inheritdoc />
    public override string OrderByKey => $"Saga:{SagaTypeName}";
}

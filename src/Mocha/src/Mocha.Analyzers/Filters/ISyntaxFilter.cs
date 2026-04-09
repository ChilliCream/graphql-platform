using Microsoft.CodeAnalysis;

namespace Mocha.Analyzers;

/// <summary>
/// Defines a filter that determines whether a <see cref="SyntaxNode"/> is a candidate for further inspection
/// during source generation.
/// </summary>
public interface ISyntaxFilter
{
    /// <summary>
    /// Gets a value indicating whether the specified <paramref name="node"/> matches this filter's criteria.
    /// </summary>
    /// <param name="node">The syntax node to evaluate.</param>
    /// <returns><see langword="true"/> if the node matches; otherwise, <see langword="false"/>.</returns>
    bool IsMatch(SyntaxNode node);
}

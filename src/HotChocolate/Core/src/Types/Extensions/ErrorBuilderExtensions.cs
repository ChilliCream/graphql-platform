using HotChocolate.Execution.Processing;

namespace HotChocolate;

/// <summary>
/// Provides extension methods for <see cref="ErrorBuilder"/>.
/// </summary>
public static class ErrorBuilderExtensions
{
    /// <summary>
    /// Adds all syntax node locations from a selection to the error builder.
    /// </summary>
    /// <param name="errorBuilder">
    /// The error builder to which locations will be added.
    /// </param>
    /// <param name="selection">
    /// The selection containing the syntax nodes whose locations will be added to the error.
    /// </param>
    /// <returns>
    /// The <paramref name="errorBuilder"/> for method chaining.
    /// </returns>
    public static ErrorBuilder AddLocations(this ErrorBuilder errorBuilder, Selection selection)
    {
        ArgumentNullException.ThrowIfNull(selection);

        foreach (var syntaxNode in selection.SyntaxNodes)
        {
            errorBuilder.AddLocation(syntaxNode.Node);
        }

        return errorBuilder;
    }
}

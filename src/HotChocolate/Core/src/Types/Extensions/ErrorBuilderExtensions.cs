using System.Diagnostics;
using HotChocolate.Execution.Processing;

namespace HotChocolate;

internal static class ErrorBuilderExtensions
{
    public static ErrorBuilder AddLocations(this ErrorBuilder errorBuilder, Selection selection)
    {
        Debug.Assert(errorBuilder is not null);
        Debug.Assert(selection is not null);

        foreach (var syntaxNode in selection.SyntaxNodes)
        {
            errorBuilder.AddLocation(syntaxNode.Node);
        }

        return errorBuilder;
    }
}

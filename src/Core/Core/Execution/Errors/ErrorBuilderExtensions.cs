using System;
using HotChocolate.Language;

namespace HotChocolate.Execution
{
    public static class ErrorBuilderExtensions
    {
        public static IErrorBuilder AddLocation(
            this IErrorBuilder builder,
            ISyntaxNode syntaxNode)
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            if (syntaxNode == null)
            {
                throw new ArgumentNullException(nameof(syntaxNode));
            }

            if (syntaxNode.Location != null)
            {
                return builder.AddLocation(
                    syntaxNode.Location.Line,
                    syntaxNode.Location.Column);
            }
            return builder;
        }
    }
}

using System.Globalization;
using System;
using HotChocolate.Language;

namespace HotChocolate
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

        public static IErrorBuilder SetMessage(
            this IErrorBuilder builder,
            string format,
            params object[] args) =>
            builder.SetMessage(string.Format(
                CultureInfo.InvariantCulture,
                format,
                args));
    }
}

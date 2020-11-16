using System.Globalization;
using System;
using HotChocolate.Language;

#nullable enable

namespace HotChocolate
{
    public static class ErrorBuilderExtensions
    {
        public static IErrorBuilder AddLocation(
            this IErrorBuilder builder,
            ISyntaxNode? syntaxNode)
        {
            if (builder is null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            if (syntaxNode is { Location: { } })
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

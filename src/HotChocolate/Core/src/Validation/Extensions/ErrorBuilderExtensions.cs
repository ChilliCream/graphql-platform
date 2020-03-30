namespace HotChocolate.Validation
{
    internal static class ErrorBuilderExtensions
    {
        public static IErrorBuilder SpecifiedBy(
            this IErrorBuilder errorBuilder,
            string section) =>
            errorBuilder.SetExtension(
                "specifiedBy",
                "http://spec.graphql.org/June2018/#" + section);
    }
}

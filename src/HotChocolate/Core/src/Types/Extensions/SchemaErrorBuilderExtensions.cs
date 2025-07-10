using System.Globalization;

namespace HotChocolate;

public static class SchemaErrorBuilderExtensions
{
    public static SchemaErrorBuilder SetMessage(
        this SchemaErrorBuilder builder,
        string format,
        params object[] args)
    {
        ArgumentNullException.ThrowIfNull(builder);

        return builder.SetMessage(string.Format(
            CultureInfo.InvariantCulture,
            format,
            args));
    }

    public static SchemaErrorBuilder SpecifiedBy(
        this SchemaErrorBuilder errorBuilder,
        string section,
        bool condition = true,
        bool isDraft = false)
    {
        if (condition)
        {
            errorBuilder.SetExtension(
                "specifiedBy",
                $"https://spec.graphql.org/{(isDraft ? "draft" : "October2021")}/#{section}");
        }

        return errorBuilder;
    }
}

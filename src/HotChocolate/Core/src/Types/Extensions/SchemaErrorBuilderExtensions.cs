using System.Globalization;

namespace HotChocolate;

public static class SchemaErrorBuilderExtensions
{
    public static SchemaErrorBuilder SetMessage(
        this SchemaErrorBuilder builder,
        string format,
        params object[] args)
    {
        if (builder is null)
        {
            throw new ArgumentNullException(nameof(builder));
        }

        return builder.SetMessage(string.Format(
            CultureInfo.InvariantCulture,
            format,
            args));
    }

    public static SchemaErrorBuilder SpecifiedBy(
        this SchemaErrorBuilder errorBuilder,
        string section,
        bool condition = true)
    {
        if (condition)
        {
            errorBuilder.SetExtension(
                "specifiedBy",
                "https://spec.graphql.org/October2021/#" + section);
        }

        return errorBuilder;
    }
}

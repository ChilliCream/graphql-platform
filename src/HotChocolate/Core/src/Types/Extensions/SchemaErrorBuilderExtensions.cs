using System;
using System.Globalization;

namespace HotChocolate
{
    public static class SchemaErrorBuilderExtensions
    {
        public static ISchemaErrorBuilder SetMessage(
            this ISchemaErrorBuilder builder,
            string format,
            params object[] args)
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            return builder.SetMessage(string.Format(
                CultureInfo.InvariantCulture,
                format,
                args));
        }
    }

}

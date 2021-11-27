using System;

namespace HotChocolate;

public static class ErrorSchemaBuilderExtensions
{
    public static ISchemaBuilder AddErrorInterfaceType<T>(this ISchemaBuilder schemaBuilder) =>
        schemaBuilder.AddErrorInterfaceType(typeof(T));

    public static ISchemaBuilder AddErrorInterfaceType(
        this ISchemaBuilder schemaBuilder,
        Type type) =>
        schemaBuilder.SetContextData(ErrorSchemaContextData.ErrorType, type);
}

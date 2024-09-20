namespace HotChocolate.Types;

/// <summary>
/// Common extensions to configure the schema with a common error interface
/// </summary>
internal static class ErrorSchemaBuilderExtensions
{
    /// <summary>
    /// Defines the common interface that all errors implement.
    /// To specify the interface you can either provide a interface runtime type or a HotChocolate
    /// interface schema type.
    ///
    /// This has to be used together with <see cref="ErrorAttribute"/>  or
    /// <see cref="ErrorObjectFieldDescriptorExtensions.Error"/>
    /// </summary>
    /// <param name="schemaBuilder">
    /// The schema builder
    /// </param>
    /// <typeparam name="T">
    /// The type that is used as the common interface
    /// </typeparam>
    /// <returns>j
    /// The schema builder
    /// </returns>
    public static ISchemaBuilder AddErrorInterfaceType<T>(this ISchemaBuilder schemaBuilder)
        => schemaBuilder.AddErrorInterfaceType(typeof(T));

    /// <summary>
    /// Defines the common interface that all errors implement.
    /// To specify the interface you can either provide a interface runtime type or a HotChocolate
    /// interface schema type.
    ///
    /// This has to be used together with <see cref="ErrorAttribute"/>  or
    /// <see cref="ErrorObjectFieldDescriptorExtensions.Error"/>
    /// </summary>
    /// <param name="schemaBuilder">
    /// The schema builder
    /// </param>
    /// <param name="type">
    /// The type that is used as the common interface
    /// </param>
    /// <returns>
    /// The schema builder
    /// </returns>
    public static ISchemaBuilder AddErrorInterfaceType(
        this ISchemaBuilder schemaBuilder,
        Type type)
        => schemaBuilder.SetContextData(ErrorContextDataKeys.ErrorType, type);
}

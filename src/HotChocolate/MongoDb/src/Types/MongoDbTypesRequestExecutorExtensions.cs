using HotChocolate;
using HotChocolate.Execution.Configuration;
using HotChocolate.Types.MongoDb;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Common extension of <see cref="ISchemaBuilder"/> for MongoDb types
/// </summary>
public static class MongoDbTypesRequestExecutorBuilderExtensions
{
    /// <summary>
    /// Registers <see cref="ObjectIdType"/> on the schema
    /// </summary>
    /// <param name="builder">The request executor builder</param>
    /// <returns>The request executor builder</returns>
    public static IRequestExecutorBuilder AddObjectIdType(this IRequestExecutorBuilder builder) =>
        builder.ConfigureSchema(x => x.AddObjectIdType());

    /// <summary>
    /// Registers <see cref="BsonType"/> and binds the BSON subtypes to the scalar
    /// </summary>
    /// <param name="builder">The request executor builder</param>
    /// <returns>The request executor builder</returns>
    public static IRequestExecutorBuilder AddBsonType(this IRequestExecutorBuilder builder) =>
        builder.ConfigureSchema(x => x.AddBsonType());
}

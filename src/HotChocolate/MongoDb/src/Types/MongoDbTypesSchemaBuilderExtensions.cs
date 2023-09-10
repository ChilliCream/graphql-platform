using HotChocolate.Types.MongoDb;
using MongoDB.Bson;
using BsonType = HotChocolate.Types.MongoDb.BsonType;

namespace HotChocolate;

/// <summary>
/// Common extension of <see cref="ISchemaBuilder"/> for MongoDb types
/// </summary>
public static class MongoDbTypesSchemaBuilderExtensions
{
    /// <summary>
    /// Registers <see cref="ObjectIdType"/> on the schema
    /// </summary>
    /// <param name="builder">The schema builder</param>
    /// <returns>The schema builder</returns>
    public static ISchemaBuilder AddObjectIdType(this ISchemaBuilder builder)
    {
        builder.AddType<ObjectIdType>();
        return builder;
    }

    /// <summary>
    /// Registers <see cref="BsonType"/> and binds the BSON subtypes to the scalar
    /// </summary>
    /// <param name="builder">The schema builder</param>
    /// <returns>The schema builder</returns>
    public static ISchemaBuilder AddBsonType(this ISchemaBuilder builder)
    {
        builder.AddType<BsonType>();
        builder.BindRuntimeType(typeof(BsonValue), typeof(BsonType));
        builder.BindRuntimeType(typeof(BsonArray), typeof(BsonType));
        builder.BindRuntimeType(typeof(BsonDocument), typeof(BsonType));
        builder.BindRuntimeType(typeof(BsonBoolean), typeof(BsonType));
        builder.BindRuntimeType(typeof(BsonDouble), typeof(BsonType));
        builder.BindRuntimeType(typeof(BsonDecimal128), typeof(BsonType));
        builder.BindRuntimeType(typeof(BsonDateTime), typeof(BsonType));
        builder.BindRuntimeType(typeof(BsonTimestamp), typeof(BsonType));
        builder.BindRuntimeType(typeof(BsonObjectId), typeof(BsonType));
        builder.BindRuntimeType(typeof(BsonBinaryData), typeof(BsonType));
        builder.BindRuntimeType(typeof(BsonInt32), typeof(BsonType));
        builder.BindRuntimeType(typeof(BsonInt64), typeof(BsonType));
        builder.BindRuntimeType(typeof(BsonNull), typeof(BsonType));
        builder.BindRuntimeType(typeof(BsonBoolean), typeof(BsonType));
        builder.BindRuntimeType(typeof(BsonString), typeof(BsonType));
        return builder;
    }
}

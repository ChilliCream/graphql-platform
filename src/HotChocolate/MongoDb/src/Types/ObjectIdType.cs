using HotChocolate.Language;
using HotChocolate.Types.MongoDb.Resources;
using MongoDB.Bson;

namespace HotChocolate.Types.MongoDb;

/// <summary>
/// The ObjectId scalar type represents a 12 byte ObjectId, represented as UTF-8 character
/// sequences.
/// </summary>
public class ObjectIdType
    : ScalarType<ObjectId, StringValueNode>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ObjectIdType"/> class.
    /// </summary>
    public ObjectIdType()
        : this(
            MongoDbScalarNames.ObjectId,
            MongoDbTypesResources.ObjectId_Type_Description,
            BindingBehavior.Implicit)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ObjectIdType"/> class.
    /// </summary>
    public ObjectIdType(
        string name,
        string? description = null,
        BindingBehavior bind = BindingBehavior.Explicit)
        : base(name, bind)
    {
        SpecifiedBy = new Uri("https://docs.mongodb.com/manual/reference/bson-types/#objectid");
        Description = description;
    }

    /// <inheritdoc />
    protected override ObjectId ParseLiteral(StringValueNode valueSyntax) =>
        new(valueSyntax.Value);

    /// <inheritdoc />
    protected override StringValueNode ParseValue(ObjectId runtimeValue) =>
        new(runtimeValue.ToString());

    /// <inheritdoc />
    public override IValueNode ParseResult(object? resultValue) =>
        ParseValue(resultValue);
}

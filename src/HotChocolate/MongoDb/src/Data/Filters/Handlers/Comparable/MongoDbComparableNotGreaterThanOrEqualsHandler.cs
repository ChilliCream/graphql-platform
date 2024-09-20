using HotChocolate.Data.Filters;
using HotChocolate.Language;
using HotChocolate.Types;
using MongoDB.Driver;

namespace HotChocolate.Data.MongoDb.Filters;

/// <summary>
/// This filter operation handler maps a NotGreaterThanOrEquals operation field to a
/// <see cref="FilterDefinition{TDocument}"/>
/// </summary>
public class MongoDbComparableNotGreaterThanOrEqualsHandler
    : MongoDbComparableOperationHandler
{
    public MongoDbComparableNotGreaterThanOrEqualsHandler(InputParser inputParser)
        : base(inputParser)
    {
        CanBeNull = false;
    }

    /// <inheritdoc />
    protected override int Operation => DefaultFilterOperations.NotGreaterThanOrEquals;

    /// <inheritdoc />
    public override MongoDbFilterDefinition HandleOperation(
        MongoDbFilterVisitorContext context,
        IFilterOperationField field,
        IValueNode value,
        object? parsedValue)
    {
        if (parsedValue is { })
        {
            var doc = new NotMongoDbFilterDefinition(
                new MongoDbFilterOperation("$gte", parsedValue));

            return new MongoDbFilterOperation(context.GetMongoFilterScope().GetPath(), doc);
        }

        throw new InvalidOperationException();
    }
}

using HotChocolate.Configuration;
using HotChocolate.Data.Filters;
using HotChocolate.Language;
using HotChocolate.Types;
using MongoDB.Driver;

namespace HotChocolate.Data.MongoDb.Filters;

/// <summary>
/// This filter operation handler maps an In operation field to a
/// <see cref="FilterDefinition{TDocument}"/>
/// </summary>
public class MongoDbInOperationHandler
    : MongoDbOperationHandlerBase
{
    public MongoDbInOperationHandler(InputParser inputParser) : base(inputParser)
    {
    }

    public static MongoDbInOperationHandler Create(FilterProviderContext context)
        => new(context.InputParser);

    /// <inheritdoc />
    public override bool CanHandle(
        ITypeCompletionContext context,
        IFilterInputTypeConfiguration typeConfiguration,
        IFilterFieldConfiguration fieldConfiguration)
    {
        return fieldConfiguration is FilterOperationFieldConfiguration operationField
            && operationField.Id is DefaultFilterOperations.In;
    }

    /// <inheritdoc />
    public override MongoDbFilterDefinition HandleOperation(
        MongoDbFilterVisitorContext context,
        IFilterOperationField field,
        IValueNode value,
        object? parsedValue)
    {
        var doc = new MongoDbFilterOperation("$in", parsedValue);

        return new MongoDbFilterOperation(context.GetMongoFilterScope().GetPath(), doc);
    }
}

using System.Text.RegularExpressions;
using HotChocolate.Data.Filters;
using HotChocolate.Language;
using HotChocolate.Types;
using MongoDB.Bson;

namespace HotChocolate.Data.MongoDb.Filters;

public class MongoDbStringNotStartsWithHandler
    : MongoDbStringOperationHandler
{
    public MongoDbStringNotStartsWithHandler(InputParser inputParser)
        : base(inputParser)
    {
        CanBeNull = false;
    }

    protected override int Operation => DefaultFilterOperations.NotStartsWith;

    public override MongoDbFilterDefinition HandleOperation(
        MongoDbFilterVisitorContext context,
        IFilterOperationField field,
        IValueNode value,
        object? parsedValue)
    {
        if (parsedValue is string str)
        {
            var doc = new NotMongoDbFilterDefinition(
                new MongoDbFilterOperation(
                    "$regex",
                    new BsonRegularExpression($"/^{Regex.Escape(str)}/")));

            return new MongoDbFilterOperation(context.GetMongoFilterScope().GetPath(), doc);
        }

        throw new InvalidOperationException();
    }
}

using System.Text.RegularExpressions;
using HotChocolate.Data.Filters;
using HotChocolate.Language;
using HotChocolate.Types;
using MongoDB.Bson;

namespace HotChocolate.Data.MongoDb.Filters;

/// <summary>
/// Handler for MongoDb contains operation that do not work case insensitive
/// </summary>
public class MongoDbStringCaseInsensitiveContainsHandler
    : MongoDbStringOperationHandler
{
    /// <summary>
    /// Creates a new instance of <see cref="MongoDbStringCaseInsensitiveContainsHandler"/>
    /// </summary>
    /// <param name="inputParser"></param>
    public MongoDbStringCaseInsensitiveContainsHandler(InputParser inputParser)
        : base(inputParser)
    {
        CanBeNull = false;
    }

    /// <inheritdoc />
    protected override int Operation => DefaultFilterOperations.CaseInsensitiveContains;

    /// <inheritdoc />
    public override MongoDbFilterDefinition HandleOperation(
        MongoDbFilterVisitorContext context,
        IFilterOperationField field,
        IValueNode value,
        object? parsedValue)
    {
        if (parsedValue is string str)
        {
            var doc = new MongoDbFilterOperation(
                "$regex",
                new BsonRegularExpression($"/{Regex.Escape(str)}/i"));

            return new MongoDbFilterOperation(context.GetMongoFilterScope().GetPath(), doc);
        }

        throw new InvalidOperationException();
    }
}

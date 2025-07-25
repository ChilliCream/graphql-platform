using MongoDB.Bson;
using MongoDB.Bson.Serialization;

namespace HotChocolate.Data.MongoDb;

/// <summary>
/// This class was ported over from the official mongo db driver
/// </summary>
public sealed class AndFilterDefinition : MongoDbFilterDefinition
{
    private static readonly string[] s_operatorsThatCannotBeCombined =
    [
        "$geoWithin",
            "$near",
            "$geoIntersects",
            "$nearSphere"
    ];

    private readonly MongoDbFilterDefinition[] _filters;

    public AndFilterDefinition(params MongoDbFilterDefinition[] filters)
    {
        _filters = filters;
    }

    public AndFilterDefinition(IEnumerable<MongoDbFilterDefinition> filters)
    {
        _filters = filters.ToArray();
    }

    public override BsonDocument Render(
        IBsonSerializer documentSerializer,
        IBsonSerializerRegistry serializerRegistry)
    {
        if (_filters.Length == 0)
        {
            return new BsonDocument("$and", new BsonArray(0));
        }

        var document = new BsonDocument();

        foreach (var filter in _filters)
        {
            var renderedFilter = filter.Render(documentSerializer, serializerRegistry);
            foreach (var clause in renderedFilter)
            {
                AddClause(document, clause);
            }
        }

        return document;
    }

    private static void AddClause(BsonDocument document, BsonElement clause)
    {
        if (clause.Name == "$and")
        {
            // flatten out nested $and
            foreach (var item in (BsonArray)clause.Value)
            {
                foreach (var element in (BsonDocument)item)
                {
                    AddClause(document, element);
                }
            }
        }
        else if (document.ElementCount == 1 && document.GetElement(0).Name == "$and")
        {
            ((BsonArray)document[0]).Add(new BsonDocument(clause));
        }
        else if (document.Contains(clause.Name))
        {
            var existingClause = document.GetElement(clause.Name);
            if (existingClause.Value is BsonDocument existingClauseValue
                && clause.Value is BsonDocument clauseValue)
            {
                var clauseOperator = clauseValue.ElementCount > 0
                    ? clauseValue.GetElement(0).Name
                    : null;
                if (clauseValue.Names.Any(existingClauseValue.Contains)
                    || s_operatorsThatCannotBeCombined.Contains(clauseOperator))
                {
                    PromoteFilterToDollarForm(document, clause);
                }
                else
                {
                    existingClauseValue.AddRange(clauseValue);
                }
            }
            else
            {
                PromoteFilterToDollarForm(document, clause);
            }
        }
        else
        {
            document.Add(clause);
        }
    }

    private static void PromoteFilterToDollarForm(BsonDocument document, BsonElement clause)
    {
        var clauses = new BsonArray();
        foreach (var queryElement in document)
        {
            clauses.Add(new BsonDocument(queryElement));
        }

        clauses.Add(new BsonDocument(clause));
        document.Clear();
        document.Add("$and", clauses);
    }
}

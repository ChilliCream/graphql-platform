using System.Collections.Generic;
using System.Linq;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;

namespace HotChocolate.MongoDb.Data
{
    /// <summary>
    /// This class was ported over from the official mongo db driver
    /// </summary>
    internal sealed class AndFilterDefinition : MongoDbFilterDefinition
    {
        #region static

        private static readonly string[] __operatorsThatCannotBeCombined = new[]
        {
            "$geoWithin", "$near", "$geoIntersects", "$nearSphere"
        };

        #endregion

        private readonly List<MongoDbFilterDefinition> _filters;

        public AndFilterDefinition(IEnumerable<MongoDbFilterDefinition> filters)
        {
            _filters = filters.ToList();
        }

        public override BsonDocument Render(
            IBsonSerializer documentSerializer,
            IBsonSerializerRegistry serializerRegistry)
        {
            if (_filters.Count == 0)
            {
                return new BsonDocument("$and", new BsonArray(0));
            }

            var document = new BsonDocument();

            foreach (var filter in _filters)
            {
                BsonDocument renderedFilter = filter.Render(documentSerializer, serializerRegistry);
                foreach (BsonElement clause in renderedFilter)
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
                    foreach (BsonElement element in (BsonDocument)item)
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
                BsonElement existingClause = document.GetElement(clause.Name);
                if (existingClause.Value is BsonDocument existingClauseValue &&
                    clause.Value is BsonDocument clauseValue)
                {
                    var clauseOperator = clauseValue.ElementCount > 0
                        ? clauseValue.GetElement(0).Name
                        : null;
                    if (clauseValue.Names.Any(op => existingClauseValue.Contains(op)) ||
                        __operatorsThatCannotBeCombined.Contains(clauseOperator))
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
            foreach (BsonElement queryElement in document)
            {
                clauses.Add(new BsonDocument(queryElement));
            }

            clauses.Add(new BsonDocument(clause));
            document.Clear();
            document.Add("$and", clauses);
        }
    }
}

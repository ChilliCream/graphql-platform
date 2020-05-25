using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using HotChocolate.Language;
using HotChocolate.Language.Visitors;
using HotChocolate.Types.Filters.Expressions;
using HotChocolate.Types.Filters.Mongo.Extensions;
using MongoDB.Bson;
using MongoDB.Driver;

namespace HotChocolate.Types.Filters.Mongo
{
    public static class ArrayFieldHandler
    {
        public static bool Enter(
            FilterOperationField field,
            ObjectFieldNode node,
            IFilterVisitorContext<FilterDefinition<BsonDocument>> context,
            [NotNullWhen(true)] out ISyntaxVisitorAction? action)
        {
            if (context is MongoFilterVisitorContext ctx)
            {
                if (field.Operation.Kind == FilterOperationKind.ArraySome
                    || field.Operation.Kind == FilterOperationKind.ArrayNone
                    || field.Operation.Kind == FilterOperationKind.ArrayAll)
                {
                    if (!field.Operation.IsNullable && node.Value.IsNull())
                    {
                        context.ReportError(
                            ErrorHelper.CreateNonNullError(field, node, context));

                        action = SyntaxVisitor.Skip;
                        return true;
                    }

                    ctx.GetMongoFilterScope().Path.Push(field.GetName());
                    context.AddScope();

                    if (node.Value.IsNull())
                    {
                        context.GetLevel().Enqueue(new BsonDocument { { "$eq", BsonNull.Value } });
                        action = SyntaxVisitor.SkipAndLeave;
                    }
                    else
                    {
                        action = SyntaxVisitor.Continue;
                    }
                    return true;
                }
                action = null;
                return false;
            }

            throw new InvalidOperationException();
        }

        public static void Leave(
            FilterOperationField field,
            ObjectFieldNode node,
            IFilterVisitorContext<FilterDefinition<BsonDocument>> context)
        {
            if (context is MongoFilterVisitorContext ctx)
            {
                if (field.Operation.Kind == FilterOperationKind.ArraySome
                    || field.Operation.Kind == FilterOperationKind.ArrayNone
                    || field.Operation.Kind == FilterOperationKind.ArrayAll)
                {
                    FilterScope<FilterDefinition<BsonDocument>> nestedScope = ctx.PopScope();

                    if (nestedScope is MongoFilterScope nestedMongoScope)
                    {
                        var path = nestedMongoScope.GetPath(field);

                        FilterDefinition<BsonDocument> expression;
                        switch (field.Operation.Kind)
                        {
                            case FilterOperationKind.ArraySome:
                                expression = ctx.Builder.ElemMatch(
                                    path,
                                    GetFilters(ctx, nestedMongoScope));
                                break;

                            case FilterOperationKind.ArrayNone:
                                expression = new BsonDocument {
                                    {path, new BsonDocument {
                                        { "$not", new BsonDocument {
                                            { "$elemMatch",
                                                GetFilters(ctx, nestedMongoScope).DefaultRender()}
                                        } } } } };
                                break;

                            case FilterOperationKind.ArrayAll:
                                if (field.Operation.IsSimpleArrayType())
                                {
                                    var negatedChilds = BsonArray.Create(
                                        nestedMongoScope.Level.Peek().Select(
                                            x => new BsonDocument {
                                                {path, new BsonDocument { {
                                                        "$not", x.DefaultRender() } } } }));

                                    var match = new BsonDocument("$nor", negatedChilds);
                                    expression = match;
                                }
                                else
                                {
                                    var negatedChilds = BsonArray.Create(
                                        nestedMongoScope.Level.Peek().Select(
                                            x => x.DefaultRender()));

                                    var match = new BsonDocument("$nor", negatedChilds);
                                    expression = new BsonDocument {
                                    {path, new BsonDocument {
                                        {"$not",  new BsonDocument {
                                            { "$elemMatch", match}} } } } };
                                }
                                break;

                            default:
                                throw new NotSupportedException();
                        }

                        ctx.GetLevel().Enqueue(expression);
                    }
                }
            }
        }

        private static FilterDefinition<BsonDocument> GetFilters(
            MongoFilterVisitorContext ctx,
            MongoFilterScope scope)
        {
            if (scope.Level.Peek().Count == 1)
            {
                return scope.Level.Peek().Peek();
            }
            else
            {
                return ctx.Builder.And(scope.Level.Peek().ToArray());
            }
        }
    }
}

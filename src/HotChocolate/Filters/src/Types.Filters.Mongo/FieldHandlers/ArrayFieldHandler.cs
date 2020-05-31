using System;
using System.Diagnostics.CodeAnalysis;
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
                if (FilterOperationKind.ArraySome.Equals(field.Operation.Kind) ||
                    FilterOperationKind.ArrayNone.Equals(field.Operation.Kind) ||
                    FilterOperationKind.ArrayAll.Equals(field.Operation.Kind))
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
                        context.GetLevel().Enqueue(new BsonDocument("$eq", BsonNull.Value));
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
                if (FilterOperationKind.ArraySome.Equals(field.Operation.Kind) ||
                    FilterOperationKind.ArrayNone.Equals(field.Operation.Kind) ||
                    FilterOperationKind.ArrayAll.Equals(field.Operation.Kind))
                {
                    FilterScope<FilterDefinition<BsonDocument>> nestedScope = ctx.PopScope();

                    if (nestedScope is MongoFilterScope scope)
                    {
                        var path = scope.GetPath(field);

                        FilterDefinition<BsonDocument> expression;
                        switch (field.Operation.Kind)
                        {
                            case FilterOperationKind.ArraySome:
                                expression = ctx.Builder.ElemMatch(path, GetFilters(ctx, scope));
                                break;
                            case FilterOperationKind.ArrayNone:
                                expression = new BsonDocument(path,
                                    new BsonDocument("$not",
                                        new BsonDocument("$elemMatch",
                                            GetFilters(ctx, scope).DefaultRender())));
                                break;
                            case FilterOperationKind.ArrayAll:
                                expression = field.Operation.IsSimpleArrayType()
                                    ? CreateArrayAllScalar(scope, path)
                                    : CreateArrayAll(scope, path);
                                break;
                            default:
                                throw new NotSupportedException();
                        }
                        ctx.GetLevel().Enqueue(expression);
                    }
                }
            }
        }

        private static BsonDocument CreateArrayAll(
            MongoFilterScope scope,
            string path)
        {
            var negatedChilds = new BsonArray();
            while (scope.Level.Peek().Count > 0)
            {
                negatedChilds.Add(scope.Level.Peek().Dequeue().DefaultRender());
            }

            return new BsonDocument(path,
                new BsonDocument("$not",
                    new BsonDocument("$elemMatch",
                        new BsonDocument("$nor", negatedChilds))));
        }

        private static BsonDocument CreateArrayAllScalar(
             MongoFilterScope scope,
             string path)
        {
            var negatedChilds = new BsonArray();
            while (scope.Level.Peek().Count > 0)
            {
                negatedChilds.Add(
                    new BsonDocument(path,
                        new BsonDocument("$not", scope.Level.Peek().Dequeue().DefaultRender())));
            }

            return new BsonDocument("$nor", negatedChilds);
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

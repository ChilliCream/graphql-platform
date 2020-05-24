using System;
using System.Diagnostics.CodeAnalysis;
using HotChocolate.Language;
using HotChocolate.Language.Visitors;
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
                    ctx.GetMongoFilterScope().Path.Push(field.GetName());

                    context.AddScope();

                    if (node.Value.IsNull())
                    {
                        // TODO fix this
                        //context.GetLevel().Enqueue();
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

                    ctx.GetMongoFilterScope().Path.Pop();

                    if (nestedScope is MongoFilterScope nestedClosure)
                    {
                        var path = nestedClosure.GetPath();

                        FilterDefinition<BsonDocument> mongoQuery =
                            ctx.Builder.And(nestedClosure.Level.Peek().ToArray());

                        FilterDefinition<BsonDocument> expression;
                        switch (field.Operation.Kind)
                        {
                            case FilterOperationKind.ArraySome:
                                expression = ctx.Builder.ElemMatch(path, mongoQuery);
                                break;

                            case FilterOperationKind.ArrayNone:
                                expression = ctx.Builder.Not(
                                    ctx.Builder.ElemMatch(path, mongoQuery));
                                break;

                            case FilterOperationKind.ArrayAll:
                                throw new NotImplementedException();

                            default:
                                throw new NotSupportedException();
                        }

                        ctx.GetLevel().Enqueue(expression);
                    }
                    ctx.PopInstance();
                }
            }
        }

    }
}

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
    public static class ObjectFieldHandler
    {
        public static bool Enter(
            FilterOperationField field,
            ObjectFieldNode node,
            IFilterVisitorContext<FilterDefinition<BsonDocument>> context,
            [NotNullWhen(true)] out ISyntaxVisitorAction? action)
        {
            if (context is MongoFilterVisitorContext ctx &&
                field.Operation is { } operation)
            {
                ctx.GetMongoFilterScope().Path.Push(field.GetName());

                if (node.Value.IsNull())
                {
                    if (operation.IsNullable == true)
                    {
                        context.GetLevel().Enqueue(
                            ctx.Builder.Eq(ctx.GetMongoFilterScope().GetPath(), BsonNull.Value));
                    }
                    else
                    {
                        context.ReportError(
                            ErrorHelper.CreateNonNullError(field, node, context));
                    }

                    action = SyntaxVisitor.Skip;
                    return true;
                }

                if (operation.Kind == FilterOperationKind.Object)
                {
                    action = SyntaxVisitor.Continue;
                    return true;
                }
            }
            action = null;
            return false;
        }

        public static void Leave(
            FilterOperationField field,
            ObjectFieldNode node,
            IFilterVisitorContext<FilterDefinition<BsonDocument>> context)
        {
            if (context is MongoFilterVisitorContext ctx)
            {
                if (field.Operation.Kind == FilterOperationKind.Object)
                {
                }
            }
            else
            {
                throw new InvalidOperationException();
            }
        }
    }
}

using System;
using System.Diagnostics.CodeAnalysis;
using HotChocolate.Language;
using HotChocolate.Language.Visitors;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.Builders;

namespace HotChocolate.Types.Filters.Mongo
{
    public static class ObjectFieldHandler
    {
        public static bool Enter(
            FilterOperationField field,
            ObjectFieldNode node,
            IFilterVisitorContext<IMongoQuery> context,
            [NotNullWhen(true)] out ISyntaxVisitorAction? action)
        {
            if (context is MongoFilterVisitorContext ctx &&
                field.Operation is { } operation)
            {
                string fieldName = field.Name;
                if (operation.Property is { } p)
                {
                    fieldName = p.Name;
                }

                ctx.GetMongoFilterScope().Path.Push(fieldName);

                if (node.Value.IsNull())
                {
                    if (operation.IsNullable == true)
                    {
                        context.GetLevel().Enqueue(
                            Query.EQ(
                                ctx.GetMongoFilterScope().GetPath(), BsonNull.Value));
                    }
                    else
                    {
                        /*
                        context.ReportError(
                            ErrorHelper.CreateNonNullError(field, node, context));
                            */
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
            IFilterVisitorContext<IMongoQuery> context)
        {
            if (context is MongoFilterVisitorContext ctx)
            {
                if (field.Operation.Kind == FilterOperationKind.Object)
                {
                    context.GetLevel().Enqueue(
                        Query.EQ(
                            ctx.GetMongoFilterScope().GetPath(),
                            Query.And(ctx.GetLevel().ToArray()).ToBson()));
                    context.PopInstance();
                }
            }
            else
            {
                throw new InvalidOperationException();
            }
        }
    }
}

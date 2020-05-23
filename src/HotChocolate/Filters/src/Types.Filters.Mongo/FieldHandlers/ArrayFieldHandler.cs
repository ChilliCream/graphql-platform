using System;
using System.Diagnostics.CodeAnalysis;
using HotChocolate.Language;
using HotChocolate.Language.Visitors;
using MongoDB.Driver;
using MongoDB.Driver.Builders;

namespace HotChocolate.Types.Filters.Mongo
{
    public static class ArrayFieldHandler
    {
        public static bool Enter(
            FilterOperationField field,
            ObjectFieldNode node,
            IFilterVisitorContext<IMongoQuery> context,
            [NotNullWhen(true)] out ISyntaxVisitorAction? action)
        {
            if (context is MongoFilterVisitorContext ctx)
            {
                if (field.Operation.Kind == FilterOperationKind.ArraySome
                    || field.Operation.Kind == FilterOperationKind.ArrayNone
                    || field.Operation.Kind == FilterOperationKind.ArrayAll)
                {
                    string fieldName = field.Name;
                    if (field.Operation?.Property is { } p)
                    {
                        fieldName = p.Name;
                    }

                    ctx.GetMongoFilterScope().Path.Push(fieldName);

                    context.AddScope();

                    if (node.Value.IsNull())
                    {
                        // TODO fix this
                        context.GetLevel().Enqueue(Query.Null);
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
            IFilterVisitorContext<IMongoQuery> context)
        {
            if (context is MongoFilterVisitorContext ctx)
            {
                if (field.Operation.Kind == FilterOperationKind.ArraySome
                    || field.Operation.Kind == FilterOperationKind.ArrayNone
                    || field.Operation.Kind == FilterOperationKind.ArrayAll)
                {
                    FilterScope<IMongoQuery> nestedScope = ctx.PopScope();

                    ctx.GetMongoFilterScope().Path.Pop();

                    if (nestedScope is MongoFilterScope nestedClosure)
                    {
                        var path = nestedClosure.GetPath();

                        IMongoQuery mongoQuery =
                            Query.And(nestedClosure.Level.Peek().ToArray());

                        IMongoQuery expression;
                        switch (field.Operation.Kind)
                        {
                            case FilterOperationKind.ArraySome:
                                expression = Query.ElemMatch(path, mongoQuery);
                                break;

                            case FilterOperationKind.ArrayNone:
                                expression = Query.Not(Query.ElemMatch(path, mongoQuery));
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

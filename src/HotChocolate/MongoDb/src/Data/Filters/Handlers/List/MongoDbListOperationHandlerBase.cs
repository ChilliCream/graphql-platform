using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using HotChocolate.Configuration;
using HotChocolate.Data.Filters;
using HotChocolate.Internal;
using HotChocolate.Language;
using HotChocolate.Language.Visitors;

namespace HotChocolate.Data.MongoDb.Filters
{
    public abstract class MongoDbListOperationHandlerBase
        : FilterFieldHandler<MongoDbFilterVisitorContext, MongoDbFilterDefinition>
    {
        protected abstract int Operation { get; }

        public override bool CanHandle(
            ITypeCompletionContext context,
            IFilterInputTypeDefinition typeDefinition,
            IFilterFieldDefinition fieldDefinition)
        {
            return context.Type is IListFilterInputType &&
                fieldDefinition is FilterOperationFieldDefinition operationField &&
                operationField.Id == Operation;
        }

        public override bool TryHandleEnter(
            MongoDbFilterVisitorContext context,
            IFilterField field,
            ObjectFieldNode node,
            [NotNullWhen(true)] out ISyntaxVisitorAction? action)
        {
            if (node.Value.IsNull())
            {
                context.ReportError(
                    ErrorHelper.CreateNonNullError(field, node.Value, context));

                action = SyntaxVisitor.Skip;
                return true;
            }

            if (context.RuntimeTypes.Count > 0 &&
                context.RuntimeTypes.Peek().TypeArguments is { Count: > 0 } args)
            {
                IExtendedType element = args[0];
                context.RuntimeTypes.Push(element);
                context.AddScope();

                action = SyntaxVisitor.Continue;
                return true;
            }

            action = null;
            return false;
        }

        public override bool TryHandleLeave(
            MongoDbFilterVisitorContext context,
            IFilterField field,
            ObjectFieldNode node,
            [NotNullWhen(true)] out ISyntaxVisitorAction? action)
        {
            IExtendedType runtimeType = context.RuntimeTypes.Pop();

            if (context.TryCreateQuery(out MongoDbFilterDefinition? lambda) &&
                context.Scopes.Pop() is MongoDbFilterScope scope)
            {
                var path = context.GetMongoFilterScope().GetPath();
                MongoDbFilterDefinition expression = HandleListOperation(
                    context,
                    field,
                    node,
                    runtimeType.Source,
                    scope,
                    path,
                    lambda);

                context.GetLevel().Enqueue(expression);
            }


            action = SyntaxVisitor.Continue;
            return true;
        }

        protected abstract MongoDbFilterDefinition HandleListOperation(
            MongoDbFilterVisitorContext context,
            IFilterField field,
            ObjectFieldNode node,
            Type closureType,
            MongoDbFilterScope scope,
            string path,
            MongoDbFilterDefinition? bsonDocument);

        protected static MongoDbFilterDefinition GetFilters(
            MongoDbFilterVisitorContext context,
            MongoDbFilterScope scope)
        {
            Queue<MongoDbFilterDefinition> level = scope.Level.Peek();
            if (level.Count == 1)
            {
                return level.Peek();
            }

            return new AndFilterDefinition(level.ToArray());
        }
    }
}

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using HotChocolate.Configuration;
using HotChocolate.Data.Filters;
using HotChocolate.Internal;
using HotChocolate.Language;
using HotChocolate.Language.Visitors;
using MongoDB.Bson;
using MongoDB.Driver;

namespace HotChocolate.MongoDb.Data.Filters
{
    public abstract class MongoDbListOperationHandlerBase
        : FilterFieldHandler<MongoDbFilterVisitorContext, FilterDefinition<BsonDocument>>
    {
        protected abstract int Operation { get; }

        public override bool CanHandle(
            ITypeDiscoveryContext context,
            FilterInputTypeDefinition typeDefinition,
            FilterFieldDefinition fieldDefinition)
        {
            return context.Type is IListFilterInput &&
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

            if (context.TryCreateQuery(out BsonDocument? lambda) &&
                context.Scopes.Pop() is MongoDbFilterScope scope)
            {
                var path = context.GetMongoFilterScope().GetPath();
                FilterDefinition<BsonDocument> expression = HandleListOperation(
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

        protected abstract FilterDefinition<BsonDocument> HandleListOperation(
            MongoDbFilterVisitorContext context,
            IFilterField field,
            ObjectFieldNode node,
            Type closureType,
            MongoDbFilterScope scope,
            string path,
            BsonDocument? bsonDocument);

        protected static FilterDefinition<BsonDocument> GetFilters(
            MongoDbFilterVisitorContext context,
            MongoDbFilterScope scope)
        {
            Queue<FilterDefinition<BsonDocument>> level = scope.Level.Peek();
            if (level.Count == 1)
            {
                return level.Peek();
            }

            return context.Builder.And(level.ToArray());
        }
    }
}

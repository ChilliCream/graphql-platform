using System.Diagnostics.CodeAnalysis;
using HotChocolate.Configuration;
using HotChocolate.Data.Filters;
using HotChocolate.Language;
using HotChocolate.Language.Visitors;
using MongoDB.Bson;
using MongoDB.Driver;

namespace HotChocolate.MongoDb.Data.Filters
{
    public class MongoDbDefaultFieldHandler
        : FilterFieldHandler<MongoDbFilterVisitorContext, FilterDefinition<BsonDocument>>
    {
        public override bool CanHandle(
            ITypeDiscoveryContext context,
            FilterInputTypeDefinition typeDefinition,
            FilterFieldDefinition fieldDefinition) =>
            !(fieldDefinition is FilterOperationFieldDefinition) &&
            fieldDefinition.Member is not null;

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

            if (field.RuntimeType is null)
            {
                action = null;
                return false;
            }

            context.GetMongoFilterScope().Path.Push(field.GetName());
            context.RuntimeTypes.Push(field.RuntimeType);
            action = SyntaxVisitor.Continue;
            return true;
        }

        public override bool TryHandleLeave(
            MongoDbFilterVisitorContext context,
            IFilterField field,
            ObjectFieldNode node,
            [NotNullWhen(true)] out ISyntaxVisitorAction? action)
        {
            context.RuntimeTypes.Pop();
            context.GetMongoFilterScope().Path.Pop();

            action = SyntaxVisitor.Continue;
            return true;
        }
    }
}

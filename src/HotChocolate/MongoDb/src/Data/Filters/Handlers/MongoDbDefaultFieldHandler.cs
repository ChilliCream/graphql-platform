using System.Diagnostics.CodeAnalysis;
using HotChocolate.Configuration;
using HotChocolate.Data.Filters;
using HotChocolate.Language;
using HotChocolate.Language.Visitors;

namespace HotChocolate.Data.MongoDb.Filters
{
    public class MongoDbDefaultFieldHandler
        : FilterFieldHandler<MongoDbFilterVisitorContext, MongoDbFilterDefinition>
    {
        public override bool CanHandle(
            ITypeCompletionContext context,
            IFilterInputTypeDefinition typeDefinition,
            IFilterFieldDefinition fieldDefinition) =>
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
                context.ReportError(ErrorHelper.CreateNonNullError(field, node.Value, context));

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

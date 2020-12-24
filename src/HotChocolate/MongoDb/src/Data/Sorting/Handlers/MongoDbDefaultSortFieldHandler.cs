using System.Diagnostics.CodeAnalysis;
using HotChocolate.Configuration;
using HotChocolate.Data.Sorting;
using HotChocolate.Language;
using HotChocolate.Language.Visitors;

namespace HotChocolate.Data.MongoDb.Sorting
{
    public class MongoDbDefaultSortFieldHandler
        : SortFieldHandler<MongoDbSortVisitorContext, MongoDbSortDefinition>
    {
        /// <inheritdoc />
        public override bool CanHandle(
            ITypeCompletionContext context,
            ISortInputTypeDefinition typeDefinition,
            ISortFieldDefinition fieldDefinition) =>
            fieldDefinition.Member is not null;

        /// <inheritdoc />
        public override bool TryHandleEnter(
            MongoDbSortVisitorContext context,
            ISortField field,
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

            context.Path.Push(field.GetName());
            action = SyntaxVisitor.Continue;
            return true;
        }

        /// <inheritdoc />
        public override bool TryHandleLeave(
            MongoDbSortVisitorContext context,
            ISortField field,
            ObjectFieldNode node,
            [NotNullWhen(true)] out ISyntaxVisitorAction? action)
        {
            context.Path.Pop();

            action = SyntaxVisitor.Continue;
            return true;
        }
    }
}

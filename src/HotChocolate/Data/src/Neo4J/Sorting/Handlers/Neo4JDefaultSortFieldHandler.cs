using System.Diagnostics.CodeAnalysis;
using HotChocolate.Configuration;
using HotChocolate.Data.Sorting;
using HotChocolate.Language;
using HotChocolate.Language.Visitors;

namespace HotChocolate.Data.Neo4J.Sorting
{
    public class Neo4JDefaultSortFieldHandler
        : SortFieldHandler<Neo4JSortVisitorContext, Neo4JSortDefinition>
    {
        /// <inheritdoc />
        public override bool CanHandle(
            ITypeCompletionContext context,
            ISortInputTypeDefinition typeDefinition,
            ISortFieldDefinition fieldDefinition) =>
            fieldDefinition.Member is not null;

        /// <inheritdoc />
        public override bool TryHandleEnter(
            Neo4JSortVisitorContext context,
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
            Neo4JSortVisitorContext context,
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

using System.Diagnostics.CodeAnalysis;
using HotChocolate.Configuration;
using HotChocolate.Data.Neo4J.Language;
using HotChocolate.Data.Sorting;
using HotChocolate.Language;
using HotChocolate.Language.Visitors;
using HotChocolate.Types.Descriptors.Definitions;

#nullable enable

namespace HotChocolate.Data.Neo4J.Sorting
{
    /// <summary>
    /// Represents a Neo4J handler that can be bound to a <see cref="SortField"/>. The handler is
    /// executed during the visitation of a input object.
    /// </summary>
    public abstract class Neo4JSortOperationHandlerBase
        : SortOperationHandler<Neo4JSortVisitorContext, Neo4JSortDefinition>
    {
        private readonly SortDirection _sortDirection;
        private readonly int _operation;

        protected Neo4JSortOperationHandlerBase(
            int operation,
            SortDirection sortDirection)
        {
            _sortDirection = sortDirection;
            _operation = operation;
        }

        /// <inheritdoc/>
        public override bool CanHandle(
            ITypeCompletionContext context,
            EnumTypeDefinition typeDefinition,
            SortEnumValueDefinition valueDefinition)
        {
            return valueDefinition.Operation == _operation;
        }

        /// <inheritdoc/>
        public override bool TryHandleEnter(
            Neo4JSortVisitorContext context,
            ISortField field,
            ISortEnumValue? sortValue,
            EnumValueNode node,
            [NotNullWhen(true)] out ISyntaxVisitorAction? action)
        {
            if (sortValue is null)
            {
                context.ReportError(
                    ErrorHelper.CreateNonNullError(field, node, context));

                action = null!;
                return false;
            }

            context.Operations.Enqueue(new Neo4JSortDefinition(context.Path.Peek(), _sortDirection));
            action = SyntaxVisitor.Continue;

            return true;
        }
    }
}

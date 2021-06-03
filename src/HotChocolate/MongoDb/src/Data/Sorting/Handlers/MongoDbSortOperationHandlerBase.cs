using System.Diagnostics.CodeAnalysis;
using HotChocolate.Configuration;
using HotChocolate.Data.Sorting;
using HotChocolate.Language;
using HotChocolate.Language.Visitors;
using HotChocolate.Types.Descriptors.Definitions;
using MongoDB.Driver;

namespace HotChocolate.Data.MongoDb.Sorting
{
    /// <summary>
    /// Represents a mongodb handler that can be bound to a <see cref="SortField"/>. The handler is
    /// executed during the visitation of a input object.
    /// </summary>
    public abstract class MongoDbSortOperationHandlerBase
        : SortOperationHandler<MongoDbSortVisitorContext, MongoDbSortDefinition>
    {
        private readonly SortDirection _sortDirection;
        private readonly int _operation;

        protected MongoDbSortOperationHandlerBase(
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
            MongoDbSortVisitorContext context,
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

            context.Operations.Enqueue(
                new MongoDbDirectionalSortOperation(context.GetPath(), _sortDirection));

            action = SyntaxVisitor.Continue;
            return true;
        }
    }
}

using System.Diagnostics.CodeAnalysis;
using HotChocolate.Configuration;
using HotChocolate.Data.Sorting;
using HotChocolate.Language;
using HotChocolate.Language.Visitors;
using HotChocolate.MongoDb.Data;
using HotChocolate.MongoDb.Data.Sorting;
using HotChocolate.MongoDb.Sorting.Convention.Extensions.Handlers;
using HotChocolate.Types.Descriptors.Definitions;
using MongoDB.Driver;

namespace HotChocolate.MongoDb.Sorting.Handlers
{
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

        public override bool CanHandle(
            ITypeDiscoveryContext context,
            EnumTypeDefinition typeDefinition,
            SortEnumValueDefinition valueDefinition)
        {
            return valueDefinition.Operation == _operation;
        }

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

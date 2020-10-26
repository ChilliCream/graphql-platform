using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using HotChocolate.Configuration;
using HotChocolate.Data.Sorting;
using HotChocolate.Language;
using HotChocolate.Language.Visitors;
using HotChocolate.MongoDb.Sorting.Convention.Extensions.Handlers;
using HotChocolate.Types.Descriptors.Definitions;

namespace HotChocolate.MongoDb.Sorting.Handlers
{
    public abstract class MongoDbSortOperationHandlerBase
        : SortOperationHandler<MongoDbSortVisitorContext, SortDefinition>
    {
        private readonly int _mongoSortValue;
        private readonly int _operation;

        protected MongoDbSortOperationHandlerBase(
            int operation,
            int mongoSortValue)
        {
            _mongoSortValue = mongoSortValue;
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
            ISortEnumValue? sortEnumValue,
            EnumValueNode valueNode,
            [NotNullWhen(true)] out ISyntaxVisitorAction? action)
        {
            if (sortEnumValue is null)
            {
                context.ReportError(
                    ErrorHelper.CreateNonNullError(field, valueNode, context));

                action = null!;
                return false;
            }

            var entityParam = Expression.Parameter(field.Member.ReflectedType, "e");
            var lambaExpression = Expression.Lambda(Expression.MakeMemberAccess(entityParam, field.Member),
                                                    entityParam);
            context.Operations.Enqueue(new SortDefinition(lambaExpression, _operation));

            action = SyntaxVisitor.Continue;
            return true;
        }
    }
}

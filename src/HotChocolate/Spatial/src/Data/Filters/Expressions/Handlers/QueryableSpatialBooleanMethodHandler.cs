using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using System.Reflection;
using HotChocolate.Configuration;
using HotChocolate.Data.Filters.Expressions;
using HotChocolate.Internal;
using HotChocolate.Language;
using HotChocolate.Language.Visitors;
using HotChocolate.Types.Descriptors;

namespace HotChocolate.Data.Filters.Spatial
{
    public abstract class QueryableSpatialBooleanMethodHandler
        : FilterFieldHandler<QueryableFilterContext, Expression>
    {
        private readonly IExtendedType _runtimeType;

        protected abstract int Operation { get; }

        protected abstract bool IsTrue { get; }
        protected string GeometryFieldName { get; }
        protected string BufferFieldName { get; }

        protected QueryableSpatialBooleanMethodHandler(
            IFilterConvention convention,
            ITypeInspector inspector,
            MethodInfo method)
        {
            _runtimeType = inspector.GetReturnType(method);
            GeometryFieldName = convention.GetOperationName(SpatialFilterOperations.Geometry);
            BufferFieldName = convention.GetOperationName(SpatialFilterOperations.Buffer);
        }

        public override bool CanHandle(
            ITypeCompletionContext context,
            IFilterInputTypeDefinition typeDefinition,
            IFilterFieldDefinition fieldDefinition) =>
            fieldDefinition is FilterOperationFieldDefinition op &&
            op.Id == Operation;

        public override bool TryHandleEnter(
            QueryableFilterContext context,
            IFilterField field,
            ObjectFieldNode node,
            [NotNullWhen(true)] out ISyntaxVisitorAction? action)
        {
            if (field is IFilterOperationField filterOperationField)
            {
                if (node.Value.IsNull())
                {
                    context.ReportError(
                        ErrorHelper.CreateNonNullError(
                            field, node.Value, context));
                    action = SyntaxVisitor.Skip;
                    return true;
                }

                if (!TryHandleOperation(
                    context,
                    filterOperationField,
                    node,
                    out Expression? nestedProperty))
                {
                    context.ReportError(
                        ErrorHelper.CouldNotCreateFilterForOperation(
                            field, node.Value, context));
                    action = SyntaxVisitor.Skip;
                    return true;
                }

                context.RuntimeTypes.Push(_runtimeType);
                context.PushInstance(nestedProperty );
                action = SyntaxVisitor.SkipAndLeave;
            }
            else
            {
                action = SyntaxVisitor.Break;
            }

            return true;
        }

        protected abstract bool TryHandleOperation(
            QueryableFilterContext context,
            IFilterOperationField field,
            ObjectFieldNode node,
            [NotNullWhen(true)] out Expression? result);

        public override bool TryHandleLeave(
            QueryableFilterContext context,
            IFilterField field,
            ObjectFieldNode node,
            [NotNullWhen(true)] out ISyntaxVisitorAction? action)
        {
            // Dequeue last
            Expression instance = context.PopInstance();
            context.RuntimeTypes.Pop();
            Expression condition = FilterExpressionBuilder.Equals(instance, IsTrue);

            if (context.InMemory)
            {
                condition = FilterExpressionBuilder.NotNullAndAlso(
                    context.GetInstance(),
                    condition);
            }

            context.GetLevel().Enqueue(condition);
            action = SyntaxVisitor.Continue;
            return true;
        }
    }
}

using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using HotChocolate.Configuration;
using HotChocolate.Data.Filters;
using HotChocolate.Data.Filters.Expressions;
using HotChocolate.Language;
using HotChocolate.Language.Visitors;
using HotChocolate.Types;
using NetTopologySuite.Geometries;

namespace HotChocolate.Data.Spatial.Filters
{
    public abstract class QueryableSpatialMethodHandler
        : FilterFieldHandler<QueryableFilterContext, Expression>
    {
        protected abstract int Operation { get; }

        protected string GeometryFieldName { get; }
        protected string BufferFieldName { get; }
        protected string ToFieldName { get; }

        protected QueryableSpatialMethodHandler(
            IFilterConvention convention)
        {
            GeometryFieldName = convention.GetOperationName(SpatialFilterOperations.Geometry);
            BufferFieldName = convention.GetOperationName(SpatialFilterOperations.Buffer);
            ToFieldName = convention.GetOperationName(SpatialFilterOperations.To);
        }

        public override bool CanHandle(
            ITypeDiscoveryContext context,
            FilterInputTypeDefinition typeDefinition,
            FilterFieldDefinition fieldDefinition) =>
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
                        ErrorHelper.CreateNonNullError(field, node.Value, context));

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
                        ErrorHelper.CouldNotCreateFilterForOperation(field, node.Value, context));

                    action = SyntaxVisitor.Skip;
                    return true;
                }

                context.PushInstance(nestedProperty);
                action = SyntaxVisitor.Continue;
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
            // Deque last
            Expression condition = context.GetLevel().Dequeue();

            context.PopInstance();

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

        protected static bool TryGetParameter<TParameter>(
            IFilterField parentField,
            IValueNode node,
            string fieldName,
            [NotNullWhen(true)] out TParameter fieldNode)
        {
            if (parentField.Type is InputObjectType inputType &&
                node is ObjectValueNode objectValueNode)
            {
                for (var i = 0; i < objectValueNode.Fields.Count; i++)
                {
                    if (objectValueNode.Fields[i].Name.Value == fieldName)
                    {
                        ObjectFieldNode field = objectValueNode.Fields[i];
                        if (inputType.Fields[fieldName].Type.ParseLiteral(field.Value) is
                            TParameter val)
                        {
                            fieldNode = val;
                            return true;
                        }

                        fieldNode = default!;
                        return false;
                    }
                }
            }

            fieldNode = default!;
            return false;
        }
    }
}

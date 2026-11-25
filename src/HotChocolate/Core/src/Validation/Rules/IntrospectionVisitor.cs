using HotChocolate.Execution;
using HotChocolate.Language;
using HotChocolate.Language.Visitors;
using HotChocolate.Types;

namespace HotChocolate.Validation.Rules;

/// <summary>
/// Validates if introspection is allowed.
/// </summary>
internal sealed class IntrospectionVisitor : TypeDocumentValidatorVisitor
{
    protected override ISyntaxVisitorAction Enter(
        DocumentNode node,
        DocumentValidatorContext context)
    {
        // The result is request independent unless a query operation is skipped or reports an error.
        context.IsRequestIndependent = true;
        return base.Enter(node, context);
    }

    protected override ISyntaxVisitorAction Enter(
        OperationDefinitionNode node,
        DocumentValidatorContext context)
    {
        if (node.Operation is not OperationType.Query)
        {
            return Skip;
        }

        if (!context.IsIntrospectionDisabled())
        {
            context.IsRequestIndependent = false;
            return Skip;
        }

        var action = base.Enter(node, context);

        if (!action.IsContinue())
        {
            context.IsRequestIndependent = false;
        }

        return action;
    }

    protected override ISyntaxVisitorAction Enter(
        FieldNode node,
        DocumentValidatorContext context)
    {
        if (IntrospectionFieldNames.TypeName.Equals(node.Name.Value, StringComparison.Ordinal))
        {
            return Skip;
        }

        if (context.Types.TryPeek(out var type))
        {
            var namedType = type.NamedType();
            if (context.Schema.QueryType == namedType
                && namedType is IComplexTypeDefinition complexType
                && complexType.Fields.TryGetField(node.Name.Value, out var field)
                && field.IsIntrospectionField)
            {
                context.IsRequestIndependent = false;
                context.ReportError(
                    context.IntrospectionNotAllowed(
                        node,
                        context.GetCustomIntrospectionErrorMessage()));
                return Break;
            }

            return Skip;
        }

        context.IsRequestIndependent = false;
        context.UnexpectedErrorsDetected = true;
        return Skip;
    }
}

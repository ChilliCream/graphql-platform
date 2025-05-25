using HotChocolate.Language;
using HotChocolate.Language.Visitors;
using HotChocolate.Types;

namespace HotChocolate.Validation;

public class TypeDocumentValidatorVisitor : DocumentValidatorVisitor
{
    protected TypeDocumentValidatorVisitor(SyntaxVisitorOptions options = default)
        : base(options)
    {
    }

    protected override ISyntaxVisitorAction Enter(
        OperationDefinitionNode node,
        DocumentValidatorContext context)
    {
        if (context.Schema.TryGetOperationType(node.Operation, out var operationType))
        {
            context.Types.Push(operationType);
            context.Variables.Clear();
            return Continue;
        }

        return Skip;
    }

    protected override ISyntaxVisitorAction Enter(
        VariableDefinitionNode node,
        DocumentValidatorContext context)
    {
        context.Variables[node.Variable.Name.Value] = node;
        return Continue;
    }

    protected override ISyntaxVisitorAction Enter(
        InlineFragmentNode node,
        DocumentValidatorContext context)
    {
        if (node.TypeCondition is null)
        {
            return Continue;
        }

        if (context.Schema.Types.TryGetType<IOutputTypeDefinition>(
            node.TypeCondition.Name.Value,
            out var type))
        {
            context.Types.Push(type);
            return Continue;
        }

        context.UnexpectedErrorsDetected = true;
        return Skip;
    }

    protected override ISyntaxVisitorAction Enter(
        FragmentDefinitionNode node,
        DocumentValidatorContext context)
    {
        if (context.Schema.Types.TryGetType<IOutputTypeDefinition>(
            node.TypeCondition.Name.Value,
            out var namedOutputType))
        {
            context.Types.Push(namedOutputType);
            return Continue;
        }

        context.UnexpectedErrorsDetected = true;
        return Skip;
    }

    protected override ISyntaxVisitorAction Leave(
       OperationDefinitionNode node,
       DocumentValidatorContext context)
    {
        context.Types.Pop();
        context.Variables.Clear();
        return Continue;
    }

    protected override ISyntaxVisitorAction Leave(
        InlineFragmentNode node,
        DocumentValidatorContext context)
    {
        if (node.TypeCondition is { })
        {
            context.Types.Pop();
        }

        return Continue;
    }

    protected override ISyntaxVisitorAction Leave(
        FragmentDefinitionNode node,
        DocumentValidatorContext context)
    {
        context.Types.Pop();
        return Continue;
    }
}

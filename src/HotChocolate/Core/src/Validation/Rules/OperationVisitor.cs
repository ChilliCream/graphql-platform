using HotChocolate.Language;
using HotChocolate.Language.Visitors;
using HotChocolate.Types;
using HotChocolate.Types.Introspection;
using static HotChocolate.Validation.ErrorHelper;

namespace HotChocolate.Validation.Rules;

/// <summary>
/// GraphQL allows a short‐hand form for defining query operations
/// when only that one operation exists in the document.
///
/// https://spec.graphql.org/June2018/#sec-Lone-Anonymous-Operation
///
/// AND
///
/// Each named operation definition must be unique within a document
/// when referred to by its name.
///
/// https://spec.graphql.org/June2018/#sec-Operation-Name-Uniqueness
///
/// AND
///
/// Subscription operations must have exactly one root field.
///
/// https://spec.graphql.org/June2018/#sec-Single-root-field
///
/// AND
///
/// Defer And Stream Directives Are Used On Valid Root Field
///
/// https://spec.graphql.org/draft/#sec-Defer-And-Stream-Directives-Are-Used-On-Valid-Root-Field
/// </summary>
/// <remarks>
/// https://spec.graphql.org/draft/#sec-Validation.Operations
/// </remarks>
public class OperationVisitor : DocumentValidatorVisitor
{
    protected override ISyntaxVisitorAction Enter(
        DocumentNode node,
        IDocumentValidatorContext context)
    {
        var hasAnonymousOp = false;
        var opCount = 0;
        OperationDefinitionNode? anonymousOp = null;

        context.Names.Clear();

        for (var i = 0; i < node.Definitions.Count; i++)
        {
            var definition = node.Definitions[i];
            if (definition.Kind == SyntaxKind.OperationDefinition)
            {
                opCount++;

                var operation = (OperationDefinitionNode)definition;

                if (operation.Name is null)
                {
                    hasAnonymousOp = true;
                    anonymousOp = operation;
                }
                else if (!context.Names.Add(operation.Name.Value))
                {
                    context.ReportError(context.OperationNameNotUnique(
                        operation,
                        operation.Name.Value));
                }
            }
        }

        if (hasAnonymousOp && opCount > 1)
        {
            context.ReportError(context.OperationAnonymousMoreThanOne(anonymousOp!, opCount));
        }

        return Continue;
    }

    protected override ISyntaxVisitorAction Enter(
        OperationDefinitionNode node,
        IDocumentValidatorContext context)
    {
        context.Names.Clear();
        context.OperationType = node.Operation;

        if (node.Operation == OperationType.Mutation)
        {
            return Continue;
        }

        if (node.Operation == OperationType.Subscription)
        {
            return Continue;
        }

        return Skip;
    }

    protected override ISyntaxVisitorAction Leave(
        OperationDefinitionNode node,
        IDocumentValidatorContext context)
    {
        if (node.Operation == OperationType.Subscription)
        {
            if (context.Names.Count > 1)
            {
                context.ReportError(context.SubscriptionSingleRootField(node));
            }
            else if (IntrospectionFields.TypeName.Equals(context.Names.Single()))
            {
                context.ReportError(context.SubscriptionNoTopLevelIntrospectionField(node));
            }
        }

        return Continue;
    }

    protected override ISyntaxVisitorAction Enter(
        FieldNode node,
        IDocumentValidatorContext context)
    {
        context.Names.Add((node.Alias ?? node.Name).Value);

        if (context.OperationType is OperationType.Mutation or OperationType.Subscription &&
            node.Directives.HasStreamOrDeferDirective())
        {
            context.ReportError(DeferAndStreamNotAllowedOnMutationOrSubscriptionRoot(node));
        }

        return Skip;
    }

    protected override ISyntaxVisitorAction Enter(
        InlineFragmentNode node,
        IDocumentValidatorContext context)
    {
        if (context.OperationType is OperationType.Mutation or OperationType.Subscription &&
            node.Directives.HasStreamOrDeferDirective())
        {
            context.ReportError(DeferAndStreamNotAllowedOnMutationOrSubscriptionRoot(node));
        }

        return base.Enter(node, context);
    }

    protected override ISyntaxVisitorAction Enter(
        FragmentSpreadNode node,
        IDocumentValidatorContext context)
    {
        if (context.OperationType is OperationType.Mutation or OperationType.Subscription &&
            node.Directives.HasStreamOrDeferDirective())
        {
            context.ReportError(DeferAndStreamNotAllowedOnMutationOrSubscriptionRoot(node));
        }

        return base.Enter(node, context);
    }
}

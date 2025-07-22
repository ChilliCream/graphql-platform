using HotChocolate.Features;
using HotChocolate.Language;
using HotChocolate.Language.Visitors;
using HotChocolate.Types;
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
/// Defer and Stream Directives Are Used On Valid Root Field
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
        DocumentValidatorContext context)
    {
        var hasAnonymousOp = false;
        var opCount = 0;
        OperationDefinitionNode? anonymousOp = null;
        var operationNames = context.Features.GetOrSet<OperationVisitorFeature>().OperationNames;

        operationNames.Clear();

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
                else if (!operationNames.Add(operation.Name.Value))
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
        DocumentValidatorContext context)
    {
        var feature = context.Features.GetRequired<OperationVisitorFeature>();
        feature.OperationType = node.Operation;
        feature.ResponseNames.Clear();

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
        DocumentValidatorContext context)
    {
        var responseNames = context.Features.GetRequired<OperationVisitorFeature>().ResponseNames;

        if (node.Operation == OperationType.Subscription)
        {
            if (responseNames.Count > 1)
            {
                context.ReportError(context.SubscriptionSingleRootField(node));
            }
            else if (IntrospectionFieldNames.TypeName.Equals(responseNames.Single()))
            {
                context.ReportError(context.SubscriptionNoTopLevelIntrospectionField(node));
            }
        }

        return Continue;
    }

    protected override ISyntaxVisitorAction Enter(
        FieldNode node,
        DocumentValidatorContext context)
    {
        var feature = context.Features.GetRequired<OperationVisitorFeature>();

        feature.ResponseNames.Add((node.Alias ?? node.Name).Value);

        if (feature.OperationType is OperationType.Subscription
            && node.Directives.HasSkipOrIncludeDirective())
        {
            context.ReportError(SkipAndIncludeNotAllowedOnSubscriptionRootField(node));
        }

        if (feature.OperationType is OperationType.Mutation or OperationType.Subscription
            && node.Directives.HasStreamOrDeferDirective())
        {
            context.ReportError(DeferAndStreamNotAllowedOnMutationOrSubscriptionRoot(node));
        }

        return Skip;
    }

    protected override ISyntaxVisitorAction Enter(
        InlineFragmentNode node,
        DocumentValidatorContext context)
    {
        var operationType = context.Features.GetRequired<OperationVisitorFeature>().OperationType;

        if (operationType is OperationType.Mutation or OperationType.Subscription
            && node.Directives.HasStreamOrDeferDirective())
        {
            context.ReportError(DeferAndStreamNotAllowedOnMutationOrSubscriptionRoot(node));
        }

        return base.Enter(node, context);
    }

    protected override ISyntaxVisitorAction Enter(
        FragmentSpreadNode node,
        DocumentValidatorContext context)
    {
        var operationType = context.Features.GetRequired<OperationVisitorFeature>().OperationType;

        if (operationType is OperationType.Mutation or OperationType.Subscription
            && node.Directives.HasStreamOrDeferDirective())
        {
            context.ReportError(DeferAndStreamNotAllowedOnMutationOrSubscriptionRoot(node));
        }

        return base.Enter(node, context);
    }

    private sealed class OperationVisitorFeature : ValidatorFeature
    {
        public OperationType OperationType { get; set; }

        public HashSet<string> OperationNames { get; } = [];

        public HashSet<string> ResponseNames { get; } = [];

        protected internal override void Reset()
        {
            OperationType = default;
            OperationNames.Clear();
            ResponseNames.Clear();
        }
    }
}

file static class DirectiveExtensions
{
    internal static bool HasSkipOrIncludeDirective(this IReadOnlyList<DirectiveNode> directives)
    {
        if (directives.Count == 0)
        {
            return false;
        }

        for (var i = 0; i < directives.Count; i++)
        {
            var directive = directives[i];

            if (directive.Name.Value.Equals(DirectiveNames.Skip.Name, StringComparison.Ordinal)
                || directive.Name.Value.Equals(DirectiveNames.Include.Name, StringComparison.Ordinal))
            {
                return true;
            }
        }

        return false;
    }

    internal static bool HasStreamOrDeferDirective(this IReadOnlyList<DirectiveNode> directives)
    {
        if (directives.Count == 0)
        {
            return false;
        }

        for (var i = 0; i < directives.Count; i++)
        {
            var directive = directives[i];

            if (directive.Name.Value.Equals(DirectiveNames.Defer.Name, StringComparison.Ordinal)
                || directive.Name.Value.Equals(DirectiveNames.Stream.Name, StringComparison.Ordinal))
            {
                return true;
            }
        }

        return false;
    }
}

using System.Linq;
using HotChocolate.Language;
using HotChocolate.Language.Visitors;
using HotChocolate.Types.Introspection;

namespace HotChocolate.Validation.Rules;

/// <summary>
/// GraphQL allows a short‐hand form for defining query operations
/// when only that one operation exists in the document.
///
/// http://spec.graphql.org/June2018/#sec-Lone-Anonymous-Operation
///
/// AND
///
/// Each named operation definition must be unique within a document
/// when referred to by its name.
///
/// http://spec.graphql.org/June2018/#sec-Operation-Name-Uniqueness
///
/// AND
///
/// Subscription operations must have exactly one root field.
///
/// http://spec.graphql.org/June2018/#sec-Single-root-field
/// </summary>
/// <remarks>
/// http://spec.graphql.org/draft/#sec-Validation.Operations
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
        if (context.Names.Count > 1)
        {
            context.ReportError(context.SubscriptionSingleRootField(node));
        }
        else if (IntrospectionFields.TypeName.Equals(context.Names.Single()))
        {
            context.ReportError(context.SubscriptionNoTopLevelIntrospectionField(node));
        }

        return Continue;
    }

    protected override ISyntaxVisitorAction Enter(
        FieldNode node,
        IDocumentValidatorContext context)
    {
        context.Names.Add((node.Alias ?? node.Name).Value);
        return Skip;
    }
}

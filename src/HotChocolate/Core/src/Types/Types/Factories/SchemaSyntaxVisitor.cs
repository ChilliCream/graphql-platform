#nullable enable

using HotChocolate.Language;
using HotChocolate.Language.Visitors;
using HotChocolate.Properties;
using HotChocolate.Types.Descriptors;
using HotChocolate.Utilities;

namespace HotChocolate.Types.Factories;

internal sealed class SchemaSyntaxVisitor : SyntaxVisitor<SchemaSyntaxVisitorContext>
{
    private readonly ObjectTypeFactory _objectTypeFactory = new();
    private readonly InterfaceTypeFactory _interfaceTypeFactory = new();
    private readonly UnionTypeFactory _unionTypeFactory = new();
    private readonly InputObjectTypeFactory _inputObjectTypeFactory = new();
    private readonly EnumTypeFactory _enumTypeFactory = new();
    private readonly DirectiveTypeFactory _directiveTypeFactory = new();

    protected override ISyntaxVisitorAction DefaultAction => Continue;

    protected override ISyntaxVisitorAction VisitChildren(
        ObjectTypeDefinitionNode node,
        SchemaSyntaxVisitorContext context)
    {
        context.Types.Add(
            TypeReference.Create(
                _objectTypeFactory.Create(
                    context.DescriptorContext,
                    node)));

        return base.VisitChildren(node, context);
    }

    protected override ISyntaxVisitorAction VisitChildren(
        ObjectTypeExtensionNode node,
        SchemaSyntaxVisitorContext context)
    {
        context.Types.Add(
            TypeReference.Create(
                _objectTypeFactory.Create(
                    context.DescriptorContext,
                    node)));

        return base.VisitChildren(node, context);
    }

    protected override ISyntaxVisitorAction VisitChildren(
        InterfaceTypeDefinitionNode node,
        SchemaSyntaxVisitorContext context)
    {
        context.Types.Add(
            TypeReference.Create(
                _interfaceTypeFactory.Create(
                    context.DescriptorContext,
                    node)));

        return base.VisitChildren(node, context);
    }

    protected override ISyntaxVisitorAction VisitChildren(
        InterfaceTypeExtensionNode node,
        SchemaSyntaxVisitorContext context)
    {
        context.Types.Add(
            TypeReference.Create(
                _interfaceTypeFactory.Create(
                    context.DescriptorContext,
                    node)));

        return base.VisitChildren(node, context);
    }

    protected override ISyntaxVisitorAction VisitChildren(
        UnionTypeDefinitionNode node,
        SchemaSyntaxVisitorContext context)
    {
        context.Types.Add(
            TypeReference.Create(
                _unionTypeFactory.Create(
                    context.DescriptorContext,
                    node)));

        return base.VisitChildren(node, context);
    }

    protected override ISyntaxVisitorAction VisitChildren(
        UnionTypeExtensionNode node,
        SchemaSyntaxVisitorContext context)
    {
        context.Types.Add(
            TypeReference.Create(
                _unionTypeFactory.Create(
                    context.DescriptorContext,
                    node)));

        return base.VisitChildren(node, context);
    }

    protected override ISyntaxVisitorAction VisitChildren(
        InputObjectTypeDefinitionNode node,
        SchemaSyntaxVisitorContext context)
    {
        context.Types.Add(
            TypeReference.Create(
                _inputObjectTypeFactory.Create(
                    context.DescriptorContext,
                    node)));

        return base.VisitChildren(node, context);
    }

    protected override ISyntaxVisitorAction VisitChildren(
        InputObjectTypeExtensionNode node,
        SchemaSyntaxVisitorContext context)
    {
        context.Types.Add(
            TypeReference.Create(
                _inputObjectTypeFactory.Create(
                    context.DescriptorContext,
                    node)));

        return base.VisitChildren(node, context);
    }

    protected override ISyntaxVisitorAction VisitChildren(
        EnumTypeDefinitionNode node,
        SchemaSyntaxVisitorContext context)
    {
        context.Types.Add(
            TypeReference.Create(
                _enumTypeFactory.Create(
                    context.DescriptorContext,
                    node)));

        return base.VisitChildren(node, context);
    }

    protected override ISyntaxVisitorAction VisitChildren(
        EnumTypeExtensionNode node,
        SchemaSyntaxVisitorContext context)
    {
        context.Types.Add(
            TypeReference.Create(
                _enumTypeFactory.Create(
                    context.DescriptorContext,
                    node)));

        return base.VisitChildren(node, context);
    }

    protected override ISyntaxVisitorAction VisitChildren(
        ScalarTypeDefinitionNode node,
        SchemaSyntaxVisitorContext context)
    {
        if (node.Directives.Count > 0)
        {
            context.ScalarDirectives[node.Name.Value] = node.Directives;
        }

        return base.VisitChildren(node, context);
    }

    protected override ISyntaxVisitorAction VisitChildren(
        DirectiveDefinitionNode node,
        SchemaSyntaxVisitorContext context)
    {
        if (context.DescriptorContext.Options.EnableTag &&
            node.Name.Value.EqualsOrdinal(WellKnownDirectives.Tag))
        {
            goto EXIT;
        }

        context.Types.Add(
            TypeReference.Create(
                _directiveTypeFactory.Create(
                    context.DescriptorContext,
                    node)));
EXIT:
        return base.VisitChildren(node, context);
    }

    protected override ISyntaxVisitorAction VisitChildren(
        SchemaDefinitionNode node,
        SchemaSyntaxVisitorContext context)
    {
        context.Description = node.Description?.Value;
        context.Directives = node.Directives;

        foreach (var operationType in node.OperationTypes)
        {
            switch (operationType.Operation)
            {
                case OperationType.Query:
                    context.QueryTypeName = operationType.Type.Name.Value;
                    break;

                case OperationType.Mutation:
                    context.MutationTypeName = operationType.Type.Name.Value;
                    break;

                case OperationType.Subscription:
                    context.SubscriptionTypeName = operationType.Type.Name.Value;
                    break;

                default:
                    throw new InvalidOperationException(
                        TypeResources.SchemaSyntaxVisitor_UnknownOperationType);
            }
        }

        return base.VisitChildren(node, context);
    }
}

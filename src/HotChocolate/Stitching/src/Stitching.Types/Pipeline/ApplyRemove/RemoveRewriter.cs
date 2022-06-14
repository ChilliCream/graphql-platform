using System;
using System.Collections.Generic;
using System.Linq;
using HotChocolate.Language;
using HotChocolate.Language.Visitors;
using static HotChocolate.Language.SyntaxKind;

namespace HotChocolate.Stitching.Types.Pipeline.ApplyRemove;

internal sealed class RemoveRewriter : SyntaxRewriter<RemoveContext>
{
    protected override RemoveContext OnEnter(ISyntaxNode node, RemoveContext context)
    {
        context.Navigator.Push(node);
        return base.OnEnter(node, context);
    }

    protected override void OnLeave(ISyntaxNode node, RemoveContext context)
    {
        context.Navigator.Pop();
        base.OnLeave(node, context);
    }

    protected override ObjectTypeDefinitionNode RewriteObjectTypeDefinition(
        ObjectTypeDefinitionNode node,
        RemoveContext context)
    {
        var typeName = node.Name.Value;

        if (context.RemovedTypes.TryGetValue(typeName, out RemoveInfo? _))
        {
            return default!;
        }

        return base.RewriteObjectTypeDefinition(node, context);
    }

    protected override InterfaceTypeDefinitionNode RewriteInterfaceTypeDefinition(
        InterfaceTypeDefinitionNode node,
        RemoveContext context)
    {
        var typeName = node.Name.Value;

        if (context.RemovedTypes.TryGetValue(typeName, out RemoveInfo? _))
        {
            return default!;
        }

        return base.RewriteInterfaceTypeDefinition(node, context);
    }

    protected override UnionTypeDefinitionNode RewriteUnionTypeDefinition(
        UnionTypeDefinitionNode node,
        RemoveContext context)
    {
        var typeName = node.Name.Value;

        if (context.RemovedTypes.TryGetValue(typeName, out RemoveInfo? _))
        {
            return default!;
        }

        return base.RewriteUnionTypeDefinition(node, context);
    }

    protected override InputObjectTypeDefinitionNode RewriteInputObjectTypeDefinition(
        InputObjectTypeDefinitionNode node,
        RemoveContext context)
    {
        var typeName = node.Name.Value;

        if (context.RemovedTypes.TryGetValue(typeName, out RemoveInfo? _))
        {
            return default!;
        }

        return base.RewriteInputObjectTypeDefinition(node, context);
    }

    protected override EnumTypeDefinitionNode RewriteEnumTypeDefinition(
        EnumTypeDefinitionNode node,
        RemoveContext context)
    {
        var typeName = node.Name.Value;

        if (context.RemovedTypes.TryGetValue(typeName, out RemoveInfo? _))
        {
            return default!;
        }

        return base.RewriteEnumTypeDefinition(node, context);
    }

    protected override ScalarTypeDefinitionNode RewriteScalarTypeDefinition(
        ScalarTypeDefinitionNode node,
        RemoveContext context)
    {
        var typeName = node.Name.Value;

        if (context.RemovedTypes.TryGetValue(typeName, out RemoveInfo? _))
        {
            return default!;
        }

        return base.RewriteScalarTypeDefinition(node, context);
    }

    protected override FieldDefinitionNode RewriteFieldDefinition(
        FieldDefinitionNode node,
        RemoveContext context)
    {
        if (context.Navigator.TryPeek(1, out ISyntaxNode? parent)
            && parent.Kind is SyntaxKind.ObjectTypeDefinition or InterfaceTypeDefinition
            && context.TypesWithRemovedFields.Contains(((INamedSyntaxNode)parent).Name.Value))
        {
            var coordinate = context.Navigator.CreateCoordinate();
            if (context.RemovedFields.ContainsKey(coordinate))
            {
                return default!;
            }
        }

        return base.RewriteFieldDefinition(node, context);
    }
}

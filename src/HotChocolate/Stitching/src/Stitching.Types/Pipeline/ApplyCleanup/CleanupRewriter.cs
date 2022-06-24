using HotChocolate.Language;
using HotChocolate.Language.Visitors;

namespace HotChocolate.Stitching.Types.Pipeline.ApplyCleanup;

/// <summary>
/// Removes the discovered Syntax nodes
/// </summary>
internal sealed class CleanupRewriter : SyntaxRewriter<CleanupContext>
{
    /// <summary>
    /// Provides additional instructions to be performed before the <see cref="ISyntaxNode" /> is rewritten.
    /// </summary>
    /// <param name="node">The <see cref="ISyntaxNode" /> to be rewritten.</param>
    /// <param name="context">A <see cref="CleanupContext"/> containing the target schema elements to be rewritten.</param>
    /// <returns>The <see cref="CleanupContext"/> used by the rewriter.</returns>
    protected override CleanupContext OnEnter(ISyntaxNode node, CleanupContext context)
    {
        context.Navigator.Push(node);
        return base.OnEnter(node, context);
    }

    /// <summary>
    /// Provides additional instructions to be performed after the <see cref="ISyntaxNode" /> is rewritten.
    /// </summary>
    /// <param name="node">The <see cref="ISyntaxNode" /> to be rewritten.</param>
    /// <param name="context">A <see cref="CleanupContext"/> containing the target schema elements to be rewritten.</param>
    protected override void OnLeave(ISyntaxNode? node, CleanupContext context)
    {
        context.Navigator.Pop();
        base.OnLeave(node, context);
    }

    /// <summary>
    /// Rewrites an <see cref="ObjectTypeDefinitionNode" />
    /// </summary>
    /// <param name="node">The <see cref="ObjectTypeDefinitionNode" /> to be rewritten.</param>
    /// <param name="context">A <see cref="CleanupContext"/> containing the target schema elements to be rewritten.</param>
    /// <returns>
    /// If the original <see cref="ISyntaxNode"/> is returned the node remains unchanged.
    /// If a new <see cref="ISyntaxNode"/> is returned, the original node is replaced.
    /// If <see langword="null" /> is returned, the node is removed from the parent.
    /// </returns>
    protected override ObjectTypeDefinitionNode? RewriteObjectTypeDefinition(
        ObjectTypeDefinitionNode node,
        CleanupContext context)
    {
        var typeName = node.Name.Value;

        if (context.Types.TryGetValue(typeName, out CleanupInfo? _))
        {
            return default;
        }

        return base.RewriteObjectTypeDefinition(node, context);
    }

    /// <summary>
    /// Rewrites an <see cref="InterfaceTypeDefinitionNode" />
    /// </summary>
    /// <param name="node">The <see cref="InterfaceTypeDefinitionNode" /> to be rewritten.</param>
    /// <param name="context">A <see cref="CleanupContext"/> containing the target schema elements to be rewritten.</param>
    /// <returns>
    /// If the original <see cref="ISyntaxNode"/> is returned the node remains unchanged.
    /// If a new <see cref="ISyntaxNode"/> is returned, the original node is replaced.
    /// If <see langword="null" /> is returned, the node is removed from the parent.
    /// </returns>
    protected override InterfaceTypeDefinitionNode? RewriteInterfaceTypeDefinition(
        InterfaceTypeDefinitionNode node,
        CleanupContext context)
    {
        var typeName = node.Name.Value;

        if (context.Types.TryGetValue(typeName, out CleanupInfo? _))
        {
            return default;
        }

        return base.RewriteInterfaceTypeDefinition(node, context);
    }

    /// <summary>
    /// Rewrites an <see cref="UnionTypeDefinitionNode" />
    /// </summary>
    /// <param name="node">The <see cref="UnionTypeDefinitionNode" /> to be rewritten.</param>
    /// <param name="context">A <see cref="CleanupContext"/> containing the target schema elements to be rewritten.</param>
    /// <returns>
    /// If the original <see cref="ISyntaxNode"/> is returned the node remains unchanged.
    /// If a new <see cref="ISyntaxNode"/> is returned, the original node is replaced.
    /// If <see langword="null" /> is returned, the node is removed from the parent.
    /// </returns>
    protected override UnionTypeDefinitionNode? RewriteUnionTypeDefinition(
        UnionTypeDefinitionNode node,
        CleanupContext context)
    {
        var typeName = node.Name.Value;

        if (context.Types.TryGetValue(typeName, out CleanupInfo? _))
        {
            return default;
        }

        return base.RewriteUnionTypeDefinition(node, context);
    }

    /// <summary>
    /// Rewrites an <see cref="InputObjectTypeDefinitionNode" />
    /// </summary>
    /// <param name="node">The <see cref="InputObjectTypeDefinitionNode" /> to be rewritten.</param>
    /// <param name="context">A <see cref="CleanupContext"/> containing the target schema elements to be rewritten.</param>
    /// <returns>
    /// If the original <see cref="ISyntaxNode"/> is returned the node remains unchanged.
    /// If a new <see cref="ISyntaxNode"/> is returned, the original node is replaced.
    /// If <see langword="null" /> is returned, the node is removed from the parent.
    /// </returns>
    protected override InputObjectTypeDefinitionNode? RewriteInputObjectTypeDefinition(
        InputObjectTypeDefinitionNode node,
        CleanupContext context)
    {
        var typeName = node.Name.Value;

        if (context.Types.TryGetValue(typeName, out CleanupInfo? _))
        {
            return default;
        }

        return base.RewriteInputObjectTypeDefinition(node, context);
    }

    /// <summary>
    /// Rewrites an <see cref="EnumTypeDefinitionNode" />
    /// </summary>
    /// <param name="node">The <see cref="EnumTypeDefinitionNode" /> to be rewritten.</param>
    /// <param name="context">A <see cref="CleanupContext"/> containing the target schema elements to be rewritten.</param>
    /// <returns>
    /// If the original <see cref="ISyntaxNode"/> is returned the node remains unchanged.
    /// If a new <see cref="ISyntaxNode"/> is returned, the original node is replaced.
    /// If <see langword="null" /> is returned, the node is removed from the parent.
    /// </returns>
    protected override EnumTypeDefinitionNode? RewriteEnumTypeDefinition(
        EnumTypeDefinitionNode node,
        CleanupContext context)
    {
        var typeName = node.Name.Value;

        if (context.Types.TryGetValue(typeName, out CleanupInfo? _))
        {
            return default;
        }

        return base.RewriteEnumTypeDefinition(node, context);
    }

    /// <summary>
    /// Rewrites an <see cref="ScalarTypeDefinitionNode" />
    /// </summary>
    /// <param name="node">The <see cref="ScalarTypeDefinitionNode" /> to be rewritten.</param>
    /// <param name="context">A <see cref="CleanupContext"/> containing the target schema elements to be rewritten.</param>
    /// <returns>
    /// If the original <see cref="ISyntaxNode"/> is returned the node remains unchanged.
    /// If a new <see cref="ISyntaxNode"/> is returned, the original node is replaced.
    /// If <see langword="null" /> is returned, the node is removed from the parent.
    /// </returns>
    protected override ScalarTypeDefinitionNode? RewriteScalarTypeDefinition(
        ScalarTypeDefinitionNode node,
        CleanupContext context)
    {
        var typeName = node.Name.Value;

        if (context.Types.TryGetValue(typeName, out CleanupInfo? _))
        {
            return default;
        }

        return base.RewriteScalarTypeDefinition(node, context);
    }

    /// <summary>
    /// Rewrites an <see cref="InterfaceTypeExtensionNode" />
    /// </summary>
    /// <param name="node">The <see cref="InterfaceTypeExtensionNode" /> to be rewritten.</param>
    /// <param name="context">A <see cref="CleanupContext"/> containing the target schema elements to be rewritten.</param>
    /// <returns>
    /// If the original <see cref="ISyntaxNode"/> is returned the node remains unchanged.
    /// If a new <see cref="ISyntaxNode"/> is returned, the original node is replaced.
    /// If <see langword="null" /> is returned, the node is removed from the parent.
    /// </returns>
    protected override InterfaceTypeExtensionNode? RewriteInterfaceTypeExtension(
        InterfaceTypeExtensionNode node,
        CleanupContext context)
    {
        var typeName = node.Name.Value;

        if (context.Types.TryGetValue(typeName, out CleanupInfo? _))
        {
            return default;
        }

        return base.RewriteInterfaceTypeExtension(node, context);
    }

    /// <summary>
    /// Rewrites an <see cref="ObjectTypeExtensionNode" />
    /// </summary>
    /// <param name="node">The <see cref="ObjectTypeExtensionNode" /> to be rewritten.</param>
    /// <param name="context">A <see cref="CleanupContext"/> containing the target schema elements to be rewritten.</param>
    /// <returns>
    /// If the original <see cref="ISyntaxNode"/> is returned the node remains unchanged.
    /// If a new <see cref="ISyntaxNode"/> is returned, the original node is replaced.
    /// If <see langword="null" /> is returned, the node is removed from the parent.
    /// </returns>
    protected override ObjectTypeExtensionNode? RewriteObjectTypeExtension(
        ObjectTypeExtensionNode node,
        CleanupContext context)
    {
        var typeName = node.Name.Value;

        if (context.Types.TryGetValue(typeName, out CleanupInfo? _))
        {
            return default;
        }

        return base.RewriteObjectTypeExtension(node, context);
    }

    /// <summary>
    /// Rewrites an <see cref="InputObjectTypeExtensionNode" />
    /// </summary>
    /// <param name="node">The <see cref="InputObjectTypeExtensionNode" /> to be rewritten.</param>
    /// <param name="context">A <see cref="CleanupContext"/> containing the target schema elements to be rewritten.</param>
    /// <returns>
    /// If the original <see cref="ISyntaxNode"/> is returned the node remains unchanged.
    /// If a new <see cref="ISyntaxNode"/> is returned, the original node is replaced.
    /// If <see langword="null" /> is returned, the node is removed from the parent.
    /// </returns>
    protected override InputObjectTypeExtensionNode? RewriteInputObjectTypeExtension(
        InputObjectTypeExtensionNode node,
        CleanupContext context)
    {
        var typeName = node.Name.Value;

        if (context.Types.TryGetValue(typeName, out CleanupInfo? _))
        {
            return default;
        }

        return base.RewriteInputObjectTypeExtension(node, context);
    }

    /// <summary>
    /// Rewrites an <see cref="UnionTypeExtensionNode" />
    /// </summary>
    /// <param name="node">The <see cref="UnionTypeExtensionNode" /> to be rewritten.</param>
    /// <param name="context">A <see cref="CleanupContext"/> containing the target schema elements to be rewritten.</param>
    /// <returns>
    /// If the original <see cref="ISyntaxNode"/> is returned the node remains unchanged.
    /// If a new <see cref="ISyntaxNode"/> is returned, the original node is replaced.
    /// If <see langword="null" /> is returned, the node is removed from the parent.
    /// </returns>
    protected override UnionTypeExtensionNode? RewriteUnionTypeExtension(
        UnionTypeExtensionNode node,
        CleanupContext context)
    {
        var typeName = node.Name.Value;

        if (context.Types.TryGetValue(typeName, out CleanupInfo? _))
        {
            return default;
        }

        return base.RewriteUnionTypeExtension(node, context);
    }

    /// <summary>
    /// Rewrites an <see cref="EnumTypeExtensionNode" />
    /// </summary>
    /// <param name="node">The <see cref="EnumTypeExtensionNode" /> to be rewritten.</param>
    /// <param name="context">A <see cref="CleanupContext"/> containing the target schema elements to be rewritten.</param>
    /// <returns>
    /// If the original <see cref="ISyntaxNode"/> is returned the node remains unchanged.
    /// If a new <see cref="ISyntaxNode"/> is returned, the original node is replaced.
    /// If <see langword="null" /> is returned, the node is removed from the parent.
    /// </returns>
    protected override EnumTypeExtensionNode? RewriteEnumTypeExtension(
        EnumTypeExtensionNode node,
        CleanupContext context)
    {
        var typeName = node.Name.Value;

        if (context.Types.TryGetValue(typeName, out CleanupInfo? _))
        {
            return default;
        }

        return base.RewriteEnumTypeExtension(node, context);
    }
}

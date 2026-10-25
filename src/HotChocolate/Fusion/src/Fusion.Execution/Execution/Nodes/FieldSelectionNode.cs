using HotChocolate.Execution;
using HotChocolate.Language;

namespace HotChocolate.Fusion.Execution.Nodes;

/// <summary>
/// Represents a field selection node with its path include flags and delivery group.
/// </summary>
public sealed record FieldSelectionNode
{
    [Obsolete("Use the ConditionFlags constructor instead. This constructor cannot express more than 64 conditions.")]
    public FieldSelectionNode(
        FieldNode node,
        ulong pathIncludeFlags,
        DeliveryGroup? deliveryGroup = null)
    {
        Node = node;
        PathIncludeFlags = pathIncludeFlags;
        DeliveryGroup = deliveryGroup;
        PathConditionFlags = new ConditionFlags(pathIncludeFlags);
    }

    /// <summary>
    /// Initializes a new instance of <see cref="FieldSelectionNode"/>.
    /// </summary>
    /// <param name="node">The syntax node that represents the field selection.</param>
    /// <param name="pathIncludeFlags">The flags that must be all set for this selection to be included.</param>
    /// <param name="deliveryGroup">The delivery group context this field was collected under.</param>
    public FieldSelectionNode(
        FieldNode node,
        ConditionFlags pathIncludeFlags,
        DeliveryGroup? deliveryGroup = null)
    {
        Node = node;
        PathIncludeFlags = pathIncludeFlags.Word0;
        DeliveryGroup = deliveryGroup;
        PathConditionFlags = pathIncludeFlags;
    }

    /// <summary>
    /// Gets the syntax node that represents the field selection.
    /// </summary>
    public FieldNode Node { get; }

    /// <summary>
    /// Gets the flags that must be all set for this selection to be included.
    /// </summary>
    public ulong PathIncludeFlags { get; }

    /// <summary>
    /// Gets the delivery group context this field was collected under.
    /// </summary>
    public DeliveryGroup? DeliveryGroup { get; }

    /// <summary>
    /// Gets the flags that must be all set for this selection to be included.
    /// </summary>
    public ConditionFlags PathConditionFlags { get; }

    /// <summary>
    /// Deconstructs this field selection node.
    /// </summary>
    /// <param name="node">The syntax node that represents the field selection.</param>
    /// <param name="pathIncludeFlags">The flags that must be all set for this selection to be included.</param>
    /// <param name="deliveryGroup">The delivery group context this field was collected under.</param>
    public void Deconstruct(
        out FieldNode node,
        out ulong pathIncludeFlags,
        out DeliveryGroup? deliveryGroup)
    {
        node = Node;
        pathIncludeFlags = PathIncludeFlags;
        deliveryGroup = DeliveryGroup;
    }
}

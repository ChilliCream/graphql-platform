using System.Reflection;
using HotChocolate.Types.Descriptors;

namespace HotChocolate.Types;

public abstract class DescriptorAttribute : Attribute, IDescriptorConfiguration
{
    /// <summary>
    /// Gets the order in which the attributes shall be applied.
    /// </summary>
    public int Order { get; set; } = int.MaxValue;

    /// <summary>
    /// Requires the attribute provide this configuration was applied to for reflection.
    /// </summary>
    public bool RequiresAttributeProvider { get; set; }

    /// <summary>
    /// Override this to implement the configuration logic for this attribute.
    /// </summary>
    protected internal abstract void TryConfigure(
        IDescriptorContext context,
        IDescriptor descriptor,
        ICustomAttributeProvider? attributeProvider);

    void IDescriptorConfiguration.TryConfigure(
        IDescriptorContext context,
        IDescriptor descriptor,
        ICustomAttributeProvider? attributeProvider)
        => TryConfigure(context, descriptor, attributeProvider);

    /// <summary>
    /// Allows to apply a child attribute withing the context of this attribute.
    /// </summary>
    protected static void ApplyAttribute<T>(
        IDescriptorContext context,
        IDescriptor descriptor,
        ICustomAttributeProvider? element,
        T attribute)
        where T : DescriptorAttribute
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(descriptor);
        ArgumentNullException.ThrowIfNull(element);
        ArgumentNullException.ThrowIfNull(attribute);

        attribute.TryConfigure(context, descriptor, element);
    }
}

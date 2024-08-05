using System.Reflection;
using HotChocolate.Types.Descriptors;

#nullable enable

namespace HotChocolate.Types;

public abstract class DescriptorAttribute : Attribute
{
    /// <summary>
    /// Gets the order in which the attributes shall be applied.
    /// </summary>
    public int Order { get; set; } = int.MaxValue;

    /// <summary>
    /// Override this to implement the configuration logic for this attribute.
    /// </summary>
    protected internal abstract void TryConfigure(
        IDescriptorContext context,
        IDescriptor descriptor,
        ICustomAttributeProvider element);

    /// <summary>
    /// Allows to apply a child attribute withing the context of this attribute.
    /// </summary>
    protected static void ApplyAttribute<T>(
        IDescriptorContext context,
        IDescriptor descriptor,
        ICustomAttributeProvider element,
        T attribute)
        where T : DescriptorAttribute
    {
        if (context == null)
        {
            throw new ArgumentNullException(nameof(context));
        }

        if (descriptor == null)
        {
            throw new ArgumentNullException(nameof(descriptor));
        }

        if (element == null)
        {
            throw new ArgumentNullException(nameof(element));
        }

        if (attribute == null)
        {
            throw new ArgumentNullException(nameof(attribute));
        }

        attribute.TryConfigure(context, descriptor, element);
    }
}

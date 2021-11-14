using System;
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
}

using System;
using HotChocolate.Types.Descriptors;

namespace HotChocolate.Types.Filters;

[Obsolete("Use HotChocolate.Data.")]
public class FilterOperationDescriptorBase
    : ArgumentDescriptorBase<FilterOperationDefinition>
{
    protected FilterOperationDescriptorBase(
        IDescriptorContext context)
        : base(context)
    {
    }

    protected internal override FilterOperationDefinition Definition { get; protected set; } =
        new();

    protected void Name(string value)
    {
        Definition.Name = value;
    }
}

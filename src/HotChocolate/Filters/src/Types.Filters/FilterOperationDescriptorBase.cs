using System;
using HotChocolate.Types.Descriptors;

namespace HotChocolate.Types.Filters;

[Obsolete("Use HotChocolate.Data.")]
public class FilterOperationDescriptorBase
    : ArgumentDescriptorBase<FilterOperationDefintion>
{
    protected FilterOperationDescriptorBase(
        IDescriptorContext context)
        : base(context)
    {
    }

    protected internal override FilterOperationDefintion Definition { get; protected set; } =
        new();

    protected void Name(string value)
    {
        Definition.Name = value;
    }
}

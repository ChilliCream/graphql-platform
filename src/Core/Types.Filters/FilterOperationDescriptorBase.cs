using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HotChocolate.Language;
using HotChocolate.Types.Descriptors;
using HotChocolate.Types.Descriptors.Definitions;

namespace HotChocolate.Types.Filters
{
    public class FilterOperationDescriptorBase
        : ArgumentDescriptorBase<FilterOperationDefintion>
    {
        protected FilterOperationDescriptorBase(
            IDescriptorContext context)
            : base(context)
        {
        }

        protected override FilterOperationDefintion Definition { get; } =
            new FilterOperationDefintion();

        protected void Name(NameString value)
        {
            Definition.Name = value.EnsureNotEmpty(nameof(value));
        }
    }
}

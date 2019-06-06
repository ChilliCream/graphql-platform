using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HotChocolate.Language;
using HotChocolate.Types.Descriptors;
using HotChocolate.Types.Descriptors.Definitions;

namespace HotChocolate.Types.Filters
{
    public class FilterDescriptorBase
        : ArgumentDescriptorBase<FilterDefintion>
    {
        protected FilterDescriptorBase(
            IDescriptorContext context,
            FilterFieldDefintion fieldDefinition)
            : base(context)
        {
            FieldDefinition = fieldDefinition;
        }

        protected FilterFieldDefintion FieldDefinition { get; }

        protected override FilterDefintion Definition { get; } =
            new FilterDefintion();
    }
}

using System;
using System.Collections.Generic;
using System.Reflection;
using HotChocolate.Types.Descriptors;

namespace HotChocolate.Types.Filters
{
    internal class IgnoredFilterFieldDescriptor
       : FilterFieldDescriptorBase
    {
        public IgnoredFilterFieldDescriptor(
            IDescriptorContext context,
            PropertyInfo property)
            : base(FilterKind.Ignored, context, property)
        {
            Definition.Ignore = true;
        }

        protected override FilterOperationDefintion CreateOperationDefinition(
            FilterOperationKind operationKind)
        {
            throw new NotSupportedException();
        }
    }
}

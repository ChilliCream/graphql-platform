using System;
using System.Reflection;
using HotChocolate.Types.Descriptors;
using HotChocolate.Types.Filters.Conventions;

namespace HotChocolate.Types.Filters
{
    internal class IgnoredFilterFieldDescriptor
       : FilterFieldDescriptorBase
    {
        public IgnoredFilterFieldDescriptor(
            IDescriptorContext context,
            PropertyInfo property,
            IFilterConvention filterConvention)
            : base(FilterKind.Ignored, context, property, filterConvention)
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

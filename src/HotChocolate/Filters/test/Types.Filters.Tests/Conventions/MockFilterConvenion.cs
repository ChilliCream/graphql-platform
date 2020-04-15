using System;
using HotChocolate.Types.Filters.Conventions;

namespace HotChocolate.Types.Filters
{
    public class MockFilterConvention
        : FilterConvention
    {
        public MockFilterConvention()
        {
        }

        public MockFilterConvention(
            Action<IFilterConventionDescriptor> descriptor) : base(descriptor)
        {
        }

        public FilterConventionDefinition GetConventionDefinition()
        {
            return GetOrCreateConfiguration();
        }

        public FilterExpressionVisitorDefintion GetExpressionDefiniton()
        {
            return GetOrCreateConfiguration().VisitorDefinition
                as FilterExpressionVisitorDefintion;
        }

        public new static readonly MockFilterConvention Default
            = new MockFilterConvention();
    }
}

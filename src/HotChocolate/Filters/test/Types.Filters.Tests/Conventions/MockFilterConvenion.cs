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

        public FilterExpressionVisitorDefinition GetExpressionDefinition()
        {
            return GetOrCreateConfiguration().VisitorDefinition
                as FilterExpressionVisitorDefinition;
        }

        public new static MockFilterConvention Default { get; } =
            new MockFilterConvention();
    }
}

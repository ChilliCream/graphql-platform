using System;
using HotChocolate.Types.Sorting.Conventions;

namespace HotChocolate.Types.Sorting
{
    public class MockSortingConvention
        : SortingConvention
    {
        public MockSortingConvention()
        {
        }

        public MockSortingConvention(
            Action<ISortingConventionDescriptor> descriptor) : base(descriptor)
        {
        }

        public SortingConventionDefinition GetConventionDefinition()
        {
            return GetOrCreateConfiguration();
        }

        public SortingExpressionVisitorDefinition GetExpressionDefinition()
        {
            return GetOrCreateConfiguration().VisitorDefinition
                as SortingExpressionVisitorDefinition;
        }

        public new static readonly MockSortingConvention Default
            = new MockSortingConvention();
    }
}

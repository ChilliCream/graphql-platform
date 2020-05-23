using System;
using System.Linq.Expressions;
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

        public FilterVisitorDefinition<Expression> GetExpressionDefinition()
        {
            return GetOrCreateConfiguration().VisitorDefinition
                as FilterVisitorDefinition<Expression>;
        }

        public new static MockFilterConvention Default { get; } =
            new MockFilterConvention();
    }
}

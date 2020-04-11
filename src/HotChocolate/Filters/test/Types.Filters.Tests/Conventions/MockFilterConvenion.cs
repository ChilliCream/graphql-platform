using System;
using System.Collections.Generic;
using HotChocolate.Language;
using HotChocolate.Types.Filters.Conventions;
using HotChocolate.Utilities;
using Xunit;

namespace HotChocolate.Types.Filters
{
    public class MockFilterConvention
        : FilterConvention
    {
        public FilterConventionDefinition GetConventionDefinition()
        {
            return GetOrCreateConfiguration();
        }

        public FilterExpressionVisitorDefintion GetExpressionDefiniton()
        {
            return GetOrCreateConfiguration().VisitorDefinition as FilterExpressionVisitorDefintion;
        }

        public new static readonly MockFilterConvention Default
            = new MockFilterConvention();
    }
}
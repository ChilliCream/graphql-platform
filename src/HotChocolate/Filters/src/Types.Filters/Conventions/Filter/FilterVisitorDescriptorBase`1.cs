using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;
using HotChocolate.Resolvers;
using HotChocolate.Types.Descriptors;
using HotChocolate.Types.Filters.Expressions;

namespace HotChocolate.Types.Filters.Conventions
{
    public abstract class FilterVisitorDescriptorBase<T>
        : IFilterVisitorDescriptorBase<T>
        where T : FilterVisitorDefinitionBase
    {
        protected abstract T Definition { get; }

        public IFilterVisitorDescriptor Convention(FilterConventionDefinition convention)
        {
            Definition.Convention = convention;
            return this;
        }

        public abstract T CreateDefinition();
    }
}

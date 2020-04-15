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
    public class FilterExpressionOperationDefinition
    {
        public FilterOperationKind OperationKind { get; set; }

        public FilterOperationHandler? Handler { get; set; }
    }
}

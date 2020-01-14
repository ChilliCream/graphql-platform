using System;
using System.Collections.Generic;
using System.Text;

namespace HotChocolate.Types.Filters.Conventions
{
    public class FilterConventionTypeDefinition
    {
        public bool Ignore { get; set; }
        public FilterKind FilterKind { get; set; }
        public TryCreateImplicitFilter TryCreateFilter { get; set; }
        public ISet<FilterOperationKind> AllowedOperations { get; set; }
            = new HashSet<FilterOperationKind>();

        public IDictionary<FilterOperationKind, CreateFieldName> OperationNames { get; set; }
            = new Dictionary<FilterOperationKind, CreateFieldName>();

        public IDictionary<FilterOperationKind, string> OperationDescriptions { get; set; }
            = new Dictionary<FilterOperationKind, string>();
    }
}

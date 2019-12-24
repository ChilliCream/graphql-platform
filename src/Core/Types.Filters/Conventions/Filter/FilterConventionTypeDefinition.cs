using System;
using System.Collections.Generic;
using System.Text;

namespace HotChocolate.Types.Filters.Conventions
{
    public class FilterConventionTypeDefinition
    {
        public NameString Name { get; set; }
        public string Description { get; set; }
        public bool Ignore { get; set; }
        public FilterKind FilterKind { get; set; }
        public TryCreateImplicitFilter TryCreateFilter { get; set; }
        public ISet<FilterOperationKind> AllowedOperations { get; set; }
            = new HashSet<FilterOperationKind>();

        public IDictionary<FilterOperationKind, CreateFieldName> OperationNames { get; set; }

        public IDictionary<FilterOperationKind, NameString> OperationDescriptions { get; set; }
    }
}

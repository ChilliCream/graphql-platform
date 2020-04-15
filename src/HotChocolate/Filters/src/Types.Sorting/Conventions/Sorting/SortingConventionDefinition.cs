using System;
using System.Collections.Generic;

namespace HotChocolate.Types.Sorting.Conventions
{
    public class SortingConventionDefinition
    {
        public NameString ArgumentName { get; set; }

        public NameString AscendingName { get; set; }

        public NameString DescendingName { get; set; }

        public GetSortingTypeName TypeNameFactory { get; set; }
            = SortingConventionDefaults.TypeName;

        public GetSortingDescription DescriptionFactory { get; set; }
            = SortingConventionDefaults.Description;

        public GetSortingTypeName OperationKindTypeNameFactory { get; set; }
            = SortingConventionDefaults.OperationKindTypeName;

        public SortingVisitorDefinitionBase? VisitorDefinition { get; set; }

        public IReadOnlyList<TryCreateImplicitSorting> ImplicitSortingFactories { get; set; }
            = Array.Empty<TryCreateImplicitSorting>();
    }
}

using System;
using System.Collections.Generic;

namespace HotChocolate.Types.Sorting.Conventions
{
    public class SortingConventionDefinition
    {
        public NameString ArgumentName { get; set; }

        public NameString AscendingName { get; set; }

        public NameString DescendingName { get; set; }

        public GetSortingTypeName? TypeNameFactory { get; set; }

        public GetSortingDescription? DescriptionFactory { get; set; }

        public GetSortingTypeName? OperationKindTypeNameFactory { get; set; }

        public SortingVisitorDefinitionBase? VisitorDefinition { get; set; }

        public IReadOnlyList<TryCreateImplicitSorting> ImplicitSortingFactories { get; set; }
            = Array.Empty<TryCreateImplicitSorting>();
    }
}

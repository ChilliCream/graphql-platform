using System;

namespace HotChocolate.Types.Sorting
{
    public class SortingNamingConventionSnakeCase : SortingNamingConventionBase
    {
        public override NameString ArgumentName => "order_by";
    }
}

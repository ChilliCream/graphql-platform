
using System;

namespace HotChocolate.Types.Sorting
{
    [Obsolete("Use HotChocolate.Data.")]
    public class SortingNamingConventionSnakeCase : SortingNamingConventionBase
    {
        public override NameString ArgumentName => "order_by";
    }
}

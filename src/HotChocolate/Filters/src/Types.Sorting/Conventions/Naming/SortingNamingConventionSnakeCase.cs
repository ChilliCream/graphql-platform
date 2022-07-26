
using System;

namespace HotChocolate.Types.Sorting;

[Obsolete("Use HotChocolate.Data.")]
public class SortingNamingConventionSnakeCase : SortingNamingConventionBase
{
    public override string ArgumentName => "order_by";
}

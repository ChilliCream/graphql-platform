using System.Globalization;
using HotChocolate.Data.Filters;
using HotChocolate.Data.Marten.Filtering;

namespace HotChocolate.Data.Marten;

internal static class ThrowHelper
{
    public static InvalidOperationException Filtering_MartenQueryableCombinator_InvalidCombinator(
        MartenQueryableCombinator combinator,
        FilterCombinator operation) =>
        new(string.Format(
            CultureInfo.CurrentCulture,
            MartenDataResources.Filtering_MartenQueryableCombinator_InvalidCombinator,
            combinator.GetType(),
            operation.ToString()));
}

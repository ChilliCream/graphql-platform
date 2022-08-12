using System.Linq.Expressions;
using HotChocolate.Data.Filters;
using HotChocolate.Data.Filters.Expressions;

namespace HotChocolate.Data.Marten.Filtering;

public class MartenQueryableFilterProvider: QueryableFilterProvider
{
    public MartenQueryableFilterProvider()
    {
    }

    public MartenQueryableFilterProvider(
        Action<IFilterProviderDescriptor<QueryableFilterContext>> configure)
        : base(configure)
    {
    }

    protected override FilterVisitor<QueryableFilterContext, Expression> Visitor { get; } =
        new(new MartenQueryableCombinator());
}

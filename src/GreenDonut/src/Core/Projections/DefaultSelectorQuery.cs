#if NET8_0_OR_GREATER
using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using static GreenDonut.Projections.ExpressionHelpers;

namespace GreenDonut.Projections;

[Experimental(Experiments.Projections)]
internal sealed class DefaultSelectorQuery<T>(
    IQueryable<T> query,
    Expression<Func<T, T>>? selector)
    : ISelectorQuery<T>
{
    public IQueryable<T> SelectKey(Expression<Func<T, object>> key)
        => selector is not null
            ? query.Select(Combine(selector, Rewrite(key)))
            : query;
}
#endif

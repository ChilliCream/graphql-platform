#if NET8_0_OR_GREATER
using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;

namespace GreenDonut.Projections;

[Experimental(Experiments.Projections)]
public interface ISelectorBuilder
{
    void Add<T>(Expression<Func<T, T>> selector);

    Expression<Func<T, T>>? TryCompile<T>();
}
#endif

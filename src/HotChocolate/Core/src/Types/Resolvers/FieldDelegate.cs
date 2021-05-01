using System.Threading.Tasks;

#nullable enable

namespace HotChocolate.Resolvers
{
    /// <summary>
    /// This delegate defines the interface of a field pipeline that the
    /// execution engine invokes to resolve a field result.
    /// </summary>
    /// <param name="context">The middleware context.</param>
    public delegate ValueTask FieldDelegate(IMiddlewareContext context);

    /// <summary>
    /// This delegate represents a pure field that can be executed without any side-effects.
    /// </summary>
    /// <param name="context">The middleware context.</param>
    public delegate void PureFieldDelegate(IMiddlewareContext context);
}

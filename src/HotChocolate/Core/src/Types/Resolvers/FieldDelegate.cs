using System.Threading.Tasks;

#nullable enable

namespace HotChocolate.Resolvers
{
    /// <summary>
    /// This delegate defines the interface of a field pipeline that the 
    /// execution engine invokes to resolve a field result.
    /// </summary>
    /// <param name="context">The resolver context.</param>
    public delegate ValueTask FieldDelegate(IMiddlewareContext context);
}

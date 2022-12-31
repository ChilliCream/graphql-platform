using System.Threading.Tasks;

namespace HotChocolate.Resolvers;

/// <summary>
/// This delegate defines the interface of a directive field pipeline that the
/// execution engine invokes to resolve a field result.
/// </summary>
/// <param name="context">The directive context.</param>
public delegate ValueTask DirectiveDelegate(IDirectiveContext context);

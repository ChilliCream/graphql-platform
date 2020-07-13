using System.Threading.Tasks;

namespace HotChocolate.Resolvers
{
    public delegate ValueTask DirectiveDelegate(IDirectiveContext context);
}

using System.Reflection;
using HotChocolate.Resolvers;

namespace HotChocolate.Types
{
    public interface IObjectField
        : IOutputField
    {
        MemberInfo Member { get; }

        FieldDelegate Middleware { get; }

        FieldResolverDelegate Resolver { get; }
    }
}

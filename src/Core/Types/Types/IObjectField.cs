using System.Reflection;
using HotChocolate.Resolvers;

namespace HotChocolate.Types
{
    public interface IObjectField
        : IOutputField
    {
        MemberInfo ClrMember { get; }

        FieldDelegate Middleware { get; }

        FieldResolverDelegate Resolver { get; }
    }
}

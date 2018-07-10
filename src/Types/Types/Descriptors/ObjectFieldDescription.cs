using System.Reflection;
using HotChocolate.Resolvers;

namespace HotChocolate.Types
{
    internal class ObjectFieldDescription
        : InterfaceFieldDescription
    {
        public MemberInfo Member { get; set; }

        public FieldResolverDelegate Resolver { get; set; }
    }
}

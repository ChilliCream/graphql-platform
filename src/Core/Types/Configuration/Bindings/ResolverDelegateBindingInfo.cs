using System.Reflection;
using System.Threading.Tasks;
using HotChocolate.Resolvers;

namespace HotChocolate.Configuration
{
    internal class ResolverDelegateBindingInfo
        : ResolverBindingInfo
    {
        public NameString FieldName { get; set; }
        public MemberInfo FieldMember { get; set; }
        public FieldResolverDelegate FieldResolver { get; set; }

        public FieldResolver CreateFieldResolver()
        {
            return new FieldResolver(ObjectTypeName, FieldName, FieldResolver);
        }
    }
}

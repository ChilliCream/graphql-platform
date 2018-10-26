using System.Reflection;
using System.Threading.Tasks;
using HotChocolate.Resolvers;

namespace HotChocolate.Configuration
{
    internal class ResolverDelegateBindingInfo
        : ResolverBindingInfo
    {
        public string FieldName { get; set; }
        public MemberInfo FieldMember { get; set; }
        public AsyncFieldResolverDelegate AsyncFieldResolver { get; set; }
        public FieldResolverDelegate FieldResolver { get; set; }

        public FieldResolver CreateFieldResolver()
        {
            return FieldResolver == null
                ? new FieldResolver(
                    ObjectTypeName,
                    FieldName,
                    AsyncFieldResolver)
                : new FieldResolver(
                    ObjectTypeName,
                    FieldName,
                    (ctx, ct) => Task.FromResult<object>(
                        FieldResolver(ctx, ct)));
        }
    }
}

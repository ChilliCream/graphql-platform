using System.Reflection;
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
            return AsyncFieldResolver == null
                ? new FieldResolver(ObjectTypeName, FieldName, FieldResolver)
                : new FieldResolver(ObjectTypeName, FieldName,
                    (ctx, ct) => AsyncFieldResolver(ctx, ct));
        }
    }
}

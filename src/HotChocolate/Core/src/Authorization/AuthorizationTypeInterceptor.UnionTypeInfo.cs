using HotChocolate.Configuration;
using HotChocolate.Types.Descriptors.Configurations;

namespace HotChocolate.Authorization;

internal sealed partial class AuthorizationTypeInterceptor
{
    private sealed class UnionTypeInfo : TypeInfo<UnionTypeConfiguration>
    {
        public UnionTypeInfo(ITypeCompletionContext context, UnionTypeConfiguration typeDef)
            : base(context, typeDef) { }
    }
}

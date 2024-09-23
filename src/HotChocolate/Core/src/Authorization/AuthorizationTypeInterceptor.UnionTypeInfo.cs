using HotChocolate.Configuration;
using HotChocolate.Types.Descriptors.Definitions;

namespace HotChocolate.Authorization;

internal sealed partial class AuthorizationTypeInterceptor
{
    private sealed class UnionTypeInfo : TypeInfo<UnionTypeDefinition>
    {
        public UnionTypeInfo(ITypeCompletionContext context, UnionTypeDefinition typeDef)
            : base(context, typeDef) { }
    }
}

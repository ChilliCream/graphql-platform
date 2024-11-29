using HotChocolate.Configuration;
using HotChocolate.Types.Descriptors;
using HotChocolate.Types.Descriptors.Definitions;

namespace HotChocolate.Authorization;

internal sealed partial class AuthorizationTypeInterceptor
{
    private sealed class ObjectTypeInfo : TypeInfo<ObjectTypeDefinition>
    {
        public ObjectTypeInfo(ITypeCompletionContext context, ObjectTypeDefinition typeDef)
            : base(context, typeDef)
        {
            TypeRef = TypeReg.TypeReference;
        }

        public TypeReference TypeRef { get; }
    }
}

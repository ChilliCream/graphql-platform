using HotChocolate.Configuration;
using HotChocolate.Types.Descriptors.Definitions;

namespace HotChocolate.Authorization;

internal sealed partial class AuthorizationTypeInterceptor
{
    private abstract class TypeInfo<TDef> : IEquatable<TypeInfo<TDef>> where TDef : DefinitionBase
    {
        protected TypeInfo(ITypeCompletionContext context, TDef typeDef)
        {
            TypeDef = typeDef;
            TypeReg = (RegisteredType)context;
        }

        public TDef TypeDef { get; }

        public RegisteredType TypeReg { get; }

        public bool Equals(TypeInfo<TDef>? other)
        {
            if (other is null)
            {
                return false;
            }

            if (ReferenceEquals(this, other))
            {
                return true;
            }

            return TypeDef.Equals(other.TypeDef);
        }

        public override bool Equals(object? obj)
            => ReferenceEquals(this, obj) ||
                (obj is ObjectTypeInfo other && Equals(other));

        public override int GetHashCode()
            => TypeDef.GetHashCode();
    }
}

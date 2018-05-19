using HotChocolate.Abstractions;

namespace HotChocolate.Introspection
{
    internal static class IntrospectionTypes
    {
        public static readonly NamedType Schema = new NamedType("__Schema");

        public static readonly NonNullType NonNullSchema = new NonNullType(Schema);

        public static readonly NamedType Type = new NamedType("__Type");

        public static readonly NonNullType NonNullType = new NonNullType(Type);

        public static readonly NamedType InputValue = new NamedType("__InputValue");

        public static readonly NonNullType NonNullInputValue = new NonNullType(InputValue);

        public static readonly NamedType TypeKind = new NamedType("__TypeKind");

        public static readonly NonNullType NonNullTypeKind = new NonNullType(TypeKind);

        public static readonly NamedType Field = new NamedType("__Field");

        public static readonly NonNullType NonNullField = new NonNullType(Field);

        public static readonly NamedType EnumValue = new NamedType("__EnumValue");

        public static readonly NonNullType NonNullEnumValue = new NonNullType(EnumValue);

        public static readonly NamedType Directive = new NamedType("__Directive");

        public static readonly NonNullType NonNullDirective = new NonNullType(Directive);

        public static readonly NamedType DirectiveLocation = new NamedType("__DirectiveLocation");

        public static readonly NonNullType NonNullDirectiveLocation = new NonNullType(DirectiveLocation);
    }
}
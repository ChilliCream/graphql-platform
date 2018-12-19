using System.Collections.Generic;
using System.Linq;
using HotChocolate.Configuration;

namespace HotChocolate.Types.Introspection
{
    [Introspection]
    internal sealed class __Type
        : ObjectType<IType>
    {
        protected override void Configure(IObjectTypeDescriptor<IType> descriptor)
        {
            descriptor.Name("__Type");

            descriptor.Description(
                "The fundamental unit of any GraphQL Schema is the type. There are " +
                "many kinds of types in GraphQL as represented by the `__TypeKind` enum." +
                "\n\nDepending on the kind of a type, certain fields describe " +
                "information about that type. Scalar types provide no information " +
                "beyond a name and description, while Enum types provide their values. " +
                "Object and Interface types provide the fields they describe. Abstract " +
                "types, Union and Interface, provide the Object types possible " +
                "at runtime. List and NonNull types compose other types.");

            descriptor.BindFields(BindingBehavior.Explicit);

            descriptor.Field("kind")
                .Type<NonNullType<__TypeKind>>()
                .Resolver(c => c.Parent<IType>().Kind);

            descriptor.Field("name")
                .Type<StringType>()
                .Resolver(c => GetName(c.Parent<IType>()));

            descriptor.Field("description")
                .Type<StringType>()
                .Resolver(c => GetDescription(c.Parent<IType>()));

            descriptor.Field("fields")
                .Type<ListType<NonNullType<__Field>>>()
                .Argument("includeDeprecated",
                    a => a.Type<BooleanType>().DefaultValue(false))
                .Resolver(c => GetFields(c.Parent<IType>(),
                    c.Argument<bool>("includeDeprecated")));

            descriptor.Field("interfaces")
                .Type<ListType<NonNullType<__Type>>>()
                .Resolver(c => GetInterfaces(c.Parent<IType>()));

            descriptor.Field("possibleTypes")
                .Type<ListType<NonNullType<__Type>>>()
                .Resolver(c => GetPossibleTypes(c.Schema, c.Parent<INamedType>()));

            descriptor.Field("enumValues")
                .Type<ListType<NonNullType<__EnumValue>>>()
                .Argument("includeDeprecated",
                    a => a.Type<BooleanType>().DefaultValue(false))
                .Resolver(c => GetEnumValues(c.Parent<IType>(),
                    c.Argument<bool>("includeDeprecated")));

            descriptor.Field("inputFields")
                .Type<ListType<NonNullType<__InputValue>>>()
                .Resolver(c => GetInputFields(c.Parent<IType>()));

            descriptor.Field("ofType")
                .Type<__Type>()
                .Resolver(c => GetOfType(c.Parent<IType>()));
        }

        private string GetName(IType type)
        {
            if (type is INamedType n)
            {
                return n.Name;
            }
            return null;
        }

        private string GetDescription(IType type)
        {
            if (type is INamedType n)
            {
                return n.Description;
            }
            return null;
        }

        private IEnumerable<IOutputField> GetFields(IType type, bool includeDeprecated)
        {
            if (type is IComplexOutputType complexType)
            {
                if (!includeDeprecated)
                {
                    return complexType.Fields
                        .Where(t => !t.IsIntrospectionField && !t.IsDeprecated);
                }
                return complexType.Fields.Where(t => !t.IsIntrospectionField);
            }
            return null;
        }

        private IEnumerable<InterfaceType> GetInterfaces(IType type)
        {
            if (type is ObjectType ot)
            {
                return ot.Interfaces.Values;
            }
            return null;
        }

        private IEnumerable<IType> GetPossibleTypes(ISchema schema, INamedType type)
        {
            if (type.IsAbstractType())
            {
                return schema.GetPossibleTypes(type);
            }
            return null;
        }

        private IEnumerable<EnumValue> GetEnumValues(IType type, bool includeDeprecated)
        {
            if (type is EnumType et)
            {
                IReadOnlyCollection<EnumValue> values = et.Values;
                if (!includeDeprecated)
                {
                    return values.Where(t => !t.IsDeprecated);
                }
                return values;
            }
            return null;
        }

        private IEnumerable<InputField> GetInputFields(IType type)
        {
            if (type is InputObjectType iot)
            {
                return iot.Fields;
            }
            return null;
        }

        private IType GetOfType(IType type)
        {
            if (type is ListType lt)
            {
                return lt.ElementType;
            }
            else if (type is NonNullType nnt)
            {
                return nnt.Type;
            }
            else
            {
                return null;
            }
        }
    }
}

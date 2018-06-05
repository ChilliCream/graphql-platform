using System.Collections.Generic;
using System.Linq;
using HotChocolate.Execution;

namespace HotChocolate.Types.Introspection
{
    internal sealed class __Type
        : ObjectType<IType>
    {
        protected override void Configure(IObjectTypeDescriptor<IType> descriptor)
        {
            descriptor.Description(
                "The fundamental unit of any GraphQL Schema is the type. There are " +
                "many kinds of types in GraphQL as represented by the `__TypeKind` enum." +
                "\n\nDepending on the kind of a type, certain fields describe " +
                "information about that type. Scalar types provide no information " +
                "beyond a name and description, while Enum types provide their values. " +
                "Object and Interface types provide the fields they describe. Abstract " +
                "types, Union and Interface, provide the Object types possible " +
                "at runtime. List and NonNull types compose other types.");

            descriptor.Field("kind")
                .Type<NonNullType<__TypeKind>>()
                .Resolver(c =>
                {
                    IType type = c.Parent<IType>();
                    if (!type.TryGetKind(out TypeKind kind))
                    {
                        return new QueryError("Unknown kind of type: " + type);
                    }
                    return kind;
                });

            descriptor.Field("name")
                .Type<StringType>()
                .Resolver(c =>
                {
                    IType type = c.Parent<IType>();
                    if (type is INamedType n)
                    {
                        return n.Name;
                    }
                    return null;
                });

            descriptor.Field("description")
                .Type<StringType>()
                .Resolver(c =>
                {
                    IType type = c.Parent<IType>();
                    if (type is INamedType n)
                    {
                        return n.Description;
                    }
                    return null;
                });

            descriptor.Field("fields")
                .Type<ListType<NonNullType<__Field>>>()
                .Argument("includeDeprecated",
                    a => a.Type<BooleanType>().DefaultValue(false))
                .Resolver(c =>
                {
                    IType type = c.Parent<IType>();
                    bool includeDeprecated = c.Argument<bool>("includeDeprecated");
                    if (type.IsObjectType() || type.IsInterfaceType())
                    {
                        IReadOnlyDictionary<string, Field> fields =
                            ((IHasFields)type).Fields;
                        if (!includeDeprecated)
                        {
                            return fields.Values.Where(t => !t.IsDeprecated);
                        }
                        return fields.Values;
                    }
                    return null;
                });

            descriptor.Field("interfaces")
                .Type<ListType<NonNullType<__Type>>>()
                .Resolver(c =>
                {
                    IType type = c.Parent<IType>();
                    if (type is ObjectType ot)
                    {
                        return ot.Interfaces.Values;
                    }
                    return null;
                });

            descriptor.Field("possibleTypes")
                .Type<ListType<NonNullType<__Directive>>>()
                .Resolver(c =>
                {
                    INamedType type = c.Parent<INamedType>();
                    if (type.IsAbstractType())
                    {
                        return c.Schema.GetPossibleTypes(type);
                    }
                    return null;
                });

            descriptor.Field("enumValues")
                .Type<ListType<NonNullType<__EnumValue>>>()
                .Argument("includeDeprecated",
                    a => a.Type<BooleanType>().DefaultValue(false))
                .Resolver(c =>
                {
                    IType type = c.Parent<IType>();
                    bool includeDeprecated = c.Argument<bool>("includeDeprecated");
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
                });

            descriptor.Field("inputFields")
                .Type<ListType<NonNullType<__InputValue>>>()
                .Resolver(c =>
                {
                    IType type = c.Parent<IType>();
                    if (type is InputObjectType iot)
                    {
                        return iot.Fields.Values;
                    }
                    return null;
                });

            descriptor.Field("ofType")
                .Type<__Type>()
                .Resolver(c =>
                {
                    IType type = c.Parent<IType>();
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
                });
        }

    }
}

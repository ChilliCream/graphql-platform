using System.Collections.Generic;
using System.Linq;
using HotChocolate.Properties;

namespace HotChocolate.Types.Introspection
{
    [Introspection]
#pragma warning disable IDE1006 // Naming Styles
    internal sealed class __Type
#pragma warning restore IDE1006 // Naming Styles
        : ObjectType<IType>
    {
        protected override void Configure(IObjectTypeDescriptor<IType> descriptor)
        {
            descriptor.Name("__Type");

            descriptor.Description(TypeResources.Type_Description);

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
                    c.ArgumentValue<bool>("includeDeprecated")));

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
                    c.ArgumentValue<bool>("includeDeprecated")));

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

        private IEnumerable<IInterfaceType> GetInterfaces(IType type)
        {
            if (type is IComplexOutputType complexType)
            {
                return complexType.Interfaces;
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

        private IEnumerable<IEnumValue> GetEnumValues(IType type, bool includeDeprecated)
        {
            if (type is EnumType et)
            {
                IReadOnlyCollection<IEnumValue> values = et.Values;
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
            return type switch
            {
                ListType lt => lt.ElementType,
                NonNullType nnt => nnt.Type,
                _ => null
            };
        }
    }
}

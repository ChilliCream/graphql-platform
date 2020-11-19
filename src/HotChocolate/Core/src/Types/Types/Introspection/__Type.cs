#pragma warning disable IDE1006 // Naming Styles
using System.Collections.Generic;
using System.Linq;
using HotChocolate.Properties;

#nullable enable

namespace HotChocolate.Types.Introspection
{
    [Introspection]
    internal sealed class __Type : ObjectType<IType>
    {
        protected override void Configure(IObjectTypeDescriptor<IType> descriptor)
        {
            descriptor
                .Name(Names.__Type)
                .Description(TypeResources.Type_Description)
                // Introspection types must always be bound explicitly so that we
                // do not get any interference with conventions.
                .BindFields(BindingBehavior.Explicit);

            descriptor
                .Field(t => t.Kind)
                .Name(Names.Kind)
                .Type<NonNullType<__TypeKind>>();

            descriptor
                .Field(Names.Name)
                .Type<StringType>()
                .ResolveWith<Resolvers>(t => t.GetName(default!));

            descriptor
                .Field(Names.Description)
                .Type<StringType>()
                .ResolveWith<Resolvers>(t => t.GetDescription(default!));

            descriptor
                .Field(Names.Fields)
                .Argument(Names.IncludeDeprecated, a => a.Type<BooleanType>().DefaultValue(false))
                .Type<ListType<NonNullType<__Field>>>()
                .ResolveWith<Resolvers>(t => t.GetFields(default!, default));

            descriptor
                .Field(Names.Interfaces)
                .Type<ListType<NonNullType<__Type>>>()
                .ResolveWith<Resolvers>(t => t.GetInterfaces(default!));

            descriptor
                .Field(Names.PossibleTypes)
                .Type<ListType<NonNullType<__Type>>>()
                .ResolveWith<Resolvers>(t => t.GetPossibleTypes(default!, default!));

            descriptor
                .Field(Names.EnumValues)
                .Argument(Names.IncludeDeprecated, a => a.Type<BooleanType>().DefaultValue(false))
                .Type<ListType<NonNullType<__EnumValue>>>()
                .ResolveWith<Resolvers>(t => t.GetEnumValues(default!, default!));

            descriptor
                .Field(Names.InputFields)
                .Type<ListType<NonNullType<__InputValue>>>()
                .ResolveWith<Resolvers>(t => t.GetInputFields(default!));

            descriptor
                .Field(Names.OfType)
                .Type<__Type>()
                .ResolveWith<Resolvers>(t => t.GetOfType(default!));

            descriptor
                .Field(Names.SpecifiedBy)
                .Type<StringType>()
                .ResolveWith<Resolvers>(t => t.GetSpecifiedBy(default!));
        }

        private class Resolvers
        {
            public string? GetName([Parent] IType type) =>
                type is INamedType n ? n.Name.Value : null;

            public string? GetDescription([Parent] IType type) =>
                type is INamedType n ? n.Description : null;

            public IEnumerable<IOutputField>? GetFields([Parent] IType type, bool includeDeprecated)
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

            public IEnumerable<IInterfaceType>? GetInterfaces([Parent] IType type) =>
                type is IComplexOutputType complexType ? complexType.Interfaces : null;

            public IEnumerable<IType>? GetPossibleTypes(ISchema schema, [Parent]INamedType type) =>
                type.IsAbstractType() ? schema.GetPossibleTypes(type) : null;

            public IEnumerable<IEnumValue>? GetEnumValues(
                [Parent] IType type,
                bool includeDeprecated)
            {
                if (type is EnumType et)
                {
                    IReadOnlyCollection<IEnumValue> values = et.Values;
                    return includeDeprecated ? values :  values.Where(t => !t.IsDeprecated);
                }
                return null;
            }

            public IEnumerable<InputField>? GetInputFields([Parent] IType type) =>
                type is InputObjectType iot ? iot.Fields : null;

            public IType? GetOfType([Parent] IType type) =>
                type switch
                {
                    ListType lt => lt.ElementType,
                    NonNullType nnt => nnt.Type,
                    _ => null
                };

            public string? GetSpecifiedBy([Parent] IType type) =>
                type is ScalarType scalar
                    ? scalar.SpecifiedBy.ToString()
                    : null;
        }

        public static class Names
        {
            public const string __Type = "__Type";
            public const string Kind = "kind";
            public const string Name = "name";
            public const string Description = "description";
            public const string Fields = "fields";
            public const string Interfaces = "interfaces";
            public const string PossibleTypes = "possibleTypes";
            public const string EnumValues = "enumValues";
            public const string InputFields = "inputFields";
            public const string OfType = "ofType";
            public const string SpecifiedBy = "specifiedBy";
            public const string IncludeDeprecated = "includeDeprecated";
        }
    }
}
#pragma warning restore IDE1006 // Naming Styles

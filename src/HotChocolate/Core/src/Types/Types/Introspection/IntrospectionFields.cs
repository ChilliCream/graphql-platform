using HotChocolate.Properties;
using HotChocolate.Resolvers;
using HotChocolate.Types.Descriptors;
using HotChocolate.Types.Descriptors.Definitions;

#nullable enable

namespace HotChocolate.Types.Introspection;

internal static class IntrospectionFields
{
    private static readonly PureFieldDelegate _typeNameResolver =
        ctx => ctx.ObjectType.Name;

    internal static ObjectFieldConfiguration CreateSchemaField(IDescriptorContext context)
    {
        var descriptor = ObjectFieldDescriptor.New(context, IntrospectionFieldNames.Schema);

        descriptor
            .Description(TypeResources.SchemaField_Description)
            .Type<NonNullType<__Schema>>();

        descriptor.Configuration.PureResolver = Resolve;

        static ISchemaDefinition Resolve(IResolverContext ctx)
            => ctx.Schema;

        return CreateConfiguration(descriptor);
    }

    internal static ObjectFieldConfiguration CreateTypeField(IDescriptorContext context)
    {
        var descriptor = ObjectFieldDescriptor.New(context, IntrospectionFieldNames.Type);

        descriptor
            .Description(TypeResources.TypeField_Description)
            .Argument("name", a => a.Type<NonNullType<StringType>>())
            .Type<__Type>()
            .Resolve(Resolve);

        descriptor.Configuration.PureResolver = Resolve;

        static ITypeDefinition? Resolve(IResolverContext ctx)
        {
            var name = ctx.ArgumentValue<string>("name");
            return ctx.Schema.Types.TryGetType(name, out var type) ? type : null;
        }

        return CreateConfiguration(descriptor);
    }

    internal static ObjectFieldConfiguration CreateTypeNameField(IDescriptorContext context)
    {
        var descriptor = ObjectFieldDescriptor.New(context, IntrospectionFieldNames.TypeName);

        descriptor
            .Description(TypeResources.TypeNameField_Description)
            .Type<NonNullType<StringType>>();

        var definition = descriptor.Extend().Configuration;
        definition.PureResolver = _typeNameResolver;
        definition.Flags |= FieldFlags.TypeNameField;

        return CreateConfiguration(descriptor);
    }

    private static ObjectFieldConfiguration CreateConfiguration(ObjectFieldDescriptor descriptor)
    {
        var definition = descriptor.CreateConfiguration();
        definition.IsIntrospectionField = true;
        return definition;
    }
}

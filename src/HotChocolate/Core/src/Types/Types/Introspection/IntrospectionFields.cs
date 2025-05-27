using HotChocolate.Properties;
using HotChocolate.Resolvers;
using HotChocolate.Types.Descriptors;
using HotChocolate.Types.Descriptors.Configurations;

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

        var configuration = descriptor.Configuration;
        configuration.PureResolver = Resolve;
        configuration.Flags |= CoreFieldFlags.SchemaIntrospectionField | CoreFieldFlags.Introspection;

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

        var configuration = descriptor.Configuration;
        configuration.PureResolver = Resolve;
        configuration.Flags |= CoreFieldFlags.TypeIntrospectionField | CoreFieldFlags.Introspection;

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

        var configuration = descriptor.Extend().Configuration;
        configuration.PureResolver = _typeNameResolver;
        configuration.Flags |= CoreFieldFlags.TypeNameIntrospectionField | CoreFieldFlags.Introspection;

        return CreateConfiguration(descriptor);
    }

    private static ObjectFieldConfiguration CreateConfiguration(ObjectFieldDescriptor descriptor)
    {
        var configuration = descriptor.CreateConfiguration();
        configuration.IsIntrospectionField = true;
        return configuration;
    }
}

using HotChocolate.Properties;
using HotChocolate.Resolvers;
using HotChocolate.Types.Descriptors;
using HotChocolate.Types.Descriptors.Definitions;

#nullable enable

namespace HotChocolate.Types.Introspection;

public static class IntrospectionFields
{
    /// <summary>
    /// Gets the field name of the __typename introspection field.
    /// </summary>
    public static string TypeName => "__typename";

    /// <summary>
    /// Gets the field name of the __schema introspection field.
    /// </summary>
    public static string Schema => "__schema";

    /// <summary>
    /// Gets the field name of the __type introspection field.
    /// </summary>
    public static string Type => "__type";

    private static readonly PureFieldDelegate _typeNameResolver =
        ctx => ctx.ObjectType.Name;

    internal static ObjectFieldDefinition CreateSchemaField(IDescriptorContext context)
    {
        var descriptor = ObjectFieldDescriptor.New(context, Schema);

        descriptor
            .Description(TypeResources.SchemaField_Description)
            .Type<NonNullType<__Schema>>();

        descriptor.Definition.PureResolver = Resolve;

        static ISchema Resolve(IResolverContext ctx)
            => ctx.Schema;

        return CreateDefinition(descriptor);
    }

    internal static ObjectFieldDefinition CreateTypeField(IDescriptorContext context)
    {
        var descriptor = ObjectFieldDescriptor.New(context, Type);

        descriptor
            .Description(TypeResources.TypeField_Description)
            .Argument("name", a => a.Type<NonNullType<StringType>>())
            .Type<__Type>()
            .Resolve(Resolve);

        descriptor.Definition.PureResolver = Resolve;

        static INamedType? Resolve(IResolverContext ctx)
        {
            var name = ctx.ArgumentValue<string>("name");
            return ctx.Schema.TryGetType<INamedType>(name, out var type) ? type : null;
        }

        return CreateDefinition(descriptor);
    }

    internal static ObjectFieldDefinition CreateTypeNameField(IDescriptorContext context)
    {
        var descriptor = ObjectFieldDescriptor.New(context, TypeName);

        descriptor
            .Description(TypeResources.TypeNameField_Description)
            .Type<NonNullType<StringType>>();

        var definition = descriptor.Extend().Definition;
        definition.PureResolver = _typeNameResolver;
        definition.Flags |= FieldFlags.TypeNameField;

        return CreateDefinition(descriptor);
    }

    private static ObjectFieldDefinition CreateDefinition(ObjectFieldDescriptor descriptor)
    {
        var definition = descriptor.CreateDefinition();
        definition.IsIntrospectionField = true;
        return definition;
    }
}

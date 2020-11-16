using HotChocolate.Properties;
using HotChocolate.Types.Descriptors;
using HotChocolate.Types.Descriptors.Definitions;

namespace HotChocolate.Types.Introspection
{
    public static class IntrospectionFields
    {
        /// <summary>
        /// Gets the field name of the __typename introspection field.
        /// </summary>
        public static NameString TypeName { get; } = "__typename";

        /// <summary>
        /// Gets the field name of the __schema introspection field.
        /// </summary>
        public static NameString Schema { get; } = "__schema";

        /// <summary>
        /// Gets the field name of the __type introspection field.
        /// </summary>
        public static NameString Type { get; } = "__type";

        internal static ObjectFieldDefinition CreateSchemaField(IDescriptorContext context)
        {
            var descriptor = ObjectFieldDescriptor.New(context, Schema);

            descriptor
                .Description(TypeResources.SchemaField_Description)
                .Type<NonNullType<__Schema>>()
                .Resolver(ctx => ctx.Schema);

            return CreateDefinition(descriptor);
        }

        internal static ObjectFieldDefinition CreateTypeField(IDescriptorContext context)
        {
            var descriptor = ObjectFieldDescriptor.New(context, Type);

            descriptor
                .Description(TypeResources.TypeField_Description)
                .Argument("name", a => a.Type<NonNullType<StringType>>())
                .Type<__Type>()
                .Resolver(ctx =>
                {
                    var name = ctx.ArgumentValue<string>("name");
                    return ctx.Schema.TryGetType(name, out INamedType type) ? type : null;
                });

            return CreateDefinition(descriptor);
        }

        internal static ObjectFieldDefinition CreateTypeNameField(IDescriptorContext context)
        {
            var descriptor = ObjectFieldDescriptor.New(context, TypeName);

            descriptor
                .Description(TypeResources.TypeNameField_Description)
                .Type<NonNullType<StringType>>()
                .Resolver(ctx => ctx.ObjectType.Name.Value);

            return CreateDefinition(descriptor);
        }

        private static ObjectFieldDefinition CreateDefinition(ObjectFieldDescriptor descriptor)
        {
            ObjectFieldDefinition definition = descriptor.CreateDefinition();
            definition.IsIntrospectionField = true;
            return definition;
        }
    }
}

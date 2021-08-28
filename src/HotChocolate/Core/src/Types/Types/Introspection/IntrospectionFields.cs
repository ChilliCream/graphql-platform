using HotChocolate.Properties;
using HotChocolate.Resolvers;
using HotChocolate.Types.Descriptors;
using HotChocolate.Types.Descriptors.Definitions;

#nullable enable

namespace HotChocolate.Types.Introspection
{
    public static class IntrospectionFields
    {
        /// <summary>
        /// Gets the field name of the __typename introspection field.
        /// </summary>
        public static NameString TypeName => "__typename";

        /// <summary>
        /// Gets the field name of the __schema introspection field.
        /// </summary>
        public static NameString Schema => "__schema";

        /// <summary>
        /// Gets the field name of the __type introspection field.
        /// </summary>
        public static NameString Type => "__type";

        internal static ObjectFieldDefinition CreateSchemaField(IDescriptorContext context)
        {
            var descriptor = ObjectFieldDescriptor.New(context, Schema);

            descriptor
                .Description(TypeResources.SchemaField_Description)
                .Type<NonNullType<__Schema>>()
                .Resolve(Resolve);

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
                .Type<NonNullType<StringType>>()
                .Resolve(Resolve);

            static string Resolve(IResolverContext ctx)
                => ctx.ObjectType.Name.Value;

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

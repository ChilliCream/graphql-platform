using HotChocolate.Configuration;
using HotChocolate.Properties;
using HotChocolate.Resolvers;
using HotChocolate.Types.Descriptors;
using HotChocolate.Types.Descriptors.Definitions;

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

            IObjectFieldDescriptor fieldDescriptor = descriptor
                .Description(TypeResources.SchemaField_Description)
                .Type<NonNullType<__Schema>>()
                .Resolve(ctx => ctx.Schema);

            if (context.Options.FieldMiddleware == FieldMiddlewareApplication.UserDefinedFields)
            {
                fieldDescriptor
                    .Extend()
                    .OnBeforeCreate(definition => definition.PureResolver = ctx => ctx.Schema);
            }

            return CreateDefinition(descriptor);
        }

        internal static ObjectFieldDefinition CreateTypeField(IDescriptorContext context)
        {
            var descriptor = ObjectFieldDescriptor.New(context, Type);

            IObjectFieldDescriptor fieldDescriptor = descriptor
                .Description(TypeResources.TypeField_Description)
                .Argument("name", a => a.Type<NonNullType<StringType>>())
                .Type<__Type>()
                .Resolve(Resolve);

            if (context.Options.FieldMiddleware == FieldMiddlewareApplication.UserDefinedFields)
            {
                fieldDescriptor
                    .Extend()
                    .OnBeforeCreate(definition => definition.PureResolver = Resolve);
            }

            INamedType Resolve(IResolverContext ctx)
            {
                var name = ctx.ArgumentValue<string>("name");
                return ctx.Schema.TryGetType(name, out INamedType type) ? type : null;
            }

            return CreateDefinition(descriptor);
        }

        internal static ObjectFieldDefinition CreateTypeNameField(IDescriptorContext context)
        {
            var descriptor = ObjectFieldDescriptor.New(context, TypeName);

            IObjectFieldDescriptor fieldDescriptor = descriptor
                .Description(TypeResources.TypeNameField_Description)
                .Type<NonNullType<StringType>>()
                .Resolver(Resolve);

            if (context.Options.FieldMiddleware == FieldMiddlewareApplication.UserDefinedFields)
            {
                fieldDescriptor
                    .Extend()
                    .OnBeforeCreate(definition => definition.PureResolver = Resolve);
            }

            string Resolve(IResolverContext ctx) => ctx.ObjectType.Name.Value;

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

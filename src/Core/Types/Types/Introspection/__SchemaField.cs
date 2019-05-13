using HotChocolate.Properties;
using HotChocolate.Types.Descriptors;
using HotChocolate.Types.Descriptors.Definitions;

namespace HotChocolate.Types.Introspection
{
    [Introspection]
#pragma warning disable IDE1006 // Naming Styles
    internal sealed class __SchemaField
#pragma warning restore IDE1006 // Naming Styles
        : ObjectField
    {
        internal __SchemaField(IDescriptorContext context)
            : base(CreateDefinition(context))
        {
        }

        public override bool IsIntrospectionField => true;

        private static ObjectFieldDefinition CreateDefinition(
            IDescriptorContext context)
        {
            var descriptor = ObjectFieldDescriptor
                .New(context, IntrospectionFields.Schema);

            descriptor.Description(TypeResources.SchemaField_Description)
                .Type<NonNullType<__Schema>>()
                .Resolver(ctx => ctx.Schema);

            return descriptor.CreateDefinition();
        }
    }
}

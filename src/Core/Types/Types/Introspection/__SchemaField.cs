using HotChocolate.Types.Descriptors;
using HotChocolate.Types.Descriptors.Definitions;

namespace HotChocolate.Types.Introspection
{
    // TODO : resources
    [Introspection]
    internal sealed class __SchemaField
        : ObjectField
    {
        internal __SchemaField(IDescriptorContext context)
            : base(CreateDefinition(context))
        {
        }

        public override bool IsIntrospectionField { get; } = true;

        private static ObjectFieldDefinition CreateDefinition(
            IDescriptorContext context)
        {
            var descriptor = ObjectFieldDescriptor
                .New(context, IntrospectionFields.Schema);

            descriptor.Description(
                "Access the current type schema of this server.")
                .Type<NonNullType<__Schema>>()
                .Resolver(ctx => ctx.Schema);

            return descriptor.CreateDefinition();
        }
    }
}

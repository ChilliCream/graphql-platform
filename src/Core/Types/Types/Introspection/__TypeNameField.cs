using HotChocolate.Types.Descriptors;
using HotChocolate.Types.Descriptors.Definitions;

namespace HotChocolate.Types.Introspection
{
    // TODO : resources
    [Introspection]
    internal sealed class __TypeNameField
        : ObjectField
    {
        internal __TypeNameField(IDescriptorContext context)
           : base(CreateDefinition(context))
        {
        }

        public override bool IsIntrospectionField { get; } = true;

        private static ObjectFieldDefinition CreateDefinition(
            IDescriptorContext context)
        {
            ObjectFieldDescriptor descriptor = ObjectFieldDescriptor
                .New(context, IntrospectionFields.TypeName);

            descriptor.Description(
                "The name of the current Object type at runtime.")
                .Type<NonNullType<StringType>>()
                .Resolver(ctx => ctx.ObjectType.Name.Value);

            return descriptor.CreateDefinition();
        }
    }
}

using HotChocolate.Properties;
using HotChocolate.Types.Descriptors;
using HotChocolate.Types.Descriptors.Definitions;

namespace HotChocolate.Types.Introspection
{
    [Introspection]
    internal sealed class __TypeNameField
        : ObjectField
    {
        internal __TypeNameField(IDescriptorContext context)
           : base(CreateDefinition(context))
        {
        }

        public override bool IsIntrospectionField => true;

        private static ObjectFieldDefinition CreateDefinition(
            IDescriptorContext context)
        {
            var descriptor = ObjectFieldDescriptor
                .New(context, IntrospectionFields.TypeName);

            descriptor.Description(TypeResources.TypeNameField_Description)
                .Type<NonNullType<StringType>>()
                .Resolver(ctx => ctx.ObjectType.Name.Value);

            return descriptor.CreateDefinition();
        }
    }
}

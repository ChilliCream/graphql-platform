using HotChocolate.Types.Descriptors;
using HotChocolate.Types.Descriptors.Definitions;

namespace HotChocolate.Types.Introspection
{
    // TODO : resources
    [Introspection]
    internal sealed class __TypeField
        : ObjectField
    {
        internal __TypeField(IDescriptorContext context)
            : base(CreateDefinition(context))
        {
        }

        public override bool IsIntrospectionField { get; } = true;

        private static ObjectFieldDefinition CreateDefinition(
            IDescriptorContext context)
        {
            ObjectFieldDescriptor descriptor = ObjectFieldDescriptor
                .New(context, IntrospectionFields.Type);

            descriptor.Description(
                "Request the type information of a single type.")
                .Argument("name", a => a.Type<NonNullType<StringType>>())
                .Type<__Type>()
                .Resolver(ctx => ctx.Schema.GetType<INamedType>(
                    ctx.Argument<string>("name")));

            return descriptor.CreateDefinition();
        }
    }
}

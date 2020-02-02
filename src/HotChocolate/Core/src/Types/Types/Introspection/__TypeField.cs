using HotChocolate.Properties;
using HotChocolate.Types.Descriptors;
using HotChocolate.Types.Descriptors.Definitions;

namespace HotChocolate.Types.Introspection
{
    [Introspection]
#pragma warning disable IDE1006 // Naming Styles
    internal sealed class __TypeField
#pragma warning restore IDE1006 // Naming Styles
        : ObjectField
    {
        internal __TypeField(IDescriptorContext context)
            : base(CreateDefinition(context))
        {
        }

        public override bool IsIntrospectionField => true;

        private static ObjectFieldDefinition CreateDefinition(
            IDescriptorContext context)
        {
            var descriptor = ObjectFieldDescriptor
                .New(context, IntrospectionFields.Type);

            descriptor.Description(
                TypeResources.TypeField_Description)
                .Argument("name", a => a.Type<NonNullType<StringType>>())
                .Type<__Type>()
                .Resolver(ctx =>
                {
                    string name = ctx.Argument<string>("name");
                    if (ctx.Schema.TryGetType(name, out INamedType type))
                    {
                        return type;
                    }
                    return null;
                });

            return descriptor.CreateDefinition();
        }
    }
}

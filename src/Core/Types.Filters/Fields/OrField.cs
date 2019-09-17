using HotChocolate.Types.Descriptors;
using HotChocolate.Types.Descriptors.Definitions;

namespace HotChocolate.Types.Filters
{
    public sealed class OrField
        : InputField
        , IOrField
    {
        internal OrField(
            IDescriptorContext context,
            InputObjectType filterType)
            : base(CreateDefinition(context, filterType))
        {
        }

        private static InputFieldDefinition CreateDefinition(
            IDescriptorContext context, InputObjectType filterType)
        {
            var definition = InputFieldDescriptor
                .New(context, "OR")
                .CreateDefinition();

            definition.Type = new SchemaTypeReference(
                new ListType(new NonNullType(filterType)));

            return definition;
        }
    }
}

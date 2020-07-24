using HotChocolate.Types;
using HotChocolate.Types.Descriptors;
using HotChocolate.Types.Descriptors.Definitions;

namespace HotChocolate.Data.Filters
{
    public sealed class AndField
        : InputField
        , IAndField
    {
        internal AndField(
            IDescriptorContext context,
            string? scope,
            InputObjectType filterType)
            : base(CreateDefinition(context, scope, filterType))
        {
        }

        private static InputFieldDefinition CreateDefinition(
            IDescriptorContext context,
            string? scope,
            InputObjectType filterType)
        {
            FilterOperationFieldDefinition? definition = FilterOperationFieldDescriptor
                .New(context, scope, Operations.And)
                .CreateDefinition();

            definition.Type = new SchemaTypeReference(
                new ListType(new NonNullType(filterType)));

            return definition;
        }
    }
}

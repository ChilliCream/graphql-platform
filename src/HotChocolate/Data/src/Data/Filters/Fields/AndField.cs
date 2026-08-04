using HotChocolate.Configuration;
using HotChocolate.Types;
using HotChocolate.Types.Descriptors;
using HotChocolate.Types.Descriptors.Configurations;

namespace HotChocolate.Data.Filters;

public sealed class AndField
    : FilterOperationField
    , IAndField
{
    internal AndField(FilterOperationFieldConfiguration configuration, int index)
        : base(configuration, index)
    {
    }

    public new FilterInputType DeclaringType => base.DeclaringType;

    IFilterInputType IAndField.DeclaringType => DeclaringType;

    protected override void OnCompleteField(
        ITypeCompletionContext context,
        ITypeSystemMember declaringMember,
        InputFieldConfiguration definition)
    {
        definition.Type = TypeReference.Parse(
            $"[{context.Type.Name}!]",
            TypeContext.Input,
            context.Type.Scope);

        base.OnCompleteField(context, declaringMember, definition);
    }

    internal static FilterOperationFieldConfiguration CreateConfiguration(
        IDescriptorContext context,
        string? scope) =>
        FilterOperationFieldDescriptor
            .New(context, DefaultFilterOperations.And, scope)
            .CreateConfiguration();
}

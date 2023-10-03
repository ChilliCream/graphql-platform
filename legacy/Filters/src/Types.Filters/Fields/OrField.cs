using System;
using HotChocolate.Configuration;
using HotChocolate.Language;
using HotChocolate.Types.Descriptors;
using HotChocolate.Types.Descriptors.Definitions;

namespace HotChocolate.Types.Filters;

[Obsolete("Use HotChocolate.Data.")]
public sealed class OrField : InputField, IOrField
{
    private const string _name = "OR";

    internal OrField(IDescriptorContext context, int index)
        : base(CreateDefinition(context), index)
    {
    }

    private static InputFieldDefinition CreateDefinition(
        IDescriptorContext context) =>
        InputFieldDescriptor
            .New(context, _name)
            .CreateDefinition();

    protected override void OnCompleteField(
        ITypeCompletionContext context,
        ITypeSystemMember declaringMember,
        InputFieldDefinition definition)
    {
        definition.Type = TypeReference.Create(
            new ListTypeNode(
                new NonNullTypeNode(
                    new NamedTypeNode(context.Type.Name))),
            TypeContext.Input,
            context.Type.Scope);

        base.OnCompleteField(context, declaringMember, definition);
    }
}

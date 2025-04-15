using HotChocolate.Configuration;
using HotChocolate.Properties;
using HotChocolate.Types.Descriptors.Definitions;
using HotChocolate.Types.Helpers;

#nullable enable

namespace HotChocolate.Types;

public sealed class EnumValue : IEnumValue, IEnumValueCompletion
{
    private EnumValueConfiguration? _enumValueDefinition;

    public EnumValue(EnumValueConfiguration enumValueDefinition)
    {
        if (enumValueDefinition is null)
        {
            throw new ArgumentNullException(nameof(enumValueDefinition));
        }

        if (enumValueDefinition.RuntimeValue is null)
        {
            throw new ArgumentException(
                TypeResources.EnumValue_ValueIsNull,
                nameof(enumValueDefinition));
        }

        _enumValueDefinition = enumValueDefinition;

        Name = string.IsNullOrEmpty(enumValueDefinition.Name)
            ? enumValueDefinition.RuntimeValue.ToString()!
            : enumValueDefinition.Name;
        Description = enumValueDefinition.Description;
        DeprecationReason = enumValueDefinition.DeprecationReason;
        IsDeprecated = !string.IsNullOrEmpty(enumValueDefinition.DeprecationReason);
        Value = enumValueDefinition.RuntimeValue;
        ContextData = enumValueDefinition.GetContextData();
    }

    public string Name { get; }

    public string? Description { get; }

    public bool IsDeprecated { get; }

    public string? DeprecationReason { get; }

    public object Value { get; }

    public IDirectiveCollection Directives { get; private set; } = default!;

    public IReadOnlyDictionary<string, object?> ContextData { get; }
    void IEnumValueCompletion.CompleteMetadata(
        ITypeCompletionContext context,
        ITypeSystemMember declaringMember)
    {
        Directives = DirectiveCollection.CreateAndComplete(
            context,
            this,
            _enumValueDefinition!.GetDirectives());
        _enumValueDefinition = null;
    }
}

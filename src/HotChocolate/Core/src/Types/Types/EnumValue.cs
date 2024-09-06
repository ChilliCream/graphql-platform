using HotChocolate.Configuration;
using HotChocolate.Properties;
using HotChocolate.Types.Descriptors.Definitions;
using HotChocolate.Utilities;

#nullable enable

namespace HotChocolate.Types;

public sealed class EnumValue : IEnumValue
{
    public EnumValue(
        ITypeCompletionContext completionContext,
        EnumValueDefinition enumValueDefinition)
    {
        if (completionContext == null)
        {
            throw new ArgumentNullException(nameof(completionContext));
        }

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

        Name = string.IsNullOrEmpty(enumValueDefinition.Name)
            ? enumValueDefinition.RuntimeValue.ToString()!
            : enumValueDefinition.Name;
        Description = enumValueDefinition.Description;
        DeprecationReason = enumValueDefinition.DeprecationReason;
        IsDeprecated = !string.IsNullOrEmpty(enumValueDefinition.DeprecationReason);
        Value = enumValueDefinition.RuntimeValue;
        ContextData = enumValueDefinition.GetContextData();

        Directives = DirectiveCollection.CreateAndComplete(
            completionContext,
            this,
            enumValueDefinition.GetDirectives());

        if (!Name.IsValidGraphQLName())
        {
            completionContext.ReportError(
                SchemaErrorBuilder.New()
                    .SetMessage(
                        TypeResources.EnumValue_OnComplete_InvalidName,
                        completionContext.Type.Name,
                        Name)
                    .SetCode(ErrorCodes.Schema.InvalidName)
                    .SetTypeSystemObject(completionContext.Type)
                    .Build());
        }
    }

    public string Name { get; }

    public string? Description { get; }

    public bool IsDeprecated { get; }

    public string? DeprecationReason { get; }

    public object Value { get; }

    public IDirectiveCollection Directives { get; }

    public IReadOnlyDictionary<string, object?> ContextData { get; }
}

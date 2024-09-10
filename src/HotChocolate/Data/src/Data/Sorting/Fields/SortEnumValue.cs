using HotChocolate.Configuration;
using HotChocolate.Types;

namespace HotChocolate.Data.Sorting;

public sealed class SortEnumValue : ISortEnumValue
{
    private readonly DirectiveCollection _directives;

    public SortEnumValue(
        ITypeCompletionContext completionContext,
        SortEnumValueDefinition enumValueDefinition)
    {
        if (completionContext == null)
        {
            throw new ArgumentNullException(nameof(completionContext));
        }

        if (enumValueDefinition is null)
        {
            throw new ArgumentNullException(nameof(enumValueDefinition));
        }

        if (enumValueDefinition.Value is null)
        {
            throw new ArgumentException(
                DataResources.SortEnumValue_ValueIsNull,
                nameof(enumValueDefinition));
        }

        Name = !string.IsNullOrEmpty(enumValueDefinition.Name)
            ? enumValueDefinition.Name
            : enumValueDefinition.Value.ToString()!;
        Description = enumValueDefinition.Description;
        DeprecationReason = enumValueDefinition.DeprecationReason;
        IsDeprecated = enumValueDefinition.IsDeprecated;
        Value = enumValueDefinition.Value;
        ContextData = enumValueDefinition.GetContextData();
        Handler = enumValueDefinition.Handler;
        Operation = enumValueDefinition.Operation;

        _directives = DirectiveCollection.CreateAndComplete(
            completionContext,
            this,
            enumValueDefinition.GetDirectives());
    }

    public string Name { get; }

    public string? Description { get; }

    public bool IsDeprecated { get; }

    public string? DeprecationReason { get; }

    public object Value { get; }

    public IDirectiveCollection Directives => _directives;

    public IReadOnlyDictionary<string, object?> ContextData { get; }

    public ISortOperationHandler Handler { get; }

    public int Operation { get; }
}

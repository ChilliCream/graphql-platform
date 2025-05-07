using HotChocolate.Configuration;
using HotChocolate.Properties;
using HotChocolate.Types.Descriptors.Definitions;

#nullable enable

namespace HotChocolate.Types;

public sealed class DefaultEnumValue : EnumValue
{
    private EnumValueConfiguration? _configuration;
    private DirectiveCollection _directives = null!;

    public DefaultEnumValue(EnumValueConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        if (configuration.RuntimeValue is null)
        {
            throw new ArgumentException(
                TypeResources.EnumValue_ValueIsNull,
                nameof(configuration));
        }

        _configuration = configuration;

        Name = string.IsNullOrEmpty(configuration.Name)
            ? configuration.RuntimeValue.ToString()!
            : configuration.Name;
        Description = configuration.Description;
        DeprecationReason = configuration.DeprecationReason;
        IsDeprecated = !string.IsNullOrEmpty(configuration.DeprecationReason);
        Value = configuration.RuntimeValue;
        ContextData = configuration.GetFeatures();
    }

    public override string Name { get; }

    public override string? Description { get; }

    public override bool IsDeprecated { get; }

    public override string? DeprecationReason { get; }

    public override object Value { get; }

    public override DirectiveCollection Directives => _directives;

    public override IReadOnlyDictionary<string, object?> ContextData { get; }

    protected override void OnCompleteMetadata(
        ITypeCompletionContext context,
        ITypeSystemMember declaringMember)
    {
        _directives = DirectiveCollection.CreateAndComplete(
            context,
            this,
            _configuration!.GetDirectives());
        _configuration = null;
    }
}

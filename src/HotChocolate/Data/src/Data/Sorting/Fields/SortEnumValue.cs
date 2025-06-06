using HotChocolate.Configuration;
using HotChocolate.Features;
using HotChocolate.Properties;
using HotChocolate.Types;

namespace HotChocolate.Data.Sorting;

public sealed class SortEnumValue : EnumValue
{
    private SortEnumValueConfiguration? _configuration;
    private DirectiveCollection _directives = null!;

    public SortEnumValue(SortEnumValueConfiguration configuration)
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
        Features = configuration.GetFeatures();
        Handler = configuration.Handler;
        Operation = configuration.Operation;
    }

    public override string Name { get; }

    public override string? Description { get; }

    public override bool IsDeprecated { get; }

    public override string? DeprecationReason { get; }

    public override object Value { get; }

    public ISortOperationHandler Handler { get; }

    public int Operation { get; }

    public override DirectiveCollection Directives => _directives;

    public override IFeatureCollection Features { get; }

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

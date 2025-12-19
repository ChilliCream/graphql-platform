using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using HotChocolate.Configuration;
using HotChocolate.Language;
using HotChocolate.Types;
using HotChocolate.Types.Descriptors.Configurations;

namespace HotChocolate.Data.Sorting;

public class SortEnumType : EnumType
{
    private Action<ISortEnumTypeDescriptor>? _configure;

    public SortEnumType()
    {
        _configure = Configure;
    }

    public SortEnumType(Action<ISortEnumTypeDescriptor> configure)
    {
        _configure = configure ??
            throw new ArgumentNullException(nameof(configure));
    }

    protected override EnumTypeConfiguration CreateConfiguration(
        ITypeDiscoveryContext context)
    {
        var descriptor = SortEnumTypeDescriptor.FromSchemaType(
            context.DescriptorContext,
            GetType(),
            context.Scope);

        _configure!(descriptor);
        _configure = null;

        return descriptor.CreateConfiguration();
    }

    protected virtual void Configure(ISortEnumTypeDescriptor descriptor)
    {
    }

    protected override bool TryCreateEnumValue(
        ITypeCompletionContext context,
        EnumValueConfiguration definition,
        [NotNullWhen(true)] out EnumValue? enumValue)
    {
        if (definition is SortEnumValueConfiguration sortDefinition)
        {
            enumValue = new SortEnumValue(sortDefinition);
            return true;
        }

        enumValue = null;
        return false;
    }

    public SortEnumValue? ParseSortLiteral(IValueNode valueSyntax)
    {
        ArgumentNullException.ThrowIfNull(valueSyntax);

        if (valueSyntax is EnumValueNode evn
            && ValueLookup.TryGetValue(evn.Value, out var ev)
            && ev is SortEnumValue sortEnumValue)
        {
            return sortEnumValue;
        }

        if (valueSyntax is StringValueNode svn
            && Values.TryGetValue(svn.Value, out ev)
            && ev is SortEnumValue sortEnumValueOfString)
        {
            return sortEnumValueOfString;
        }

        if (valueSyntax is NullValueNode)
        {
            return null;
        }

        throw new LeafCoercionException(
            string.Format(
                CultureInfo.InvariantCulture,
                DataResources.SortingEnumType_Cannot_ParseLiteral,
                Name,
                valueSyntax.GetType().Name),
            this);
    }

    protected sealed override void Configure(IEnumTypeDescriptor descriptor)
        => throw new NotSupportedException();
}

using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using HotChocolate.Configuration;
using HotChocolate.Language;
using HotChocolate.Types;
using HotChocolate.Types.Descriptors.Definitions;

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

    protected override EnumTypeDefinition CreateDefinition(
        ITypeDiscoveryContext context)
    {
        var descriptor = SortEnumTypeDescriptor.FromSchemaType(
            context.DescriptorContext,
            GetType(),
            context.Scope);

        _configure!(descriptor);
        _configure = null;

        return descriptor.CreateDefinition();
    }

    protected virtual void Configure(ISortEnumTypeDescriptor descriptor)
    {
    }

    protected override bool TryCreateEnumValue(
        ITypeCompletionContext context,
        EnumValueDefinition definition,
        [NotNullWhen(true)] out IEnumValue? enumValue)
    {
        if (definition is SortEnumValueDefinition sortDefinition)
        {
            enumValue = new SortEnumValue(context, sortDefinition);
            return true;
        }

        enumValue = null;
        return false;
    }

    public ISortEnumValue? ParseSortLiteral(IValueNode valueSyntax)
    {
        if (valueSyntax is null)
        {
            throw new ArgumentNullException(nameof(valueSyntax));
        }

        if (valueSyntax is EnumValueNode evn &&
            ValueLookup.TryGetValue(evn.Value, out var ev) &&
            ev is ISortEnumValue sortEnumValue)
        {
            return sortEnumValue;
        }

        if (valueSyntax is StringValueNode svn &&
            NameLookup.TryGetValue(svn.Value, out ev) &&
            ev is ISortEnumValue sortEnumValueOfString)
        {
            return sortEnumValueOfString;
        }

        if (valueSyntax is NullValueNode)
        {
            return null;
        }

        throw new SerializationException(
            string.Format(
                CultureInfo.InvariantCulture,
                DataResources.SortingEnumType_Cannot_ParseLiteral,
                Name,
                valueSyntax.GetType().Name),
            this);
    }

    protected sealed override void Configure(IEnumTypeDescriptor descriptor)
    {
        throw new NotSupportedException();
    }
}

using HotChocolate.Configuration;
using HotChocolate.Internal;
using HotChocolate.Types;
using HotChocolate.Types.Descriptors.Configurations;
using static HotChocolate.Internal.FieldInitHelper;

namespace HotChocolate.Data.Filters;

public class FilterInputType
    : InputObjectType
    , IFilterInputType
{
    private Action<IFilterInputTypeDescriptor>? _configure;

    public FilterInputType()
    {
        _configure = Configure;
    }

    public FilterInputType(Action<IFilterInputTypeDescriptor> configure)
    {
        _configure = configure ??
            throw new ArgumentNullException(nameof(configure));
    }

    public IExtendedType EntityType { get; private set; } = null!;

    protected override InputObjectTypeConfiguration CreateConfiguration(
        ITypeDiscoveryContext context)
    {
        try
        {
            if (Configuration is null)
            {
                var descriptor = FilterInputTypeDescriptor
                    .FromSchemaType(context.DescriptorContext, GetType(), context.Scope);
                _configure!(descriptor);
                Configuration = descriptor.CreateConfiguration();
            }

            return Configuration;
        }
        finally
        {
            _configure = null;
        }
    }

    protected override void OnRegisterDependencies(
        ITypeDiscoveryContext context,
        InputObjectTypeConfiguration configuration)
    {
        base.OnRegisterDependencies(context, configuration);
        if (configuration is FilterInputTypeConfiguration { EntityType: { } } filterDefinition)
        {
            SetTypeIdentity(typeof(FilterInputType<>)
                .MakeGenericType(filterDefinition.EntityType));
        }
    }

    protected virtual void Configure(IFilterInputTypeDescriptor descriptor)
    {
    }

    protected override void OnCompleteType(
        ITypeCompletionContext context,
        InputObjectTypeConfiguration configuration)
    {
        base.OnCompleteType(context, configuration);

        if (configuration is FilterInputTypeConfiguration ft &&
            ft.EntityType is { })
        {
            EntityType = context.TypeInspector.GetType(ft.EntityType);
        }
    }

    protected override InputFieldCollection OnCompleteFields(
        ITypeCompletionContext context,
        InputObjectTypeConfiguration definition)
    {
        var fields = new InputField[definition.Fields.Count + 2];
        var index = 0;

        if (definition is FilterInputTypeConfiguration { UseAnd: true } def)
        {
            fields[index] = new AndField(context.DescriptorContext, index, def.Scope);
            index++;
        }

        if (definition is FilterInputTypeConfiguration { UseOr: true } defOr)
        {
            fields[index] = new OrField(context.DescriptorContext, index, defOr.Scope);
            index++;
        }

        foreach (var fieldDefinition in
            definition.Fields.Where(t => !t.Ignore))
        {
            switch (fieldDefinition)
            {
                case FilterOperationFieldConfiguration operation:
                    fields[index] = new FilterOperationField(operation, index);
                    index++;
                    break;

                case FilterFieldConfiguration field:
                    fields[index] = new FilterField(field, index);
                    index++;
                    break;
            }
        }

        if (fields.Length > index)
        {
            Array.Resize(ref fields, index);
        }

        return new InputFieldCollection(CompleteFields(context, this, fields));
    }

    // we are disabling the default configure method so
    // that this does not lead to confusion.
    protected sealed override void Configure(
        IInputObjectTypeDescriptor descriptor)
    {
        throw new NotSupportedException();
    }
}

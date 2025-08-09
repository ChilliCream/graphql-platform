using HotChocolate.Configuration;
using HotChocolate.Internal;
using HotChocolate.Types;
using HotChocolate.Types.Descriptors.Configurations;
using static HotChocolate.Internal.FieldInitHelper;

namespace HotChocolate.Data.Sorting;

public class SortInputType
    : InputObjectType
    , ISortInputType
{
    private Action<ISortInputTypeDescriptor>? _configure;

    public SortInputType()
    {
        _configure = Configure;
    }

    public SortInputType(Action<ISortInputTypeDescriptor> configure)
    {
        _configure = configure ?? throw new ArgumentNullException(nameof(configure));
    }

    public IExtendedType EntityType { get; private set; } = null!;

    protected override InputObjectTypeConfiguration CreateConfiguration(ITypeDiscoveryContext context)
    {
        if (Configuration is null)
        {
            var descriptor = SortInputTypeDescriptor.FromSchemaType(
                context.DescriptorContext,
                GetType(),
                context.Scope);

            _configure!(descriptor);
            _configure = null;

            Configuration = descriptor.CreateConfiguration();
        }

        return Configuration;
    }

    protected virtual void Configure(ISortInputTypeDescriptor descriptor)
    {
    }

    protected override void OnRegisterDependencies(
        ITypeDiscoveryContext context,
        InputObjectTypeConfiguration configuration)
    {
        base.OnRegisterDependencies(context, configuration);
        if (configuration is SortInputTypeConfiguration { EntityType: { } } sortDefinition)
        {
            SetTypeIdentity(
                typeof(SortInputType<>).MakeGenericType(sortDefinition.EntityType));
        }
    }

    protected override void OnCompleteType(
        ITypeCompletionContext context,
        InputObjectTypeConfiguration configuration)
    {
        base.OnCompleteType(context, configuration);

        if (configuration is SortInputTypeConfiguration { EntityType: not null } ft)
        {
            EntityType = context.TypeInspector.GetType(ft.EntityType);
        }
    }

    protected override InputFieldCollection OnCompleteFields(
        ITypeCompletionContext context,
        InputObjectTypeConfiguration configuration)
    {
        var fieldConfigurations = new List<SortFieldConfiguration>(configuration.Fields.Count);

        foreach (var fieldDefinition in configuration.Fields)
        {
            if (fieldDefinition is SortFieldConfiguration { Ignore: false } field)
            {
                fieldConfigurations.Add(field);
            }
        }

        return new InputFieldCollection(CompleteFields(context, this, fieldConfigurations, CreateField));
        static InputField CreateField(SortFieldConfiguration fieldDef, int index) => new SortField(fieldDef, index);
    }

    // we are disabling the default configure method so
    // that this does not lead to confusion.
    protected sealed override void Configure(IInputObjectTypeDescriptor descriptor)
    {
        throw new NotSupportedException();
    }
}

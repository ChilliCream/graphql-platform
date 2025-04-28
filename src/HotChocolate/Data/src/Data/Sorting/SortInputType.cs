using HotChocolate.Configuration;
using HotChocolate.Internal;
using HotChocolate.Types;
using HotChocolate.Types.Descriptors.Definitions;
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

    public IExtendedType EntityType { get; private set; } = default!;

    protected override InputObjectTypeConfiguration CreateConfiguration(ITypeDiscoveryContext context)
    {
        if (Definition is null)
        {
            var descriptor = SortInputTypeDescriptor.FromSchemaType(
                context.DescriptorContext,
                GetType(),
                context.Scope);

            _configure!(descriptor);
            _configure = null;

            Definition = descriptor.CreateConfiguration();
        }

        return Definition;
    }

    protected virtual void Configure(ISortInputTypeDescriptor descriptor)
    {
    }

    protected override void OnRegisterDependencies(
        ITypeDiscoveryContext context,
        InputObjectTypeConfiguration definition)
    {
        base.OnRegisterDependencies(context, definition);
        if (definition is SortInputTypeConfiguration {EntityType: { }, } sortDefinition)
        {
            SetTypeIdentity(
                typeof(SortInputType<>).MakeGenericType(sortDefinition.EntityType));
        }
    }

    protected override void OnCompleteType(
        ITypeCompletionContext context,
        InputObjectTypeConfiguration definition)
    {
        base.OnCompleteType(context, definition);

        if (definition is SortInputTypeConfiguration { EntityType: not null, } ft)
        {
            EntityType = context.TypeInspector.GetType(ft.EntityType);
        }
    }

    protected override FieldCollection<InputField> OnCompleteFields(
        ITypeCompletionContext context,
        InputObjectTypeConfiguration definition)
    {
        var fields = new InputField[definition.Fields.Count];
        var index = 0;

        foreach (var fieldDefinition in definition.Fields)
        {
            if (fieldDefinition is SortFieldConfiguration {Ignore: false, } field)
            {
                fields[index] = new SortField(field, index);
                index++;
            }
        }

        if (fields.Length < index)
        {
            Array.Resize(ref fields, index);
        }

        return CompleteFields(context, this, fields);
    }

    // we are disabling the default configure method so
    // that this does not lead to confusion.
    protected sealed override void Configure(IInputObjectTypeDescriptor descriptor)
    {
        throw new NotSupportedException();
    }
}

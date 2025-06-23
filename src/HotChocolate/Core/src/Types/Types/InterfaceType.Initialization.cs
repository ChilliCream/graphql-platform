using HotChocolate.Configuration;
using HotChocolate.Internal;
using HotChocolate.Types.Descriptors;
using HotChocolate.Types.Descriptors.Configurations;
using HotChocolate.Types.Helpers;
using static HotChocolate.Internal.FieldInitHelper;
using static HotChocolate.Types.Helpers.CompleteInterfacesHelper;

#nullable enable

namespace HotChocolate.Types;

public partial class InterfaceType
{
    private InterfaceTypeCollection _implements = InterfaceTypeCollection.Empty;
    private Action<IInterfaceTypeDescriptor>? _configure;
    private ResolveAbstractType? _resolveAbstractType;
    private Schema _schema = null!;

    protected override InterfaceTypeConfiguration CreateConfiguration(
        ITypeDiscoveryContext context)
    {
        try
        {
            if (Configuration is null)
            {
                var descriptor = InterfaceTypeDescriptor.FromSchemaType(
                    context.DescriptorContext,
                    GetType());
                _configure!(descriptor);
                return descriptor.CreateConfiguration();
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
        InterfaceTypeConfiguration configuration)
    {
        base.OnRegisterDependencies(context, configuration);
        context.RegisterDependencies(configuration);
        SetTypeIdentity(typeof(InterfaceType<>));
    }

    protected override void OnCompleteType(
        ITypeCompletionContext context,
        InterfaceTypeConfiguration configuration)
    {
        base.OnCompleteType(context, configuration);

        Fields = OnCompleteFields(context, configuration);
        context.DescriptorContext.OnSchemaCreated(schema => _schema = schema);

        CompleteAbstractTypeResolver(configuration.ResolveAbstractType);
        _implements = CompleteInterfaces(context, configuration.GetInterfaces(), this);
    }

    protected override void OnCompleteMetadata(
        ITypeCompletionContext context,
        InterfaceTypeConfiguration configuration)
    {
        base.OnCompleteMetadata(context, configuration);

        foreach (IFieldCompletion field in Fields)
        {
            field.CompleteMetadata(context, this);
        }
    }

    protected override void OnMakeExecutable(
        ITypeCompletionContext context,
        InterfaceTypeConfiguration configuration)
    {
        base.OnMakeExecutable(context, configuration);

        foreach (IFieldCompletion field in Fields)
        {
            field.MakeExecutable(context, this);
        }
    }

    protected override void OnFinalizeType(
        ITypeCompletionContext context,
        InterfaceTypeConfiguration configuration)
    {
        base.OnFinalizeType(context, configuration);

        foreach (IFieldCompletion field in Fields)
        {
            field.Finalize(context, this);
        }
    }

    protected virtual InterfaceFieldCollection OnCompleteFields(
        ITypeCompletionContext context,
        InterfaceTypeConfiguration definition)
    {
        return new InterfaceFieldCollection(
            CompleteFields(
                context,
                this,
                definition.Fields,
                CreateField));
        static InterfaceField CreateField(InterfaceFieldConfiguration fieldDef, int index)
            => new(fieldDef, index);
    }

    private void CompleteAbstractTypeResolver(ResolveAbstractType? resolveAbstractType)
    {
        if (resolveAbstractType is null)
        {
            // if there is no custom type resolver we will use this default
            // abstract type resolver.
            IReadOnlyCollection<ObjectType>? types = null;
            _resolveAbstractType = (c, r) =>
            {
                types ??= _schema.GetPossibleTypes(this);

                foreach (var type in types)
                {
                    if (type.IsInstanceOfType(c, r))
                    {
                        return type;
                    }
                }

                return null;
            };
        }
        else
        {
            _resolveAbstractType = resolveAbstractType;
        }
    }
}

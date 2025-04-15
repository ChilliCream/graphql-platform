using HotChocolate.Configuration;
using HotChocolate.Internal;
using HotChocolate.Types.Descriptors;
using HotChocolate.Types.Descriptors.Definitions;
using HotChocolate.Types.Helpers;
using static HotChocolate.Internal.FieldInitHelper;
using static HotChocolate.Types.Helpers.CompleteInterfacesHelper;

#nullable enable

namespace HotChocolate.Types;

public partial class InterfaceType
{
    private InterfaceType[] _implements = [];
    private Action<IInterfaceTypeDescriptor>? _configure;
    private ResolveAbstractType? _resolveAbstractType;
    private ISchema _schema = default!;

    protected override InterfaceTypeConfiguration CreateDefinition(
        ITypeDiscoveryContext context)
    {
        try
        {
            if (Definition is null)
            {
                var descriptor = InterfaceTypeDescriptor.FromSchemaType(
                    context.DescriptorContext,
                    GetType());
                _configure!(descriptor);
                return descriptor.CreateDefinition();
            }

            return Definition;
        }
        finally
        {
            _configure = null;
        }
    }

    protected override void OnRegisterDependencies(
        ITypeDiscoveryContext context,
        InterfaceTypeConfiguration definition)
    {
        base.OnRegisterDependencies(context, definition);
        context.RegisterDependencies(definition);
        SetTypeIdentity(typeof(InterfaceType<>));
    }

    protected override void OnCompleteType(
        ITypeCompletionContext context,
        InterfaceTypeConfiguration definition)
    {
        base.OnCompleteType(context, definition);

        Fields = OnCompleteFields(context, definition);
        context.DescriptorContext.OnSchemaCreated(schema => _schema = schema);

        CompleteAbstractTypeResolver(definition.ResolveAbstractType);
        _implements = CompleteInterfaces(context, definition.GetInterfaces(), this);
    }

    protected override void OnCompleteMetadata(
        ITypeCompletionContext context,
        InterfaceTypeConfiguration definition)
    {
        base.OnCompleteMetadata(context, definition);

        foreach (IFieldCompletion field in Fields)
        {
            field.CompleteMetadata(context, this);
        }
    }

    protected override void OnMakeExecutable(
        ITypeCompletionContext context,
        InterfaceTypeConfiguration definition)
    {
        base.OnMakeExecutable(context, definition);

        foreach (IFieldCompletion field in Fields)
        {
            field.MakeExecutable(context, this);
        }
    }

    protected override void OnFinalizeType(
        ITypeCompletionContext context,
        InterfaceTypeConfiguration definition)
    {
        base.OnFinalizeType(context, definition);

        foreach (IFieldCompletion field in Fields)
        {
            field.Finalize(context, this);
        }
    }

    protected virtual FieldCollection<InterfaceField> OnCompleteFields(
        ITypeCompletionContext context,
        InterfaceTypeConfiguration definition)
    {
        return CompleteFields(context, this, definition.Fields, CreateField);
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

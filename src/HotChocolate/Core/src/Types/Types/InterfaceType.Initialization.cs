using HotChocolate.Configuration;
using HotChocolate.Internal;
using HotChocolate.Types.Descriptors;
using HotChocolate.Types.Descriptors.Definitions;
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

    protected override InterfaceTypeDefinition CreateDefinition(
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
        InterfaceTypeDefinition definition)
    {
        base.OnRegisterDependencies(context, definition);
        context.RegisterDependencies(definition);
        SetTypeIdentity(typeof(InterfaceType<>));
    }

    protected override void OnCompleteType(
        ITypeCompletionContext context,
        InterfaceTypeDefinition definition)
    {
        base.OnCompleteType(context, definition);

        Fields = OnCompleteFields(context, definition);
        context.DescriptorContext.OnSchemaCreated(schema => _schema = schema);

        CompleteAbstractTypeResolver(definition.ResolveAbstractType);
        _implements = CompleteInterfaces(context, definition.GetInterfaces(), this);
    }

    protected virtual FieldCollection<InterfaceField> OnCompleteFields(
        ITypeCompletionContext context,
        InterfaceTypeDefinition definition)
    {
        return CompleteFields(context, this, definition.Fields, CreateField);
        static InterfaceField CreateField(InterfaceFieldDefinition fieldDef, int index)
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

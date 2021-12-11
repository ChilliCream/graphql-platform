using System;
using System.Collections.Generic;
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
    private InterfaceType[] _implements = Array.Empty<InterfaceType>();
    private Action<IInterfaceTypeDescriptor>? _configure;
    private ResolveAbstractType? _resolveAbstractType;

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

        SyntaxNode = definition.SyntaxNode;
        Fields = OnCompleteFields(context, definition);

        CompleteAbstractTypeResolver(context, definition.ResolveAbstractType);
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

    private void CompleteAbstractTypeResolver(
        ITypeCompletionContext context,
        ResolveAbstractType? resolveAbstractType)
    {
        if (resolveAbstractType is null)
        {
            Func<ISchema> schemaResolver = context.GetSchemaResolver();

            // if there is no custom type resolver we will use this default
            // abstract type resolver.
            IReadOnlyCollection<ObjectType>? types = null;
            _resolveAbstractType = (c, r) =>
            {
                if (types is null)
                {
                    ISchema schema = schemaResolver.Invoke();
                    types = schema.GetPossibleTypes(this);
                }

                foreach (ObjectType type in types)
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

using HotChocolate.Configuration;
using HotChocolate.Internal;
using HotChocolate.Resolvers;
using HotChocolate.Types.Descriptors;
using HotChocolate.Types.Descriptors.Definitions;
using HotChocolate.Types.Helpers;
using HotChocolate.Utilities;
using static HotChocolate.Internal.FieldInitHelper;
using static HotChocolate.Types.Helpers.CompleteInterfacesHelper;
using static HotChocolate.Utilities.ErrorHelper;

#nullable enable

namespace HotChocolate.Types;

public partial class ObjectType
{
    private InterfaceType[] _implements = [];
    private Action<IObjectTypeDescriptor>? _configure;
    private IsOfType? _isOfType;

    protected override ObjectTypeDefinition CreateDefinition(
        ITypeDiscoveryContext context)
    {
        try
        {
            if (Definition is null)
            {
                var descriptor = ObjectTypeDescriptor.FromSchemaType(
                    context.DescriptorContext,
                    GetType());
                _configure!.Invoke(descriptor);
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
        ObjectTypeDefinition definition)
    {
        base.OnRegisterDependencies(context, definition);
        context.RegisterDependencies(definition);
        SetTypeIdentity(typeof(ObjectType<>));
    }

    protected override void OnCompleteType(
        ITypeCompletionContext context,
        ObjectTypeDefinition definition)
    {
        base.OnCompleteType(context, definition);

        if (ValidateFields(context, definition))
        {
            _isOfType = definition.IsOfType;
            _implements = CompleteInterfaces(context, definition.GetInterfaces(), this);
            Fields = OnCompleteFields(context, definition);
            CompleteTypeResolver(context);
        }
    }

    protected virtual FieldCollection<ObjectField> OnCompleteFields(
        ITypeCompletionContext context,
        ObjectTypeDefinition definition)
    {
        var interfaceFields = TypeMemHelper.RentInterfaceFieldDefinitionMap();
        var processed = TypeMemHelper.RentNameSet();

        foreach (var interfaceType in _implements)
        {
            foreach (var field in interfaceType.Definition!.Fields)
            {
                if (interfaceFields.ContainsKey(field.Name))
                {
                    continue;
                }

                if (field.Resolvers.HasResolvers)
                {
                    interfaceFields.Add(field.Name, field);
                }
            }
        }

        foreach (var field in definition.Fields)
        {
            if (processed.Add(field.Name)
                && !field.Resolvers.HasResolvers
                && interfaceFields.TryGetValue(field.Name, out var interfaceField))
            {
                field.Resolvers = interfaceField.Resolvers;
            }
        }

        foreach (var interfaceField in interfaceFields.Values)
        {
            if (processed.Add(interfaceField.Name))
            {
                var field = new ObjectFieldDefinition();
                interfaceField.CopyTo(field);
                definition.Fields.Add(field);
            }
        }

        if (((RegisteredType)context).IsMutationType ?? false)
        {
            // if this type represents the mutation type we flag all fields as serially executable
            // so that the operation compiler and execution engine will uphold the spec
            // algorithm to execute mutations serially.
            foreach (var field in definition.Fields)
            {
                field.IsParallelExecutable = false;
            }
        }

        var collection = CompleteFields(context, this, definition.Fields, CreateField);
        TypeMemHelper.Return(interfaceFields);
        TypeMemHelper.Return(processed);
        return collection;

        static ObjectField CreateField(ObjectFieldDefinition fieldDef, int index)
            => new(fieldDef, index);
    }

    private void CompleteTypeResolver(ITypeCompletionContext context)
    {
        if (_isOfType is not null)
        {
            return;
        }

        if (context.IsOfType is not null)
        {
            var isOfType = context.IsOfType;
            _isOfType = (ctx, obj) => isOfType(this, ctx, obj);
        }
        else if (RuntimeType == typeof(object))
        {
            _isOfType = IsOfTypeWithName;
        }
        else
        {
            _isOfType = IsOfTypeWithRuntimeType;
        }
    }

    private bool ValidateFields(
        ITypeCompletionContext context,
        ObjectTypeDefinition definition)
    {
        var hasErrors = false;

        foreach (var field in definition.Fields.Where(t => t.Type is null))
        {
            hasErrors = true;
            context.ReportError(ObjectType_UnableToInferOrResolveType(Name, this, field));
        }

        return !hasErrors;
    }

    private bool IsOfTypeWithRuntimeType(
        IResolverContext context,
        object? result) =>
        result is null || RuntimeType == result.GetType();

    private bool IsOfTypeWithName(
        IResolverContext context,
        object? result)
    {
        if (result is null)
        {
            return true;
        }

        var type = result.GetType();
        return Name.EqualsOrdinal(type.Name);
    }
}

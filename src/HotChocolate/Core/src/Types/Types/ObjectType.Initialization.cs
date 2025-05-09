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
    private InterfaceTypeCollection _implements = InterfaceTypeCollection.Empty;
    private Action<IObjectTypeDescriptor>? _configure;
    private IsOfType? _isOfType;

    protected override ObjectTypeConfiguration CreateConfiguration(
        ITypeDiscoveryContext context)
    {
        try
        {
            if (Configuration is null)
            {
                var descriptor = ObjectTypeDescriptor.FromSchemaType(
                    context.DescriptorContext,
                    GetType());
                _configure!.Invoke(descriptor);

                if (!descriptor.Configuration.NeedsNameCompletion)
                {
                    context.DescriptorContext.TypeConfiguration.Apply(descriptor.Configuration.Name, descriptor);
                }

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
        ObjectTypeConfiguration configuration)
    {
        base.OnRegisterDependencies(context, configuration);
        context.RegisterDependencies(configuration);
        SetTypeIdentity(typeof(ObjectType<>));
    }

    protected override void OnCompleteType(
        ITypeCompletionContext context,
        ObjectTypeConfiguration configuration)
    {
        base.OnCompleteType(context, configuration);

        if (ValidateFields(context, configuration))
        {
            _isOfType = configuration.IsOfType;
            _implements = CompleteInterfaces(context, configuration.GetInterfaces(), this);
            Fields = OnCompleteFields(context, configuration);
            CompleteTypeResolver(context);
        }
    }

    protected override void OnCompleteMetadata(
        ITypeCompletionContext context,
        ObjectTypeConfiguration configuration)
    {
        base.OnCompleteMetadata(context, configuration);

        foreach (IFieldCompletion field in Fields)
        {
            field.CompleteMetadata(context, this);
        }
    }

    protected override void OnMakeExecutable(
        ITypeCompletionContext context,
        ObjectTypeConfiguration configuration)
    {
        base.OnMakeExecutable(context, configuration);

        foreach (IFieldCompletion field in Fields)
        {
            field.MakeExecutable(context, this);
        }
    }

    protected override void OnFinalizeType(
        ITypeCompletionContext context,
        ObjectTypeConfiguration configuration)
    {
        base.OnFinalizeType(context, configuration);

        foreach (IFieldCompletion field in Fields)
        {
            field.Finalize(context, this);
        }
    }

    protected virtual ObjectFieldCollection OnCompleteFields(
        ITypeCompletionContext context,
        ObjectTypeConfiguration definition)
    {
        var interfaceFields = TypeMemHelper.RentInterfaceFieldConfigurationMap();
        var processed = TypeMemHelper.RentNameSet();

        foreach (var interfaceType in _implements)
        {
            foreach (var field in interfaceType.Configuration!.Fields)
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
                var field = new ObjectFieldConfiguration();
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

        var fields = CompleteFields(context, this, definition.Fields, CreateField);

        TypeMemHelper.Return(interfaceFields);
        TypeMemHelper.Return(processed);

        return new ObjectFieldCollection(fields);

        static ObjectField CreateField(ObjectFieldConfiguration fieldDef, int index)
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
        ObjectTypeConfiguration definition)
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

using System.Runtime.CompilerServices;
using HotChocolate.Configuration;
using HotChocolate.Internal;
using HotChocolate.Language;
using HotChocolate.Types.Descriptors;
using HotChocolate.Types.Descriptors.Configurations;
using HotChocolate.Types.Helpers;
using HotChocolate.Utilities;
using static HotChocolate.Internal.FieldInitHelper;
using static HotChocolate.Utilities.Serialization.InputObjectCompiler;

#nullable enable

namespace HotChocolate.Types;

/// <summary>
/// Represents a GraphQL input object type
/// </summary>
public partial class InputObjectType
{
    private Action<IInputObjectTypeDescriptor>? _configure;
    private Func<object?[], object> _createInstance = null!;
    private Action<object, object?[]> _getFieldValues = null!;

    protected override InputObjectTypeConfiguration CreateConfiguration(ITypeDiscoveryContext context)
    {
        try
        {
            if (Configuration is null)
            {
                var descriptor = InputObjectTypeDescriptor.FromSchemaType(
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
        InputObjectTypeConfiguration configuration)
    {
        base.OnRegisterDependencies(context, configuration);
        context.RegisterDependencies(configuration);
        SetTypeIdentity(typeof(InputObjectType<>));
    }

    protected override void OnCompleteType(
        ITypeCompletionContext context,
        InputObjectTypeConfiguration configuration)
    {
        base.OnCompleteType(context, configuration);

        Fields = OnCompleteFields(context, configuration);
        IsOneOf = configuration.GetDirectives().Any(static t => t.IsOneOf());

        _createInstance = OnCompleteCreateInstance(context, configuration);
        _getFieldValues = OnCompleteGetFieldValues(context, configuration);
    }

    protected override void OnCompleteMetadata(
        ITypeCompletionContext context,
        InputObjectTypeConfiguration configuration)
    {
        base.OnCompleteMetadata(context, configuration);

        foreach (IFieldCompletion field in Fields)
        {
            field.CompleteMetadata(context, this);
        }
    }

    protected override void OnMakeExecutable(
        ITypeCompletionContext context,
        InputObjectTypeConfiguration configuration)
    {
        base.OnMakeExecutable(context, configuration);

        foreach (IFieldCompletion field in Fields)
        {
            field.MakeExecutable(context, this);
        }
    }

    protected override void OnFinalizeType(
        ITypeCompletionContext context,
        InputObjectTypeConfiguration configuration)
    {
        base.OnFinalizeType(context, configuration);

        foreach (IFieldCompletion field in Fields)
        {
            field.Finalize(context, this);
        }
    }

    protected virtual InputFieldCollection OnCompleteFields(
        ITypeCompletionContext context,
        InputObjectTypeConfiguration configuration)
    {
        return new InputFieldCollection(
            CompleteFields(
                context,
                this,
                configuration.Fields,
                CreateField));
        static InputField CreateField(InputFieldConfiguration fieldDef, int index)
            => new(fieldDef, index);
    }

    protected virtual Func<object?[], object> OnCompleteCreateInstance(
        ITypeCompletionContext context,
        InputObjectTypeConfiguration configuration)
    {
        Func<object?[], object>? createInstance = null;

        if (configuration.CreateInstance is not null)
        {
            createInstance = configuration.CreateInstance;
        }

        if (RuntimeType == typeof(object) || Fields.Any(t => t.Property is null))
        {
            createInstance ??= CreateDictionaryInstance;
        }
        else
        {
            createInstance ??= CompileFactory(this);
        }

        return createInstance;
    }

    protected virtual Action<object, object?[]> OnCompleteGetFieldValues(
        ITypeCompletionContext context,
        InputObjectTypeConfiguration configuration)
    {
        Action<object, object?[]>? getFieldValues = null;

        if (configuration.GetFieldData is not null)
        {
            getFieldValues = configuration.GetFieldData;
        }

        if (RuntimeType == typeof(object) || Fields.Any(t => t.Property is null))
        {
            getFieldValues ??= CreateDictionaryGetValues;
        }
        else
        {
            getFieldValues ??= CompileGetFieldValues(this);
        }

        return getFieldValues;
    }

    private object CreateDictionaryInstance(object?[] fieldValues)
    {
        var dictionary = new Dictionary<string, object?>();

        foreach (var field in Fields.AsSpan())
        {
            dictionary.Add(field.Name, fieldValues[field.Index]);
        }

        return dictionary;
    }

    private void CreateDictionaryGetValues(object obj, object?[] fieldValues)
    {
        var map = (Dictionary<string, object?>)obj;

        foreach (var field in Fields.AsSpan())
        {
            if (map.TryGetValue(field.Name, out var val))
            {
                fieldValues[field.Index] = val;
            }
        }
    }
}

file static class Extensions
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsOneOf(this DirectiveConfiguration directiveDef)
        => directiveDef.Value is DirectiveNode node
            && node.Name.Value.EqualsOrdinal(DirectiveNames.OneOf.Name);
}

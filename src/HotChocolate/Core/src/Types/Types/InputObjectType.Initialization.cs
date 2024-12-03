using HotChocolate.Configuration;
using HotChocolate.Internal;
using HotChocolate.Types.Descriptors;
using HotChocolate.Types.Descriptors.Definitions;
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
    private Func<object?[], object> _createInstance = default!;
    private Action<object, object?[]> _getFieldValues = default!;

    protected override InputObjectTypeDefinition CreateDefinition(ITypeDiscoveryContext context)
    {
        try
        {
            if (Definition is null)
            {
                var descriptor = InputObjectTypeDescriptor.FromSchemaType(
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
        InputObjectTypeDefinition definition)
    {
        base.OnRegisterDependencies(context, definition);
        context.RegisterDependencies(definition);
        SetTypeIdentity(typeof(InputObjectType<>));
    }

    protected override void OnCompleteType(
        ITypeCompletionContext context,
        InputObjectTypeDefinition definition)
    {
        base.OnCompleteType(context, definition);

        Fields = OnCompleteFields(context, definition);

        _createInstance = OnCompleteCreateInstance(context, definition);
        _getFieldValues = OnCompleteGetFieldValues(context, definition);
    }

    protected virtual FieldCollection<InputField> OnCompleteFields(
        ITypeCompletionContext context,
        InputObjectTypeDefinition definition)
    {
        return CompleteFields(context, this, definition.Fields, CreateField);
        static InputField CreateField(InputFieldDefinition fieldDef, int index)
            => new(fieldDef, index);
    }

    protected virtual Func<object?[], object> OnCompleteCreateInstance(
        ITypeCompletionContext context,
        InputObjectTypeDefinition definition)
    {
        Func<object?[], object>? createInstance = null;

        if (definition.CreateInstance is not null)
        {
            createInstance = definition.CreateInstance;
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
        InputObjectTypeDefinition definition)
    {
        Action<object, object?[]>? getFieldValues = null;

        if (definition.GetFieldData is not null)
        {
            getFieldValues = definition.GetFieldData;
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

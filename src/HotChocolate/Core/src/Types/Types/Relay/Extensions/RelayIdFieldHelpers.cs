using System.Collections;
using HotChocolate.Configuration;
using HotChocolate.Internal;
using HotChocolate.Types.Descriptors;
using HotChocolate.Types.Descriptors.Definitions;
using HotChocolate.Utilities;
using static HotChocolate.WellKnownContextData;

#nullable enable

namespace HotChocolate.Types.Relay;

/// <summary>
/// This class provides internal helpers to apply value rewriter on input and output types
/// so that node IDs are correctly encoded or decoded.
/// </summary>
internal static class RelayIdFieldHelpers
{
    /// <summary>
    /// Applies the <see cref="RelayIdFieldExtensions"><c>.ID()</c></see> to an argument
    /// descriptor
    /// </summary>
    /// <remarks>
    /// You most likely want to call `<c>.ID()</c>` directly and do not use this helper
    /// </remarks>
    public static void ApplyIdToField(
        IDescriptor<ArgumentDefinition> descriptor,
        string? typeName = default)
    {
        if (descriptor is null)
        {
            throw new ArgumentNullException(nameof(descriptor));
        }

        var extend = descriptor.Extend();

        // rewrite type
        extend.OnBeforeCreate(RewriteDefinition);

        // add serializer if globalID support is enabled.
        if (extend.Context.ContextData.ContainsKey(GlobalIdSupportEnabled))
        {
            extend.OnBeforeCompletion((c, d) => AddSerializerToInputField(c, d, typeName));
        }
    }

    /// <summary>
    /// Applies the <see cref="RelayIdFieldExtensions"><c>.ID()</c></see> to an argument
    /// descriptor
    /// </summary>
    /// <remarks>
    /// You most likely want to call `<c>.ID()</c>` directly and do not use this helper
    /// </remarks>
    public static void ApplyIdToField(
        IDescriptor<OutputFieldDefinitionBase> descriptor,
        string? typeName = default)
    {
        if (descriptor is null)
        {
            throw new ArgumentNullException(nameof(descriptor));
        }

        // rewrite type
        descriptor.Extend().OnBeforeCreate(RewriteDefinition);

        if (descriptor is IDescriptor<ObjectFieldDefinition> objectFieldDescriptor)
        {
            var extend = objectFieldDescriptor.Extend();

            // add serializer if globalID support is enabled.
            if (extend.Context.ContextData.ContainsKey(GlobalIdSupportEnabled))
            {
                ApplyIdToField(extend.Definition, typeName);
            }
        }
    }

    /// <summary>
    /// Applies the <see cref="RelayIdFieldExtensions"><c>.ID()</c></see> to an argument
    /// descriptor
    /// </summary>
    /// <remarks>
    /// You most likely want to call `<c>.ID()</c>` directly and do not use this helper
    /// </remarks>
    internal static void ApplyIdToField(
        ObjectFieldDefinition definition,
        string? typeName = default)
    {
        var placeholder = new ResultFormatterDefinition(
            (_, r) => r,
            isRepeatable: false,
            key: WellKnownMiddleware.GlobalId);
        definition.FormatterDefinitions.Add(placeholder);

        var configuration = new CompleteConfiguration(
            (ctx, def) => AddSerializerToObjectField(
                ctx,
                (ObjectFieldDefinition)def,
                placeholder,
                typeName),
            definition,
            ApplyConfigurationOn.BeforeCompletion);

        definition.Configurations.Add(configuration);
    }

    private static void RewriteDefinition(
        IDescriptorContext context,
        FieldDefinitionBase definition)
    {
        if (definition.Type is ExtendedTypeReference typeReference)
        {
            var typeInfo = context.TypeInspector.CreateTypeInfo(typeReference.Type);
            var type = RewriteType(context.TypeInspector, typeInfo);
            definition.Type = typeReference.WithType(type);
        }
        else
        {
            throw ThrowHelper.RelayIdFieldHelpers_NoFieldType(definition.Name);
        }
    }

    private static IExtendedType RewriteType(ITypeInspector typeInspector, ITypeInfo typeInfo)
    {
        var current = typeof(IdType);

        if (typeInfo.Components.Count > 1)
        {
            foreach (var component in typeInfo.Components.Reverse().Skip(1))
            {
                if (component.Kind == TypeComponentKind.NonNull)
                {
                    current = typeof(NonNullType<>).MakeGenericType(current);
                }
                else if (component.Kind == TypeComponentKind.List)
                {
                    current = typeof(ListType<>).MakeGenericType(current);
                }
            }
        }

        return typeInspector.GetType(current);
    }

    internal static void AddSerializerToInputField(
        ITypeCompletionContext completionContext,
        ArgumentDefinition definition,
        string? typeName)
    {
        var typeInspector = completionContext.TypeInspector;
        IExtendedType? resultType;

        if (definition is InputFieldDefinition { RuntimeType: { } runtimeType, })
        {
            resultType = typeInspector.GetType(runtimeType);
        }
        else if (definition is InputFieldDefinition { Property: not null, } inputField)
        {
            resultType = typeInspector.GetReturnType(inputField.Property, true);
        }
        else if (definition.Parameter is not null)
        {
            resultType = typeInspector.GetArgumentType(definition.Parameter, true);
        }
        else if (definition.Type is ExtendedTypeReference typeReference)
        {
            resultType = typeReference.Type;
        }
        else
        {
            throw ThrowHelper.RelayIdFieldHelpers_NoFieldType(
                definition.Name,
                completionContext.Type);
        }

        var validateType = typeName is not null;
        typeName ??= completionContext.Type.Name;
        SetSerializerInfos(completionContext.DescriptorContext, typeName, resultType);
        var serializer = CreateSerializer(completionContext, resultType, typeName, validateType);
        definition.Formatters.Add(serializer);
    }

    private static void AddSerializerToObjectField(
        ITypeCompletionContext completionContext,
        ObjectFieldDefinition definition,
        ResultFormatterDefinition placeholder,
        string? typeName)
    {
        var typeInspector = completionContext.TypeInspector;
        IExtendedType? resultType;

        if (definition.ResultType is not null)
        {
            resultType = typeInspector.GetType(definition.ResultType);
        }
        else if (definition.Type is ExtendedTypeReference typeReference)
        {
            resultType = typeReference.Type;
        }
        else
        {
            throw ThrowHelper.RelayIdFieldHelpers_NoFieldType(
                definition.Name,
                completionContext.Type);
        }

        var serializerAccessor = completionContext.DescriptorContext.NodeIdSerializerAccessor;
        var index = definition.FormatterDefinitions.IndexOf(placeholder);

        typeName ??= completionContext.Type.Name;
        SetSerializerInfos(completionContext.DescriptorContext, typeName, resultType);

        definition.FormatterDefinitions[index] =
            CreateResultFormatter(typeName, resultType, serializerAccessor);
    }

    private static ResultFormatterDefinition CreateResultFormatter(
        string typeName,
        IExtendedType resultType,
        INodeIdSerializerAccessor serializerAccessor)
    {
        INodeIdSerializer? serializer = null;

        return new((_, result) =>
            {
                serializer ??= serializerAccessor.Serializer;

                if (result is not null)
                {
                    if (resultType.IsArrayOrList)
                    {
                        var list = new List<object?>();

                        foreach (var element in (IEnumerable)result)
                        {
                            list.Add(element is null
                                ? element
                                : serializer.Format(typeName, element));
                        }

                        return list;
                    }

                    return serializer.Format(typeName, result);
                }

                return result;
            },
            isRepeatable: false,
            key: WellKnownMiddleware.GlobalId);
    }

    private static IInputValueFormatter CreateSerializer(
        ITypeCompletionContext completionContext,
        IExtendedType resultType,
        string? typeName,
        bool validateType)
    {
        var resultTypeInfo = completionContext.DescriptorContext.TypeInspector.CreateTypeInfo(resultType);

        return new GlobalIdInputValueFormatter(
            completionContext.DescriptorContext.NodeIdSerializerAccessor,
            resultTypeInfo.NamedType,
            resultType.ElementType?.Type ?? resultTypeInfo.NamedType,
            typeName ?? completionContext.Type.Name,
            validateType);
    }

    internal static void SetSerializerInfos(IDescriptorContext context, string typeName, Type runtimeType)
    {
        var extendedType = context.TypeInspector.GetType(runtimeType);
        SetSerializerInfos(context, typeName, extendedType);
    }

    internal static void SetSerializerInfos(IDescriptorContext context, string typeName, IExtendedType runtimeType)
    {
        if (!context.TypeInspector.TryCreateTypeInfo(runtimeType, out var runtimeTypeInfo))
        {
            return;
        }

        if (runtimeTypeInfo.NamedType == typeof(object))
        {
            return;
        }

        if (!context.ContextData.TryGetValue(SerializerTypes, out var obj))
        {
            obj = new Dictionary<string, Type>();
            context.ContextData[SerializerTypes] = obj;
        }

        var mappings = (Dictionary<string, Type>)obj!;
        mappings.TryAdd(typeName, runtimeTypeInfo.NamedType);
    }
}

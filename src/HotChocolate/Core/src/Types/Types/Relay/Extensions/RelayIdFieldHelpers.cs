using System.Collections;
using HotChocolate.Configuration;
using HotChocolate.Features;
using HotChocolate.Internal;
using HotChocolate.Types.Descriptors;
using HotChocolate.Types.Descriptors.Configurations;
using HotChocolate.Utilities;

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
        IDescriptor<ArgumentConfiguration> descriptor,
        string? typeName = null) =>
        ApplyIdToFieldCore(descriptor, NodeIdNameDefinitionUnion.Create(typeName));

    /// <inheritdoc cref="ApplyIdToField(IDescriptor{ArgumentConfiguration},string?)"/>
    /// <typeparam name="T">
    /// the type from which the <see cref="IDAttribute.TypeName">type name</see> is derived
    /// </typeparam>
    public static void ApplyIdToField<T>(
        IDescriptor<ArgumentConfiguration> descriptor) =>
    ApplyIdToFieldCore(descriptor, NodeIdNameDefinitionUnion.Create<T>());

    /// <summary>
    /// Applies the <see cref="RelayIdFieldExtensions"><c>.ID()</c></see> to an argument
    /// descriptor
    /// </summary>
    /// <remarks>
    /// You most likely want to call `<c>.ID()</c>` directly and do not use this helper
    /// </remarks>
    public static void ApplyIdToField(
        IDescriptor<OutputFieldConfiguration> descriptor,
        string? typeName = null) =>
        ApplyIdToFieldCore(descriptor, NodeIdNameDefinitionUnion.Create(typeName));

    /// <inheritdoc cref="ApplyIdToField(IDescriptor{OutputFieldConfiguration},string?)"/>
    /// <typeparam name="T">
    /// the type from which the <see cref="IDAttribute.TypeName">type name</see> is derived
    /// </typeparam>
    public static void ApplyIdToField<T>(
        IDescriptor<OutputFieldConfiguration> descriptor) =>
        ApplyIdToFieldCore(descriptor, NodeIdNameDefinitionUnion.Create<T>());

    /// <summary>
    /// Applies the <see cref="RelayIdFieldExtensions"><c>.ID()</c></see> to an argument
    /// descriptor
    /// </summary>
    /// <remarks>
    /// You most likely want to call `<c>.ID()</c>` directly and do not use this helper
    /// </remarks>
    internal static void ApplyIdToField(
        ObjectFieldConfiguration configuration,
        NodeIdNameDefinitionUnion? nameDefinition = null,
        TypeReference? dependsOn = null)
    {
        var placeholder = new ResultFormatterConfiguration(
            (_, r) => r,
            isRepeatable: false,
            key: WellKnownMiddleware.GlobalId);
        configuration.FormatterConfigurations.Add(placeholder);

        var configurationTask = new OnCompleteTypeSystemConfigurationTask(
            (ctx, def) => AddSerializerToObjectField(
                ctx,
                (ObjectFieldConfiguration)def,
                placeholder,
                nameDefinition),
            configuration,
            ApplyConfigurationOn.BeforeCompletion,
            typeReference: dependsOn);

        configuration.Tasks.Add(configurationTask);
    }
    internal static void ApplyIdToFieldCore(
        IDescriptor<OutputFieldConfiguration> descriptor,
        NodeIdNameDefinitionUnion? nameDefinition)
    {
        ArgumentNullException.ThrowIfNull(descriptor);

        // rewrite type
        descriptor.Extend().OnBeforeCreate(RewriteConfiguration);

        if (descriptor is IDescriptor<ObjectFieldConfiguration> objectFieldDescriptor)
        {
            var extend = objectFieldDescriptor.Extend();

            // add serializer if globalID support is enabled.
            if (extend.Context.Features.Get<NodeSchemaFeature>()?.IsEnabled == true)
            {
                if (nameDefinition?.Type != null)
                {
                    var dependsOn = extend.Context.TypeInspector.GetTypeRef(nameDefinition.Type);
                    ApplyIdToField(extend.Configuration, nameDefinition, dependsOn);
                }
                else
                {
                    ApplyIdToField(extend.Configuration, nameDefinition);
                }
            }
        }
    }

    public static void ApplyIdToFieldCore(
        IDescriptor<ArgumentConfiguration> descriptor,
        NodeIdNameDefinitionUnion? nameDefinition)
    {
        ArgumentNullException.ThrowIfNull(descriptor);

        var extend = descriptor.Extend();

        // rewrite type
        extend.OnBeforeCreate(RewriteConfiguration);

        // add serializer if globalID support is enabled.
        if (extend.Context.Features.Get<NodeSchemaFeature>()?.IsEnabled == true)
        {
            if (nameDefinition?.Type == null)
            {
                extend.OnBeforeCompletion((c, d) =>
                    AddSerializerToInputField(c, d, nameDefinition));
            }
            else
            {
                var dependsOn = extend.Context.TypeInspector.GetTypeRef(nameDefinition.Type);

                var configurationTask = new OnCompleteTypeSystemConfigurationTask(
                    (ctx, def) => AddSerializerToInputField(ctx, (ArgumentConfiguration)def, nameDefinition),
                    extend.Configuration,
                    ApplyConfigurationOn.BeforeCompletion,
                    typeReference: dependsOn);

                extend.Configuration.Tasks.Add(configurationTask);
            }
        }
    }

    private static void RewriteConfiguration(
        IDescriptorContext context,
        FieldConfiguration configuration)
    {
        if (configuration.Type is ExtendedTypeReference typeReference)
        {
            var typeInfo = context.TypeInspector.CreateTypeInfo(typeReference.Type);
            var type = RewriteType(context.TypeInspector, typeInfo);
            configuration.Type = typeReference.WithType(type);
        }
        else
        {
            throw ThrowHelper.RelayIdFieldHelpers_NoFieldType(configuration.Name);
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
        ArgumentConfiguration configuration,
        NodeIdNameDefinitionUnion? nameDefinition)
    {
        var typeInspector = completionContext.TypeInspector;
        IExtendedType? resultType;

        if (configuration is InputFieldConfiguration { RuntimeType: { } runtimeType })
        {
            resultType = typeInspector.GetType(runtimeType);
        }
        else if (configuration is InputFieldConfiguration { Property: not null } inputField)
        {
            resultType = typeInspector.GetReturnType(inputField.Property, true);
        }
        else if (configuration.Parameter is not null)
        {
            resultType = typeInspector.GetArgumentType(configuration.Parameter, true);
        }
        else if (configuration.Type is ExtendedTypeReference typeReference)
        {
            resultType = typeReference.Type;
        }
        else
        {
            throw ThrowHelper.RelayIdFieldHelpers_NoFieldType(
                configuration.Name,
                completionContext.Type);
        }

        var typeName = GetIdTypeName(completionContext, nameDefinition, typeInspector);

        var validateType = typeName is not null;
        typeName ??= completionContext.Type.Name;
        SetSerializerInfos(completionContext.DescriptorContext, typeName, resultType);
        var serializer = CreateSerializer(completionContext, resultType, typeName, validateType);
        configuration.Formatters.Add(serializer);
    }

    private static void AddSerializerToObjectField(
        ITypeCompletionContext completionContext,
        ObjectFieldConfiguration configuration,
        ResultFormatterConfiguration placeholder,
        NodeIdNameDefinitionUnion? nameDefinition)
    {
        var typeInspector = completionContext.TypeInspector;
        IExtendedType? resultType;

        if (configuration.ResultType is not null)
        {
            resultType = typeInspector.GetType(configuration.ResultType);
        }
        else if (configuration.Type is ExtendedTypeReference typeReference)
        {
            resultType = typeReference.Type;
        }
        else
        {
            throw ThrowHelper.RelayIdFieldHelpers_NoFieldType(
                configuration.Name,
                completionContext.Type);
        }

        var serializerAccessor = completionContext.DescriptorContext.NodeIdSerializerAccessor;
        var index = configuration.FormatterConfigurations.IndexOf(placeholder);

        var typeName = GetIdTypeName(completionContext, nameDefinition, typeInspector);

        typeName ??= completionContext.Type.Name;
        SetSerializerInfos(completionContext.DescriptorContext, typeName, resultType);

        configuration.FormatterConfigurations[index] =
            CreateResultFormatter(typeName, resultType, serializerAccessor);
    }

    private static ResultFormatterConfiguration CreateResultFormatter(
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
            resultType.ElementType?.Source ?? resultTypeInfo.NamedType,
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

        var feature = context.Features.GetOrSet<NodeSchemaFeature>();
        feature.NodeIdTypes.TryAdd(typeName, runtimeTypeInfo.NamedType);
    }

    private static string? GetIdTypeName(ITypeCompletionContext completionContext,
        NodeIdNameDefinitionUnion? nameDefinition,
        ITypeInspector typeInspector)
    {
        var typeName = nameDefinition?.Literal;
        if (nameDefinition?.Type is { } t)
        {
            var referencedType = typeInspector.GetType(t);
            var foo = completionContext.GetType<IType>(TypeReference.Create(referencedType));
            typeName = foo.NamedType().Name;
        }

        return typeName;
    }
}

/// <summary>
/// A discriminated union, containing either a literal or a type that defines
/// the name of the node identifier.
/// </summary>
internal class NodeIdNameDefinitionUnion(string? Literal, Type? Type)
{
    public static NodeIdNameDefinitionUnion? Create(string? literal) =>
        literal == null ? null : new NodeIdNameDefinitionUnion(literal, null);

    public static NodeIdNameDefinitionUnion Create<T>() =>
        new NodeIdNameDefinitionUnion(null, typeof(T));
}

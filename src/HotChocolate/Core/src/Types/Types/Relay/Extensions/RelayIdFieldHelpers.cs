using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using HotChocolate.Configuration;
using HotChocolate.Internal;
using HotChocolate.Types.Descriptors;
using HotChocolate.Types.Descriptors.Definitions;
using HotChocolate.Utilities;
using Microsoft.Extensions.DependencyInjection;
using static HotChocolate.WellKnownContextData;

#nullable enable

namespace HotChocolate.Types.Relay;

/// <summary>
/// This class provides internal helpers to apply value rewriter on input and output types
/// so that node IDs are correctly encoded or decoded.
/// </summary>
internal static class RelayIdFieldHelpers
{
    private static IdSerializer? _idSerializer;

    /// <summary>
    /// Applies the <see cref="RelayIdFieldExtensions"><c>.ID()</c></see> to a argument
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
    /// Applies the <see cref="RelayIdFieldExtensions"><c>.ID()</c></see> to a argument
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
    /// Applies the <see cref="RelayIdFieldExtensions"><c>.ID()</c></see> to a argument
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

        definition.Formatters.Add(CreateSerializer(completionContext, resultType, typeName));
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

        string? schemaName = default;
        completionContext.DescriptorContext.SchemaCompleted += (_, args) =>
            schemaName = args.Schema.Name;

        var serializer =
            completionContext.Services.GetService<IIdSerializer>() ??
            new IdSerializer();
        var index = definition.FormatterDefinitions.IndexOf(placeholder);

        typeName ??= completionContext.Type.Name;

        definition.FormatterDefinitions[index] = new((_, result) =>
            {
                if (result is not null)
                {
                    if (resultType.IsArrayOrList)
                    {
                        var list = new List<object?>();

                        foreach (var element in (IEnumerable)result)
                        {
                            list.Add(element is null
                                ? element
                                : serializer.Serialize(schemaName, typeName, element));
                        }

                        return list;
                    }

                    return serializer.Serialize(schemaName, typeName, result);
                }

                return result;
            },
            isRepeatable: false,
            key: WellKnownMiddleware.GlobalId);
    }

    private static IInputValueFormatter CreateSerializer(
        ITypeCompletionContext completionContext,
        IExtendedType resultType,
        string? typeName)
    {
        var serializer =
            completionContext.Services.GetService<IIdSerializer>() ??
            (_idSerializer ??= new IdSerializer());

        return new GlobalIdInputValueFormatter(
            typeName ?? completionContext.Type.Name,
            serializer,
            resultType,
            typeName is not null);
    }
}

﻿using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.Json;
using HotChocolate.Resolvers;
using HotChocolate.Types.Descriptors.Definitions;

namespace HotChocolate.Types;

/// <summary>
/// Provides <see cref="IObjectFieldDescriptor"/> extensions to handle JSON objects.
/// </summary>
public static class JsonObjectTypeExtensions
{
    /// <summary>
    /// Specifies that this field will be resolved from the JsonElement representing the instance
    /// of this type.
    /// </summary>
    /// <param name="descriptor">
    /// The <see cref="IObjectFieldDescriptor"/> representing the field configuration.
    /// </param>
    /// <param name="propertyName">
    /// If specified this name will be used as the property name to get the field data from.
    /// </param>
    /// <returns>
    /// Returns the <see cref="IObjectFieldDescriptor"/> for configuration chaining.
    /// </returns>
    public static IObjectFieldDescriptor FromJson(
        this IObjectFieldDescriptor descriptor,
        string? propertyName = null)
    {
        if (descriptor is null)
        {
            throw new ArgumentNullException(nameof(descriptor));
        }

        descriptor
            .Extend()
            .OnBeforeCompletion(
                (ctx, def) =>
                {
                    propertyName ??= def.Name;
                    var type = ctx.GetType<IType>(def.Type!);
                    var namedType = type.NamedType();

                    if (type.IsListType() || namedType is not ScalarType)
                    {
                        InferResolver(type, def, propertyName);
                        return;
                    }

                    if (namedType is ScalarType scalarType)
                    {
                        InferResolver(ctx.Type, def, scalarType, propertyName);
                        return;
                    }

                    throw ThrowHelper.CannotInferTypeFromJsonObj(ctx.Type.Name);
                });

        return descriptor;
    }

    /// <summary>
    /// Specifies that this field will be resolved from the JsonElement representing the instance
    /// of this type.
    /// </summary>
    /// <param name="descriptor">
    /// The <see cref="IObjectFieldDescriptor"/> representing the field configuration.
    /// </param>
    /// <param name="resolve">
    /// A resolver that will be applied to the JsonElement representing the instance of this type.
    /// </param>
    /// <returns>
    /// Returns the <see cref="IObjectFieldDescriptor"/> for configuration chaining.
    /// </returns>
    public static IObjectFieldDescriptor FromJson<TResult>(
        this IObjectFieldDescriptor descriptor,
        Func<JsonElement, TResult> resolve)
    {
        if (descriptor is null)
        {
            throw new ArgumentNullException(nameof(descriptor));
        }

        if (resolve is null)
        {
            throw new ArgumentNullException(nameof(resolve));
        }

        descriptor
            .Extend()
            .OnBeforeCreate(
                def =>
                {
                    def.ResultType = typeof(TResult);
                    def.PureResolver = ctx => resolve(ctx.Parent<JsonElement>());
                });

        return descriptor;
    }

    internal static void InferResolver(
        ITypeSystemObject type,
        ObjectFieldDefinition def,
        ScalarType scalarType,
        string propertyName)
    {
        switch (scalarType.Name)
        {
            case ScalarNames.ID:
            case ScalarNames.String:
                def.PureResolver = ctx => ctx.GetProperty(propertyName)?.GetString();
                return;

            case ScalarNames.Boolean:
                def.PureResolver = ctx =>
                {
                    var property = ctx.GetProperty(propertyName);

                    return property is null or { ValueKind: JsonValueKind.Null }
                        ? null
                        : property.Value.GetBoolean();
                };
                return;

            case ScalarNames.Short:
                def.PureResolver = ctx =>
                {
                    var property = ctx.GetProperty(propertyName);

                    return property is null or { ValueKind: JsonValueKind.Null }
                        ? null
                        : property.Value.GetInt16();
                };
                return;

            case ScalarNames.Int:
                def.PureResolver = ctx =>
                {
                    var property = ctx.GetProperty(propertyName);

                    return property is null or { ValueKind: JsonValueKind.Null }
                        ? null
                        : property.Value.GetInt32();
                };
                return;

            case ScalarNames.Long:
                def.PureResolver = ctx =>
                {
                    var property = ctx.GetProperty(propertyName);

                    return property is null or { ValueKind: JsonValueKind.Null }
                        ? null
                        : property.Value.GetUInt64();
                };
                return;

            case ScalarNames.Float:
                def.PureResolver = ctx =>
                {
                    var property = ctx.GetProperty(propertyName);

                    return property is null or { ValueKind: JsonValueKind.Null }
                        ? null
                        : property.Value.GetDouble();
                };
                return;

            case ScalarNames.Decimal:
                def.PureResolver = ctx =>
                {
                    var property = ctx.GetProperty(propertyName);

                    return property is null or { ValueKind: JsonValueKind.Null }
                        ? null
                        : property.Value.GetDecimal();
                };
                return;

            case ScalarNames.URL:
                def.PureResolver = ctx =>
                {
                    var property = ctx.GetProperty(propertyName);

                    if (property is null or { ValueKind: JsonValueKind.Null })
                    {
                        return null;
                    }

                    return new Uri(property.Value.GetString()!);
                };
                return;

            case ScalarNames.UUID:
                def.PureResolver = ctx =>
                {
                    var property = ctx.GetProperty(propertyName);

                    return property is null or { ValueKind: JsonValueKind.Null }
                        ? null
                        : property.Value.GetGuid();
                };
                return;

            case ScalarNames.Byte:
                def.PureResolver = ctx =>
                {
                    var property = ctx.GetProperty(propertyName);

                    return property is null or { ValueKind: JsonValueKind.Null }
                        ? null
                        : property.Value.GetByte();
                };
                return;

            case ScalarNames.ByteArray:
                def.PureResolver = ctx =>
                {
                    var property = ctx.GetProperty(propertyName);

                    return property is null or { ValueKind: JsonValueKind.Null }
                        ? null
                        : property.Value.GetBytesFromBase64();
                };
                return;

            case ScalarNames.Date:
                def.PureResolver = ctx =>
                {
                    var property = ctx.GetProperty(propertyName);

                    if (property is null or { ValueKind: JsonValueKind.Null })
                    {
                        return null;
                    }

                    return DateTime.Parse(
                        property.Value.GetString()!,
                        CultureInfo.InvariantCulture,
                        DateTimeStyles.AssumeUniversal);
                };
                return;

            case ScalarNames.DateTime:
                def.PureResolver = ctx =>
                {
                    var property = ctx.GetProperty(propertyName);

                    return property is null or { ValueKind: JsonValueKind.Null }
                        ? null
                        : property.Value.GetDateTimeOffset();
                };
                return;

            default:
                throw ThrowHelper.CannotInferTypeFromJsonObj(type.Name);
        }
    }
    
    internal static void InferResolver(
        IType type,
        ObjectFieldDefinition def,
        string propertyName)
    {
        if (type.IsListType())
            def.PureResolver = ctx => ctx.GetListProperty(propertyName);
        else
        {
            def.PureResolver = ctx => ctx.GetUserDefinedProperty(propertyName);
        }
    }

    private static List<JsonElement>? GetListProperty(this IPureResolverContext context, string propertyName)
    {
        var parent = context.Parent<JsonElement>();
        return parent.ValueKind == JsonValueKind.Null || !parent.TryGetProperty(propertyName, out var property)
            ? null
            : property.EnumerateArray().ToList();
    }

    private static JsonElement? GetUserDefinedProperty(this IPureResolverContext context, string propertyName)
    {
        var parent = context.Parent<JsonElement>();
        if(parent.ValueKind == JsonValueKind.Null || !parent.TryGetProperty(propertyName, out var property))
        {
            return null;
        }
        else
        {
            return property.ValueKind is JsonValueKind.Null or JsonValueKind.Undefined
                ? null
                : property;
        }
    }

    private static JsonElement? GetProperty(this IPureResolverContext context, string propertyName)
        => context.Parent<JsonElement>().TryGetProperty(propertyName, out var element)
            ? element
            : null;
}

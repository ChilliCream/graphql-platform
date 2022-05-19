using System;
using System.Globalization;
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
    /// Specifies that this field will be resolved from the JsonELement representing the instance
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
            .OnBeforeCompletion((ctx, def) =>
            {
                propertyName ??= def.Name.Value;
                IType type = ctx.GetType<IType>(def.Type!);
                INamedType namedType = type.NamedType();

                if (type.IsListType())
                {
                    throw ThrowHelper.CannotInferTypeFromJsonObj(ctx.Type.Name);
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
    /// Specifies that this field will be resolved from the JsonELement representing the instance
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
            .OnBeforeCreate(def =>
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
        switch (scalarType.Name.Value)
        {
            case ScalarNames.ID:
            case ScalarNames.String:
                def.PureResolver = ctx => ctx.GetProperty(propertyName)?.GetString();
                return;
            case ScalarNames.Boolean:
                def.PureResolver = ctx => ctx.GetProperty(propertyName)?.GetBoolean();
                return;
            case ScalarNames.Short:
                def.PureResolver = ctx => ctx.GetProperty(propertyName)?.GetInt16();
                return;
            case ScalarNames.Int:
                def.PureResolver = ctx => ctx.GetProperty(propertyName)?.GetInt32();
                return;
            case ScalarNames.Long:
                def.PureResolver = ctx => ctx.GetProperty(propertyName)?.GetInt64();
                return;
            case ScalarNames.Float:
                def.PureResolver = ctx => ctx.GetProperty(propertyName)?.GetDouble();
                return;
            case ScalarNames.Decimal:
                def.PureResolver = ctx => ctx.GetProperty(propertyName)?.GetDecimal();
                return;
            case ScalarNames.URL:
                def.PureResolver = ctx => new Uri(ctx.GetProperty(propertyName)?.GetString()!);
                return;
            case ScalarNames.UUID:
                def.PureResolver = ctx => ctx.GetProperty(propertyName)?.GetGuid();
                return;
            case ScalarNames.Byte:
                def.PureResolver = ctx => ctx.GetProperty(propertyName)?.GetByte();
                return;
            case ScalarNames.ByteArray:
                def.PureResolver = ctx => ctx.GetProperty(propertyName)?.GetBytesFromBase64();
                return;
            case ScalarNames.Date:
                def.PureResolver = ctx =>
                {
                    var value = ctx.GetProperty(propertyName)?.GetString();

                    if (value is null)
                    {
                        return null;
                    }

                    return DateTime.Parse(
                        value,
                        CultureInfo.InvariantCulture,
                        DateTimeStyles.AssumeUniversal);
                };
                return;
            case ScalarNames.DateTime:
                def.PureResolver = ctx => ctx.GetProperty(propertyName)?.GetDateTimeOffset();
                return;
            default:
                throw ThrowHelper.CannotInferTypeFromJsonObj(type.Name);
        }
    }

    private static JsonElement? GetProperty(this IPureResolverContext context, string propertyName)
        => context.Parent<JsonElement>().TryGetProperty(propertyName, out JsonElement element)
            ? element
            : null;
}

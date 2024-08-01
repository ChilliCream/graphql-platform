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

                    if (type.IsListType())
                    {
                        InferListResolver(def);
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

    internal static void InferListResolver(ObjectFieldDefinition def)
    {
        def.PureResolver = ctx => new ValueTask<object?>(ctx.ToEnumerable());
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

                    return property is null or { ValueKind: JsonValueKind.Null, }
                        ? null
                        : property.Value.GetBoolean();
                };
                return;

            case ScalarNames.Short:
                def.PureResolver = ctx =>
                {
                    var property = ctx.GetProperty(propertyName);

                    return property is null or { ValueKind: JsonValueKind.Null, }
                        ? null
                        : property.Value.GetInt16();
                };
                return;

            case ScalarNames.Int:
                def.PureResolver = ctx =>
                {
                    var property = ctx.GetProperty(propertyName);

                    return property is null or { ValueKind: JsonValueKind.Null, }
                        ? null
                        : property.Value.GetInt32();
                };
                return;

            case ScalarNames.Long:
                def.PureResolver = ctx =>
                {
                    var property = ctx.GetProperty(propertyName);

                    return property is null or { ValueKind: JsonValueKind.Null, }
                        ? null
                        : property.Value.GetUInt64();
                };
                return;

            case ScalarNames.Float:
                def.PureResolver = ctx =>
                {
                    var property = ctx.GetProperty(propertyName);

                    return property is null or { ValueKind: JsonValueKind.Null, }
                        ? null
                        : property.Value.GetDouble();
                };
                return;

            case ScalarNames.Decimal:
                def.PureResolver = ctx =>
                {
                    var property = ctx.GetProperty(propertyName);

                    return property is null or { ValueKind: JsonValueKind.Null, }
                        ? null
                        : property.Value.GetDecimal();
                };
                return;

            case ScalarNames.URL:
                def.PureResolver = ctx =>
                {
                    var property = ctx.GetProperty(propertyName);

                    if (property is null or { ValueKind: JsonValueKind.Null, })
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

                    return property is null or { ValueKind: JsonValueKind.Null, }
                        ? null
                        : property.Value.GetGuid();
                };
                return;

            case ScalarNames.Byte:
                def.PureResolver = ctx =>
                {
                    var property = ctx.GetProperty(propertyName);

                    return property is null or { ValueKind: JsonValueKind.Null, }
                        ? null
                        : property.Value.GetByte();
                };
                return;

            case ScalarNames.ByteArray:
                def.PureResolver = ctx =>
                {
                    var property = ctx.GetProperty(propertyName);

                    return property is null or { ValueKind: JsonValueKind.Null, }
                        ? null
                        : property.Value.GetBytesFromBase64();
                };
                return;

            case ScalarNames.Date:
                def.PureResolver = ctx =>
                {
                    var property = ctx.GetProperty(propertyName);

                    if (property is null or { ValueKind: JsonValueKind.Null, })
                    {
                        return null;
                    }

                    return DateTime.Parse(
                        property.Value.GetString()!,
                        CultureInfo.InvariantCulture,
                        DateTimeStyles.None);
                };
                return;

            case ScalarNames.DateTime:
                def.PureResolver = ctx =>
                {
                    var property = ctx.GetProperty(propertyName);

                    return property is null or { ValueKind: JsonValueKind.Null, }
                        ? null
                        : property.Value.GetDateTimeOffset();
                };
                return;

            default:
                throw ThrowHelper.CannotInferTypeFromJsonObj(type.Name);
        }
    }

    private static IEnumerable<JsonElement> ToEnumerable(this IResolverContext context)
        => context.Parent<JsonElement>().EnumerateArray();

    private static JsonElement? GetProperty(this IResolverContext context, string propertyName)
        => context.Parent<JsonElement>().TryGetProperty(propertyName, out var element)
            ? element
            : null;
}

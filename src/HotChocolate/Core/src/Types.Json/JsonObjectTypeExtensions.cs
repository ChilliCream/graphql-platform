using System;
using System.Text.Json;
using HotChocolate.Resolvers;
using HotChocolate.Types.Descriptors.Definitions;

namespace HotChocolate.Types;

public static class JsonObjectTypeExtensions
{
    public static IObjectFieldDescriptor FromJson(
        this IObjectFieldDescriptor descriptor,
        string? propertyName = null)
    {
        descriptor
            .Extend()
            .OnBeforeCompletion((ctx, def) =>
            {
                propertyName ??= def.Name.Value;
                IType type = ctx.GetType<IType>(def.Type!);
                INamedType namedType = type.NamedType();

                if (type.IsListType())
                {
                    throw new SchemaException();
                }

                if (namedType is ScalarType scalarType)
                {
                    InferResolver(def, scalarType, propertyName);
                }

                throw new SchemaException();
            });

        return descriptor;
    }

    public static IObjectFieldDescriptor FromJson<TResult>(
        this IObjectFieldDescriptor descriptor,
        Func<JsonElement, TResult> resolve)
    {
        descriptor
            .Extend()
            .OnBeforeCreate(def =>
            {
                def.ResultType = typeof(TResult);
                def.PureResolver = ctx => resolve(ctx.Parent<JsonElement>());
            });

        return descriptor;
    }

    private static void InferResolver(
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
            case ScalarNames.DateTime:
                def.PureResolver = ctx => ctx.GetProperty(propertyName)?.GetDateTimeOffset();
                return;
            default:
                throw new SchemaException(
                    SchemaErrorBuilder.New()
                        .SetMessage(
                            "Could not infer the correct mapping for the JSON object type `{0}`.",
                            def.Name)
                        .Build());
        }
    }

    private static JsonElement? GetProperty(this IPureResolverContext context, string propertyName)
        => context.Parent<JsonElement>().TryGetProperty(propertyName, out JsonElement element)
            ? element
            : null;
}
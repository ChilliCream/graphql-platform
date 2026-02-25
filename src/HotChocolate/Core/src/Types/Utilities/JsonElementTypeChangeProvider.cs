using System.Collections;
using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace HotChocolate.Utilities;

[RequiresDynamicCode("Uses Expression.Compile which requires dynamic code generation.")]
[RequiresUnreferencedCode("Uses reflection to access the Serialize method.")]
internal sealed class JsonElementTypeChangeProvider : IChangeTypeProvider
{
    private static readonly JsonSerializerOptions s_options =
        new JsonSerializerOptions(JsonSerializerDefaults.Web)
        {
            Converters = { new JsonStringEnumConverter() }
        };

    public bool TryCreateConverter(
        Type source,
        Type target,
        ChangeTypeProvider root,
        [NotNullWhen(true)] out ChangeType? converter)
    {
        if (target == typeof(JsonElement))
        {
            // Prefer explicit reference-type -> string converters so AnyType can serialize
            // custom runtime values (for example TimeZoneInfo) as strings.
            if (source is { IsValueType: false }
                && source != typeof(string)
                && !typeof(IEnumerable).IsAssignableFrom(source)
                && root(source, typeof(string), out var stringify)
                && root(typeof(string), typeof(JsonElement), out var toJsonElement))
            {
                converter = sourceValue => toJsonElement(stringify(sourceValue));
                return true;
            }

            var serializeMethod = typeof(JsonElementTypeChangeProvider)
                .GetMethod(nameof(Serialize), BindingFlags.NonPublic | BindingFlags.Static)!
                .MakeGenericMethod(source);

            var parameter = Expression.Parameter(typeof(object), "obj");
            var castParameter = Expression.Convert(parameter, source);
            var methodCall = Expression.Call(serializeMethod, castParameter);
            var convertResult = Expression.Convert(methodCall, typeof(object));
            var lambda = Expression.Lambda<ChangeType>(convertResult, parameter);
            converter = lambda.Compile();

            return true;
        }

        if (source == typeof(JsonElement))
        {
            var serializeMethod = typeof(JsonElementTypeChangeProvider)
                .GetMethod(nameof(Deserialize), BindingFlags.NonPublic | BindingFlags.Static)!
                .MakeGenericMethod(target);

            var parameter = Expression.Parameter(typeof(object), "obj");
            var castParameter = Expression.Convert(parameter, source);
            var methodCall = Expression.Call(serializeMethod, castParameter);
            var convertResult = Expression.Convert(methodCall, typeof(object));
            var lambda = Expression.Lambda<ChangeType>(convertResult, parameter);
            converter = lambda.Compile();

            return true;
        }

        converter = null;
        return false;
    }

    private static JsonElement Serialize<T>(T obj)
        => JsonSerializer.SerializeToElement(obj, s_options);

    private static T Deserialize<T>(JsonElement obj)
        => obj.Deserialize<T>(s_options)!;
}

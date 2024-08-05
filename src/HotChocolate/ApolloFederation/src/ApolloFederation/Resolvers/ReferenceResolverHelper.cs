using HotChocolate.Language;
using HotChocolate.Resolvers;
using static HotChocolate.ApolloFederation.FederationContextData;

namespace HotChocolate.ApolloFederation.Resolvers;

/// <summary>
/// This class provides helpers for the reference resolver expression generators.
/// </summary>
internal static class ReferenceResolverHelper
{
    public static bool Matches(
        IResolverContext context,
        IReadOnlyList<string[]> required)
        => ArgumentParser.Matches(
            context.GetLocalStateOrDefault<IValueNode>(DataField)!,
            required);

    public static ValueTask<object?> ExecuteAsync(
        IResolverContext context,
        FieldResolverDelegate resolver)
        => resolver(context);

    public static ValueTask<object?> Invalid(IResolverContext context)
    {
        var representation = context.GetLocalStateOrDefault<IValueNode>(DataField)?.ToString() ?? "null";

        throw new GraphQLException(
            new Error(
                "The entity for the given representation could not be resolved.",
                extensions: new Dictionary<string, object?>
                {
                    { nameof(representation), representation },
                }));
    }

    public static void TrySetExternal<TValue>(
        ObjectType type,
        IValueNode data,
        object entity,
        string[] path,
        Action<object, TValue?> setValue)
    {
        if (ArgumentParser.TryGetValue<TValue>(data, type, path, out var value))
        {
            setValue(entity, value);
        }
    }
}

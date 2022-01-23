using System.Collections.Generic;
using System.Threading.Tasks;
using HotChocolate.Language;
using HotChocolate.Resolvers;

namespace HotChocolate.ApolloFederation;

internal static class ReferenceResolverHelper
{
    public static bool Matches(
        IResolverContext context,
        IReadOnlyList<string[]> required)
        => ArgumentParser.Matches(
            context.GetLocalValue<IValueNode>("data")!,
            required);

    public static ValueTask<object?> ExecuteAsync(
        IResolverContext context,
        FieldResolverDelegate resolver)
        => resolver(context);

    public static ValueTask<object?> Invalid(IResolverContext context)
    {
        var representation = context.GetLocalValue<IValueNode>("data")?.ToString() ?? "null";

        throw new GraphQLException(
            new Error(
                "The entity for the given representation could not be resolved.",
                extensions: new Dictionary<string, object?>
                {
                    { nameof(representation), representation }
                }));
    }
}

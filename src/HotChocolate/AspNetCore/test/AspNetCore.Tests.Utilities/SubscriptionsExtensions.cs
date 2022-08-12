using System.Runtime.CompilerServices;
using HotChocolate.Types;

namespace HotChocolate.AspNetCore.Tests.Utilities;

[ExtendObjectType(OperationTypeNames.Subscription)]
public class SubscriptionsExtensions
{
    [SubscribeAndResolve]
    public async IAsyncEnumerable<string> OnNext(
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            yield return "next";
            await Task.Delay(50, cancellationToken);
        }
    }

    [SubscribeAndResolve]
    public async IAsyncEnumerable<string> OnException(
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        yield return "next";
        await Task.Delay(50, cancellationToken);

        yield return "next";
        await Task.Delay(50, cancellationToken);

        throw new GraphQLException(ErrorBuilder.New().SetMessage("Foo").Build());
    }
}

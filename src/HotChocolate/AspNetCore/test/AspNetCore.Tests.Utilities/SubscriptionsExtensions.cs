using System.Runtime.CompilerServices;
using HotChocolate.Types;

namespace HotChocolate.AspNetCore.Tests.Utilities;

[ExtendObjectType(OperationTypeNames.Subscription)]
public class SubscriptionsExtensions
{
#pragma warning disable CS0618
    [SubscribeAndResolve]
#pragma warning restore CS0618
    public async IAsyncEnumerable<string> OnNext(
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            yield return "next";
            await Task.Delay(50, cancellationToken);
        }
    }
#pragma warning disable CS0618
    [SubscribeAndResolve]
#pragma warning restore CS0618
    public async IAsyncEnumerable<string> OnException(
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        yield return "next";
        await Task.Delay(50, cancellationToken);

        yield return "next";
        await Task.Delay(50, cancellationToken);

        throw new GraphQLException(ErrorBuilder.New().SetMessage("Foo").Build());
    }

#pragma warning disable CS0618
    [SubscribeAndResolve]
#pragma warning restore CS0618
    public async IAsyncEnumerable<string> Delay(
        [EnumeratorCancellation] CancellationToken cancellationToken,
        int delay,
        int count)
    {
        while (!cancellationToken.IsCancellationRequested && count-- > 0)
        {
            yield return "next";
            await Task.Delay(delay, cancellationToken);
        }
    }
}

using System.Runtime.CompilerServices;
using HotChocolate.Types;

namespace HotChocolate.AspNetCore.Tests.Utilities;

[ExtendObjectType(OperationTypeNames.Subscription)]
public class SubscriptionsExtensions
{
    public async IAsyncEnumerable<string> DelaySubscribe(
        int delay,
        int count,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested && count-- > 0)
        {
            yield return "next";
            await Task.Delay(delay, cancellationToken);
        }
    }

    [Subscribe(With = nameof(DelaySubscribe))]
    public string Delay([EventMessage] string payload)
    {
        return payload;
    }
}

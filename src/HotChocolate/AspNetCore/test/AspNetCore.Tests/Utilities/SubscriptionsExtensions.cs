using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using HotChocolate.Types;
using Microsoft.AspNetCore.Http.Extensions;

namespace HotChocolate.AspNetCore.Utilities;

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

using System.Collections.Immutable;

namespace Mocha;

internal sealed class RegisteredConsumers(ImmutableArray<Consumer> consumers)
{
    public ImmutableArray<Consumer> Consumers { get; } = consumers;
}

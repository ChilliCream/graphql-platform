namespace Mocha.Mediator;

internal sealed class MediatorSetup
{
    public List<Action<IMediatorBuilder>> ConfigureMediator { get; } = [];
}

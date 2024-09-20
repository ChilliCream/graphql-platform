using GreenDonut;

namespace HotChocolate.Fetching;

/// <summary>
/// The execution engine batch scheduler and dispatcher.
/// </summary>
public interface IBatchHandler : IBatchDispatcher, IBatchScheduler;

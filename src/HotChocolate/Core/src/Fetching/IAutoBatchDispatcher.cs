using GreenDonut;

namespace HotChocolate.Fetching
{
    /// <summary>
    /// Describes a batch dispatcher that dispatches immediately whenever a new batch arrives.
    /// </summary>
    public interface IAutoBatchDispatcher
        : IBatchScheduler
    { }
}

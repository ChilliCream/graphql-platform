using GreenDonut;

namespace HotChocolate.Fetching
{
    /// <summary>
    /// Defines a batch dispatcher that immediately dispatches batch jobs.
    /// </summary>
    public interface IAutoBatchDispatcher : IBatchScheduler { }
}

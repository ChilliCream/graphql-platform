namespace HotChocolate.Data.Projections
{
    /// <summary>
    /// If this contract is implemented <see cref="FirstOrDefaultMiddleware{T}"/> will
    /// set <see cref="IFirstOrDefaultExecutable.FirstOrDefault"/> to true
    /// </summary>
    public interface IFirstOrDefaultExecutable
    {
        /// <summary>
        /// Set the behaviour of the executable to first or default
        /// </summary>
        /// <param name="mode">If set to true, first or default is applied</param>
        IExecutable FirstOrDefault(bool mode = true);
    }
}

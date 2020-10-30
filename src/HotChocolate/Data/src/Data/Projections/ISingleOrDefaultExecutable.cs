namespace HotChocolate.Data.Projections
{
    /// <summary>
    /// If this contract is implemented <see cref="SingleOrDefaultMiddleware{T}"/> will
    /// set <see cref="AddSingleOrDefault"/> to true
    /// </summary>
    public interface ISingleOrDefaultExecutable
    {
        /// <summary>
        /// Set the behaviour of the executable to single or default
        /// </summary>
        /// <param name="mode">If set to true, single or default is applied</param>
        IExecutable AddSingleOrDefault(bool mode = true);
    }
}

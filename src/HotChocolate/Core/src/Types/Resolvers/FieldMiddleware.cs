#nullable enable

namespace HotChocolate.Resolvers
{
    /// <summary>
    /// This delegate defines the factory to integrate a field middleware into the field pipeline.
    /// </summary>
    /// <param name="next">
    /// The next field middleware that has to be invoked after the middleware that is 
    /// created by this factory.
    /// </param>
    /// <returns>
    /// Returns the field middleware that is created by this factory.
    /// </returns>
    public delegate FieldDelegate FieldMiddleware(FieldDelegate next);
}

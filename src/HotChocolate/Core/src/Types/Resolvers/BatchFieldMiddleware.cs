namespace HotChocolate.Resolvers;

/// <summary>
/// This delegate defines the factory to integrate a batch field middleware
/// into the batch field pipeline.
/// </summary>
/// <param name="next">
/// The next batch field middleware that has to be invoked after the middleware
/// that is created by this factory.
/// </param>
/// <returns>
/// Returns the batch field middleware that is created by this factory.
/// </returns>
public delegate BatchFieldDelegate BatchFieldMiddleware(BatchFieldDelegate next);

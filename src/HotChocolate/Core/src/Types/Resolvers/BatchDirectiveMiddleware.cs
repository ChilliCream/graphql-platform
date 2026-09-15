using HotChocolate.Types;

namespace HotChocolate.Resolvers;

/// <summary>
/// Creates batch field middleware for a directive instance.
/// </summary>
public delegate BatchFieldDelegate BatchDirectiveMiddleware(BatchFieldDelegate next, Directive directive);

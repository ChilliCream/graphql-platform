namespace HotChocolate.Data.Projections;

public record ProjectionFeature(
    bool AlwaysProjected = false,
    bool HasProjectionMiddleware = false);

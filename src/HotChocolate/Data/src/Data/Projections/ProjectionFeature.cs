using System.Collections.Immutable;

namespace HotChocolate.Data.Projections;

public record ProjectionFeature(
    bool AlwaysProjected = false,
    bool HasProjectionMiddleware = false);

public record ProjectionTypeFeature(
    ImmutableArray<string> AlwaysProjectedFields);

using System.Collections.Immutable;

namespace HotChocolate.Data.Projections;

public record ProjectionTypeFeature(
    ImmutableArray<string> AlwaysProjectedFields);

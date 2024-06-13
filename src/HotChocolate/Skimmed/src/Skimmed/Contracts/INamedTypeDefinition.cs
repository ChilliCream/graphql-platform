using HotChocolate.Features;

namespace HotChocolate.Skimmed;

public interface INamedTypeDefinition
    : ITypeDefinition
    , INameProvider
    , IDirectivesProvider
    , IDescriptionProvider
    , IFeatureProvider;

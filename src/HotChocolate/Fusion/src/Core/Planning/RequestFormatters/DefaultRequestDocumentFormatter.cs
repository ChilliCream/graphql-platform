using HotChocolate.Fusion.Metadata;

namespace HotChocolate.Fusion.Planning;

internal sealed class DefaultRequestDocumentFormatter(
    FusionGraphConfiguration configuration)
    : RequestDocumentFormatter(configuration);

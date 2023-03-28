using HotChocolate.Fusion.Metadata;

namespace HotChocolate.Fusion.Planning;

internal sealed class DefaultRequestDocumentFormatter : RequestDocumentFormatter
{
    public DefaultRequestDocumentFormatter(FusionGraphConfiguration configuration)
        : base(configuration)
    {

    }
}

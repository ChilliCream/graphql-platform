using Microsoft.Extensions.Configuration;

namespace HotChocolate.Fusion.Aspire;

internal sealed record FusionPipelineInvocation(string Tag, string? Stage)
{
    public static FusionPipelineInvocation Resolve(IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        var tag = configuration["tag"] ?? configuration["NITRO_TAG"];
        if (string.IsNullOrWhiteSpace(tag))
        {
            throw new InvalidOperationException(
                "The Fusion pipeline requires a non-empty 'tag' command argument or NITRO_TAG.");
        }

        FusionPipelineExecutor.ValidatePathSegment(tag, "configuration tag");

        var stage = configuration["stage"] ?? configuration["NITRO_STAGE"];
        return new FusionPipelineInvocation(
            tag,
            string.IsNullOrWhiteSpace(stage) ? null : stage);
    }

    public string RequireStage()
        => Stage
            ?? throw new InvalidOperationException(
                "The Fusion publish pipeline requires a non-empty 'stage' command argument or "
                + "NITRO_STAGE.");
}

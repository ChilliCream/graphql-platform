# ChilliCream.Nitro.Aspire

Publishes Hot Chocolate Fusion configurations through the .NET Aspire deployment pipeline.

See [Publishing Fusion with Aspire](FUSION_PUBLISHING.md) for the command contract, artifact
layout, failure behavior, and CI/CD examples.

```csharp
var nitroApiKey = builder.AddParameter("nitroApiKey", secret: true);
var releaseId = builder.AddParameter("releaseId");
var releaseManifest = builder.AddParameter("fusionReleaseManifest");

var nitro = builder.AddNitroTarget("nitro")
    .WithCloudUrl("https://api.chillicream.com")
    .WithApiId("products-fusion")
    .WithApiKey(nitroApiKey);

nitro.AddFusionDeployment("production")
    .ForEnvironment("Production")
    .ToStage("production")
    .WithCompositionEnvironment("production")
    .WithConfigurationTag(releaseId)
    .WithFusionReleaseManifest(releaseManifest)
    .WithApproval(waitForApproval: true)
    .WithForce(false)
    .WithTimeouts(
        operation: TimeSpan.FromMinutes(15),
        approval: TimeSpan.FromHours(2));
```

The Fusion `aspire publish` step creates environment-neutral source archives without resolving a
Nitro credential or calling Nitro. Use `aspire do fusion-upload` in the build job to upload the
immutable source versions and finalize a portable release manifest. Deployment jobs pass that exact
manifest through `Parameters__fusionReleaseManifest` and run `aspire do fusion-publish`. Only the
final `fusion-release.json` needs to cross runners; apply downloads exact verified sources from
Nitro and rejects a different Fusion composition-tool version. A matching `aspire deploy` reaches
the same download, verification, environment-specific composition, readiness, and publication
graph.

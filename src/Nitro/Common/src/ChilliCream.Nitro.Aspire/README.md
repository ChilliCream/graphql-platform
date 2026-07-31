# ChilliCream.Nitro.Aspire

Publishes Hot Chocolate Fusion configurations through the .NET Aspire deployment pipeline.

See [Publishing Fusion with Aspire](FUSION_PUBLISHING.md) for the command contract, failure
behavior, ordering, and CI/CD example.

```csharp
var tag = builder.AddParameter("tag");
var nitroApiKey = builder.AddParameter("nitroApiKey", secret: true);

var nitro = builder.AddNitroTarget("nitro")
    .WithCloudUrl("https://api.chillicream.com")
    .WithApiId("products-fusion")
    .WithApiKey(nitroApiKey);

nitro.AddFusionDeployment("production")
    .ForEnvironment("Production")
    .ToStage("production")
    .WithCompositionEnvironment("production")
    .WithConfigurationTag(tag)
    .WithApproval(waitForApproval: true)
    .WithForce(false)
    .WithTimeouts(
        operation: TimeSpan.FromMinutes(15),
        approval: TimeSpan.FromHours(2));
```

Run `aspire do fusion-upload --environment Production` after building the source schemas. It
exports the AppHost-declared source set and reconciles every source as the immutable version
`name@tag` on the selected Nitro target.

Run `aspire do fusion-publish --environment Production` on the deployment runner. It infers the
same complete source set from the AppHost, downloads each exact `name@tag` from Nitro, composes with
the selected environment settings, deploys and checks the source services, publishes the Nitro
stage, and then deploys the gateway. It does not read source schemas, use Git, or upload source
versions. No manifest or CI artifact is passed between upload and publish.

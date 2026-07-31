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
same complete source set from the AppHost, preflight-downloads each exact `name@tag`, deploys the
source services, downloads and digest-matches the exact versions again, composes with the selected
environment settings, checks readiness, publishes the Nitro stage, and then deploys the gateway.
The preflight retains only identities and digests while the providers run. It does not read source
schemas, use Git, or upload source versions. No manifest or CI artifact is passed between upload and
publish. Exact source archives
and the composed FAR stay in a bounded, invocation-scoped memory session that is cleared after
success, failure, or cancellation. The Fusion-specific publish steps create no Fusion apply-state
files and do not resolve Aspire's output-path service. Hosting-provider dependencies may still
write target artifacts or Aspire deployment state according to that provider's configuration.

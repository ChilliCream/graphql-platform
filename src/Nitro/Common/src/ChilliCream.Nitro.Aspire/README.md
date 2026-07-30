# ChilliCream.Nitro.Aspire

Publishes Hot Chocolate Fusion configurations through the .NET Aspire deployment pipeline.

```csharp
var nitroApiKey = builder.AddParameter("nitroApiKey", secret: true);
var releaseId = builder.AddParameter("releaseId");

var nitro = builder.AddNitro("nitro")
    .WithCloudUrl("https://api.chillicream.com")
    .WithApiId("products-fusion")
    .WithApiKey(nitroApiKey);

nitro.AddFusionDeployment("production")
    .ForEnvironment("Production")
    .ToStage("production")
    .WithConfigurationTag(releaseId)
    .WithDefaultSourceVersionFromGitCommit()
    .WithApproval(waitForApproval: true)
    .WithForce(false)
    .WithTimeouts(
        operation: TimeSpan.FromMinutes(15),
        approval: TimeSpan.FromHours(2));
```

`aspire publish` creates portable Fusion artifacts and has no Nitro side effects. Use
`aspire do fusion-upload` to reconcile source versions and `aspire do fusion-publish` to run the
full readiness, composition, and publication graph. A matching `aspire deploy` also requires
`fusion-publish` to finish successfully.

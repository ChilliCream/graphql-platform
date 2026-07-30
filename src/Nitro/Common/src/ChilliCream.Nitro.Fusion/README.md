# ChilliCream.Nitro.Fusion

Provides the high-level workflow used to reconcile immutable Fusion source schema versions and
publish composed Fusion configurations to Nitro.

```csharp
services.AddNitroFusionDeploymentWorkflow();

var workflow = services.BuildServiceProvider()
    .GetRequiredService<IFusionDeploymentWorkflow>();
```

The public API uses deployment DTOs only. Nitro's generated GraphQL management types remain
internal to this package.

`FusionSourceSchemaContent.ComputeSha256Async` computes the normalized content identity recorded
in a release manifest. `IFusionDeploymentWorkflow.DownloadSourceSchemaAsync` downloads an exact
name and version, validates the archive settings, and verifies that identity before returning the
archive bytes.

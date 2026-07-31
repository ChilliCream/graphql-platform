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

`FusionSourceSchemaContent.ComputeSha256Async` computes a normalized content identity.
`IFusionDeploymentWorkflow.DownloadSourceSchemaAsync` downloads an exact name and version,
validates the archive and settings name, then returns the archive bytes with their canonical
content identity.

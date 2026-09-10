# Fusion Compatibility Tests

This project is a deliberately non-friend Fusion consumer. It proves the 16.7 router rename kept
its promised compatibility surface, and it separately records the small set of direct breaks the
router rename was explicitly approved to make.

## Covered compatibility promise

The following surface is proven from outside the product assemblies, with no `InternalsVisibleTo`
access:

- `IServiceCollection.AddGraphQLGateway` and `IServiceCollection.AddGraphQLGatewayServer`.
- `IHostApplicationBuilder.AddGraphQLGateway`.
- `IFusionGatewayBuilder` and its router counterpart, `IFusionRouterBuilder`.
- The five legacy extension families (Core, Caching, Diagnostics, InMemory, AspNetCore) and their
  router-named twins: same method names, same normalized signatures, the legacy methods marked
  obsolete on `IFusionGatewayBuilder`, the router methods clean on `IFusionRouterBuilder`.
- Declaring-class static calls against the legacy extension classes, not just extension-method
  call syntax.
- A custom, legacy-only builder that implements `IFusionGatewayBuilder` alone, flowing through
  both a legacy extension and a third-party-shaped extension written against the old interface.
- Third-party extensions written against `IFusionGatewayBuilder`.
- The pinned 16.6.4 binary baseline: an unchanged, compiled 16.6.4 consumer, executed against the
  current product assemblies (see `Baseline/artifacts/PROVENANCE.md`).

## Excluded from the promise (approved direct breaks)

The router rename directly renamed the following surfaces without an obsolete compatibility
layer. Each exclusion was explicitly approved; see
`.nitro/designs/repo-oek-fusion-router/decisions.md` for the full rulings.

- **Packaging C# API** (repo-oek.14): `GatewayConfiguration` is `RouterConfiguration`,
  `GetSupportedGatewayFormatsAsync`/`SupportedGatewayFormats` is
  `GetSupportedRouterFormatsAsync`/`SupportedRouterFormats`, and
  `TryGetGatewayConfigurationAsync` is `TryGetRouterConfigurationAsync`. Archive entries and
  metadata names are unchanged.
- **Fusion.Execution.Types** (repo-oek.15): `IsGatewayField`/`isGatewayField` is
  `IsRouterField`/`isRouterField`.
- **Raw setup plumbing** (repo-oek.16): `FusionGatewaySetup` is `FusionRouterSetup`,
  `FusionSetupUtilities` signatures changed accordingly, and
  `IOptionsMonitor<FusionGatewaySetup>` registrations are now
  `IOptionsMonitor<FusionRouterSetup>`.
- **The internal-only build hook** (repo-oek.8): `BuildRouterAsync` is internal, and the internal
  obsolete `BuildGatewayAsync` alias exists only to keep in-product callers compiling. Neither is
  part of the public compatibility promise; ordinary product tests use `BuildRouterAsync`.

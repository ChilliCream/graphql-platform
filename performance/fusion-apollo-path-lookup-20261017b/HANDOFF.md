C5 shared BenchmarkDotNet queue handoff

Baseline43a8dca2bd479ddc411d832ed97e42d9fcc8bd85; isolated prototype contains two production files and one new test file. See source.patch and manifest.json. No production change after frozen engine identity. No custom timing or allocation measurement exists for C5.

Use the common measured engine layout and overlay only frozen HotChocolate.Fusion.Execution.dll/PDB from /workspaces/platform-1/perf/fusion-apollo-path-lookup-20261017b/frozen. All other runtime/config/dependencies must remain common. Default runtime policy; no allocation-only heap overrides.

Minimal actual-executor cases ordinary, batch9 and duplicate96-to1 exercise the unchanged ordinary path and the changed target resolution during entity merge. A deeper existing original-shaped case is preferable if already available without adding a new benchmark framework. This is a CPU-only hypothesis: no allocation delta is predicted. The warm profile bounds potential benefit to a small part of whole-executor cost, so absence of clear worthwhile gain should end the candidate without favorable-result retries or VU.

Source review confirmed logical terminal and null-reference cursor preservation. Unsupported traversal returns to the original method for error behavior. Tests93/93 include fields/root,150-elementmulti-chunk lists, shared references/nullmarkers, null ancestors, invalid-kind fallback, existing representation merge cases and result formatting. Build0errors with two existing ASP.NET deprecation warnings.

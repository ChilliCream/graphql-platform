# Nitro Router releases

The release workflow coordinates artifact generation and publication across
graphql-platform and nitro-router.

1. build-router selects router main and dispatches release-build.yml with that
   router commit, the platform release commit, and the platform version. It waits
   for the downstream result.
2. artifacts-ready waits for platform NuGet packages, CLI/npm artifacts, and the
   router build. Every platform package publisher depends on this job.
3. After platform NuGet and npm publication, publish-router dispatches
   release-publish.yml with the version and the router build run ID. It waits
   for publication to finish.
4. publish-release downloads the prepared router native archives into the
   platform draft release before publishing the immutable GitHub release.

A router build or publication failure fails the platform workflow. Router
publication downloads the specified build's artifacts without rebuilding.
Release workflows do not run application tests or runtime checks.

The existing GitHub App needs access to nitro-router with Actions write and
Contents read. Share the platform CLI signing credentials with nitro-router and
configure its Docker Hub credentials. NuGet publishing can be enabled later
using the router's NITRO_ROUTER_PUBLISH_NUGET variable and NuGet trusted publishing.

See the router repository's docs/releases.md for the native target matrix,
container settings, and credential names. No repository settings or credentials
are changed by these workflows.

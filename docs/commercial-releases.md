# Commercial releases

The release workflow coordinates artifact generation and publication across
graphql-platform and graphql-platform-commercial ("the commercial repo").

## Release lanes

The commercial repo has no main branch. It keeps one `release/vX.Y` branch
(a lane) per platform minor, and its default branch is the newest lane. A
platform release `X.Y.Z` runs the workflows of the lane `release/vX.Y` and
builds the commit at the tip of that lane.

Before tagging the first version of a new minor or major, run
`./prepare-release.sh <version>` from the repository root. It creates the lane
where it is missing in the commercial repo, the Nitro GitHub action repos, and
nitro-azure-pipelines-tasks (stable versions only). It makes the lane the
default branch where it is the newest lane of a repository. `--dry-run` prints
the changes without making them. Patch releases and later previews of a minor
need no run.

The script uses the gh CLI with your login. You need admin rights on the
repositories whose default branch it changes.

## Release flow

1. build-commercial derives the lane from the version and dispatches build.yml
   on it with the platform release commit and the version. It waits for the
   result. A missing lane fails this job.
2. artifacts-ready waits for platform NuGet packages, CLI/npm artifacts, and the
   commercial build. Every platform package publisher depends on this job.
3. After platform NuGet and npm publication, publish-commercial dispatches
   publish.yml on the lane with the version and the build run ID. It waits for
   publication to finish.
4. publish-release downloads the Nitro Router native archives of the build run
   into the platform draft release before publishing the immutable GitHub
   release.

A commercial build or publication failure fails the platform workflow.
Publication downloads the specified build's artifacts without rebuilding.
Release workflows do not run application tests or runtime checks.

Publication tags the commercial repo with the version, and build.yml refuses a
version that is already tagged. After a failure, re-run only the failed jobs. A
full re-run of a release that already published its commercial artifacts fails
at build-commercial.

## Pull requests

commercial.yml builds the commercial repo against a pull request after its CI
succeeds and reports the result as the commit status "Commercial
Compatibility".

- A pull request targeting main builds the newest lane.
- A pull request targeting main-version-X-Y builds `release/vX.Y`. Without that
  lane there is no build and no status.
- A fork pull request is not built automatically. After reviewing it, run the
  workflow by hand with its head SHA as `graphql_platform_ref`.

Before tagging an older minor from main, run the workflow by hand with
`commercial_ref` set to the lane of that minor.

## Access

The GitHub App behind `ACTIONS_APP_CLIENT_ID` needs access to
graphql-platform-commercial with the repository permission Actions: Read and
write. It needs no Contents permission.

The commercial repo holds its own signing credentials and logs in to Docker Hub
through an OIDC connection. No repository settings or credentials are changed by
these workflows.

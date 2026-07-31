# Aspire publish and deploy

Research date: 2026-07-30. Version reviewed: Aspire 13.4.6, the latest stable
release on the research date.

## Executive summary

Aspire deployment is driven by the application model in the AppHost. A hosting
integration provides the API for adding a compute-environment resource, such as
Docker Compose, Kubernetes, Azure Kubernetes Service (AKS), Azure Container
Apps, or Azure App Service. That resource contributes target-specific steps to
Aspire's deployment pipeline.

- `aspire publish` runs the pipeline's publish entry point and writes
  target-specific artifacts for another tool, CI stage, GitOps system, or person
  to apply later. In the documented built-in targets, unresolved parameter
  requirements, including secrets, remain represented as target-specific
  placeholders.
- `aspire deploy` runs the deploy entry point and its dependencies. Aspire
  resolves parameters, generates any target output it needs, and applies the
  deployment itself.
- `aspire deploy` does **not** consume artifacts from an earlier
  `aspire publish`. They are separate pipeline entry points.
- `aspire do <step>` is the lower-level option for workflows that need to split
  building, pushing, publishing, and deployment across stages.

These semantics and the no-op behavior when no target contributes a matching
step are documented in [How Aspire deployment works](https://aspire.dev/deployment/deploy-with-aspire/).

## Version and stability status

The official release page identifies
[Aspire 13.4.6](https://github.com/microsoft/aspire/releases/tag/v13.4.6) as the
latest stable release. It was released on 2026-06-20.

There are two distinct stability statements to keep in mind:

1. Aspire 13.4.6 is a stable Aspire release.
2. Aspire 13.4 made `aspire publish` and `aspire deploy` generally available,
   supported workflows, according to the
   [Aspire 13.4 release notes](https://aspire.dev/whats-new/aspire-13-4/#deployment-commands-are-generally-available).
   Some current reference navigation still displays "Preview" badges for these
   commands. The lower-level `aspire do` command and pipeline extension APIs
   remain preview/experimental, and the pipeline system is described as
   experimental in Aspire 13 by the
   [pipeline documentation](https://aspire.dev/deployment/pipelines/).

Production automation should pin and test the CLI/SDK versions and treat custom
pipeline APIs, in particular, as capable of changing in a future release.

Target integrations have their own stability and package versions, independent
of the core commands. On the research date, the official packages for
[Docker](https://www.nuget.org/packages/Aspire.Hosting.Docker/13.4.6) and
[Azure Container Apps](https://www.nuget.org/packages/Aspire.Hosting.Azure.AppContainers/13.4.6)
were stable 13.4.6 packages. The
[Kubernetes](https://www.nuget.org/packages/Aspire.Hosting.Kubernetes/13.4.6-preview.1.26319.6)
and
[AKS](https://www.nuget.org/packages/Aspire.Hosting.Azure.Kubernetes/13.4.6-preview.1.26319.6)
packages were prerelease `13.4.6-preview.1.26319.6` packages. The
[App Service deployment guide](https://aspire.dev/deployment/azure/app-service/)
explicitly marks that target as Preview, even though its package version is
13.4.6.

### Current terminology: pipelines and compute environments

Older Aspire 9.x articles refer to top-level "publishers" and APIs such as
`AddDockerComposePublisher` or `IDistributedApplicationPublisher`. Those are not
the current Aspire 13 model. Aspire 13.0 replaced publishing callbacks and the
publisher interface with pipeline steps. Current applications add a hosting
integration and a compute-environment resource, and resources contribute
pipeline steps. See the official
[Aspire 13 migration section](https://aspire.dev/deployment/pipelines/#migrating-from-publishing-callbacks).

Methods whose names start with `PublishAs`, such as
`PublishAsDockerComposeService` or `PublishAsAzureContainerApp`, still exist for
target-specific customization. They customize a generated resource; they do not
replace adding the target environment itself.

## How the pipeline works

The AppHost is both the local orchestration model and the source of deployment
behavior. For a publish or deploy operation, the CLI:

1. Finds the AppHost from `--apphost`, a rooted `aspire.config.json`, or a search
   below the current directory, in that order.
2. Records the selected AppHost in the rooted `aspire.config.json`.
3. Verifies Aspire's local hosting certificates.
4. Builds and starts the AppHost and its resources.
5. Runs the requested pipeline entry point and its dependency graph.
6. Prints a hierarchical step summary with status and duration.

The exact command lifecycle is in the
[`aspire publish` reference](https://aspire.dev/reference/cli/commands/aspire-publish/)
and
[`aspire deploy` reference](https://aspire.dev/reference/cli/commands/aspire-deploy/).

Independent pipeline steps may run concurrently, while declared dependencies
determine ordering. Hosting integrations typically contribute the steps for
infrastructure provisioning, container builds and pushes, artifact generation,
and applying the target deployment. Applications can add their own steps, but
the pipeline API is experimental.

### No target means no work

If the AppHost has no resource that contributes work to the selected entry
point, the current CLI completes successfully as a no-op. A successful
`aspire publish` or `aspire deploy` with an almost empty step summary therefore
does not prove that anything was generated or deployed. Add the appropriate
target integration and environment, then inspect the plan:

```shell
aspire publish --list-steps
aspire deploy --list-steps
```

## `aspire publish` versus `aspire deploy`

| Concern | `aspire publish` | `aspire deploy` |
| --- | --- | --- |
| Intent | One-way artifact handoff | Aspire-managed end-to-end deployment |
| Pipeline entry point | `publish` | `deploy` plus its dependent steps, which may include `publish` |
| Parameters | Preserves requirements as target-specific placeholders in emitted artifacts | Resolves required values before applying changes |
| Side effects on target | Does not apply the emitted artifacts | Provisions or updates infrastructure and workloads as the target integration defines |
| Reuse of prior output | Output is for external consumers | Does not read an earlier publish output as its input |
| Default artifact directory | `<AppHost>/aspire-output` | `<AppHost>/aspire-output` when the target emits deployment artifacts |
| Default Aspire environment | `Production` | `Production` |
| Best fit | Reviewable/promotable artifacts, GitOps, separate approval/apply stages | Local or CI stage has target credentials and Aspire should own the complete operation |

The
[deployment model](https://aspire.dev/deployment/deploy-with-aspire/#pipeline-entry-points)
defines the command distinction. The command references document
`--output-path`, `--environment`, `--apphost`, `--list-steps`, `--no-build`,
logging options, and `--non-interactive`.

Useful current command forms are:

```shell
aspire publish --apphost ./MyApp.AppHost/MyApp.AppHost.csproj \
  --environment Staging \
  --output-path ./artifacts

aspire deploy --apphost ./MyApp.AppHost/MyApp.AppHost.csproj \
  --environment Production \
  --non-interactive
```

Arguments after `--` are passed to the AppHost:

```shell
aspire publish --apphost ./MyApp.AppHost/MyApp.AppHost.csproj -- --my-apphost-option
```

`--apphost` is the current documented option name. The
[tagged 13.4.6 CLI source](https://github.com/microsoft/aspire/blob/v13.4.6/src/Aspire.Cli/Commands/PipelineCommandBase.cs)
retains `--project` as a legacy alias, but new scripts should use `--apphost`.
`--publisher` belongs to the removed top-level publisher workflow and should not
be used in an Aspire 13.4 workflow.

## Built-in deployment targets

The current built-in capability matrix in
[How Aspire deployment works](https://aspire.dev/deployment/deploy-with-aspire/#built-in-target-capabilities)
is:

| Hosting integration | Reviewed version/status | AppHost environment | Publish result | Deploy behavior |
| --- | --- | --- | --- | --- |
| `Aspire.Hosting.Docker` | 13.4.6 | `AddDockerComposeEnvironment` | Docker Compose files and parameter placeholders | Builds images and starts the generated Compose application |
| `Aspire.Hosting.Kubernetes` | 13.4.6 prerelease | `AddKubernetesEnvironment` | Helm chart for an existing Kubernetes cluster | Installs through Helm into the current `kubectl` context |
| `Aspire.Hosting.Azure.Kubernetes` | 13.4.6 prerelease | `AddAzureKubernetesEnvironment` | Helm chart plus Azure Bicep infrastructure | Provisions AKS/ACR and Azure dependencies, pushes images, and installs the chart |
| `Aspire.Hosting.Azure.AppContainers` | 13.4.6 | `AddAzureContainerAppEnvironment` | Azure target artifacts, including generated provisioning resources | Provisions or attaches the Container Apps environment and ACR, pushes images, and deploys Container Apps |
| `Aspire.Hosting.Azure.AppService` | Preview target, 13.4.6 package | `AddAzureAppServiceEnvironment` | Azure target artifacts | Provisions or attaches the App Service plan and ACR, pushes images, and deploys supported websites |

Compute resources automatically attach when exactly one compatible environment
exists. If multiple compatible compute environments exist, use
`WithComputeEnvironment` to make the target explicit. This also enables hybrid
deployments in which resources from one AppHost go to different targets.

### Generated artifact details

Artifact contents are target-specific:

- Docker Compose writes `docker-compose.yaml`, an unfilled `.env` from
  `aspire publish`, an environment-specific `.env.{environment}` from prepare or
  deploy, and per-resource Dockerfiles when the resource uses an existing or
  generated Dockerfile build context. See
  [Docker Compose output artifacts](https://aspire.dev/deployment/docker-compose/#output-artifacts).
- Kubernetes turns projects and containers into Deployments or StatefulSets,
  endpoints into Services, configuration into ConfigMaps and Secrets, and
  volumes into persistent-volume resources. Publish produces a Helm chart. See
  [Deploy to Kubernetes](https://aspire.dev/deployment/kubernetes/).
- AKS publish produces Helm and Bicep artifacts. Direct deploy additionally
  provisions the cluster, ACR, managed identity, and referenced Azure services.
  See
  [Publish AKS artifacts](https://aspire.dev/deployment/kubernetes/aks/#publish-aks-artifacts).
- Azure integrations generate provisioning resources from the AppHost. For
  example, the default Container Apps environment model generates Bicep for the
  managed environment, ACR, managed identity, Log Analytics, role assignments,
  and Aspire Dashboard. See
  [Configure Azure Container Apps environments](https://aspire.dev/integrations/cloud/azure/configure-container-apps/).

Generated output should be reviewed as target configuration, not assumed to be a
portable intermediate representation. Each integration owns its output shape.

## Prerequisites

### Base prerequisites

For a C# AppHost in Aspire 13.4, the official prerequisites require:

- .NET 10 SDK for the AppHost. Aspire can still orchestrate applications that
  target .NET 8 or later.
- The Aspire CLI.
- An OCI-compliant container runtime where the selected workflow builds or runs
  containers. Docker Desktop is the recommended default; Podman is supported.

The current details, including TypeScript AppHost runtime requirements, are in
[Aspire prerequisites](https://aspire.dev/get-started/prerequisites/).

### Target-specific prerequisites

- Docker Compose: Docker or Podman. Podman must be 5.0 or later for the current
  Compose deployment support. Aspire can auto-detect the runtime or use
  `ASPIRE_CONTAINER_RUNTIME=docker|podman`.
- Existing Kubernetes cluster: `kubectl` on `PATH`, Helm 4.2.0 or later, and a
  valid current `kubectl` context.
- AKS: the Kubernetes tools above, Azure CLI, an Azure account, and an active
  subscription.
- Azure Container Apps or App Service: an Azure account/subscription and, for the
  default local credential source, Azure CLI on `PATH` followed by `az login`.

Use
[Deploy to Docker Compose](https://aspire.dev/deployment/docker-compose/),
[Deploy to Kubernetes](https://aspire.dev/deployment/kubernetes/), and
[Deploy to Azure](https://aspire.dev/deployment/azure/) for the target-specific
checks. The deployment identity also needs the target-specific permissions
required for resources that the deployment provisions, updates, or attaches.

## Configuration, parameters, and secrets

### Parameter declaration and resolution

An external value is modeled as a parameter resource:

```csharp
var builder = DistributedApplication.CreateBuilder(args);

var apiKey = builder.AddParameter("apiKey", secret: true);

builder.AddDockerComposeEnvironment("compose");

builder.AddProject<Projects.Api>("api")
    .WithEnvironment("API_KEY", apiKey);

builder.Build().Run();
```

The official
[external parameters guide](https://aspire.dev/fundamentals/external-parameters/)
documents this resolution order:

1. Environment variables named `Parameters__*`.
2. AppHost configuration sources, including `appsettings.json` and .NET user
   secrets.
3. An interactive prompt when no value is available.

For example, `apiKey` can be supplied in automation as:

```shell
export Parameters__apiKey='value-from-the-ci-secret-store'
```

For a parameter containing dashes, the documented environment-variable mapping
uses a single underscore for each dash. `registry-endpoint` therefore becomes
`Parameters__registry_endpoint`.

`secret: true` is a hint to deployment integrations that the value should be
treated as sensitive; it is not a guarantee that every storage path is
encrypted. In the publish handoff, the value remains a placeholder. For the
Docker Compose example above, the generated shape is conceptually:

```yaml
# docker-compose.yaml
services:
  api:
    environment:
      API_KEY: ${APIKEY}
```

```dotenv
# .env
APIKEY=
```

`API_KEY` is the variable received by the application. `APIKEY` is the
target-generated placeholder in the Compose artifacts. Placeholder naming is
target-specific and should not be inferred from the application variable name.

### Aspire environment versus compute environment

`--environment Staging` sets the Aspire environment used while evaluating the
AppHost and scopes deployment state. It is not the compute environment resource.
It also does not automatically set `DOTNET_ENVIRONMENT`, `ASPNETCORE_ENVIRONMENT`,
or `NODE_ENV` in child applications. Set those on the child resources when
needed. See [Aspire environments](https://aspire.dev/deployment/environments/).

### Deployment state and its secret-handling consequence

`aspire deploy` manages environment-scoped deployment state and can cache
provisioning inputs and parameter values per AppHost and environment under:

```text
~/.aspire/deployments/{AppHostSha}/{environment}.json
```

The official
[deployment state caching guide](https://aspire.dev/deployment/deployment-state-caching/)
warns that these files can include secrets and are stored in plain text outside
the repository, following the same security model as .NET user secrets. Any
process running as the same OS user can read them.

Consequences:

- Do not commit, publish, or indiscriminately cache this directory.
- Protect CI caches and restrict who can restore them.
- Prefer the CI platform's secret store plus `Parameters__*` variables for
  non-interactive deployments.
- Use `aspire deploy --clear-cache` when values need to be re-entered. This
  clears the selected environment's cache and does not save the newly prompted
  values after that deployment.

### Azure authentication and settings

Local Azure deploys use Azure CLI credentials by default:

```shell
az login
```

The shared Azure inputs can be supplied using configuration or environment
variables:

| Setting | Environment variable |
| --- | --- |
| `Azure:SubscriptionId` | `Azure__SubscriptionId` |
| `Azure:Location` | `Azure__Location` |
| `Azure:ResourceGroup` | `Azure__ResourceGroup` |
| `Azure:CredentialSource` | `Azure__CredentialSource` |

Supported credential sources and CI guidance are in
[Deploy to Azure](https://aspire.dev/deployment/azure/). For GitHub Actions and
Azure DevOps, the official guidance prefers workload identity federation over
long-lived client secrets.

Before using `--non-interactive`, provide all required target settings and
`Parameters__*` values. The flag disables prompts; it does not invent defaults
for unresolved inputs.

## Practical examples from the official guides

The examples below use the current C# AppHost APIs and Aspire 13.4 CLI forms.

### 1. Publish or directly deploy Docker Compose

Add a Compose environment. With a single compatible environment, compute
resources are included automatically:

```csharp
var builder = DistributedApplication.CreateBuilder(args);

builder.AddDockerComposeEnvironment("compose");
builder.AddProject<Projects.Api>("api");

builder.Build().Run();
```

Generate handoff artifacts without building container images:

```shell
aspire publish --output-path ./aspire-output
```

The result includes `docker-compose.yaml` and an unfilled `.env`. A later stage
fills the placeholders and applies the Compose definition. Alternatively, let
Aspire perform the entire local Compose deployment:

```shell
aspire deploy --environment Staging
```

For this target, direct deploy generates the Compose and filled
`.env.Staging`, builds images, and runs
`docker compose up -d --remove-orphans`. The complete progressive workflow is in
the official
[Docker Compose deployment guide](https://aspire.dev/deployment/docker-compose/#publishing-and-deployment-workflow).

### 2. Publish a Helm chart or deploy to an existing Kubernetes cluster

Install the integration and add a Kubernetes environment:

```shell
aspire add kubernetes
```

```csharp
var builder = DistributedApplication.CreateBuilder(args);

#pragma warning disable ASPIRECOMPUTE003
var registry = builder.AddContainerRegistry(
    "registry",
    "myregistry.example.com:5000");

var k8s = builder.AddKubernetesEnvironment("k8s")
    .WithContainerRegistry(registry);
#pragma warning restore ASPIRECOMPUTE003

builder.AddProject<Projects.Api>("api")
    .WithComputeEnvironment(k8s)
    .WithExternalHttpEndpoints();

builder.Build().Run();
```

For an artifact/GitOps workflow:

```shell
aspire publish --output-path ./k8s-artifacts
```

For direct deployment to the current `kubectl` context:

```shell
aspire deploy --environment Production
```

The registry must be reachable from both the machine running Aspire and the
cluster, and the deployment environment must have the required registry
credentials. The container-registry API is Preview and currently requires the
`ASPIRECOMPUTE003` warning suppression shown above. The Kubernetes target
publishes a Helm chart; direct deploy builds and pushes project images, then
installs the chart with Helm. Verify the current context before invoking deploy.
Also configure the target's ingress or Gateway API support if external endpoints
must be reachable from outside the cluster. See the official
[cluster deployment guide](https://aspire.dev/deployment/kubernetes/clusters/)
for registry, Helm, context, and exposure details.

### 3. Directly deploy to Azure Container Apps

Install the target integration:

```shell
aspire add azure-appcontainers
```

Add the environment and an externally reachable workload:

```csharp
var builder = DistributedApplication.CreateBuilder(args);

builder.AddAzureContainerAppEnvironment("aca-env");

builder.AddDockerfile("web", "./web")
    .WithHttpEndpoint(port: 8080, targetPort: 8080, name: "http")
    .WithExternalHttpEndpoints();

builder.Build().Run();
```

Authenticate and deploy interactively:

```shell
az login
aspire deploy
```

Aspire provisions or attaches the Container Apps environment, ACR, identity and
supporting Azure resources, builds and pushes the image, and deploys the
Container App. This example and the target's endpoint constraints are in
[Deploy to Azure Container Apps](https://aspire.dev/deployment/azure/container-apps/).

A CI deployment supplies Azure authentication, the three shared Azure settings,
and all AppHost parameters before running:

```shell
aspire deploy \
  --apphost ./MyApp.AppHost/MyApp.AppHost.csproj \
  --environment Production \
  --non-interactive
```

### 4. Publish reviewable AKS infrastructure and workload artifacts

The current Kubernetes and AKS hosting integrations are prerelease packages,
even though the core `aspire publish` and `aspire deploy` commands are GA.

Configure an AKS environment:

```shell
aspire add azure-kubernetes
```

```csharp
var builder = DistributedApplication.CreateBuilder(args);

builder.AddAzureKubernetesEnvironment("aks");
builder.AddProject<Projects.Api>("api");

builder.Build().Run();
```

Publish without applying:

```shell
aspire publish --output-path ./aks-artifacts
```

The result contains Helm charts and Bicep infrastructure templates for review
or use by a separate CI/CD or GitOps workflow. Running `aspire deploy` instead
provisions AKS, ACR, managed identity and referenced Azure resources, pushes the
images, and installs the Helm chart. See the official
[AKS deployment guide](https://aspire.dev/deployment/kubernetes/aks/).

### 5. Target Azure App Service (Preview)

The official guide marks this deployment target as Preview. For public websites
and APIs that fit App Service's single-public-endpoint model:

```shell
aspire add azure-appservice
```

```csharp
var builder = DistributedApplication.CreateBuilder(args);

builder.AddAzureAppServiceEnvironment("app-service-env");

builder.AddDockerfile("web", "./web")
    .WithHttpEndpoint(port: 8080, targetPort: 8080, name: "http")
    .WithExternalHttpEndpoints();

builder.Build().Run();
```

```shell
az login
aspire deploy
```

Aspire provisions the App Service plan, ACR and managed identity, then builds,
pushes and deploys supported website images. Arbitrary infrastructure containers
and internal-only or multi-port workloads do not fit this target; use managed
backing services or a different compute environment. See
[Deploy to Azure App Service](https://aspire.dev/deployment/azure/app-service/).

## CI/CD workflow choices

The
[Aspire CI/CD overview](https://aspire.dev/deployment/ci-cd/) recommends keeping
application topology and target behavior in the AppHost while the CI system owns
checkout, tests, approvals, credentials, artifact retention, and promotion.

- Artifact-first: run `aspire publish`, retain the output, review or promote it,
  then use the target's normal tool in a later stage.
- Direct deployment: authenticate the runner and run `aspire deploy
  --environment <name> --non-interactive`.
- Split workflow: use the Preview `aspire do` command for `build`, `push`, named
  target steps, or custom AppHost steps when build, push, and apply must cross
  stage boundaries. The command and custom pipeline APIs are not GA.

Useful safeguards:

```shell
# Show the resolved step graph without executing it.
aspire deploy --list-steps

# Skip the AppHost project build and restore if that project is already built.
# Deployment steps can still build workload images.
aspire deploy --no-build --non-interactive

# Increase pipeline diagnostics while troubleshooting.
aspire deploy --pipeline-log-level debug --include-exception-details
```

The Aspire environment should be passed explicitly in automation because it
affects AppHost evaluation and deployment-state scope:

```shell
aspire publish --environment Staging --output-path ./artifacts
aspire deploy --environment Production --non-interactive
```

## Common mistakes

1. **Running a successful no-op.** A target environment must contribute publish
   or deploy steps. Use `--list-steps` before trusting the command.
2. **Treating deploy as "apply my published folder."** Deploy regenerates what it
   needs and does not consume a previous publish result.
3. **Using Aspire 9.x publisher examples.** Current Aspire uses compute
   environments and pipeline steps, not `Add*Publisher`,
   `IDistributedApplicationPublisher`, `--publisher`, or publishing callbacks.
4. **Assuming placeholders and application variables have identical names.**
   Placeholder naming belongs to the target integration.
5. **Assuming `--environment` propagates to child processes.** Explicitly set
   framework runtime environment variables on child resources.
6. **Using `--non-interactive` without complete inputs.** Supply credentials,
   target settings, and `Parameters__*` variables first.
7. **Treating the deployment cache as encrypted secret storage.** It can contain
   plain-text parameter values and needs OS- and CI-level protection.
8. **Deploying to an unintended Kubernetes cluster.** The non-Azure Kubernetes
   target uses the current `kubectl` context.
9. **Omitting a registry for direct Kubernetes deployment.** Project images
   need a registry that is reachable from the deployment machine and cluster.
10. **Copying old CLI flags.** Current AppHost selection uses `--apphost`.
    Current parameter input uses AppHost configuration or `Parameters__*`;
    current command references do not list old flags such as `--parameter` or
    `--deployment-params-file`.

## Primary official sources

- [Aspire 13.4.6 release](https://github.com/microsoft/aspire/releases/tag/v13.4.6)
- [What's new in Aspire 13.4](https://aspire.dev/whats-new/aspire-13-4/)
- [Aspire prerequisites](https://aspire.dev/get-started/prerequisites/)
- [Aspire deployment overview](https://aspire.dev/deployment/)
- [How Aspire deployment works](https://aspire.dev/deployment/deploy-with-aspire/)
- [Pipelines and app topology](https://aspire.dev/deployment/pipelines/)
- [`aspire publish` command](https://aspire.dev/reference/cli/commands/aspire-publish/)
- [`aspire deploy` command](https://aspire.dev/reference/cli/commands/aspire-deploy/)
- [External parameters](https://aspire.dev/fundamentals/external-parameters/)
- [Environments](https://aspire.dev/deployment/environments/)
- [Deployment state caching](https://aspire.dev/deployment/deployment-state-caching/)
- [CI/CD overview](https://aspire.dev/deployment/ci-cd/)
- [Docker Compose deployment](https://aspire.dev/deployment/docker-compose/)
- [Kubernetes deployment](https://aspire.dev/deployment/kubernetes/)
- [Kubernetes cluster deployment](https://aspire.dev/deployment/kubernetes/clusters/)
- [AKS deployment](https://aspire.dev/deployment/kubernetes/aks/)
- [Azure deployment overview](https://aspire.dev/deployment/azure/)
- [Azure Container Apps deployment](https://aspire.dev/deployment/azure/container-apps/)
- [Azure App Service deployment](https://aspire.dev/deployment/azure/app-service/)

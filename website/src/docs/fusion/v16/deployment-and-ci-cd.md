---
title: "Deployment and CI/CD"
---

Each subgraph deploys independently. Composition validates compatibility before changes reach production.

# The Deployment Lifecycle

A typical Fusion deployment follows this lifecycle:

1. **Build container image** — Package your subgraph as a container (Docker, Azure Container Apps, etc.)
2. **Export schema** — Run `dotnet run -- schema export` to generate the `.graphqls` file
3. **Upload to Nitro** — Run `nitro fusion upload --source-schema-file schema.graphqls --tag v1.0.0` to version and store the schema
4. **Deploy container** — Deploy your container to your hosting infrastructure (Azure App Service, Kubernetes, etc.)
5. **Publish to trigger recomposition** — Run `nitro fusion publish --source-schema products-api --tag v1.0.0 --stage production` to compose all schemas and deploy to the gateway
6. **Gateway automatically downloads new config** — The gateway picks up the new configuration from Nitro via `.AddNitro()` and hot-reloads

This workflow ensures that composition validates schema compatibility before deployment, catching conflicts at build time rather than when a user hits a broken query path in production.

# Local Development Workflow

## Manual Composition

For local development without Nitro, use the Nitro CLI to compose manually:

```bash
nitro fusion compose \
  --source-schema-file ./Products/schema.graphqls \
  --source-schema-file ./Reviews/schema.graphqls \
  --archive gateway.far \
  --environment Development \
  --enable-global-object-identification
```

Then configure your gateway to use the local archive:

```csharp
builder
    .AddGraphQLGateway()
    .AddFileSystemConfiguration("./gateway.far");
```

## Aspire Integration

For orchestrated local development, .NET Aspire auto-composes schemas on build. See the [Composition](/docs/fusion/v16/composition) page for the Aspire integration snippet.

# Nitro Cloud Workflow

Nitro provides cloud-managed schema delivery with automatic gateway hot-reload.

## Upload Source Schema

After building and exporting your schema, upload it to Nitro with a version tag:

```bash
nitro fusion upload \
  --source-schema-file ./src/SourceSchemas/Products/schema.graphqls \
  --tag v1.2.3 \
  --api-id QXBpCmcwMTk5MGUzNDVlMWU3MjMyYjc2MjYxYzFiNjRkMGQzYg== \
  --api-key $NITRO_API_KEY
```

The version tag typically comes from your CI/CD pipeline (e.g., git commit SHA, semantic version, or timestamp).

## Publish to Stage

After deploying your container, trigger composition and publish to a deployment stage:

```bash
nitro fusion publish \
  --source-schema products-api \
  --tag v1.2.3 \
  --stage production \
  --api-id QXBpCmcwMTk5MGUzNDVlMWU3MjMyYjc2MjYxYzFiNjRkMGQzYg== \
  --api-key $NITRO_API_KEY
```

This command:
1. Downloads all source schemas for the specified stage
2. Composes them into a gateway configuration
3. Validates compatibility
4. Publishes the configuration to the stage
5. Notifies the gateway to reload

## Gateway Configuration with Nitro

Configure your gateway to download its configuration from Nitro:

```csharp
using HotChocolate.AspNetCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;

var builder = WebApplication.CreateBuilder(args);

// AddServiceDefaults registers shared configuration (OpenTelemetry, health checks, etc.)
// from a shared defaults project. Remove this line if your project does not use shared defaults.
builder.AddServiceDefaults("gateway-api", "1.0.0");

builder.Services
    .AddCors()
    .AddHeaderPropagation(c =>
    {
        c.Headers.Add("GraphQL-Preflight");
        c.Headers.Add("Authorization");
    });

builder.Services
    .AddHttpClient("fusion")
    .AddHeaderPropagation();

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.Authority = builder.Configuration["Auth:Authority"];
        options.Audience = builder.Configuration["Auth:Audience"];
        options.RequireHttpsMetadata = true;
    });

builder.Services.AddAuthorization();

builder
    .AddGraphQLGateway()
    .AddNitro(options =>
    {
        options.ApiId = builder.Configuration["Nitro:ApiId"];
        options.ApiKey = builder.Configuration["Nitro:ApiKey"];
        options.Stage = builder.Configuration["Nitro:Stage"];
        options.Metrics.Enabled = true;
    })
    .ModifyRequestOptions(o => o.CollectOperationPlanTelemetry = true);

var app = builder.Build();

app.UseCors(c => c.AllowAnyHeader().AllowAnyMethod().AllowAnyOrigin());
app.UseHeaderPropagation();
app.UseAuthentication();
app.UseAuthorization();
app.MapGraphQL();

app.Run();
```

The gateway downloads configuration at startup and subscribes to updates. When you publish a new version, the gateway hot-reloads without a restart.

## Stages

Nitro supports multiple deployment stages (e.g., `dev`, `staging`, `production`). Each stage has its own composed configuration. You can upload schemas to multiple stages and publish independently:

```bash
# Upload once
nitro fusion upload --source-schema-file schema.graphqls --tag v1.2.3 --api-id ... --api-key ...

# Publish to dev
nitro fusion publish --source-schema products-api --tag v1.2.3 --stage dev --api-id ... --api-key ...

# Test in dev, then promote to production
nitro fusion publish --source-schema products-api --tag v1.2.3 --stage production --api-id ... --api-key ...
```

# Schema Validation in CI

Validate schema changes before deployment to catch breaking changes early:

```bash
nitro fusion validate \
  --source-schema-file ./schema.graphqls \
  --stage production \
  --api-id ... \
  --api-key ...
```

Exit codes:
- `0`: Validation passed
- Non-zero: Validation failed (breaking change detected)

Use this in your CI pipeline to block merges or deployments that would break composition.

# Reference GitHub Actions Pipeline

This example shows a complete CI/CD workflow for deploying a subgraph to Azure Container Apps with Nitro integration.

## Per-Subgraph Workflow

Example: `deploy-products.yml`

```yaml
name: Products API Release
on:
  push:
    branches: [main]
    paths:
      - "src/SourceSchemas/Products/**"
      - "src/Defaults/**"
      - ".github/workflows/deploy-products.yml"
      - ".github/workflows/deploy-source-schema.yml"
      - "Directory.Packages.props"
  workflow_dispatch: {}

jobs:
  deploy:
    uses: ./.github/workflows/deploy-source-schema.yml
    with:
      app_name: ccc-eu1-demo-ca-products
      project_path: src/SourceSchemas/Products
      container_port: 8080
      schema_file: src/SourceSchemas/Products/schema.graphqls
      source_schema_name: products-api
    secrets: inherit
```

## Reusable Deployment Workflow

Example: `deploy-source-schema.yml` (reusable workflow)

```yaml
name: deploy-source-schema

on:
  workflow_call:
    inputs:
      app_name:
        description: Name of the App Service + repo path in ACR
        required: true
        type: string
      project_path:
        description: Path to the .csproj folder
        required: true
        type: string
      container_port:
        description: App port exposed in the container
        required: false
        type: number
        default: 8080
      schema_file:
        description: Source Schema File
        required: true
        type: string
      source_schema_name:
        description: Name of the source schema (from schema-settings.json)
        required: true
        type: string
    secrets:
      AZURE_CREDENTIALS: { required: true }
      AZURE_SUBSCRIPTION_ID: { required: true }
      AZURE_RESOURCE_GROUP: { required: true }
      APP_SERVICE_PLAN: { required: true }
      ACR_LOGIN_SERVER: { required: true }
      ACR_USERNAME: { required: true }
      ACR_PASSWORD: { required: true }
      NITRO_API_KEY: { required: true }
      NITRO_API_ID: { required: true }
      NITRO_STAGE: { required: true }

jobs:
  version:
    runs-on: ubuntu-latest
    outputs:
      tag: ${{ steps.meta.outputs.tag }}
      version: ${{ steps.meta.outputs.version }}
    steps:
      - name: Compute image tag
        id: meta
        run: |
          TS=$(date -u +'%Y%m%dT%H%M%SZ')
          SHORTSHA=${GITHUB_SHA::7}
          VERSION=${{ inputs.app_name }}-${SHORTSHA}
          echo "tag=${TS}-${SHORTSHA}" >> $GITHUB_OUTPUT
          echo "version=${VERSION}" >> $GITHUB_OUTPUT

  build:
    runs-on: ubuntu-latest
    needs: version
    steps:
      - name: Checkout
        uses: actions/checkout@v4

      - name: Set up .NET 10 SDK
        uses: actions/setup-dotnet@v4
        with:
          dotnet-version: 10.0.x

      - name: Azure login
        uses: azure/login@v2
        with:
          creds: ${{ secrets.AZURE_CREDENTIALS }}

      - name: ACR login (for dotnet publish push)
        uses: docker/login-action@v3
        with:
          registry: ${{ secrets.ACR_LOGIN_SERVER }}
          username: ${{ secrets.ACR_USERNAME }}
          password: ${{ secrets.ACR_PASSWORD }}

      - name: dotnet publish -> container (push to ACR)
        working-directory: ${{ inputs.project_path }}
        run: |
          dotnet restore
          dotnet publish -c Release \
          -p:PublishProfile=DefaultContainer \
          -p:ContainerRepository=${{ inputs.app_name }} \
          -p:ContainerImageTag=${{ needs.version.outputs.tag }} \
          -p:ContainerRegistry=${{ secrets.ACR_LOGIN_SERVER }} \
          -p:ContainerPort=${{ inputs.container_port }} \
          -p:ContainerBaseImage=mcr.microsoft.com/dotnet/aspnet:10.0

      - name: Export Source Schema
        working-directory: ${{ inputs.project_path }}
        run: dotnet run -- schema export

      - name: Upload Source Schema
        run: |
          dotnet tool exec ChilliCream.Nitro.CommandLine --prerelease --yes -- fusion upload \
          --source-schema-file "${{ inputs.schema_file }}" \
          --tag ${{ needs.version.outputs.version }} \
          --api-id ${{ secrets.NITRO_API_ID }} \
          --api-key ${{ secrets.NITRO_API_KEY }}

  deploy:
    runs-on: ubuntu-latest
    needs: [version, build]
    concurrency:
      group: deploy-${{ inputs.app_name }}
      cancel-in-progress: false
    steps:
      - name: Azure login
        uses: azure/login@v2
        with:
          creds: ${{ secrets.AZURE_CREDENTIALS }}

      - name: Create or Update App Service
        run: |
          if ! az webapp show --name ${{ inputs.app_name }} \
            --resource-group ${{ secrets.AZURE_RESOURCE_GROUP }} &> /dev/null; then
            az webapp create \
              --name ${{ inputs.app_name }} \
              --resource-group ${{ secrets.AZURE_RESOURCE_GROUP }} \
              --plan ${{ secrets.APP_SERVICE_PLAN }} \
              --deployment-container-image-name \
                ${{ secrets.ACR_LOGIN_SERVER }}/${{ inputs.app_name }}:${{ needs.version.outputs.tag }}
          fi

          az webapp config container set \
            --name ${{ inputs.app_name }} \
            --resource-group ${{ secrets.AZURE_RESOURCE_GROUP }} \
            --container-image-name \
              ${{ secrets.ACR_LOGIN_SERVER }}/${{ inputs.app_name }}:${{ needs.version.outputs.tag }} \
            --container-registry-url https://${{ secrets.ACR_LOGIN_SERVER }} \
            --container-registry-user ${{ secrets.ACR_USERNAME }} \
            --container-registry-password ${{ secrets.ACR_PASSWORD }}

          az webapp config appsettings set \
            --name ${{ inputs.app_name }} \
            --resource-group ${{ secrets.AZURE_RESOURCE_GROUP }} \
            --settings WEBSITES_PORT=${{ inputs.container_port }}

          az webapp deployment container config \
            --name ${{ inputs.app_name }} \
            --resource-group ${{ secrets.AZURE_RESOURCE_GROUP }} \
            --enable-cd true

          az webapp restart \
            --name ${{ inputs.app_name }} \
            --resource-group ${{ secrets.AZURE_RESOURCE_GROUP }}

      - name: Publish Gateway Schema
        run: |
          dotnet tool exec ChilliCream.Nitro.CommandLine --prerelease --yes -- fusion publish \
          --source-schema ${{ inputs.source_schema_name }} \
          --tag ${{ needs.version.outputs.version }} \
          --stage ${{ secrets.NITRO_STAGE }} \
          --api-id ${{ secrets.NITRO_API_ID }} \
          --api-key ${{ secrets.NITRO_API_KEY }}
```

## Workflow Explanation

### Version Job
Generates a timestamp-based tag and version string from the current git commit SHA.

### Build Job
1. **Restore and publish** — `dotnet publish` produces a container image and pushes to Azure Container Registry
2. **Export schema** — `dotnet run -- schema export` generates the `.graphqls` file
3. **Upload to Nitro** — `nitro fusion upload` versions the schema in Nitro cloud

### Deploy Job
1. **Create or update App Service** — Deploys the container image to Azure App Service
2. **Publish to Nitro** — `nitro fusion publish` triggers server-side composition and deploys the new configuration to the specified stage
3. **Gateway hot-reloads** — The gateway automatically downloads the new config from Nitro

## GitHub Action (nitro-fusion-publish-action)

For simpler pipelines, use the pre-built action:

```yaml
- name: Publish to Production
  uses: ChilliCream/nitro-fusion-publish-action@v1
  with:
    tag: ${{ github.ref_name }}
    stage: 'production'
    api-id: 'QXBpCmcwMTk5MGUzNDVlMWU3MjMyYjc2MjYxYzFiNjRkMGQzYg=='
    api-key: ${{ secrets.NITRO_API_KEY }}
    source-schema-file: './src/Products/schema.graphqls'
```

# Gateway Configuration

## Transport Options

The gateway communicates with subgraphs via HTTP. You can configure transport behavior in your `schema-settings.json`:

```json
{
  "name": "products-api",
  "transports": {
    "http": {
      "clientName": "fusion",
      "url": "{{API_URL}}"
    }
  }
}
```

### HTTP (Default)

Standard HTTP requests. The gateway makes one HTTP call per field resolution that requires a subgraph fetch.

### HTTP Batching

Multiple queries to the same subgraph are batched into a single HTTP request. Enable in the subgraph:

```csharp
builder
    .AddGraphQLServer()
    .ModifyRequestOptions(o => o.AllowBatching = true);
```

### Server-Sent Events (SSE) for Subscriptions

Real-time subscriptions over HTTP. The gateway uses SSE to stream subscription results from subgraphs.

```csharp
builder
    .AddGraphQLServer()
    .AddSubscriptionType<MySubscriptions>();
```

Gateway configuration:

```json
{
  "transports": {
    "http": {
      "clientName": "fusion",
      "url": "{{API_URL}}"
    },
    "subscriptions": {
      "transport": "sse"
    }
  }
}
```

### WebSocket

For bidirectional subscriptions, configure WebSocket transport. Subscriptions configuration will be covered in detail in future documentation.

## Defaults

By default:
- All HTTP requests use the `"fusion"` named HTTP client
- Requests are not batched unless explicitly enabled
- Subscriptions use SSE if available, otherwise HTTP polling

Change defaults by modifying your gateway's HTTP client configuration:

```csharp
builder.Services
    .AddHttpClient("fusion")
    .AddHeaderPropagation()
    .AddStandardResilienceHandler(options =>
    {
        options.AttemptTimeout.Timeout = TimeSpan.FromSeconds(10);
        options.TotalRequestTimeout.Timeout = TimeSpan.FromSeconds(30);
        options.CircuitBreaker.SamplingDuration = TimeSpan.FromSeconds(60);
    });
```

# Schema Evolution

## Progressive Field Migration with `[Override]`

Use `[Override]` to migrate a field from one subgraph to another without breaking existing queries.

**Before (Products subgraph owns `Product.reviews`):**

```csharp
// Products subgraph
[ObjectType<Product>]
public static partial class ProductNode
{
    public static async Task<IEnumerable<Review>> GetReviewsAsync(
        [Parent] Product product,
        ReviewService reviewService)
        => await reviewService.GetReviewsByProductIdAsync(product.Id);
}
```

**After (Reviews subgraph takes ownership):**

```csharp
// Reviews subgraph
[ObjectType<Product>]
public static partial class ProductNode
{
    [Override(from: "products-api")]
    public static async Task<Connection<Review>> GetReviewsAsync(
        [Parent] Product product,
        PagingArguments args,
        IReviewsByProductIdDataLoader loader,
        CancellationToken ct)
        => await loader
            .With(args)
            .LoadAsync(product.Id, ct)
            .ToConnectionAsync();
}
```

The `[Override]` attribute tells the gateway: "This field used to be resolved by the `products-api` subgraph, but now this subgraph resolves it." The gateway routes queries to the new resolver, and the old resolver is no longer called.

## Excluding Experimental Features with `[Tag]`

Mark fields or types with `[Tag]` to exclude them from composition during development:

```csharp
[Tag("experimental")]
public static async Task<Recommendation> GetRecommendationsAsync(
    [Parent] Product product,
    RecommendationService service)
    => await service.GetRecommendationsAsync(product.Id);
```

Exclude tagged fields during composition:

```bash
nitro fusion compose \
  --source-schema-file schema.graphqls \
  --exclude-tag experimental \
  --archive gateway.far
```

This lets you develop and test new features without exposing them to clients until they are ready.

## Breaking vs. Non-Breaking Changes

### Non-Breaking Changes
- Adding a new type
- Adding a new field to an existing type (nullable or with a default)
- Adding a new optional argument to a field
- Marking a field as `[Shareable]` (allowing multiple subgraphs to resolve it)

### Breaking Changes
- Removing a type or field
- Changing a field's return type
- Changing a field's arguments
- Making a nullable field non-nullable
- Removing `[Shareable]` from a field (making it exclusive to one subgraph)

Always run `nitro fusion validate` in CI before deploying to catch breaking changes.

# Nitro Touchpoint

The Nitro cloud schema registry manages schema versions, stages, and gateway hot-reload. For the full workflow, see [Nitro: Schema Delivery](https://chillicream.com/docs/nitro/apis/fusion).

# Next Steps

- **[Authentication and Authorization](/docs/fusion/v16/authentication-and-authorization)** — Secure your gateway and subgraphs
- **Monitoring and Observability** — Setting up OpenTelemetry and Nitro telemetry will be covered in future documentation
- **Error Handling and Resilience** — Handling subgraph failures gracefully will be covered in future documentation

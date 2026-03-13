---
title: "Adding a Subgraph"
---

You have an existing Fusion project with a gateway, one or more subgraphs, and a working composition pipeline. Now you need to add a new subgraph. Maybe your team owns a new domain (shipping, billing, inventory), or you are splitting an existing subgraph into smaller services. Either way, the process is the same: create a new Hot Chocolate project, define your types and any entity extensions, export the schema, compose, and verify.

This page walks you through adding a Shipping subgraph to an existing project that already has Products and Reviews subgraphs. If you have not set up a Fusion project yet, start with the [Getting Started](/docs/fusion/v16/getting-started) tutorial first.

## Prerequisites

Before you begin, you need:

- An existing Fusion project with at least one subgraph and a gateway
- The [Nitro CLI](/docs/fusion/v16/nitro-cli-reference) installed (`dotnet tool install -g ChilliCream.Nitro.CommandLine`)
- The .NET 10 SDK or later

You should be able to compose and run your existing project successfully. If composition is currently broken, fix that first.

## Subgraph Project Structure

Every Fusion subgraph follows the same layout:

```text
src/
  Shipping/
    Types/
      Product.cs              # Entity stub
      ShipmentNode.cs         # Type extension (replaces foreign key with entity reference)
      ShippingQueries.cs      # Query resolvers (lookups)
    Shipment.cs               # Domain type
    ShipmentRepository.cs     # In-memory data
    Program.cs                # Server configuration
    Shipping.csproj           # Project file
    appsettings.json          # App settings
    schema-settings.json      # Fusion subgraph settings (created on first schema export)
    schema.graphqls           # Exported schema (generated, do not edit)
```

Your folder structure may differ, but the Fusion components are always the same: a GraphQL server, your type definitions, an exported schema, and a `schema-settings.json`.

## Create the Subgraph Project

Create a new GraphQL server project from the template:

```bash
dotnet new graphql -n Shipping
```

The template creates sample files in `Shipping/Types` (`Author.cs`, `Book.cs`, `Query.cs`). Delete those files before continuing so your schema only contains your own types.

Your `Shipping/Shipping.csproj` should look like this:

```xml
<Project Sdk="Microsoft.NET.Sdk.Web">

  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
  </PropertyGroup>

  <ItemGroup Condition="'$(ImplicitUsings)' == 'enable'">
    <Using Include="Shipping" />
  </ItemGroup>

  <ItemGroup>
    <PackageReference Include="HotChocolate.AspNetCore" Version="16.0.0-p.11.36" />
    <PackageReference Include="HotChocolate.AspNetCore.CommandLine" Version="16.0.0-p.11.36" />
    <PackageReference Include="HotChocolate.Types.Analyzers" Version="16.0.0-p.11.36">
      <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
      <PrivateAssets>all</PrivateAssets>
    </PackageReference>
  </ItemGroup>

</Project>
```

### Configure the Port

The `dotnet new graphql` template already defines a default port in `launchSettings.json`. Change it so the Shipping subgraph runs on port 5003. Edit `Shipping/Properties/launchSettings.json` and set `launchUrl` and `applicationUrl` under the `http` profile to:

```json
"launchUrl": "http://localhost:5003/graphql",
"applicationUrl": "http://localhost:5003"
```

This ensures the subgraph runs on port 5003, which matches what you will configure in `schema-settings.json` later.

## Define Your Types

The Shipping subgraph owns shipment data and contributes a `shipments` field to the existing `Product` type. It does not own Product. The Products subgraph does. The Shipping subgraph extends it with shipping information.

### Define the Shipment Type

Create `Shipment.cs` in the Shipping project:

```csharp
// Shipping/Shipment.cs

namespace Shipping;

public class Shipment
{
    public int Id { get; set; }

    public int ProductId { get; set; }

    public required string TrackingNumber { get; set; }

    public required string Status { get; set; }
}
```

Each shipment has a `ProductId` that references a product from the Products subgraph.

### Add In-Memory Data

Create `ShipmentRepository.cs`:

```csharp
// Shipping/ShipmentRepository.cs

namespace Shipping;

public static class ShipmentRepository
{
    private static readonly List<Shipment> Shipments =
    [
        new Shipment { Id = 1, ProductId = 1, TrackingNumber = "SH-001", Status = "Delivered" },
        new Shipment { Id = 2, ProductId = 1, TrackingNumber = "SH-002", Status = "In Transit" },
        new Shipment { Id = 3, ProductId = 2, TrackingNumber = "SH-003", Status = "Shipped" },
    ];

    public static Shipment? GetById(int id)
        => Shipments.FirstOrDefault(s => s.Id == id);

    public static List<Shipment> GetByProductId(int productId)
        => Shipments.Where(s => s.ProductId == productId).ToList();
}
```

### Create the Entity Stub

An entity stub is a lightweight declaration that says "I know this type exists in the graph and I want to add fields to it." Create `Types/Product.cs`:

```csharp
// Shipping/Types/Product.cs

namespace Shipping;

public sealed record Product(int Id)
{
    public List<Shipment> GetShipments()
        => ShipmentRepository.GetByProductId(Id);
}
```

This is not a duplicate of the Product type from the Products subgraph. It is an entity stub. The Shipping subgraph does not define `name`, `price`, or any other Product field. It only contributes the `shipments` field. When the gateway composes the schema, it merges this stub with the full `Product` type from the Products subgraph. Clients see one `Product` type with all fields from all subgraphs.

### Add Query Resolvers

Create `Types/ShippingQueries.cs` with a public lookup for `Shipment` and an internal lookup for `Product`:

```csharp
// Shipping/Types/ShippingQueries.cs

namespace Shipping;

[QueryType]
public static partial class ShippingQueries
{
    [Lookup]
    public static Shipment? GetShipmentById(int id)
        => ShipmentRepository.GetById(id);

    [Internal, Lookup]
    public static Product? GetProductById(int id)
        => new(id);
}
```

- `GetShipmentById` is a **public lookup**. Clients can call it directly, and the gateway uses it for entity resolution.
- `GetProductById` is an **internal lookup**. It is hidden from the composite schema and exists only for the gateway to enter the Shipping subgraph's `Product` type during entity resolution. It constructs a stub from the ID without checking whether the product exists, which is safe because the gateway only calls internal lookups after another subgraph has already confirmed the entity exists.

For more on public vs. internal lookups and when to use each, see [Entities and Lookups](/docs/fusion/v16/entities-and-lookups).

### Replace Foreign Keys with Entity References

The `Shipment` type currently exposes a raw `ProductId`. To expose `shipment.product` instead, add a type extension. Create `Types/ShipmentNode.cs`:

```csharp
// Shipping/Types/ShipmentNode.cs

using HotChocolate.Types;

namespace Shipping;

[ObjectType<Shipment>]
public static partial class ShipmentNode
{
    [BindMember(nameof(Shipment.ProductId))]
    public static Product GetProduct([Parent] Shipment shipment)
        => new(shipment.ProductId);
}
```

`[BindMember(nameof(Shipment.ProductId))]` tells Hot Chocolate to replace the `productId` field on `Shipment` with the `product` field returned by this resolver. In the exported schema, clients see `shipment.product` (returning a full `Product`) instead of `shipment.productId` (a raw integer). The gateway resolves the full Product from whichever subgraph owns it.

## Configure the Server

Set `Program.cs` to:

```csharp
var builder = WebApplication.CreateBuilder(args);

builder
    .AddGraphQL("Shipping")
    .AddTypes();

var app = builder.Build();

app.MapGraphQL();
app.RunWithGraphQLCommands(args);
```

`AddGraphQL("Shipping")` sets the subgraph name used during schema export. `AddTypes()` is source-generated and registers all types discovered by the analyzer. `RunWithGraphQLCommands(args)` enables CLI commands like schema export.

## Export the Schema

From the project root, export the schema:

```bash
dotnet run --project ./Shipping -- schema export
```

This generates two files in the Shipping directory:

- **`schema.graphqls`** contains the subgraph's GraphQL schema.
- **`schema-settings.json`** contains the subgraph settings.

Because `Program.cs` uses `AddGraphQL("Shipping")`, the generated `schema-settings.json` already contains `"name": "Shipping"`. The transport URL defaults to `http://localhost:5000/graphql`, so update it to match port 5003:

```json
{
  "name": "Shipping",
  "transports": {
    "http": {
      "url": "http://localhost:5003/graphql"
    }
  }
}
```

The `name` field identifies this subgraph within the composite schema and must be unique. The `url` is where the gateway sends requests to this subgraph at runtime.

## Compose

Run composition with all subgraph schemas, including your new one:

```bash
nitro fusion compose \
  -s Products/schema.graphqls \
  -s Reviews/schema.graphqls \
  -s Shipping/schema.graphqls \
  -a gateway.far
```

If composition succeeds, copy the updated `gateway.far` to your gateway project directory:

```bash
cp gateway.far Gateway/gateway.far
```

If you already have a composed `gateway.far` with the Products and Reviews subgraphs, you can add the new subgraph to the existing archive:

```bash
nitro fusion compose \
  -s Shipping/schema.graphqls \
  -a gateway.far
```

## Test Cross-Subgraph Queries

Start all services and the gateway. With the Shipping subgraph added, you can now query shipment data that crosses subgraph boundaries:

```graphql
query {
  productById(id: 1) {
    name
    price
    shipments {
      trackingNumber
      status
    }
  }
}
```

This query touches two subgraphs:

1. The gateway calls the Products subgraph to fetch `name` and `price`.
2. The gateway uses the internal lookup to enter the Shipping subgraph and resolve `shipments` for the same product.

The client sees one unified response with fields from both subgraphs merged into a single `Product`.

You can also query in the other direction, starting from a shipment and navigating to the product:

```graphql
query {
  shipmentById(id: 1) {
    trackingNumber
    status
    product {
      name
      price
    }
  }
}
```

Here the gateway resolves the shipment from the Shipping subgraph, then uses the Products subgraph to fetch `name` and `price` for the referenced product.

## Troubleshooting Composition Errors

If composition fails after adding your new subgraph, the error messages point to specific issues.

### Duplicate field without sharing

**"Field X is defined in multiple subgraphs"**. Your new subgraph defines a field that already exists in another subgraph. Key fields (like `id`) are automatically shareable, but all other duplicated fields need `@shareable` on every definition. See [Field Ownership and Sharing](/docs/fusion/v16/field-ownership-and-sharing) for details.

### Missing lookup

**"No lookup found for entity X"**. Your subgraph references an entity type but no subgraph provides a lookup for it. Add a lookup resolver (public or internal) for that entity. See [Entities and Lookups](/docs/fusion/v16/entities-and-lookups).

### Incompatible field types

**"Incompatible field types for X"**. Two subgraphs define the same field with different types. The types must be compatible according to the composition merging rules.

## Next Steps

- **Need cross-subgraph field dependencies?** See [Data Requirements](/docs/fusion/v16/data-requirements-and-mapping) for the full range of `@require` patterns and FieldSelectionMap syntax.
- **Composition failed?** See [Composition](/docs/fusion/v16/composition) for the full merging rules, common errors, and fixes.
- **Want to understand entities more deeply?** See [Entities and Lookups](/docs/fusion/v16/entities-and-lookups) for the complete guide to entity stubs, public vs. internal lookups, and composite keys.
- **Ready to deploy?** See [Deployment and CI/CD](/docs/fusion/v16/deployment-and-ci-cd) for setting up independent subgraph deployments with the Nitro CLI.

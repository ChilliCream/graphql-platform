---
title: Spatial Data
description: Learn how to expose NetTopologySuite spatial types as GeoJSON in Hot Chocolate v16.
---

> Experimental: This feature is community-driven and not yet finalized. The core team has limited experience with spatial data and welcomes your feedback to guide next steps. While we try not to introduce breaking changes, we reserve the possibility to adjust the API in future releases.

Spatial data describes locations or shapes as objects. Many database providers support storing this type of data. APIs often use GeoJSON to send spatial data over the network.

The most common library for spatial data in .NET is [NetTopologySuite](https://github.com/NetTopologySuite/NetTopologySuite). Entity Framework supports [Spatial Data](https://docs.microsoft.com/en-gb/ef/core/modeling/spatial) and uses NetTopologySuite as its data representation.

The `HotChocolate.Spatial` package integrates NetTopologySuite into Hot Chocolate. Your resolvers can return NetTopologySuite shapes, and they are transformed into GeoJSON.

# Getting Started

Install the `HotChocolate.Spatial` package:

<PackageInstallation packageName="HotChocolate.Spatial" />

Register the spatial types on the schema builder:

```csharp
builder.Services
    .AddGraphQLServer()
    .AddSpatialTypes();
```

If you use data extensions to project data from a database, also install `HotChocolate.Data.Spatial`:

<PackageInstallation packageName="HotChocolate.Data.Spatial" />

Register the data extensions:

```csharp
builder.Services
    .AddGraphQLServer()
    .AddSpatialTypes()
    .AddFiltering()
    .AddProjections()
    .AddSpatialFiltering()
    .AddSpatialProjections();
```

All NetTopologySuite runtime types are now bound to the corresponding GeoJSON type:

```csharp
public class Pub
{
    public int Id { get; set; }

    public string Name { get; set; }

    public Point Location { get; set; }
}

public class Query
{
    public IQueryable<Pub> GetPubs(SomeDbContext someDbContext)
    {
        return someDbContext.Pubs;
    }
}
```

```sdl
type Pub {
  id: Int!
  name: String!
  location: GeoJSONPointType!
}

type Query {
  pubs: [Pub!]!
}
```

```graphql
{
  pubs {
    id
    location {
      __typename
      bbox
      coordinates
      crs
      type
    }
    name
  }
}
```

```json
{
  "data": {
    "pubs": [
      {
        "id": 1,
        "location": {
          "__typename": "GeoJSONPointType",
          "bbox": [12, 12, 12, 12],
          "coordinates": [[12, 12]],
          "crs": 4326,
          "type": "Point"
        },
        "name": "The Winchester"
      }
    ]
  }
}
```

# Spatial Types

Hot Chocolate supports GeoJSON input and output types, along with a GeoJSON scalar for generic inputs.

## Output Types

| NetTopologySuite | GraphQL                    |
| ---------------- | -------------------------- |
| Point            | GeoJSONPointType           |
| MultiPoint       | GeoJSONMultiPointType      |
| LineString       | GeoJSONLineStringType      |
| MultiLineString  | GeoJSONMultiLineStringType |
| Polygon          | GeoJSONPolygonType         |
| MultiPolygon     | GeoJSONMultiPolygonType    |
| Geometry         | GeoJSONInterface           |

All GeoJSON output types implement:

```sdl
interface GeoJSONInterface {
  "The geometry type of the GeoJson object"
  type: GeoJSONGeometryType!
  "The minimum bounding box around the geometry object"
  bbox: [Float]
  "The coordinate reference system integer identifier"
  crs: Int
}
```

## Input Types

| NetTopologySuite | GraphQL                     |
| ---------------- | --------------------------- |
| Point            | GeoJSONPointInput           |
| MultiPoint       | GeoJSONMultiPointInput      |
| LineString       | GeoJSONLineStringInput      |
| MultiLineString  | GeoJSONMultiLineStringInput |
| Polygon          | GeoJSONPolygonInput         |
| MultiPolygon     | GeoJSONMultiPolygonInput    |

## Scalar

The `Geometry` scalar accepts any geometry type as input. This is useful when a resolver expects any `Geometry` type. Use this scalar with caution, as input and output types are more expressive.

```sdl
scalar Geometry
```

# Projections

Register the spatial projection handler with `.AddSpatialProjections()`:

```csharp
services
    .AddGraphQLServer()
    .AddProjections()
    .AddSpatialTypes()
    .AddSpatialProjections()
```

The projection middleware uses this handler to project spatial data directly to the database:

```csharp
[UseProjection]
public IQueryable<Pub> GetPubs(SomeDbContext someDbContext)
{
    return someDbContext.Pubs;
}
```

# Filtering

Entity Framework supports filtering on NetTopologySuite objects. `HotChocolate.Spatial` provides handlers for filtering spatial types on `IQueryable`. Register them with `.AddSpatialFiltering()`:

```csharp
services
    .AddGraphQLServer()
    .AddFiltering()
    .AddSpatialTypes()
    .AddSpatialFiltering()
```

After registration, `UseFiltering()` infers the possible filter types for all `Geometry`-based types.

```csharp
[UseFiltering]
public IQueryable<Pub> GetPubs(SomeDbContext someDbContext)
{
    return someDbContext.Pubs;
}
```

## Distance

The `distance` filter requires an input geometry. You can optionally buffer the geometry. All comparable filter operations are available.

```graphql
{
  pubs(
    where: {
      location: {
        distance: { geometry: { type: Point, coordinates: [1, 1] }, lt: 120 }
      }
    }
  ) {
    id
    name
  }
}
```

## Contains

The `contains` filter is an implementation of `Geometry.Contains`. It requires an input geometry with an optional buffer.

```graphql
{
  counties(
    where: {
      area: { contains: { geometry: { type: Point, coordinates: [1, 1] } } }
    }
  ) {
    id
    name
  }
}
```

The negation is `ncontains`.

## Touches

The `touches` filter is an implementation of `Geometry.Touches`.

```graphql
{
  counties(
    where: {
      area: {
        touches: {
          geometry: {
            type: Polygon
            coordinates: [[1, 1], ...]
          }
        }
      }
    }
  ) {
    id
    name
  }
}
```

The negation is `ntouches`.

## Intersects

The `intersects` filter is an implementation of `Geometry.Intersects`.

```graphql
{
  roads(
    where: {
      road: {
        intersects: {
          geometry: {
            type: LineString
            coordinates: [[1, 1], ...]
          }
        }
      }
    }
  ) {
    id
    name
  }
}
```

The negation is `nintersects`.

## Overlaps

The `overlaps` filter is an implementation of `Geometry.Overlaps`. The negation is `noverlaps`.

## Within

The `within` filter is an implementation of `Geometry.Within`.

```graphql
{
  pubs(
    where: {
      location: {
        within: { geometry: { type: Point, coordinates: [1, 1] }, buffer: 200 }
      }
    }
  ) {
    id
    name
  }
}
```

The negation is `nwithin`.

# Troubleshooting

**Spatial types not appearing in the schema**
Verify that `AddSpatialTypes()` is called on the schema builder. If you are using filtering or projections, also call `AddSpatialFiltering()` or `AddSpatialProjections()`.

**"No handler found" error for spatial filter operations**
Ensure that both `AddFiltering()` and `AddSpatialFiltering()` are registered.

**Coordinate reference system mismatch**
The CRS is currently fixed. The user must know the CRS of the backend to perform spatial filtering. Re-projection features are planned for future releases.

# Next Steps

- [Filtering](/docs/hotchocolate/v16/fetching-data/filtering) for general filtering concepts
- [Projections](/docs/hotchocolate/v16/fetching-data/projections) for projection setup
- [Entity Framework integration](/docs/hotchocolate/v16/integrations/entity-framework) for EF Core setup

<!-- spell-checker:ignore ndistance -->

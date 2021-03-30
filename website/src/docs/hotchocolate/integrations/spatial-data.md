---
title: Spatial Data
---

> **Experimental** With version 11 we release support for spatial data. This feature is not totally finished
> and polished yet. Spatial types is a community-driven feature. As the core team has little experience
> with spatial data, we need you feedback and decided to release what we have. It is important for use
> to deliver you the best experience, so reach out to us if you run into issues or have ideas to improve it.
> We try not to introduce breaking changes, but we save ourself the possibility to make changes to the api
> in future releases when we find flaws in the current design.

Spatial data describes locations or shapes in form of objects. Many database providers have support
for storing this type of data. APIs often use GeoJSON to send spatial data over the network.

The most common library used for spatial data in .NET is [NetTopologySuite](https://github.com/NetTopologySuite/NetTopologySuite).
Entity Framework supports [Spatial Data](https://docs.microsoft.com/en-gb/ef/core/modeling/spatial) and uses
NetToplogySuite as its data representation.

The package `HotChocolate.Spatial` integrates NetTopologySuite into HotChocolate. With this package your resolvers
can return NetTopologySuite shapes and they will be transformed into GeoJSON.

# Getting Started

You first need to add the package reference to your project. You can do this with the `dotnet` cli:

```bash
  dotnet add package HotChocolate.Spatial
```

To make the schema recognize the spatial types you need to register them on the schema builder.

```csharp
services
    .AddGraphQLServer()
    .AddSpatialTypes();
```

All NetToplogySuite runtime types are now bound to the corresponding GeoJSON type.

```csharp
public class Pub
{
    public int Id { get; set; }

    public string Name { get; set; }

    public Point Location { get; set; }
}

public class Query
{
    // we use ef in this example
    [UseDbContext(typeof(SomeDbContext))]
    public IQueryable<Pub> GetPubs([ScopedService] SomeDbContext someDbContext)
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
      },
      {
        "id": 2,
        "location": {
          "__typename": "GeoJSONPointType",
          "bbox": [43, 534, 43, 534],
          "coordinates": [[43, 534]],
          "crs": 4326,
          "type": "Point"
        },
        "name": "Fountains Head"
      }
    ]
  }
}
```

# Spatial Types

HotChocolate supports GeoJSON input and output types. There is also a GeoJSON scalar to make generic inputs possible.

## Output Types

The following mappings are available by default:

| NetTopologySuite | GraphQL                    |
| ---------------- | -------------------------- |
| Point            | GeoJSONPointType           |
| MultiPoint       | GeoJSONMultiPointType      |
| LineString       | GeoJSONLineStringType      |
| MultiLineString  | GeoJSONMultiLineStringType |
| Polygon          | GeoJSONPolygonType         |
| MultiPolygon     | GeoJSONMultiPolygonType    |
| Geometry         | GeoJSONInterface           |

All GeoJSON output types implement the following interface.

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

A `NetTopologySuite.Gemeotry` is mapped to this interface by default.

## Input Types

For each output type there is a corresponding input type

| NetTopologySuite | GraphQL                     |
| ---------------- | --------------------------- |
| Point            | GeoJSONPointInput           |
| MultiPoint       | GeoJSONMultiPointInput      |
| LineString       | GeoJSONLineStringInput      |
| MultiLineString  | GeoJSONMultiLineStringInput |
| Polygon          | GeoJSONPolygonInput         |
| MultiPolygon     | GeoJSONMultiPolygonInput    |

## Scalar

With interfaces or unions it is possible to have multiple possible return types.
Input types do not yet have a way of defining multiple possibilities.
As an addition to output and input types there is the `Geometry` scalar, which closes this gap.
When a resolver expects any `Geometry` type as an input, you can use this scalar.
This scalar should be used with caution. Input and output types are much more expressive than a custom scalar.

```sdl
scalar Geometry
```

# Projections

To project spatial types, a special handler is needed. This handler can be registered on the schema with `.AddSpatialProjections()`

```csharp
    services
        .AddGraphQLServer()
        .AddProjections()
        .AddSpatialTypes()
        .AddSpatialProjections()
```

The projection middleware will use this handler to project the spatial data directly to the database

```csharp
[UseDbContext(typeof(SomeDbContext))]
[UseProjection]
public IQueryable<Pub> GetPubs([ScopedService] SomeDbContext someDbContext)
{
    return someDbContext.Pubs;
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

```sql
SELECT p."Id", p."Location", p."Name"
FROM "Pubs" AS p
```

# Filtering

Entity framework supports filtering on NetTopologySuite objects.
`HotChocolate.Spatial` provides handlers for filtering spatial types on `IQueryable`.
These handlers can be registered on the schema with `.AddSpatialFiltering()`

```csharp
    services
        .AddGraphQLServer()
        .AddProjections()
        .AddSpatialTypes()
        .AddSpatialFiltering()
```

After the registration of the handlers `UseFiltering()` will infer the possible filter types
for all `Geometry` based types.

```csharp
[UseDbContext(typeof(SomeDbContext))]
[UseFiltering]
public IQueryable<Pub> GetPubs([ScopedService] SomeDbContext someDbContext)
{
    return someDbContext.Pubs;
}
```

```sdl {10}
type Query {
  pubs(where: PubFilterInput): [Pub!]!
}

input PubFilterInput {
  and: [PubFilterInput!]
  or: [PubFilterInput!]
  id: ComparableInt32OperationFilterInput
  name: StringOperationFilterInput
  location: PointFilterInput
}

input PointFilterInput {
  and: [PointFilterInput!]
  or: [PointFilterInput!]
  m: ComparableDoubleOperationFilterInput
  x: ComparableDoubleOperationFilterInput
  y: ComparableDoubleOperationFilterInput
  z: ComparableDoubleOperationFilterInput
  area: ComparableDoubleOperationFilterInput
  boundary: GeometryFilterInput
  centroid: PointFilterInput
  dimension: DimensionOperationFilterInput
  envelope: GeometryFilterInput
  geometryType: StringOperationFilterInput
  interiorPoint: PointFilterInput
  isSimple: BooleanOperationFilterInput
  isValid: BooleanOperationFilterInput
  length: ComparableDoubleOperationFilterInput
  numPoints: ComparableInt32OperationFilterInput
  ogcGeometryType: OgcGeometryTypeOperationFilterInput
  pointOnSurface: PointFilterInput
  srid: ComparableInt32OperationFilterInput
  contains: GeometryContainsOperationFilterInput
  distance: GeometryDistanceOperationFilterInput
  intersects: GeometryIntersectsOperationFilterInput
  overlaps: GeometryOverlapsOperationFilterInput
  touches: GeometryTouchesOperationFilterInput
  within: GeometryWithinOperationFilterInput
  ncontains: GeometryContainsOperationFilterInput
  ndistance: GeometryDistanceOperationFilterInput
  nintersects: GeometryIntersectsOperationFilterInput
  noverlaps: GeometryOverlapsOperationFilterInput
  ntouches: GeometryTouchesOperationFilterInput
  nwithin: GeometryWithinOperationFilterInput
}
```

## Distance

The `distance` filter is an implementation of [`Geometry.Within`](http://nettopologysuite.github.io/NetTopologySuite/api/NetTopologySuite.Geometries.Geometry.html#NetTopologySuite_Geometries_Geometry_Within_NetTopologySuite_Geometries_Geometry_)

The filter requires an input geometry. You can optionally buffer this geometry with the input field buffer.
The filter also has all comparable filters.

```sdl
input GeometryDistanceOperationFilterInput {
  geometry: Geometry!
  buffer: Float
  eq: Float
  neq: Float
  in: [Float!]
  nin: [Float!]
  gt: Float
  ngt: Float
  gte: Float
  ngte: Float
  lt: Float
  nlt: Float
  lte: Float
  nlte: Float
}
```

```graphql
{
  pubs(
    where: {
      location: {
        within: { geometry: { type: Point, coordinates: [1, 1] }, lt: 120 }
      }
    }
  ) {
    id
    name
    location
  }
}
```

```sql
SELECT c."Id", c."Name", c."Area"
FROM "Counties" AS c
WHERE ST_Within(c."Area", @__p_0)
```

The negation of this operation is `nwithin`

```sql
SELECT c."Id", c."Name", c."Area"
FROM "Counties" AS c
WHERE NOT ST_Within(c."Area", @__p_0)
```

## Contains

The `contains` filter is an implementation of [`Geometry.Contains`](http://nettopologysuite.github.io/NetTopologySuite/api/NetTopologySuite.Geometries.Geometry.html#NetTopologySuite_Geometries_Geometry_Contains_NetTopologySuite_Geometries_Geometry)

The filter requires an input geometry. You can optionally buffer this geometry with the input field buffer.

```sdl
input GeometryContainsOperationFilterInput {
  geometry: Geometry!
  buffer: Float
}
```

```graphql
{
  counties(
    where: {
      area: { contains: { geometry: { type: Point, coordinates: [1, 1] } } }
    }
  ) {
    id
    name
    area
  }
}
```

```sql
SELECT c."Id", c."Name", c."Area"
FROM "Counties" AS c
WHERE ST_Contains(c."Area", @__p_0)
```

The negation of this operation is `ncontains`

```sql
SELECT c."Id", c."Name", c."Area"
FROM "Counties" AS c
WHERE NOT ST_Contains(c."Area", @__p_0)
```

## Touches

The `touches` filter is an implementation of [`Geometry.Touches`](http://nettopologysuite.github.io/NetTopologySuite/api/NetTopologySuite.Geometries.Geometry.html#NetTopologySuite_Geometries_Geometry_Touches_NetTopologySuite_Geometries_Geometry_)

The filter requires an input geometry. You can optionally buffer this geometry with the input field buffer.

```sdl
input GeometryTouchesOperationFilterInput {
  geometry: Geometry!
  buffer: Float
}
```

```graphql
{
  counties(
    where: {
      area: {
        touches: {
          geometry: {
            type: Polygon,
            coordinates: [[1, 1], ....]
          }
        }
      }
    }){
      id
      name
      area
    }
}
```

```sql
SELECT c."Id", c."Name", c."Area"
FROM "Counties" AS c
WHERE ST_Touches(c."Area", @__p_0)
```

The negation of this operation is `ntouches`

```sql
SELECT c."Id", c."Name", c."Area"
FROM "Counties" AS c
WHERE NOT ST_Touches(c."Area", @__p_0)
```

## Intersects

The `intersects` filter is an implementation of [`Geometry.Intersects`](http://nettopologysuite.github.io/NetTopologySuite/api/NetTopologySuite.Geometries.Geometry.html#NetTopologySuite_Geometries_Geometry_Intersects_NetTopologySuite_Geometries_Geometry_)

The filter requires an input geometry. You can optionally buffer this geometry with the input field buffer.

```sdl
input GeometryIntersectsOperationFilterInput {
  geometry: Geometry!
  buffer: Float
}
```

```graphql
{
  roads(
    where: {
      road: {
        intersects: {
          geometry: {
            type: LineString,
            coordinates: [[1, 1], ....]
          }
        }
      }
    }){
      id
      name
      road
    }
}
```

```sql
SELECT r."Id", r."Name", r."Road"
FROM "Roads" AS r
WHERE ST_Intersects(r."Road", @__p_0)
```

The negation of this operation is `nintersects`

```sql
SELECT r."Id", r."Name", r."Road"
FROM "Roads" AS r
WHERE NOT ST_Intersects(r."Road", @__p_0)
```

## Overlaps

The `overlaps` filter is an implementation of [`Geometry.Overlaps`](http://nettopologysuite.github.io/NetTopologySuite/api/NetTopologySuite.Geometries.Geometry.html#NetTopologySuite_Geometries_Geometry_Overlaps_NetTopologySuite_Geometries_Geometry_)

```sdl
input GeometryOverlapsOperationFilterInput {
  geometry: Geometry!
  buffer: Float
}
```

```graphql
{
  county(
    where: {
      area: {
        overlaps: {
          geometry: {
            type: Polygon,
            coordinates: [[1, 1], ....]
          }
        }
      }
    }){
      id
      name
      area
    }
}
```

```sql
SELECT c."Id", c."Name", c."Area"
FROM "Counties" AS c
WHERE ST_Overlaps(c."Area", @__p_0)
```

The negation of this operation is `noverlaps`

```sql
SELECT c."Id", c."Name", c."Area"
FROM "Counties" AS c
WHERE NOT ST_Overlaps(c."Area", @__p_0)
```

## Within

The `within` filter is an implementation of [`Geometry.Within`](http://nettopologysuite.github.io/NetTopologySuite/api/NetTopologySuite.Geometries.Geometry.html#NetTopologySuite_Geometries_Geometry_Within_NetTopologySuite_Geometries_Geometry_)

```sdl
input GeometryWithinOperationFilterInput {
  geometry: Geometry!
  buffer: Float
}
```

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
    location
  }
}
```

```sql
SELECT c."Id", c."Name", c."Area"
FROM "Counties" AS c
WHERE ST_Within(c."Area", @__p_0)
```

The negation of this operation is `nwithin`

```sql
SELECT c."Id", c."Name", c."Area"
FROM "Counties" AS c
WHERE NOT ST_Within(c."Area", @__p_0)
```

# What's next?

In upcoming releases spatial data will get reprojection features and sorting capabilities.

## Reprojection

At the moment the coordinate reference system (crs) is fixed. The user has to know the crs of the backend
to do spatial filtering. The API will furthermore always return the data in the crs it was stored in the database.

We want to improve this. The user should be able to send data to the backend without knowing what the crs. The
backend should reproject the incoming data automatically to the correct crs.

Additionally we want to provide a way for users, to specify in what CRS they want to receive the data.

## Sorting

Currently we only support filtering for spatial data. We also want to provide a way for users to sort results.
This can e.g. be used to find the nearest result for a given point.

---
title: Spatial Data
---

Spatial data describes locations or shapes in form of objects. More and more database providers have support
for storing this type of data. APIs often use GeoJSON to send spatial data over the network. 

The most common library used for spatial data in .NET is [NetTopologySuite](https://github.com/NetTopologySuite/NetTopologySuite).
Entity Framework supports [Spatial Data](https://docs.microsoft.com/en-gb/ef/core/modeling/spatial) and uses 
NetToplogySuite as its data representation. 

The package `HotChocolate.Spatial` integrates NetTopologySuite into HotChocolate. With this package your resolvers
can return NetTopologySuite shapes and they will be transformed into GeoJSON.

# Getting Started
You first need to add the package reference to you project. You can do this with the `dotnet` cli:

```
  dotnet add package HotChocolate.Spatial
```

To make the schema aware of the spatial types you need to register them on the schema builder.

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
          "bbox": [
            12,
            12,
            12,
            12
          ],
          "coordinates": [
            [
              12,
              12
            ]
          ],
          "crs": 4326,
          "type": "Point"
        },
        "name": "The Winchester"
      },
      {
        "id": 2,
        "location": {
          "__typename": "GeoJSONPointType",
          "bbox": [
            43,
            534,
            43,
            534
          ],
          "coordinates": [
            [
              43,
              534
            ]
          ],
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

| NetTopologySuite | GraphQL                    |
| ---------------- | -------------------------- |
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
```sdl{10}
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
}
```

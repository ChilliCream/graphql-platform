---
title: Integrations
description: Learn how to integrate Hot Chocolate v16 with Entity Framework Core, MongoDB, Spatial Data, and Marten.
---

This section covers how to integrate different data technologies into your Hot Chocolate GraphQL server.

# Entity Framework Core

Entity Framework Core requires additional setup to work with the concurrent nature of GraphQL resolvers. You will learn how to use your `DbContext` correctly in different scenarios and tune Entity Framework Core for maximum throughput.

[Learn more about the Entity Framework Core integration](/docs/hotchocolate/v16/integrations/entity-framework)

# MongoDB

You will learn how to access MongoDB from your resolvers and how to translate pagination, projection, filtering, and sorting capabilities to native MongoDB queries.

[Learn more about the MongoDB integration](/docs/hotchocolate/v16/integrations/mongodb)

# Spatial Data

You will learn how to expose [NetTopologySuite types](https://github.com/NetTopologySuite/NetTopologySuite) as [GeoJSON](https://geojson.org/) and how to integrate them with the data APIs.

[Learn more about the Spatial Data integration](/docs/hotchocolate/v16/integrations/spatial-data)

# Marten

Marten requires custom configurations to work with the `HotChocolate.Data` package. You will learn how to configure your schema to integrate with Marten.

[Learn more about the Marten integration](/docs/hotchocolate/v16/integrations/marten)

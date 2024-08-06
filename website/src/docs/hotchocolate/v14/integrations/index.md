---
title: "Integrations"
---

In this section we will look at different technologies and how you can integrate them into your GraphQL server.

# Entity Framework Core

Using Entity Framework Core requires some additional setup to play nicely with the concurrent nature of GraphQL resolvers. You will learn how to correctly use your `DbContext` in different scenarios and also how to tune Entity Framework Core for maximum throughput of your GraphQL server.

[Learn more about our Entity Framework Core integration](/docs/hotchocolate/v14/integrations/entity-framework)

# MongoDB

You will learn how to access MongoDB from within your resolvers and how to translate our pagination, projection, filtering and sorting capabilities to native MongoDB queries.

[Learn more about our MongoDB integration](/docs/hotchocolate/v14/integrations/mongodb)

# Spatial Data

You will learn how you can expose [NetTopologySuite types](https://github.com/NetTopologySuite/NetTopologySuite) in form of [GeoJSON](https://geojson.org/) and how to integrate it with our data APIs.

[Learn more about our Spatial Data integration](/docs/hotchocolate/v14/integrations/spatial-data)

# Marten

Marten requires some custom configurations to work well with the `HotChocolate.Data` package. You will learn how to configure your schema to integrate seamlessly with Marten.

[Learn more about our Marten integration](/docs/hotchocolate/v14/integrations/marten)

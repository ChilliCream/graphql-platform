---
title: Overview
---

In this section we will learn everything about fetching data with Hot Chocolate.

# Resolvers

Resolvers are the main building blocks when it comes to fetching data. Every field in our GraphQL schema is backed by such a resolver function, responsible for returning the field's value. Since a resolver is just a function, we can use it to retrieve data from a database, a REST service, or any other data source as needed.

[Learn more about resolvers](/docs/hotchocolate/v15/fetching-data/resolvers)

Even though we can connect Hot Chocolate to any data source, most of the time it will be either a database or a REST service.

[Learn how to fetch data from a database](/docs/hotchocolate/v15/fetching-data/fetching-from-databases)

[Learn how to fetch data from a REST service](/docs/hotchocolate/v15/fetching-data/fetching-from-rest)

# DataLoader

DataLoaders provide a way to deduplicate and batch requests to data sources. They can significantly improve the performance of our queries and ease the load on our data sources.

[Learn more about DataLoaders](/docs/hotchocolate/v15/fetching-data/dataloader)

# Pagination

Hot Chocolate provides pagination capabilities out of the box. They allow us to expose pagination in a standardized way and can even take care of crafting the necessary pagination queries to our databases.

[Learn more about pagination](/docs/hotchocolate/v15/fetching-data/pagination)

# Filtering

When returning a list of entities, we often need to filter them using operations like `equals`, `contains`, `startsWith`, etc. Hot Chocolate takes away a lot of the boilerplate, by handling the generation of necessary input types and even translating the applied filters into native database queries.

[Learn more about filtering](/docs/hotchocolate/v15/fetching-data/filtering)

# Sorting

Similar to filtering, Hot Chocolate can also autogenerate input types related to sorting. They allow us to specify by which fields and in which direction our entities should be sorted. These can also be translated into native database queries automatically.

[Learn more about sorting](/docs/hotchocolate/v15/fetching-data/sorting)

# Projections

Projections allow Hot Chocolate to transform an incoming GraphQL query with a sub-selection of fields into an optimized database operation.

For example, if the client only requests the `name` and `id` of a user in their GraphQL query, Hot Chocolate will only query the database for those two columns.

[Learn more about projections](/docs/hotchocolate/v15/fetching-data/projections)

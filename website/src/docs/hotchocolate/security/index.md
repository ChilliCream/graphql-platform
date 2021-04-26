---
title: "Overview"
---

When we think about API security, we, in many cases, only think about authorization. With GraphQL, we need to think further about security concepts to ensure that our GraphQL API performs predictably and malicious actors do not bring our service down or degrade performance for others. In this document, we will give you an overview of the various approaches.

# Authorization

Authorization is one of the most basic security concepts. With authorization, you can limit what data people can fetch. Hot Chocolate integrates with the ASP.NET Core authorization policies, which can be applied to fields.

Read more on how to use authorization with Hot Chocolate [here](authorization).

# Introspection

The introspection is one of the GraphQL core features and powers many of the GraphQL IDEs and tools. But introspection also can produce large results, which can degrade the server performance. Apart from the performance aspect, you are also exposing the exact structure of your graph to anyone. In some cases, we might want to limit the access to introspection. Hot Chocolate allows you to control who can access introspection fields by using query validation rules.

Read more on controlling access to the GraphQL introspection with Hot Chocolate [here](introspection).

# Pagination

Pagination is another topic we often forget when thinking about securing our GraphQL API. Hot Chocolate, by default, will apply strict defaults so that APIs will only allow a certain amount of nodes per connection. While we set defaults, they might not be the right ones for your environment and might yield too much load.

Read more on paging with Hot Chocolate [here]().

# Execution Timeout

By default, Hot Chocolate has an internal execution timeout of 30 seconds. This is to ensure that requests do not occupy server resources for an extended amount of time. Make sure that the execution options are correctly covering your use case.

Read more on Hot Chocolate execution options [here]().

# Query Depth

With GraphQL, we give the consumer of our API the ability to drill into our data graph arbitrarily. The user can pick and choose what data he or she needs. This is one of the powerful concepts with GraphQL. It also is one of its vulnerabilities. We need to control how deep a user can drill into our data graph to ensure that requests perform consistently.

Read more on query depth validation rules [here](query-depth).

# Operation Complexity

With technologies like REST, it was easy to scale servers and measure the impact of a single request on our server infrastructure. With GraphQL, we need to do a bit more to enforce that requests have a consistent impact on our servers. Hot Chocolate can track the cost of fields and deny the execution of requests that exceed the allowed impact on our system.

Read more about the operation complexity analyzer [here](operation-complexity).

# Persisted Queries

Depending on your setup and requirements, the simplest way to make your server secure and control the request impact is to use persisted queries. With this approach, you can export the request from your client applications at development time and only allow the set of known queries to be executed in your production environment.

Read more on persisted queries [here](../performance/persisted-queries).

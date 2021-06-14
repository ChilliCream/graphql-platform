---
title: Authentication
---

GraphQL as defined by the spec does not specify how a user has to authenticate against a schema in order to execute queries. GraphQL does not even specify how requests are sent to the server using HTTP or any other protocol. _Facebook_ specified GraphQL as transport agnostic, meaning GraphQL focuses on one specific problem domain and does not try to solve other problems like how the transport might work, how authentication might work or how a schema implements authorization. These subjects are considered out of scope.

If we are accessing GraphQL servers through HTTP then authenticating against a GraphQL server can be done in various ways and Hot Chocolate does not prescribe any particular.

We basically can do it in any way ASP.NET core allows us to.

[Overview of ASP.NET Core authentication](https://docs.microsoft.com/en-us/aspnet/core/security/authentication/?view=aspnetcore-3.1)

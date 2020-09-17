---
title: Introduction
---

**Green Donut** is a port of _facebook's_ _DataLoader_ utility, written in C#
for _.NET Core_ and _.NET Framework_.

> DataLoader is a generic utility to be used as part of your application's data
> fetching layer to provide a consistent API over various backends and reduce
> requests to those backends via batching and caching. -- facebook

_DataLoader_ are perfect in various client-side and server-side scenarios.
Although, they are usually known for solving the `N+1` problem in _GraphQL_

_APIs_. _DataLoader_ decouple any kind of request in a simplified way to a
backend resource like a database or a web service to reduce the overall traffic
to those resources by using two common techniques in computer science namely
batching and caching. With batching we decrease the amount of requests to a
backend resource by grouping single requests into one batch request. Whereas
with caching we avoid requesting a backend resource at all.

On the next page we will see how to install Green Donut via _NuGet_.

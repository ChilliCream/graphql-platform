---
path: "/blog/2019/05/08/performance"
date: "2019-05-08"
title: "GraphQL - Hot Chocolate 9.0.0 - Performance Improvements"
tags: ["hotchocolate", "graphql", "dotnet", "aspnetcore"]
author: Michael Staib
authorUrl: https://github.com/michaelstaib
authorImageUrl: https://avatars1.githubusercontent.com/u/9714350?s=100&v=4
---

Today we release preview 27 of version 9 and we are heading toward RC status which we are planning to hit next week.

This post will describe what we have done since preview 9 and where we are heading.

One of the main focuses on the second part of this release was performance. Performance will stay one big focus point for us going forward. This means that every new release should be faster then the previous.

Lets have a look at what we did with version 9 and what we are planing to do in this area in the next releases.

## Parser

The version 8 parser that we have built and maintained since version 1 was a very close port of the nodejs parser of `graphql-js`. `graphql-js` is the reference implementation of _GraphQL_ and also is basically the core of _Apollo_ and _relayjs_.

The problem that we had with the approach of the parser was that it parsed a string. Basically, the parser tokenized the string which meant that there was a lot of substrings creating new strings and so on.

Each time we use the V8 parser to parse a _GraphQL_ request we basically created a lot of objects. Instead of just producing our parsed _GraphQL_ document we have created a lot of garbage for the runtime to clean up.

With version 9 we wrote the parser from scratch to be allocation free. This means that we only use memory to create our GraphQL document tree but not for the actual parsing.

In order to do that we are no longer parsing using a string but a `ReadOnlySpan<byte>`. With spans on byte we can basically read the query from a binary stream and produce the GraphQL document without producing string objects. Also, the span allows us to slice the incoming data and create new windows on the underlying memory. So, each time we slice the data, we no longer create new string objects that the GC has to get rid of. All of the GraphQL keywords in a GraphQL document that is being parsed are never transformed to a string representation, but will only be represented to the parser as one byte on which the parser makes a decision on what the parsed token means. Also, comments and descriptions will only become strings if they are consumed saving us from unescaping those and more. On a production GraphQL server we do not have the need to consume comment tokens for instance, so we can just skip over them.

Furthermore, unescaping strings is now much more efficient since we create the string representation just once, all the escape logic is done on the span. We still have to get a second array on which we insert the escaped data but this second byte array can be rented if to large or in the best of cases be allocated on the stack with stackaloc.

But again the parser will only escape a string sequence and create an actual string object if needed.

Moreover, our new parser is now a `ref struct` meaning that all the memory we allocate for the parser state is allocated on the stack.

We still will keep our old parser around and will update both parsers going forward.

But we did not stop here. Actually, the GraphQL HTTP request is really bad to be processed efficiently. So, with version 9 we are actually still parsing from a string with our new parser.

**GraphQL Request Example**:

```json
{
  "query": "...",
  "operationName": "...",
  "variables": { "myVariable": "someValue", ... }
}
```

The issue here is that we first have to parse the server request which is JSON and then can use the GraphQL query stored as string in the JSON structure to parse the actual GraphQL query document.

This means that with version 9 we are around 2 to 3 times faster than any .Net parser implementation.

But as I said we are **NOT** stopping here, we are working on a new specialized request parser that will integrate the JSON parser with the GraphQL parser. That means that we are able to read the GraphQL request directly from the network stream and parse it without any manifestation to a string object.

Version 9 will bring the new `Utf8GraphQLParser` and we will follow that up with the `Utf8GraphQLRequestParser` in version 9.1.

In our experiments we see that this new request parser is about 10 times faster then the GraphQL-DotNet parser combined with Json.Net.

Also, as a side note the version 9 parser now supports all the GraphQL draft features and represents the most GraphQL spec compliant implementation on the .Net platform.

## Resolver Compiler

With version 9 we have removed the Roslyn compiler and are now using the expression compiler to compile our resolvers. This change was done since Roslyn caused the server to consume a lot of memory. Most of the memory was consumed by native metadata references and we were not able to solve that memory consumption issue. At Microsoft Build I talked to David Fowler about that and he knew about the issue and recommended that we move to expressions. The downside here is that the resolvers produced by the expression compiler are actually a little bit slower than resolvers compiled with roslyn. This has many reasons I do not want to go in here.

With version 9.1 we will further optimize the resolver compilation by allowing lazy compilation, this will improve startup performance and memory usage.

## Execution Engine

We have updated our execution engine to use less memory and execute faster. The new execution engine is at least 2.3 times faster and uses half of the memory GraphQL-DotNet does to execute a query. If you are using schema first we are actually seeing 8.9 times faster executon of queries with Hot Chocolate compared to GraphQL-DotNet.

GraphQL-DotNet is still faster when validating queries, but this is offset since we are caching validation results. Validation will be one of the things we will work on for version 9.1. So, expect improvements here.

Also, we are putting a lot of work in our new execution plan feature. With execution plans we are seeing 3 times faster query executions compared to the current Hot Chocolate version 9 preview bits.

The execution plan feature allows us to pre-analyze the query graph and in many cases optimize the execution of resolvers significantly. We will talk about this in more detail after we have shipped version 9.

## Serialization

The serialization of query results is one of the areas we want to improve. Microsoft did a lot of work in this area and we are waiting here for the new UTF8 APIs that will ship with .Net Core 3. We are completely removing Json.Net over the next releases in order to improve performance further.

## Summary

We are investing heavily in performance and stability and see perfomance as feature. One other area we are working on is the subscription implementation. We will replace the current implementation with one built on top of the Microsoft pipeline API, this is why we are moving again subscription stitching to the next version.

Stitching is also one area we will start to improve performance-wise once we have the execution plan feature implemented.

The bottom line here is that if you go with Hot Chocolate you will get the most spec compliant and most performant GraphQL server on the .Net platform.

Each time a GraphQL spec element hits draft status we will go ahead and implement it with Hot Chocolate, this means that with Hot Chocolate you will always get the latest GraphQL features.

Also, we are working to have all the benchmarkings ready with GraphQL-Bench. This will make it more transparant what we are testing and will let us more easily assess where we are heading performance wise.

[hot chocolate]: https://hotchocolate.io
[hot chocolate source code]: https://github.com/ChilliCream/hotchocolate

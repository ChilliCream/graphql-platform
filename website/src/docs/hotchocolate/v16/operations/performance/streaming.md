---
title: Streaming and Incremental Delivery Performance
---

Use Hot Chocolate v16 incremental delivery when your query needs to deliver fast, critical data alongside slower, secondary data. The purpose is not to reduce the total work, but to let the client display useful information as soon as possible, even before all resolvers finish.

This page explains how to optimize operations performance for finite query responses using `@defer` and `@stream`. It does not cover Fusion streaming. For full directive syntax and build instructions, see [stream results with defer and stream](/docs/hotchocolate/v16/build/realtime/stream-results-with-defer-and-stream). For SSE setup, see [configure SSE](/docs/hotchocolate/v16/build/transport/configure-sse).

# Demonstrate Streaming Changes in the User Timeline

To verify streaming works, start with a control request that prints each chunk as it arrives. The following example enables the necessary directives, sends a multipart request, and expects an initial payload followed by a later patch.

```csharp
// Program.cs
var builder = WebApplication.CreateBuilder(args);

builder
    .AddGraphQL()
    .AddQueryType<Query>()
    .ModifyOptions(options =>
    {
        options.EnableDefer = true;
        options.EnableStream = true;
    });

var app = builder.Build();

app.MapGraphQL();
app.Run();
```

Apply `@defer` to an inline fragment or fragment spread. Place the fields needed for the first render outside the deferred fragment.

```graphql
query ProductPage($id: Int!) {
  product(id: $id) {
    id
    name
    ...ProductDetails @defer(label: "details")
  }
}

fragment ProductDetails on Product {
  description
  recommendations {
    id
    name
  }
}
```

Send a request for the v16 incremental delivery format. Use `curl -N` to disable output buffering so you can see chunks as they arrive:

```bash
curl -N http://localhost:5000/graphql \
  -H 'Content-Type: application/json' \
  -H 'Accept: multipart/mixed; incrementalSpec=v0.2' \
  --data '{"query":"query ProductPage($id: Int!){ product(id:$id){ id name ...ProductDetails @defer(label:\"details\") } } fragment ProductDetails on Product { description recommendations { id name } }","variables":{"id":1}}'
```

The server chooses a boundary value for multipart responses, so the exact boundary may vary. However, the payload should look similar to this:

```text
---
Content-Type: application/json; charset=utf-8

{"data":{"product":{"id":1,"name":"Chai"}},"pending":[{"id":"2","path":["product"]}],"hasNext":true}
---
Content-Type: application/json; charset=utf-8

{"incremental":[{"id":"2","data":{"description":"Black tea","recommendations":[{"id":2,"name":"Coffee"}]}}],"completed":[{"id":"2"}],"hasNext":false}
-----
```

Focus on three key moments:

1. When the request starts.
2. When the first useful payload arrives (enough to render the page shell).
3. When the complete payload arrives (the final chunk where `hasNext` is `false`).

If these moments are not distinct, streaming is not providing a performance benefit yet. In that case, review the operation, transport, and infrastructure as described below.

# Decide if Incremental Delivery Is the Right Fit

The `@defer` and `@stream` directives keep a single GraphQL operation open and return a finite stream of responses. They are most effective when the initial selection can complete without waiting for slower fields.

These directives do not reduce database load, resolver CPU usage, or response size on their own. They may increase connection lifetimes, the number of concurrent open responses, client parser complexity, and sensitivity to proxy behavior.

| Scenario                                                            | Use                            | Reason                                                                                       |
| ------------------------------------------------------------------- | ------------------------------ | -------------------------------------------------------------------------------------------- |
| Product header is fast, but details load slowly                     | `@defer`                       | The page can show identity, title, and layout before optional panels finish loading.         |
| You need a bounded recommendations list, and early items are useful | `@stream`                      | The operation still fetches the full list, but the client can render the first items sooner. |
| New chat messages should arrive after the initial page load         | Subscription                   | Subscriptions are designed for future server events.                                         |
| Catalog or feed may have thousands of items                         | Pagination                     | Users fetch the next slice as needed.                                                        |
| Several independent operations can run in parallel                  | Batching or client concurrency | Incremental delivery does not replace independent requests.                                  |
| Route navigation waits for JavaScript before starting data fetches  | Route or intent preloading     | Start the request earlier to avoid fetch-on-render waterfalls.                               |

# Prerequisites for Reliable Streaming

Before you optimize an operation for incremental delivery, make sure all of the following are true:

- The ASP.NET Core GraphQL endpoint is mapped using `app.MapGraphQL()`.
- `EnableDefer` is set to `true` before using `@defer`.
- `EnableStream` is set to `true` before using `@stream`.
- The client sends a streaming `Accept` header (not `Accept: application/json`).
- The client parser can handle and merge multiple payloads in the v16 v0.2 format.
- Proxies, CDNs, compression, and hosting layers do not buffer or block flushed chunks.
- Critical fields are outside deferred fragments or streamed list tails.
- Resolvers and downstream APIs accept and honor `CancellationToken`.
- Cost limits, page sizes, and request timeouts are set for your slowest allowed operation.

# Shape Your Operation for a Useful First Chunk

Design your query around UI regions. Place the fields needed for a stable initial render in the first payload, and defer optional regions for later patches.

```graphql
query ProductPage($id: Int!, $deferPanels: Boolean! = true) {
  product(id: $id) {
    # The header and layout render from the first payload.
    id
    name
    availability
    price

    ...ProductDetails @defer(label: "details", if: $deferPanels)
    ...ProductReviews @defer(label: "reviews", if: $deferPanels)
    ...ProductRecommendations @defer(label: "recommendations", if: $deferPanels)
  }
}

fragment ProductDetails on Product {
  description
  specifications {
    name
    value
  }
}

fragment ProductReviews on Product {
  reviews(first: 5) {
    nodes {
      id
      rating
      body
    }
  }
}

fragment ProductRecommendations on Product {
  recommendations {
    id
    name
  }
}
```

Choose labels that match UI regions, like `details`, `reviews`, and `recommendations`. Avoid splitting into many small deferred fragments unless you have measured a benefit. Each patch adds client merge work and increases the number of UI states to handle.

Use the `if:` argument to support clients that cannot process patches. For those clients, set it to `false` and return a standard complete result.

# Use `@stream` for Bounded List Streaming Only

Apply `@stream` to list fields. It accepts `label`, `initialCount`, and `if` arguments.

```graphql
query ProductPage($id: Int!) {
  product(id: $id) {
    id
    recommendations @stream(label: "recommendations", initialCount: 3) {
      id
      name
    }
  }
}
```

Use `@stream` only when all of the following are true:

- The list is bounded.
- The operation already needs the full logical list.
- The first `initialCount` items are useful even before the rest arrive.
- The client can merge list patches for the chosen response format.

Do not use `@stream` for unbounded feeds, infinite scroll, or lists where users may never need later items. Use [pagination](/docs/hotchocolate/v16/resolvers-and-data/pagination) for those scenarios.

The initial payload includes up to `initialCount` items. Later patches deliver the remaining items. Always test your schema and client to confirm the list patch shape works as expected.

# Choose a Response Format Your Infrastructure Can Stream

Hot Chocolate determines the response format based on the HTTP `Accept` header.

| Request header                                    | When to use                                                                   | What to check for                                              |
| ------------------------------------------------- | ----------------------------------------------------------------------------- | -------------------------------------------------------------- |
| `Accept: multipart/mixed; incrementalSpec=v0.2`   | For a standard format for incremental queries                                 | Response `Content-Type` starts with `multipart/mixed`.         |
| `Accept: text/event-stream; incrementalSpec=v0.2` | For Server-Sent Events (SSE) frames for incremental delivery or subscriptions | Response contains `event: next` and a final `event: complete`. |
| `Accept: application/jsonl; incrementalSpec=v0.2` | If your client parses one JSON result per line                                | Response `Content-Type` is `application/jsonl`.                |
| `Accept: application/json`                        | For legacy clients only                                                       | Do not use for incremental query streaming.                    |

If the client sends no `Accept` header or uses `*/*`, single results use `application/graphql-response+json`. Streaming operations default to `multipart/mixed` unless the client requests another streaming format.

In v16, v0.2 is the default incremental delivery wire format, using `pending`, `incremental`, and `completed`. Legacy clients can request `incrementalSpec=v0.1`, but new clients should use v0.2.

To test SSE, request only `text/event-stream`. If you send mixed headers, the server negotiates the format based on its content negotiation rules.

# Configure Clients to Process Patches Without Buffering

Clients must process multiple payloads for a single operation and merge patches according to the selected incremental delivery format. Support depends on the client library and transport, so always verify the versions in your application.

For example, a custom Relay network can parse multipart responses using `meros`. The following sketch highlights the key transport logic:

```ts
import { meros } from "meros/browser";
import { Observable } from "relay-runtime";
import type { FetchFunction, GraphQLResponse } from "relay-runtime";

type MultipartPart = {
  json: boolean;
  body: GraphQLResponse;
};

function isAsyncIterable<T>(value: unknown): value is AsyncIterable<T> {
  return (
    typeof value === "object" && value !== null && Symbol.asyncIterator in value
  );
}

export const fetchGraphQL: FetchFunction = (operation, variables) => {
  return Observable.create<GraphQLResponse>((sink) => {
    const controller = new AbortController();
    const abortSignal = controller.signal;

    fetch("/graphql", {
      method: "POST",
      headers: {
        Accept: "multipart/mixed; incrementalSpec=v0.2",
        "Content-Type": "application/json",
      },
      body: JSON.stringify({
        query: operation.text,
        variables,
      }),
      signal: abortSignal,
    })
      .then(async (response) => {
        const parts = await meros<GraphQLResponse>(response);

        if (isAsyncIterable<MultipartPart>(parts)) {
          for await (const part of parts) {
            if (!part.json) {
              sink.error(new Error("Expected a JSON multipart section."));
              return;
            }

            sink.next(part.body);
          }
        } else {
          sink.next((await parts.json()) as GraphQLResponse);
        }

        sink.complete();
      })
      .catch((error: Error) => sink.error(error));

    return () => controller.abort();
  });
};
```

In production, also handle non-2xx responses, parse failures, non-JSON parts, network errors, completion, retries, and aborts. Make sure to propagate route-change abort signals so abandoned UI work cancels the HTTP request.

For SSE with POST bodies or custom headers, use a GraphQL-over-SSE capable client. The browser `EventSource` API only supports GET requests and cannot send custom bodies or headers.

# Write Resolvers That Enable Early Delivery

Incremental delivery is only effective if the root resolver does not load the entire object graph before child fields execute. Return the parent object as soon as the initial selection is ready, and place slower, optional work behind child resolvers selected by deferred fragments.

```csharp
public sealed class Query
{
    public async Task<Product> GetProductAsync(
        int id,
        ProductRepository productRepository,
        CancellationToken cancellationToken)
        => await productRepository.GetByIdAsync(id, cancellationToken);
}

[ObjectType<Product>]
public static partial class ProductNode
{
    public static async Task<IReadOnlyList<Review>> GetReviewsAsync(
        [Parent] Product product,
        IReviewsByProductIdDataLoader reviewsByProductId,
        CancellationToken cancellationToken)
        => await reviewsByProductId.LoadAsync(product.Id, cancellationToken) ?? [];
}
```

For streamed operations, follow these guidelines:

- Avoid blocking on asynchronous work with `.Result`, `.Wait()`, or synchronous I/O.
- Do not materialize large lists on hot paths before the deferred boundary.
- Use DataLoader in deferred branches to prevent N+1 queries in later patches.
- Pass `CancellationToken` into all database, HTTP, and queue APIs.
- Keep authorization checks needed for the first render outside slow deferred work.

# Protect Resources During Long-Lived Responses

Incremental responses keep HTTP requests open longer than standard JSON responses. Track these separately from normal query throughput.

Set request timeouts based on the longest streamed operation you want to support:

```csharp
// Program.cs
builder
    .AddGraphQL()
    .ModifyRequestOptions(options =>
    {
        options.ExecutionTimeout = TimeSpan.FromSeconds(30);
    });
```

Apply cost and size controls as you would for any expensive query. A streamed query can still be costly, even if the first chunk arrives quickly.

```csharp
// Program.cs
builder
    .AddGraphQL()
    .ModifyCostOptions(options =>
    {
        options.MaxFieldCost = 5_000;
        options.MaxTypeCost = 5_000;
        options.EnforceCostLimits = true;
    });
```

Operational checklist:

- Limit the number of concurrent streamed requests at the gateway or application layer.
- Set request timeouts that protect the server but do not cut off valid operations.
- Use maximum page sizes and list-size policies for fields that can return many items.
- Apply cost analysis for complex operations.
- Test cancellation by disconnecting a client during delayed resolver work.
- Monitor slow clients and buffering proxies, which can hold server resources after resolver work completes.

Subscription topic buffers are for real-time subscriptions, not for finite incremental query responses. Tune subscription providers as described on the [subscriptions](/docs/hotchocolate/v16/building-a-schema/subscriptions) page.

# Measure First Useful Data, Not Just Total Duration

A successful streaming change may not reduce total completion time and can even add a small overhead. The key metric is whether the first useful UI state arrives sooner.

| Metric                    | How to collect                                                                     | Success signal                                      | Failure signal                                               |
| ------------------------- | ---------------------------------------------------------------------------------- | --------------------------------------------------- | ------------------------------------------------------------ |
| Time to first byte        | Browser network tools, reverse proxy logs, or `curl -w`                            | Headers and first bytes arrive early.               | Headers wait for all resolver work.                          |
| First useful payload time | Client logs with timestamps for each received patch                                | The shell can render before optional panels finish. | The first patch lacks enough data to render.                 |
| Last payload time         | Client logs or server response completion logs                                     | Total time stays within your SLO.                   | Streaming hides a slow operation that still violates limits. |
| Chunk gaps                | Timestamp each `next` payload in the client                                        | Gaps match slow optional work.                      | Chunks arrive together after buffering.                      |
| Response bytes            | Browser tools, proxy logs, or server metrics                                       | Bytes stay acceptable for the UI.                   | Patch overhead creates excessive traffic.                    |
| Resolver spans            | [Instrumentation](/docs/hotchocolate/v16/server/instrumentation) and OpenTelemetry | Slow fields align with deferred regions.            | Slow work happens before the deferred boundary.              |
| Open stream count         | Application, gateway, or load balancer metrics                                     | Concurrency is stable under load.                   | Open streams grow without completing.                        |
| Canceled stream count     | Server logs and client abort metrics                                               | Abandoned routes cancel downstream work.            | Canceled clients keep resolvers running.                     |
| Error rate                | GraphQL errors, HTTP status, client parser errors                                  | Parser and merge failures stay near zero.           | Transport or v0.1/v0.2 mismatches appear.                    |

`curl -N -w` can report overall transfer timings, but does not timestamp each chunk. Add server logs or client-side timestamps if you need per-chunk timing.

# Troubleshoot Delayed or Missing Chunks

Before changing application code, test with a direct Kestrel request:

```bash
curl -N http://localhost:5000/graphql \
  -H 'Content-Type: application/json' \
  -H 'Accept: multipart/mixed; incrementalSpec=v0.2' \
  --data '{"query":"{ product { name ... @defer { description } } }"}'
```

Then repeat the request through your proxy or CDN and compare when each chunk arrives.

| Symptom                                     | Likely cause                                                                                       | Fix                                                                                                          |
| ------------------------------------------- | -------------------------------------------------------------------------------------------------- | ------------------------------------------------------------------------------------------------------------ |
| `Unknown directive "defer"`                 | `EnableDefer` is disabled or the executor was not rebuilt.                                         | Enable the option and restart the application.                                                               |
| `Unknown directive "stream"`                | `EnableStream` is disabled or the executor was not rebuilt.                                        | Enable the option and restart the application.                                                               |
| Validation error for `@defer`               | The directive is on a field.                                                                       | Move it to an inline fragment or fragment spread.                                                            |
| Validation error for `@stream`              | The directive is not on a list field.                                                              | Place it on a list field and verify the selected field type.                                                 |
| One JSON response                           | Wrong `Accept` header, no deferred work, `if: false`, or all work finishes before the first flush. | Send a streaming `Accept` header and test with a delayed deferred resolver.                                  |
| Multipart response when you expected SSE    | The `Accept` header allowed multipart and the server selected it.                                  | Request only `text/event-stream` for SSE tests.                                                              |
| Chunks arrive together                      | Proxy, CDN, response compression, client parser, or devtools buffered the response.                | Test direct to Kestrel, disable buffering layers for the route, and timestamp payload receipt in the client. |
| Client parse errors                         | Client expects v0.1, but the server sends v0.2, or the client cannot parse the transport.          | Align `incrementalSpec` and transport support.                                                               |
| No earlier UI state                         | Root resolver or first selection still waits for slow data.                                        | Move slow optional fields behind child resolvers and deferred fragments.                                     |
| Canceled clients keep work running          | Resolvers ignore `CancellationToken` or downstream APIs do not observe it.                         | Add token parameters and pass them through every I/O call.                                                   |
| Duplicate list items after realtime updates | Mutation, pagination, and subscription cache updates all insert the same entity.                   | Pick one owner for each insert path and rely on stable IDs for record updates.                               |

When troubleshooting, inspect these headers:

- Request `Accept`
- Response `Content-Type`
- `Cache-Control: no-cache` for SSE responses
- Response compression settings
- Proxy buffering headers or route settings

# Next Steps and Related Topics

- [Stream results with defer and stream](/docs/hotchocolate/v16/build/realtime/stream-results-with-defer-and-stream): Full enablement, directive syntax, runnable examples, and payload details
- [Configure SSE](/docs/hotchocolate/v16/build/transport/configure-sse): SSE setup, client notes, authentication, and proxy considerations
- [HTTP transport](/docs/hotchocolate/v16/server/http-transport): Content negotiation and streaming transport reference
- [Performance tuning](/docs/hotchocolate/v16/guides/performance): General Hot Chocolate performance guidance
- [Options reference](/docs/hotchocolate/v16/api-reference/options): `EnableDefer`, `EnableStream`, request timeout, socket, and subscription options
- [Subscriptions](/docs/hotchocolate/v16/building-a-schema/subscriptions): Realtime server events
- [DataLoader](/docs/hotchocolate/v16/resolvers-and-data/dataloader): Batching deferred branch loads
- [Pagination](/docs/hotchocolate/v16/resolvers-and-data/pagination): Large, unbounded, or user-controlled lists
- [Instrumentation](/docs/hotchocolate/v16/server/instrumentation): OpenTelemetry and diagnostics
- [Cost analysis](/docs/hotchocolate/v16/securing-your-api/cost-analysis): Bounding expensive operations
- [Trusted documents](/docs/hotchocolate/v16/performance/trusted-documents): Persisted operations and request-size reduction

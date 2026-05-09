---
title: Streaming and Incremental Delivery Performance
---

Use Hot Chocolate v16 incremental delivery when a query has fast critical data and slower secondary data. The goal is not to make the operation do less work. The goal is to let the client render a useful state before every resolver completes.

This page covers operations performance for finite query responses that use `@defer` and `@stream`. It does not cover Fusion streaming behavior. For full directive syntax and build-focused setup, see [stream results with defer and stream](/docs/hotchocolate/v16/build/realtime/stream-results-with-defer-and-stream). For SSE setup, see [configure SSE](/docs/hotchocolate/v16/build/transport/configure-sse).

# Prove streaming changes the user-visible timeline

Start with a control request that prints chunks as they arrive. The example below enables the directives, sends a multipart request, and expects an initial payload followed by a later patch.

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

Use `@defer` on an inline fragment or fragment spread. Keep the fields needed for the first render outside the deferred fragment.

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

Send a request that asks for the v16 incremental delivery format. `curl -N` disables curl output buffering so you can see whether chunks arrive over time.

```bash
curl -N http://localhost:5000/graphql \
  -H 'Content-Type: application/json' \
  -H 'Accept: multipart/mixed; incrementalSpec=v0.2' \
  --data '{"query":"query ProductPage($id: Int!){ product(id:$id){ id name ...ProductDetails @defer(label:\"details\") } } fragment ProductDetails on Product { description recommendations { id name } }","variables":{"id":1}}'
```

A multipart response uses a boundary value chosen by the server. The exact boundary can differ, but the payload shape should look like this:

```text
---
Content-Type: application/json; charset=utf-8

{"data":{"product":{"id":1,"name":"Chai"}},"pending":[{"id":"2","path":["product"]}],"hasNext":true}
---
Content-Type: application/json; charset=utf-8

{"incremental":[{"id":"2","data":{"description":"Black tea","recommendations":[{"id":2,"name":"Coffee"}]}}],"completed":[{"id":"2"}],"hasNext":false}
-----
```

Track three moments:

1. Request start.
2. First useful payload, the first chunk that can render the page shell.
3. Complete payload, the final chunk where `hasNext` becomes `false`.

If those moments are indistinguishable, you do not yet have a streaming performance win. Continue with the operation, transport, and infrastructure checks below.

# Decide whether incremental delivery is the right tool

`@defer` and `@stream` keep one GraphQL operation open and return a finite response stream. They help when your initial selection can finish without waiting for slower work.

They do not reduce database cost, resolver CPU, or response size by themselves. They can increase connection lifetime, concurrent open responses, client parser requirements, and proxy sensitivity.

| Problem                                                                | Prefer                         | Why                                                                                                 |
| ---------------------------------------------------------------------- | ------------------------------ | --------------------------------------------------------------------------------------------------- |
| Product header is fast, below-the-fold details are slow                | `@defer`                       | The page can render identity, title, and layout before optional panels complete.                    |
| A bounded recommendations list is required, and early items are useful | `@stream`                      | The operation still needs the full logical list, but the client can render the first items earlier. |
| New chat messages should arrive after the initial page load            | Subscription                   | Subscriptions model future server events.                                                           |
| Catalog or activity feed may have thousands of items                   | Pagination                     | Users request the next slice when they need it.                                                     |
| Several independent operations can run together                        | Batching or client concurrency | Incremental delivery is not a replacement for independent requests.                                 |
| Route navigation waits for JavaScript before starting data fetches     | Route or intent preloading     | Start the request earlier to avoid fetch-on-render waterfalls.                                      |

# Check prerequisites before you depend on chunks

Use this checklist before you tune an operation around incremental delivery:

- The ASP.NET Core GraphQL endpoint is mapped with `app.MapGraphQL()`.
- `EnableDefer` is `true` before you use `@defer`.
- `EnableStream` is `true` before you use `@stream`.
- The client sends a streaming `Accept` header, not `Accept: application/json`.
- The client parser can consume and merge multiple payloads in the v16 v0.2 format.
- Proxies, CDNs, compression, and hosting layers pass flushed chunks through.
- Critical fields stay outside deferred fragments or streamed list tails.
- Resolvers and downstream APIs accept and honor `CancellationToken`.
- Cost limits, page sizes, and request timeouts match the worst operation you allow.

# Shape the operation so the first chunk is useful

Design the query around UI regions. Put the fields required for a stable initial render in the first payload, and defer optional regions.

```graphql
query ProductPage($id: Int!, $deferPanels: Boolean! = true) {
  product(id: $id) {
    # Render the header and stable layout from the first payload.
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

Use labels that map to UI regions, such as `details`, `reviews`, and `recommendations`. Avoid many tiny deferred fragments unless you have measured that the extra patches help. Each patch adds client merge work and increases the number of states your UI must handle.

Use the `if:` argument when you support clients that cannot consume patches. For those clients, send `false` and return a normal complete result.

# Use `@stream` only for bounded list streaming

`@stream` belongs on list fields. It accepts `label`, `initialCount`, and `if`.

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

Use `@stream` when all of these are true:

- The list is bounded.
- The operation already needs the full logical list.
- The first `initialCount` items are useful without the remaining items.
- The client can merge list patches for the selected response format.

Do not use `@stream` for unbounded feeds, infinite scroll, or lists where a user may never need later items. Use [pagination](/docs/hotchocolate/v16/resolvers-and-data/pagination) for those cases.

Conceptually, the initial payload includes up to `initialCount` items. Later patches add remaining items. Test your exact schema and client before you depend on a specific list patch shape.

# Choose the response format your infrastructure can stream

Hot Chocolate selects the response format from the HTTP `Accept` header.

| Request header                                    | Use when                                                                      | Check                                                          |
| ------------------------------------------------- | ----------------------------------------------------------------------------- | -------------------------------------------------------------- |
| `Accept: multipart/mixed; incrementalSpec=v0.2`   | You want a common format for incremental queries.                             | Response `Content-Type` starts with `multipart/mixed`.         |
| `Accept: text/event-stream; incrementalSpec=v0.2` | You want Server-Sent Events frames for incremental delivery or subscriptions. | Response contains `event: next` and a final `event: complete`. |
| `Accept: application/jsonl; incrementalSpec=v0.2` | Your client parses one JSON result per line.                                  | Response `Content-Type` is `application/jsonl`.                |
| `Accept: application/json`                        | You must support a legacy client.                                             | Do not use this for incremental query streaming.               |

When the client sends no `Accept` header or sends `*/*`, single results use `application/graphql-response+json`. Streaming operations default to `multipart/mixed` unless the client explicitly asks for another streaming format.

In v16, v0.2 is the default incremental delivery wire format. It uses `pending`, `incremental`, and `completed`. Clients that still require the legacy shape can request `incrementalSpec=v0.1`, but new clients should use v0.2.

If you need SSE for a test, request only `text/event-stream`. Mixed headers can negotiate a different supported format based on the server's content negotiation rules.

# Configure clients to consume patches without buffering

The client must process more than one payload for a single operation and merge patches according to the selected incremental delivery format. Support varies by client library version and transport, so verify the exact versions in your application.

A custom Relay network can parse multipart responses with `meros`. This sketch shows the important transport pieces and leaves production error handling in place as comments.

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

Production code should also handle non-2xx responses, parse failures, non-JSON parts, network errors, completion, retries, and aborts. Propagate route-change abort signals so abandoned UI work cancels the HTTP request.

For SSE with POST bodies or custom headers, use a GraphQL-over-SSE capable client. Browser `EventSource` is limited to GET-style usage and cannot send arbitrary request bodies or headers.

# Write resolvers that allow early delivery

Incremental delivery cannot help if the root resolver loads the entire object graph before child fields execute. Return the parent object as soon as the initial selection has enough data, then put slower optional work behind child resolvers selected by deferred fragments.

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

Follow these rules for streamed operations:

- Do not block on asynchronous work with `.Result`, `.Wait()`, or synchronous I/O.
- Do not materialize giant lists on hot paths before the deferred boundary.
- Use DataLoader in deferred branches so later patches do not create N+1 queries.
- Pass `CancellationToken` into database, HTTP, and queue APIs.
- Keep authorization decisions needed for the first render outside slow deferred work.

# Protect resources during long-lived responses

Incremental responses keep HTTP requests open longer than ordinary JSON responses. Track them separately from normal query throughput.

Configure request timeouts around the longest streamed operation you intend to support:

```csharp
// Program.cs
builder
    .AddGraphQL()
    .ModifyRequestOptions(options =>
    {
        options.ExecutionTimeout = TimeSpan.FromSeconds(30);
    });
```

Use cost and size controls with the same mindset. A streamed query can still be expensive even when the first chunk arrives quickly.

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

- Limit maximum concurrent streamed requests at the gateway or application layer.
- Set request timeouts that protect the server without cutting off valid operations.
- Use maximum page sizes and list-size policies for fields that can return many items.
- Use cost analysis for complex operations.
- Test direct cancellation by disconnecting a client during delayed resolver work.
- Watch slow clients and buffering proxies, which can hold server resources after resolver work completes.

Subscription topic buffers are for realtime subscriptions, not finite incremental query responses. Tune subscription providers on the [subscriptions](/docs/hotchocolate/v16/building-a-schema/subscriptions) page instead.

# Measure first useful data, not only total duration

A successful streaming change can leave total completion time unchanged. It can even add a small amount of overhead. Measure whether the first useful UI state arrives earlier.

| Metric                    | How to collect                                                                     | Success signal                                      | Failure signal                                               |
| ------------------------- | ---------------------------------------------------------------------------------- | --------------------------------------------------- | ------------------------------------------------------------ |
| Time to first byte        | Browser network tooling, reverse proxy logs, or `curl -w`                          | Headers and first bytes arrive early.               | Headers wait for all resolver work.                          |
| First useful payload time | Client logs with timestamps around each received patch                             | The shell can render before optional panels finish. | The first patch lacks enough data to render.                 |
| Last payload time         | Client logs or server response completion logs                                     | Total time stays within your SLO.                   | Streaming hides a slow operation that still violates limits. |
| Chunk gaps                | Timestamp each `next` payload in the client                                        | Gaps match slow optional work.                      | Chunks arrive together after buffering.                      |
| Response bytes            | Browser tooling, proxy logs, or server metrics                                     | Bytes stay acceptable for the UI.                   | Patch overhead creates excessive traffic.                    |
| Resolver spans            | [Instrumentation](/docs/hotchocolate/v16/server/instrumentation) and OpenTelemetry | Slow fields align with deferred regions.            | Slow work happens before the deferred boundary.              |
| Open stream count         | Application, gateway, or load balancer metrics                                     | Concurrency is stable under load.                   | Open streams grow without completing.                        |
| Canceled stream count     | Server logs and client abort metrics                                               | Abandoned routes cancel downstream work.            | Canceled clients keep resolvers running.                     |
| Error rate                | GraphQL errors, HTTP status, client parser errors                                  | Parser and merge failures stay near zero.           | Transport or v0.1/v0.2 mismatches appear.                    |

`curl -N -w` can report overall transfer timings, but it does not timestamp each chunk for you. Add server logs or client-side timestamps when you need per-chunk timing.

# Troubleshoot delayed or missing chunks

Use a direct Kestrel control before changing application code:

```bash
curl -N http://localhost:5000/graphql \
  -H 'Content-Type: application/json' \
  -H 'Accept: multipart/mixed; incrementalSpec=v0.2' \
  --data '{"query":"{ product { name ... @defer { description } } }"}'
```

Then repeat the same request through your proxy or CDN and compare when each chunk appears.

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

Inspect these headers during troubleshooting:

- Request `Accept`.
- Response `Content-Type`.
- `Cache-Control: no-cache` for SSE responses.
- Response compression settings.
- Proxy buffering headers or route settings.

# Continue to build and operations topics

- [Stream results with defer and stream](/docs/hotchocolate/v16/build/realtime/stream-results-with-defer-and-stream) for full enablement, directive syntax, runnable examples, and payload details.
- [Configure SSE](/docs/hotchocolate/v16/build/transport/configure-sse) for SSE setup, client notes, authentication, and proxy considerations.
- [HTTP transport](/docs/hotchocolate/v16/server/http-transport) for content negotiation and streaming transport reference.
- [Performance tuning](/docs/hotchocolate/v16/guides/performance) for general Hot Chocolate performance guidance.
- [Options reference](/docs/hotchocolate/v16/api-reference/options) for `EnableDefer`, `EnableStream`, request timeout, socket, and subscription options.
- [Subscriptions](/docs/hotchocolate/v16/building-a-schema/subscriptions) for realtime server events.
- [DataLoader](/docs/hotchocolate/v16/resolvers-and-data/dataloader) for batching deferred branch loads.
- [Pagination](/docs/hotchocolate/v16/resolvers-and-data/pagination) for large, unbounded, or user-controlled lists.
- [Instrumentation](/docs/hotchocolate/v16/server/instrumentation) for OpenTelemetry and diagnostics.
- [Cost analysis](/docs/hotchocolate/v16/securing-your-api/cost-analysis) for bounding expensive operations.
- [Trusted documents](/docs/hotchocolate/v16/performance/trusted-documents) for persisted operations and request-size reduction.

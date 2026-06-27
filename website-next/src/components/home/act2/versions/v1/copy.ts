import type { SceneCopy } from "@/src/components/home/act2/SceneHookGallery";

/** Hook copy for scene-illustrations v1 ("Product Panels", the literal baseline). */
export const V1_COPY: SceneCopy = {
  build: {
    1: {
      headline: "One annotated class, three generated files",
      blurb:
        "An annotated [QueryType] C# class on the left traces by connector lines into the schema.graphql SDL, the resolver-pipeline registration, and the typed DataLoader it emits.",
    },
    2: {
      headline: "Six key-requests, one batched fetch",
      blurb:
        "Six resolver key-requests arriving in one tick line up and merge into a single batched fetch, duplicate keys dimmed beside a live dedupe count.",
    },
    3: {
      headline: "The source-gen pass on a 0.4s axis",
      blurb:
        "The server source-generation stages run as duration-proportional bars on a 0 to 0.4s axis, with the schema-emit bar landing the 0.4s flag.",
    },
    4: {
      headline: "dotnet build, source-gen emit log",
      blurb:
        "A dotnet build terminal streams the source generator emitting types and the schema, then prints an honest green build-succeeded line before anything runs.",
    },
    5: {
      headline: "A glue tangle reduced to one token",
      blurb:
        "A before-panel graph of separately hand-maintained kept-in-sync files collapses into a single [QueryType] ProductApi pill that everything is generated from.",
    },
  },
  feedback: {
    1: {
      headline: "createReview held at the approval gate",
      blurb:
        "An agent terminal calls createReview, pauses at a human gate as PENDING flips to GRANTED, then prints the one safe-patch diff line it applies.",
    },
    2: {
      headline: "The /graphql/mcp tool catalog",
      blurb:
        "An inspector table lists published operations as MCP tools, each row tagged query or mutation and carrying idempotent, destructive, and openWorld behavior-hint badges.",
    },
    3: {
      headline: "Four grounding sources into one MCP core",
      blurb:
        "Schema, published ops, the client registry, and skillz converge inward to the /graphql/mcp core, which emits one tool-call out to a coding agent.",
    },
    4: {
      headline: "A tool's governed promotion path",
      blurb:
        "One tool walks the release path left to right, author to validate to stage to trace, through a resolved approval gate and into production.",
    },
    5: {
      headline: "The SKILL.md an agent loads",
      blurb:
        "A checked-in SKILL.md file shows its YAML frontmatter, a fenced GraphQL example calling /graphql/mcp, and the createReview destructive hint the agent reads to use the tools.",
    },
  },
  observe: {
    1: {
      headline: "The checkout operation card mid-spike",
      blurb:
        "A Nitro operation-detail card catches checkout as p99 kinks past the SLO line, with an amber Investigating pill and four headline metrics underneath.",
    },
    2: {
      headline: "One request as a span waterfall",
      blurb:
        "A distributed trace nests checkout over users-svc (REST), billing (gRPC), worker, and db, with the slow billing hop flagged coral on the critical path at 63 of 94ms.",
    },
    3: {
      headline: "Operations ranked by impact",
      blurb:
        "A sortable table ranks GraphQL operations by p95, error rate, and throughput with a stacked 2xx/4xx/5xx bar, checkout pinned at #1 and firing.",
    },
    4: {
      headline: "The checkout topology, degrading hop lit",
      blurb:
        "A directed topology fans the api node out to users-svc, billing (gRPC), worker, and db, with the api to billing edge glowing amber as the degrading hop.",
    },
    5: {
      headline: "nitro trace 4b1c8f2a in the terminal",
      blurb:
        "Running nitro trace prints the resolved span tree with per-hop timings, flags the slow billing gRPC hop, and recommends the next action.",
    },
  },
  workflows: {
    1: {
      headline: "The compile-time wiring manifest",
      blurb:
        "Mocha's source generator lists the handlers, events, and sagas it discovered and wired at build time, with one CreateReview command still in flight to its handler.",
    },
    2: {
      headline: "ReviewSaga as a state machine",
      blurb:
        "The ReviewSaga strip shows Draft and Checked done and Published pending, the saga processing the transition into Published on an in-flight event.",
    },
    3: {
      headline: "One PublishAsync, five transports",
      blurb:
        "A Mocha config panel runs the same PublishAsync call over five pluggable transports, RabbitMQ selected and carrying the in-flight message.",
    },
    4: {
      headline: "Mediator and bus, one generated wiring",
      blurb:
        "An in-process mediator command/handler sits beside a cross-service bus publish/consume, sharing the same generated wiring with the in-flight publish lit.",
    },
    5: {
      headline: "Outbox to inbox, one row across both",
      blurb:
        "A transactional outbox and an idempotent inbox share one message mid-transit, drawn as a single row spanning both tables and carrying its MessageId.",
    },
  },
  guardrails: {
    1: {
      headline: "schema.graphql diff, each line classified",
      blurb:
        "A schema.graphql diff tags every changed line SAFE or BREAKING, with a pinned registry-bot Resolve thread hanging off the one breaking line.",
    },
    2: {
      headline: "The registry check blocking the merge",
      blurb:
        "A failing PR check for the Nitro schema registry shows one required check whose four sub-steps each name a facet of the 3-change set, merging blocked.",
    },
    3: {
      headline: "Published-client impact matrix",
      blurb:
        "A client-registry table lists each published client hit by the breaking change with a per-client readiness bar reading OK, at-risk, or queued.",
    },
    4: {
      headline: "Generated client fails to compile",
      blurb:
        "A dotnet build surfaces the breaking change as a real C# compiler error when the StrawberryShake generated client hits a field whose type changed.",
    },
    5: {
      headline: "Schema versions gated on the rail",
      blurb:
        "A registry version history puts three published schema.graphql versions plus a gated ghost version on a rail, the current one blocked pending review with verdict by node color.",
    },
  },
};

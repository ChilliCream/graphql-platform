import type { SceneCopy } from "@/src/components/home/act2/SceneHookGallery";

/** Hook copy for scene-illustrations v2 ("Flow Diagrams"). */
export const V2_COPY: SceneCopy = {
  build: {
    1: {
      headline: "One class becomes three artifacts",
      blurb:
        "An annotated [QueryType] class fans out into your schema, resolver pipeline, and typed DataLoader, with no glue code to maintain.",
    },
    2: {
      headline: "Six requests, one fetch",
      blurb:
        "DataLoaders collect resolver key requests within a tick, drop duplicates, and hit your data source exactly once.",
    },
    3: {
      headline: "One build pass emits your whole schema",
      blurb:
        "An ordered source-generation stage runs inside dotnet build and emits the schema, resolvers, and DataLoaders in 0.4s.",
    },
    4: {
      headline: "Errors fail the build, not the request",
      blurb:
        "The source generator runs inside dotnet build, so type and schema mistakes break the compile and never reach runtime.",
    },
    5: {
      headline: "Four glue files become one token",
      blurb:
        "Stop hand-syncing schema, types, client, and DTOs; generate them all from one [QueryType] ProductApi source.",
    },
  },
  feedback: {
    1: {
      headline: "Risky agent calls route through a human gate",
      blurb:
        "The agent's createReview call pauses at a PENDING approval gate and only lands its single patch once a human grants it.",
    },
    2: {
      headline: "Published operations become MCP tools",
      blurb:
        "The /graphql/mcp catalog exposes each operation as a typed tool with behavior hints, so an agent knows exactly what it can call and what needs a gate.",
    },
    3: {
      headline: "Your schema becomes the agent's tools",
      blurb:
        "Schema, published ops, the client registry, and your skills converge into one /graphql/mcp endpoint the coding agent calls.",
    },
    4: {
      headline: "Every tool walks the same gated path",
      blurb:
        "Author, validate, stage, and trace each tool, then ship it only after the approval gate clears to production.",
    },
    5: {
      headline: "A checked-in SKILL.md grounds your agent",
      blurb:
        "One reviewed markdown file teaches coding agents your MCP tools, so they stop guessing and call the real GraphQL operations.",
    },
  },
  observe: {
    1: {
      headline: "When p99 crosses the SLO line",
      blurb:
        "Nitro ties one operation's latency windows, breach threshold, and error rate into a single health view you read at a glance.",
    },
    2: {
      headline: "Where the 94ms actually goes",
      blurb:
        "One trace nests every span under the checkout request, so the slow billing gRPC hop shows up as 63 of 94 milliseconds instead of a guess.",
    },
    3: {
      headline: "Rank operations by what breaks",
      blurb:
        "Nitro orders every operation by real impact, so the firing checkout mutation sits at number one instead of buried in a dashboard.",
    },
    4: {
      headline: "Trace the dependency going bad",
      blurb:
        "Nitro maps your checkout request across every service so the one degrading hop, api to billing over gRPC, surfaces before customers feel it.",
    },
    5: {
      headline: "One command resolves the span tree",
      blurb:
        "nitro trace replays a request into per-hop timings, so the slow billing gRPC call points you straight at the fix.",
    },
  },
  workflows: {
    1: {
      headline: "Your handlers wire themselves at compile time",
      blurb:
        "Mocha's source generator discovers every command, event, handler, and saga and wires them into a working system at build time, with zero registration code.",
    },
    2: {
      headline: "Work that advances state by state",
      blurb:
        "Model long-running work as a saga, and each event moves it one durable state forward, even when the request is long gone.",
    },
    3: {
      headline: "One publish, any transport",
      blurb:
        "The same PublishAsync routes over RabbitMQ, Kafka, Azure SB, Postgres, or in-process by swapping one line of config.",
    },
    4: {
      headline: "One wiring, mediator or bus",
      blurb:
        "The same generated dispatch runs your command in-process or publishes it across services, no rewiring.",
    },
    5: {
      headline: "One MessageId, exactly one delivery",
      blurb:
        "A transactional outbox hands each message to an idempotent inbox keyed on its MessageId, so retries and duplicate deliveries get processed exactly once.",
    },
  },
  guardrails: {
    1: {
      headline: "Every schema change, classified by risk",
      blurb:
        "The registry tags each diff line SAFE or BREAKING and pins a resolve thread to the one field that breaks live clients.",
    },
    2: {
      headline: "One breaking facet shuts the merge",
      blurb:
        "The required registry check fans your PR into four facets, and a single breaking schema diff blocks the merge before it lands.",
    },
    3: {
      headline: "See which clients a change breaks",
      blurb:
        "Before you publish, the registry fans your schema change out to every published client and shows exactly whose operations still validate.",
    },
    4: {
      headline: "Schema drift becomes a compiler error",
      blurb:
        "When a field changes type, the regenerated client stops compiling, so dotnet build fails before the break ever reaches runtime.",
    },
    5: {
      headline: "Every version gated before it ships",
      blurb:
        "The registry holds each schema version on a timeline and blocks the next one until the breaking change clears review.",
    },
  },
};

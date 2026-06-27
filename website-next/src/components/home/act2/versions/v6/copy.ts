import type { SceneCopy } from "@/src/components/home/act2/SceneHookGallery";

/**
 * Hook copy for scene-illustrations v6 ("Bespoke Hooks"), the research-driven
 * production-candidate set. Outcome-first marketing lines; "source generation"
 * is deliberately never used as a selling point.
 */
export const V6_COPY: SceneCopy = {
  build: {
    1: {
      headline: "One class is the whole API.",
      blurb:
        "The schema you serve, the resolvers behind it, the batching that guards your database, and a typed .NET client all come from one annotated C# class.",
    },
    2: {
      headline: "N+1 queries, gone by default.",
      blurb:
        "Repeated lookups in a single request are deduplicated and batched, so a field that fired N database calls resolves in one.",
    },
    3: {
      headline: "If it compiles, it ships.",
      blurb:
        "Every build reconciles your schema, batching, and typed client, so the contract is verified before the first request.",
    },
    4: {
      headline: "Caught at build, not in prod.",
      blurb:
        "A rename that would break a resolver or client shows up as a red build error instead of a 500 in production.",
    },
    5: {
      headline: "One source of truth, not four.",
      blurb:
        "The schema file, resolver map, and client schema collapse into one annotated class, so nothing on the side falls out of step.",
    },
  },
  feedback: {
    1: {
      headline: "Risky agent edits stop here.",
      blurb:
        "A destructive agent call pauses at a human approval gate and applies its patch only after you grant it.",
    },
    2: {
      headline: "Agents call real, approved tools.",
      blurb:
        "Your published operations appear at /graphql/mcp as typed tools, each labeled with exactly how it behaves.",
    },
    3: {
      headline: "Your graph grounds the agent.",
      blurb:
        "Schema, published operations, the client registry, and checked-in skills converge into one surface, so the agent works from facts your system already proves.",
    },
    4: {
      headline: "Every tool earns its way up.",
      blurb:
        "Each tool walks author, validate, stage, and trace, clearing an approval gate before it is ever served.",
    },
    5: {
      headline: "One file teaches every agent.",
      blurb:
        "A checked-in SKILL.md grounds the agent in how to use your tools and is reviewed like any other code.",
    },
  },
  observe: {
    1: {
      headline: "Watch p99 cross the line.",
      blurb:
        "The instant a p99 climbs past your SLO, the operation that is hurting is already flagged, not buried in a chart you have to go find.",
    },
    2: {
      headline: "See exactly which hop is slow.",
      blurb:
        "One request fans out into a span waterfall, and the gRPC call eating 201 of 318ms is the one bar lit coral.",
    },
    3: {
      headline: "Fix what hurts most first.",
      blurb:
        "Operations rank by impact, not call count, so checkout at #1 tells you where the next hour goes.",
    },
    4: {
      headline: "Spot the degrading hop instantly.",
      blurb:
        "The whole call graph renders on one trace, and the degrading hop glows amber so your eye lands on the cause.",
    },
    5: {
      headline: "Replay the trace, see it green.",
      blurb:
        "Run nitro trace and the span tree replays in your terminal, the slow hop resolved end to end.",
    },
  },
  workflows: {
    1: {
      headline: "Your handlers, already wired.",
      blurb:
        "Implement the handler interface and Mocha connects your commands, events, and sagas into one running pipeline, with no registration glue.",
    },
    2: {
      headline: "A workflow that can't get stuck.",
      blurb:
        "Define the state machine once and Mocha drives it across many messages, persisting state so it resumes after a restart.",
    },
    3: {
      headline: "One publish, any broker.",
      blurb:
        "A single PublishAsync runs over RabbitMQ, Postgres, Kafka, Azure, or in-process, so going to production is a registration change, not a rewrite.",
    },
    4: {
      headline: "Same handlers, near or far.",
      blurb:
        "The same model serves the command you dispatch in-process and the event you publish across services; you change the verb, not the mental model.",
    },
    5: {
      headline: "Sent once, processed once.",
      blurb:
        "The outbox commits each message in your data's transaction and the inbox drops duplicates, so every consumer processes exactly once.",
    },
  },
  guardrails: {
    1: {
      headline: "See the breaking line before merge.",
      blurb:
        "A schema diff stamps each line safe or breaking, so a risky removal is obvious in review instead of in production.",
    },
    2: {
      headline: "Block the merge, not production.",
      blurb:
        "The registry check fails any PR that would break a published client and holds the merge until the contract is safe.",
    },
    3: {
      headline: "Know exactly which clients break.",
      blurb:
        "An impact matrix names every published client and shows which are clear and which would break.",
    },
    4: {
      headline: "The break becomes a compiler error.",
      blurb:
        "When the contract shifts, the consuming client stops compiling, so the break surfaces as a red error before a user sees it.",
    },
    5: {
      headline: "Risky releases wait at the gate.",
      blurb:
        "Each schema version lands on a timeline, and a flagged release is held pending review.",
    },
  },
};

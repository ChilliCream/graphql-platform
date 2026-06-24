import type { Metadata } from "next";
import type { ComponentType } from "react";

import { BuildVariant1 } from "@/src/components/home/act2/variants/build/BuildVariant1";
import { BuildVariant2 } from "@/src/components/home/act2/variants/build/BuildVariant2";
import { BuildVariant3 } from "@/src/components/home/act2/variants/build/BuildVariant3";
import { BuildVariant4 } from "@/src/components/home/act2/variants/build/BuildVariant4";
import { BuildVariant5 } from "@/src/components/home/act2/variants/build/BuildVariant5";
import { FeedbackVariant1 } from "@/src/components/home/act2/variants/feedback/FeedbackVariant1";
import { FeedbackVariant2 } from "@/src/components/home/act2/variants/feedback/FeedbackVariant2";
import { FeedbackVariant3 } from "@/src/components/home/act2/variants/feedback/FeedbackVariant3";
import { FeedbackVariant4 } from "@/src/components/home/act2/variants/feedback/FeedbackVariant4";
import { FeedbackVariant5 } from "@/src/components/home/act2/variants/feedback/FeedbackVariant5";
import { GuardrailsVariant1 } from "@/src/components/home/act2/variants/guardrails/GuardrailsVariant1";
import { GuardrailsVariant2 } from "@/src/components/home/act2/variants/guardrails/GuardrailsVariant2";
import { GuardrailsVariant3 } from "@/src/components/home/act2/variants/guardrails/GuardrailsVariant3";
import { GuardrailsVariant4 } from "@/src/components/home/act2/variants/guardrails/GuardrailsVariant4";
import { GuardrailsVariant5 } from "@/src/components/home/act2/variants/guardrails/GuardrailsVariant5";
import { ObserveVariant1 } from "@/src/components/home/act2/variants/observe/ObserveVariant1";
import { ObserveVariant2 } from "@/src/components/home/act2/variants/observe/ObserveVariant2";
import { ObserveVariant3 } from "@/src/components/home/act2/variants/observe/ObserveVariant3";
import { ObserveVariant4 } from "@/src/components/home/act2/variants/observe/ObserveVariant4";
import { ObserveVariant5 } from "@/src/components/home/act2/variants/observe/ObserveVariant5";
import { WorkflowsVariant1 } from "@/src/components/home/act2/variants/workflows/WorkflowsVariant1";
import { WorkflowsVariant2 } from "@/src/components/home/act2/variants/workflows/WorkflowsVariant2";
import { WorkflowsVariant3 } from "@/src/components/home/act2/variants/workflows/WorkflowsVariant3";
import { WorkflowsVariant4 } from "@/src/components/home/act2/variants/workflows/WorkflowsVariant4";
import { WorkflowsVariant5 } from "@/src/components/home/act2/variants/workflows/WorkflowsVariant5";

export const metadata: Metadata = {
  title: "Scene Illustrations",
  robots: { index: false, follow: false },
};

type VariantComponent = ComponentType<{ readonly className?: string }>;

/**
 * One illustration option for a scene. `Component` is `null` when the variant
 * file is not present, so the gallery renders a placeholder instead of failing
 * the build.
 */
interface Variant {
  readonly n: number;
  /** The concept name (e.g. "DataLoader batch ledger"). */
  readonly name: string;
  /** The one-line description of what the visual depicts. */
  readonly depicts: string;
  readonly Component: VariantComponent | null;
}

/** One homepage scene: its eyebrow label, headline, accent rationale, variants. */
interface Scene {
  readonly key: string;
  readonly label: string;
  readonly headline: string;
  /** The scene's color rationale, shown as a mono note under the headline. */
  readonly accent: string;
  readonly variants: readonly Variant[];
}

const SCENES: readonly Scene[] = [
  {
    key: "build",
    label: "Build loop",
    headline: "Ship from the code that runs it.",
    accent:
      "cyan #16b9e4 warming to teal #5eead4 (cool / compile-time); at most one cyan->teal gradient",
    variants: [
      {
        n: 1,
        name: "Annotated source, generated file contents",
        depicts:
          "A real C# editor card showing an annotated partial class on the left, whose [QueryType] and resolver lines trace by thin connector lines into three small generated-artifact tiles on the right, each tile showing the actual emitted code (a 3-line schema.graphql SDL snippet, a one-line resolver-pipeline registration, a one-line DataLoader signature), not just a filename.",
        Component: BuildVariant1,
      },
      {
        n: 2,
        name: "DataLoader batch ledger",
        depicts:
          "A precise request-collapsing diagram: six inbound resolver key-requests arriving within one tick line up on the left and merge into a single batched fetch on the right, with the duplicate keys dimmed and a dedupe count.",
        Component: BuildVariant2,
      },
      {
        n: 3,
        name: "Server source-gen Gantt ribbon",
        depicts:
          "A horizontal time-proportional ribbon of the SERVER source-generation pass only: ordered generator tasks laid out as duration-proportional bars on a 0 -> 0.4s axis, with the schema-emit bar landing the '0.4s' flag.",
        Component: BuildVariant3,
      },
      {
        n: 4,
        name: "Build terminal: emit log",
        depicts:
          "A realistic terminal session running 'dotnet build', streaming the source generator's emit lines (one of which reports the 0.4s schema-emit cost), then a types summary and an honest green build-succeeded line carrying the real total elapsed.",
        Component: BuildVariant4,
      },
      {
        n: 5,
        name: "Before / after: glue tangle collapses",
        depicts:
          "A split comparison whose HERO is the left 'before' panel, a node-and-tangled-edge graph of a few separately hand-maintained, kept-in-sync files; the right 'after' is reduced to a single small labeled token, a '[QueryType] ProductApi' pill, captioned that everything is generated from it.",
        Component: BuildVariant5,
      },
    ],
  },
  {
    key: "feedback",
    label: "Agentic coding",
    headline: "Give coding agents a feedback loop.",
    accent:
      "violet #7c92c6 / cc-info (governance); coral #f0786a ONLY for the destructive tool annotation",
    variants: [
      {
        n: 1,
        name: "Approval-Gate Terminal",
        depicts:
          "A coding-agent terminal transcript where the agent invokes the createReview tool, the run pauses at an approval gate showing a live PENDING -> GRANTED transition, then prints a single safe-patch diff line. The one believable moment: an agent's destructive action blocked behind a human gate, unblocked, then applied. This concept OWNS the animated governance transition; no other concept re-performs it.",
        Component: FeedbackVariant1,
      },
      {
        n: 2,
        name: "Tool Catalog Registry Table",
        depicts:
          "The /graphql/mcp tool catalog rendered as an inspector TABLE: published operations exposed as MCP tools, each row carrying a concrete mono type tag (query / mutation) and a uniform cluster of behavior-hint pill badges (idempotentHint / destructiveHint / openWorldHint). The believable thing: a registry listing the exact tools an agent can call and how each behaves, scannable as data, not marketing chips.",
        Component: FeedbackVariant2,
      },
      {
        n: 3,
        name: "Inward MCP Convergence",
        depicts:
          "A hub-and-spoke SVG where four grounding sources (schema, published ops, client registry, skillz) converge INWARD along spokes to a single /graphql/mcp core, which emits ONE tool-call spoke outward to a coding agent. No return arc. The believable thing: the server's existing artifacts feeding the MCP surface that grounds the agent. It answers 'what grounds the agent', distinct from the lifecycle and the tool list.",
        Component: FeedbackVariant3,
      },
      {
        n: 4,
        name: "Lifecycle Promotion Strip",
        depicts:
          "The governed tool lifecycle as ONE flat horizontal release path: author -> validate -> stage -> trace -> [approval gate: GRANTED] -> production. A single tool walks left-to-right through governed stages and clears a STATIC, already-resolved approval gate. The believable thing: a completed gated promotion path with a real approval gate at rest, NOT a re-performed transition animation.",
        Component: FeedbackVariant4,
      },
      {
        n: 5,
        name: "SKILL.md Source-of-Truth Tile",
        depicts:
          "A single SKILL.md file rendered as code chrome: the authored markdown skill an agent loads to use this server's MCP tools, with YAML frontmatter, a fenced GraphQL example calling /graphql/mcp, and an in-file annotation that encodes createReview's destructive behavior hint. The believable thing: the real authored artifact (the 'skillz' token nothing else renders) checked into the repo, reviewed like any code.",
        Component: FeedbackVariant5,
      },
    ],
  },
  {
    key: "observe",
    label: "Production view",
    headline: "See what the API is doing.",
    accent:
      "teal #5eead4 with status semantics (green healthy, amber investigating, coral firing); color rationed as data",
    variants: [
      {
        n: 1,
        name: "Incident dashboard tile, mid-spike",
        depicts:
          "A single floating Nitro operation-detail card for the `checkout` operation caught at the moment p99 spikes: a live p99 latency sparkline kinking upward past the SLO line, an amber 'Investigating' status pill, and the four headline metrics underneath.",
        Component: ObserveVariant1,
      },
      {
        n: 2,
        name: "Distributed-trace span waterfall",
        depicts:
          "One request's distributed trace expanded as a horizontal span waterfall: the root GraphQL `checkout` span at top, then nested child spans for users-svc (REST), billing (gRPC), worker (job), and db, with the slow billing gRPC hop tinted as the coral bottleneck and a critical-path hairline tying the whole trace together.",
        Component: ObserveVariant2,
      },
      {
        n: 3,
        name: "Operations table ranked by impact",
        depicts:
          "A Nitro operations list view: a sortable table of GraphQL operations ranked by impact score, each row showing p95, error rate, throughput, and a compact stacked status-mix bar (2xx/4xx/5xx), with `checkout` pinned at #1 and flagged firing.",
        Component: ObserveVariant3,
      },
      {
        n: 4,
        name: "Service-topology graph, 60s before the spike",
        depicts:
          "A directed service-topology graph for the checkout request, explicitly stamped as the earlier moment of the same incident shown in concept 2: the GraphQL `api` node fanning out to users-svc, billing (gRPC), worker, and db as real nodes-and-edges, with the api->billing gRPC edge just starting to glow amber as the degrading hop while it is still under investigation.",
        Component: ObserveVariant4,
      },
      {
        n: 5,
        name: "`nitro trace` terminal replay",
        depicts:
          "A terminal session where an engineer runs `nitro trace 4b1c8f2a` and the CLI prints the resolved span tree of the checkout request with per-hop timings and a summary that flags the slow billing gRPC hop and a recommended next action.",
        Component: ObserveVariant5,
      },
    ],
  },
  {
    key: "workflows",
    label: "Workflow",
    headline: "Let work continue after the request.",
    accent:
      "amber #f59e0b warming to coral #f0786a (work-in-motion); the accent tracks the single in-flight message",
    variants: [
      {
        n: 1,
        name: "Compile-Time Wiring Manifest",
        depicts:
          "A light compile-time manifest card where Mocha's source generator lists the handlers, events, and sagas it discovered and wired during the build, with one CreateReview command still in flight to its handler.",
        Component: WorkflowsVariant1,
      },
      {
        n: 2,
        name: "Saga State-Machine Strip",
        depicts:
          "The ReviewSaga as a horizontal state machine: Draft and Checked completed, Published pending, with the saga currently processing the transition into Published triggered by an in-flight event.",
        Component: WorkflowsVariant2,
      },
      {
        n: 3,
        name: "Transport Swap Panel",
        depicts:
          "A Mocha configuration panel showing the same PublishAsync call running over five pluggable transports, with RabbitMQ selected and currently carrying the in-flight message.",
        Component: WorkflowsVariant3,
      },
      {
        n: 4,
        name: "Mediator vs Bus Code Pair",
        depicts:
          "Two short side-by-side C# snippets: an in-process mediator command/handler (CQRS) on the left and a cross-service bus publish/consume on the right, sharing the same generated wiring, with the in-flight publish line lit.",
        Component: WorkflowsVariant4,
      },
      {
        n: 5,
        name: "Outbox-to-Inbox Ledger",
        depicts:
          "A two-table database-style ledger showing the transactional outbox on the left and the idempotent inbox on the right, with one message mid-transit drawn as a single straight row spanning both tables, carrying its MessageId.",
        Component: WorkflowsVariant5,
      },
    ],
  },
  {
    key: "guardrails",
    label: "Release safety",
    headline: "Change contracts with a safety net.",
    accent:
      "status-driven: cc-success green safe, cc-warning amber dangerous, cc-danger coral #f0786a breaking",
    variants: [
      {
        n: 1,
        name: "Schema Diff with Inline Classifier Chips",
        depicts:
          "A code-review diff of schema.graphql where each changed line carries a SAFE/BREAKING chip and the one breaking line has a pinned registry-bot Resolve thread hanging off it, all on the light cream card surface (a diff panel, not a dark terminal).",
        Component: GuardrailsVariant1,
      },
      {
        n: 2,
        name: "Registry Check PR Merge-Box",
        depicts:
          "A failing pull-request check run for the Nitro schema registry, framed as one required check whose four sub-steps each name a distinct, non-overlapping facet of the same 3-change set, with merging blocked.",
        Component: GuardrailsVariant2,
      },
      {
        n: 3,
        name: "Published-Client Impact Matrix",
        depicts:
          "A client-registry impact table showing each published client affected by the breaking change, with a per-client readiness bar (OK / at-risk / queued), matching the scene tokens exactly with one row per named client.",
        Component: GuardrailsVariant3,
      },
      {
        n: 4,
        name: "Generated-Client Build Drift Terminal",
        depicts:
          "A dotnet build terminal where the StrawberryShake generated client fails to compile because a schema field changed type, surfacing the breaking change as a real, causally-consistent C# compiler error. The one dark-chrome concept in the set.",
        Component: GuardrailsVariant4,
      },
      {
        n: 5,
        name: "Schema Version Timeline with Breaking Marker",
        depicts:
          "A horizontal registry version history of schema.graphql where three published versions plus a gated ghost version sit on a rail, verdict encoded by node color alone, the current version blocked pending review.",
        Component: GuardrailsVariant5,
      },
    ],
  },
];

/** Bordered cell holding one variant, captioned with its number, name, and depiction. */
function VariantCard({ variant }: { readonly variant: Variant }) {
  const { n, name, depicts, Component } = variant;

  return (
    <div className="border-cc-card-border bg-cc-surface flex flex-col gap-4 rounded-2xl border p-5">
      <div className="flex min-h-48 flex-1 items-center justify-center">
        {Component === null ? (
          <div className="border-cc-ink-faint text-cc-ink-dim text-caption w-full rounded-xl border border-dashed px-4 py-10 text-center">
            variant file missing
          </div>
        ) : (
          <Component />
        )}
      </div>

      <div className="border-cc-card-border space-y-1.5 border-t pt-4">
        <p className="text-cc-nav-label font-mono text-[0.6rem] tracking-[0.12em] uppercase">
          Variant {n}
        </p>
        <p className="text-cc-heading text-sm font-medium">{name}</p>
        <p className="text-cc-ink-dim text-xs/relaxed">{depicts}</p>
      </div>
    </div>
  );
}

export default function SceneIllustrationsPage() {
  return (
    <div className="space-y-20">
      <header className="space-y-3">
        <p className="text-cc-nav-label font-mono text-xs tracking-[0.2em] uppercase">
          Internal gallery
        </p>
        <h1 className="font-heading text-h2 text-cc-heading">
          Scene illustrations
        </h1>
        <p className="text-cc-ink max-w-2xl text-base/relaxed">
          Every illustration variant for the five homepage scroll-scenes, side
          by side. Each scene shows its eyebrow label, headline, and accent
          rationale, then five candidate visuals built entirely from cc-* tokens
          and inline SVG. Pick one per scene.
        </p>
      </header>

      {SCENES.map((scene) => (
        <section key={scene.key} className="space-y-6">
          <div className="space-y-2">
            <p className="text-cc-nav-label font-mono text-xs tracking-[0.18em] uppercase">
              {scene.label}
            </p>
            <h2 className="font-heading text-h4 text-cc-heading">
              {scene.headline}
            </h2>
            <p className="text-cc-ink-dim font-mono text-[0.7rem]/relaxed">
              accent: {scene.accent}
            </p>
          </div>

          <div className="grid grid-cols-1 gap-5 sm:grid-cols-2 lg:grid-cols-3">
            {scene.variants.map((variant) => (
              <VariantCard key={variant.n} variant={variant} />
            ))}
          </div>
        </section>
      ))}
    </div>
  );
}

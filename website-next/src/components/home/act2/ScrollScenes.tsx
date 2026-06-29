import type { ReactNode } from "react";

import { CheckIcon } from "@/src/components/CheckIcon";
import { RevealOnScroll } from "@/src/components/RevealOnScroll";
import { OutlineButton, SolidButton } from "@/src/design-system/Button";

/**
 * Brand spectrum (cyan -> violet -> coral). Used at most once per screen, here only
 * to tint a single phrase in the section lead. Everything else stays in the calm
 * cream / grey / teal palette.
 */
const SPECTRUM =
  "linear-gradient(100deg,#16b9e4 0%,#7c92c6 33%,#b681a9 63%,#f0786a 100%)";

interface SceneContent {
  readonly key: string;
  readonly label: string;
  readonly headline: string;
  readonly blurb: string;
  readonly learnMore: string;
}

// Story spine: a complete API development loop. Agentic coding is one scene in
// the platform story, not the whole story.
const SCENES: readonly SceneContent[] = [
  {
    key: "build",
    label: "Build loop",
    headline: "Ship from the code that runs it.",
    blurb:
      "Define the contract where the implementation lives, then let the platform generate the server pipeline, DataLoaders, clients, and local tooling around it. Less glue code, fewer places for drift to hide.",
    learnMore: "/platform",
  },
  {
    key: "feedback",
    label: "Agentic coding",
    headline: "Give coding agents a feedback loop.",
    blurb:
      "Agents can move fast, but without product context they guess. Published operations show the fields clients actually use, while registry checks and approved tools turn risky edits into actionable feedback.",
    learnMore: "/docs/nitro/apis/client-registry",
  },
  {
    key: "observe",
    label: "Production view",
    headline: "See what the API is doing.",
    blurb:
      "Nitro turns traffic into operation metrics, traces, latency, error, and impact views. When something gets slow or noisy, debugging starts with evidence instead of another dashboard project.",
    learnMore: "/platform/analytics",
  },
  {
    key: "workflows",
    label: "Workflow",
    headline: "Let work continue after the request.",
    blurb:
      "Mocha turns backend behavior into commands, events, handlers, and sagas. The source generator discovers those small pieces and wires them into CQRS and messaging patterns that are easy to extend.",
    learnMore: "/docs/mocha",
  },
  {
    key: "guardrails",
    label: "Release safety",
    headline: "Change contracts with a safety net.",
    blurb:
      "Registry checks compare proposed schema changes with published operations and clients. Generated clients and Nitro validation can stop unsafe releases before consumers discover them.",
    learnMore: "/platform/continuous-integration",
  },
];

/** Small reusable chip for the operational illustrations. */
function Chip({
  children,
  active = false,
}: {
  readonly children: ReactNode;
  readonly active?: boolean;
}) {
  return (
    <span
      className={[
        "rounded-lg border px-2.5 py-1.5 font-mono text-[0.65rem] whitespace-nowrap",
        active
          ? "border-cc-accent/60 text-cc-accent bg-cc-surface"
          : "border-cc-card-border text-cc-ink bg-cc-surface",
      ].join(" ")}
    >
      {children}
    </span>
  );
}

function Arrow() {
  return (
    <span aria-hidden="true" className="text-cc-ink-faint px-0.5 text-sm">
      &rarr;
    </span>
  );
}

/** Scene 1 visual: source code produces the API contract and generated pieces. */
function BuildLoop() {
  const steps = ["implementation", "schema", "DataLoaders", "client"];

  return (
    <div className="relative mx-auto w-full max-w-sm select-none">
      <div className="border-cc-card-border bg-cc-card-bg/60 rounded-2xl border p-5 backdrop-blur-sm">
        <p className="text-cc-nav-label font-mono text-[0.58rem] tracking-[0.15em] uppercase">
          build loop
        </p>

        <div className="mt-4 flex flex-wrap items-center justify-center gap-1">
          {steps.map((step, index) => (
            <span key={step} className="flex items-center">
              {index > 0 && <Arrow />}
              <Chip active={index === 0}>{step}</Chip>
            </span>
          ))}
        </div>

        <div className="border-cc-card-border mt-5 space-y-2 border-t pt-4">
          {[
            { label: "source", value: "[QueryType] ProductApi" },
            { label: "generated", value: "resolver pipeline + client" },
            { label: "feedback", value: "build before runtime" },
          ].map((row) => (
            <div
              key={row.label}
              className="border-cc-card-border bg-cc-surface flex items-center gap-3 rounded-lg border px-3 py-2"
            >
              <span className="text-cc-nav-label w-20 shrink-0 font-mono text-[0.55rem] tracking-[0.08em] uppercase">
                {row.label}
              </span>
              <span className="text-cc-ink font-mono text-xs">{row.value}</span>
            </div>
          ))}
        </div>
      </div>
    </div>
  );
}

/** Scene 1 visual: coding agent feedback loop grounded by registered operations. */
function AgentFeedbackLoop() {
  const rows = [
    { label: "agent patch", value: "Add Review" },
    { label: "published ops", value: "ProductCard { id name price }" },
    { label: "feedback", value: "price is in use" },
  ];

  return (
    <div className="relative mx-auto w-full max-w-sm select-none">
      <div className="border-cc-card-border bg-cc-card-bg/60 rounded-2xl border p-5 backdrop-blur-sm">
        <p className="text-cc-nav-label font-mono text-[0.58rem] tracking-[0.15em] uppercase">
          agent feedback loop
        </p>

        <div className="mt-3 flex flex-wrap items-center justify-center gap-1.5">
          <Chip active>coding agent</Chip>
          <Arrow />
          <Chip>registry</Chip>
          <Arrow />
          <Chip>safe patch</Chip>
        </div>

        <div className="border-cc-card-border mt-4 space-y-2 border-t pt-4">
          {rows.map((row) => (
            <div
              key={row.label}
              className="border-cc-card-border bg-cc-surface flex items-center gap-3 rounded-lg border px-3 py-2"
            >
              <span className="text-cc-nav-label w-24 shrink-0 font-mono text-[0.55rem] tracking-[0.08em] uppercase">
                {row.label}
              </span>
              <span className="text-cc-ink text-sm">{row.value}</span>
            </div>
          ))}
        </div>

        <div className="border-cc-ink-faint mt-4 border-t border-dashed pt-3">
          <p className="text-cc-ink-dim text-center text-xs">
            not endpoint hits; declared field demand
          </p>
        </div>
      </div>
    </div>
  );
}

/**
 * Scene 3 visual: a service topology feeding a Nitro operation view. Source nodes
 * (API/REST/gRPC/worker/DB) converge into a trace timeline + key stats.
 */
function ServiceTopology() {
  const spans = [
    { label: "api", left: 0, width: 100 },
    { label: "users-svc", left: 12, width: 46 },
    { label: "billing (grpc)", left: 30, width: 34 },
    { label: "worker", left: 58, width: 30 },
    { label: "db", left: 64, width: 22 },
  ];

  return (
    <div className="relative mx-auto w-full max-w-sm select-none">
      <div className="border-cc-card-border bg-cc-card-bg/60 rounded-2xl border p-5 backdrop-blur-sm">
        <p className="text-cc-nav-label font-mono text-[0.58rem] tracking-[0.15em] uppercase">
          traffic, traces, impact
        </p>

        <div className="mt-3 flex flex-wrap items-center justify-center gap-1.5">
          <Chip active>API</Chip>
          <Arrow />
          <Chip>REST</Chip>
          <Chip>gRPC</Chip>
          <Chip>worker</Chip>
          <Chip>DB</Chip>
        </div>

        <div className="border-cc-card-border mt-4 space-y-1.5 border-t pt-4">
          {spans.map((span) => (
            <div key={span.label} className="flex items-center gap-2">
              <span className="text-cc-ink-dim w-20 shrink-0 text-right font-mono text-[0.55rem]">
                {span.label}
              </span>
              <span className="bg-cc-surface relative h-2 flex-1 overflow-hidden rounded-full">
                <span
                  className="bg-cc-accent absolute top-0 h-full rounded-full opacity-70"
                  style={{ left: `${span.left}%`, width: `${span.width}%` }}
                />
              </span>
            </div>
          ))}
        </div>

        <div className="border-cc-card-border mt-4 grid grid-cols-2 gap-4 border-t pt-4">
          <Stat figure="42ms" label="p95 latency" />
          <Stat figure="#1" label="impact: checkout" />
        </div>
      </div>
    </div>
  );
}

function Stat({
  figure,
  label,
}: {
  readonly figure: string;
  readonly label: string;
}) {
  return (
    <div>
      <p className="font-heading text-cc-heading text-h4 leading-none font-semibold">
        {figure}
      </p>
      <p className="text-cc-ink-dim mt-1.5 text-xs">{label}</p>
    </div>
  );
}

/**
 * Scene 4 visual: one command flows through handler, event, and a saga state
 * machine, with a trace timeline. Transports are a small secondary detail.
 */
function WorkflowFlow() {
  const states = [
    { name: "Draft", done: true },
    { name: "Checked", active: true },
    { name: "Published", done: false },
  ];

  return (
    <div className="relative mx-auto w-full max-w-sm select-none">
      <div className="border-cc-card-border bg-cc-card-bg/60 rounded-2xl border p-5 backdrop-blur-sm">
        <div className="flex flex-wrap items-center justify-center gap-1">
          <Chip active>CreateReview</Chip>
          <Arrow />
          <Chip>handler</Chip>
          <Arrow />
          <Chip>ReviewCreated</Chip>
        </div>

        <div className="border-cc-card-border mt-4 border-t pt-4">
          <p className="text-cc-nav-label font-mono text-[0.55rem] tracking-[0.15em] uppercase">
            saga
          </p>
          <div className="mt-2 flex items-center justify-center gap-1">
            {states.map((s, i) => (
              <span key={s.name} className="flex items-center">
                {i > 0 && <Arrow />}
                <span
                  className={[
                    "rounded-md border px-2 py-1 font-mono text-[0.6rem]",
                    s.active
                      ? "border-cc-accent/60 text-cc-accent"
                      : s.done
                        ? "border-cc-card-border text-cc-ink-dim"
                        : "border-cc-ink-faint text-cc-ink-dim border-dashed",
                  ].join(" ")}
                >
                  {s.name}
                </span>
              </span>
            ))}
          </div>
        </div>

        <div className="border-cc-card-border mt-4 space-y-1.5 border-t pt-4">
          {[
            { label: "dispatch", left: 0, width: 30 },
            { label: "validate", left: 26, width: 44 },
            { label: "publish", left: 66, width: 28 },
          ].map((s) => (
            <div key={s.label} className="flex items-center gap-2">
              <span className="text-cc-ink-dim w-16 shrink-0 text-right font-mono text-[0.55rem]">
                {s.label}
              </span>
              <span className="bg-cc-surface relative h-2 flex-1 overflow-hidden rounded-full">
                <span
                  className="bg-cc-accent absolute top-0 h-full rounded-full opacity-70"
                  style={{ left: `${s.left}%`, width: `${s.width}%` }}
                />
              </span>
            </div>
          ))}
        </div>

        <p className="text-cc-nav-label mt-4 text-center font-mono text-[0.55rem] tracking-[0.12em] uppercase">
          RabbitMQ &middot; Postgres &middot; in-process
        </p>
      </div>
    </div>
  );
}

/**
 * Scene 4 visual: the safe-change pipeline. Agent patch -> registry validation ->
 * published operations affected -> CI feedback.
 */
function ChangePipeline() {
  const steps = [
    "agent patch",
    "registry validate",
    "published ops affected",
    "CI feedback",
  ];

  return (
    <div className="relative mx-auto w-full max-w-sm select-none">
      <div className="border-cc-card-border bg-cc-card-bg/60 rounded-2xl border p-5 backdrop-blur-sm">
        <div className="flex items-center justify-between">
          <p className="text-cc-nav-label font-mono text-[0.58rem] tracking-[0.15em] uppercase">
            pull request
          </p>
          <span className="border-cc-accent/50 text-cc-accent rounded-full border px-2 py-0.5 font-mono text-[0.55rem] tracking-[0.1em] uppercase">
            guarded
          </span>
        </div>

        <ol className="mt-4 space-y-2">
          {steps.map((step, i) => (
            <li
              key={step}
              className="border-cc-card-border bg-cc-surface flex items-center gap-3 rounded-lg border px-3 py-2.5"
            >
              <span className="text-cc-nav-label w-4 shrink-0 text-center font-mono text-[0.6rem]">
                {i + 1}
              </span>
              <span className="text-cc-ink flex-1 text-sm">{step}</span>
              {i < 2 && (
                <span className="text-cc-accent shrink-0">
                  <CheckIcon />
                </span>
              )}
            </li>
          ))}
        </ol>

        <div className="border-cc-ink-faint mt-4 border-t border-dashed pt-3">
          <p className="text-cc-ink-dim text-center text-xs">
            unsafe edits become actionable feedback
          </p>
        </div>
      </div>
    </div>
  );
}

const VISUALS: Record<string, ReactNode> = {
  build: <BuildLoop />,
  feedback: <AgentFeedbackLoop />,
  observe: <ServiceTopology />,
  workflows: <WorkflowFlow />,
  guardrails: <ChangePipeline />,
};

function LearnMore({ href }: { readonly href: string }) {
  return (
    <a
      href={href}
      className="text-cc-accent hover:text-cc-accent-hover mt-6 inline-flex items-center gap-1.5 text-sm font-medium transition-colors"
    >
      Learn more
      <span aria-hidden="true">&rarr;</span>
    </a>
  );
}

function SceneRow({
  scene,
  flip,
}: {
  readonly scene: SceneContent;
  readonly flip: boolean;
}) {
  return (
    <RevealOnScroll className="border-cc-card-border bg-cc-card-bg/60 hover:border-cc-card-border-hover grid grid-cols-1 items-center gap-8 rounded-3xl border p-6 backdrop-blur-sm sm:p-8 lg:grid-cols-2 lg:gap-12 lg:p-10">
      <div className={flip ? "lg:order-2" : "lg:order-1"}>
        {VISUALS[scene.key]}
      </div>

      <div className={flip ? "lg:order-1" : "lg:order-2"}>
        <span className="text-cc-nav-label font-mono text-xs tracking-[0.15em] uppercase">
          {scene.label}
        </span>
        <h3 className="font-heading text-cc-heading text-h4 sm:text-h3 mt-4 leading-[1.1] font-semibold text-balance">
          {scene.headline}
        </h3>
        <p className="text-cc-ink mt-5 text-base/relaxed text-pretty">
          {scene.blurb}
        </p>
        <LearnMore href={scene.learnMore} />
      </div>
    </RevealOnScroll>
  );
}

/** Compact, honest "generated/validated" proof strip after the scenes. */
function ProofStrip() {
  const chips = [
    "Source-generated server",
    "Published client operations",
    "Nitro telemetry",
    "Mocha CQRS handlers",
    "Nitro registry checks",
  ];

  return (
    <div className="mx-auto mt-16 max-w-3xl text-center">
      <p className="text-cc-heading font-heading text-lg font-semibold">
        Built for people, agents, and the feedback loops between them.
      </p>
      <div className="mt-5 flex flex-wrap items-center justify-center gap-2">
        {chips.map((chip) => (
          <span
            key={chip}
            className="border-cc-card-border text-cc-ink-dim rounded-full border px-3 py-1.5 font-mono text-[0.6rem] tracking-[0.08em] uppercase"
          >
            {chip}
          </span>
        ))}
      </div>
    </div>
  );
}

/**
 * Act 2: five platform-value scenes: build loop, agent feedback loop,
 * production visibility, workflow automation, and release guardrails.
 */
export function ScrollScenes() {
  return (
    <section className="mx-auto max-w-7xl px-5 py-16 sm:px-12 sm:py-24">
      <div className="mx-auto max-w-3xl text-center">
        <p className="text-cc-nav-label font-mono text-xs tracking-[0.2em] uppercase">
          API platform
        </p>
        <h2 className="font-heading text-cc-heading text-h3 sm:text-h2 mt-5 leading-[1.1] font-semibold text-balance">
          Fast changes need{" "}
          <span
            className="bg-clip-text text-transparent"
            style={{ backgroundImage: SPECTRUM }}
          >
            feedback loops
          </span>
          .
        </h2>
        <p className="text-cc-ink mt-6 text-base text-pretty sm:text-lg">
          Modern API work moves through code generation, client usage,
          production telemetry, backend workflows, and release checks.
          ChilliCream keeps those feedback loops close so people and agents can
          make changes with context.
        </p>
      </div>

      <div className="mt-14 flex flex-col gap-10 sm:gap-16">
        {SCENES.map((scene, i) => (
          <SceneRow key={scene.key} scene={scene} flip={i % 2 === 1} />
        ))}
      </div>

      <ProofStrip />

      <div className="mt-12 flex flex-wrap items-center justify-center gap-3">
        <SolidButton href="/get-started">Start for Free</SolidButton>
        <OutlineButton href="/docs">Read the Docs</OutlineButton>
      </div>
    </section>
  );
}

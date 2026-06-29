import type { ComponentType } from "react";

import Link from "next/link";

import { RevealOnScroll } from "@/src/components/RevealOnScroll";
import { AgenticIllu } from "@/src/components/home/platform/illustrations/AgenticIllu";
import { BuildIllu } from "@/src/components/home/platform/illustrations/BuildIllu";
import { GuardrailsIllu } from "@/src/components/home/platform/illustrations/GuardrailsIllu";
import { ObserveIllu } from "@/src/components/home/platform/illustrations/ObserveIllu";
import { WorkflowsIllu } from "@/src/components/home/platform/illustrations/WorkflowsIllu";

type TagTone = "neutral" | "safe" | "dangerous" | "breaking";

interface IlluProps {
  readonly className?: string;
}

interface Tag {
  readonly label: string;
  readonly tone: TagTone;
}

interface Row {
  readonly index: string;
  readonly eyebrow: string;
  readonly headline: string;
  readonly blurb: string;
  readonly proofs: readonly [string, string];
  readonly tags: readonly Tag[];
  readonly href: string;
  readonly Illu: ComponentType<IlluProps>;
}

// Every row's full content is rendered at once: this is a static spec sheet, not
// an accordion. Nothing is hidden behind interaction.
const ROWS: readonly Row[] = [
  {
    index: "01",
    eyebrow: "Build loop",
    headline: "Ship from the code that runs it.",
    blurb:
      "Write your API as annotated C#, and the schema, resolvers, batching, and typed clients all stay in step with it, so the contract you publish is the code that answers the request.",
    proofs: [
      "schema, resolvers, and clients stay in step with the code",
      "the contract you publish is the code that answers it",
    ],
    tags: [
      { label: "annotated C#", tone: "neutral" },
      { label: "schema", tone: "neutral" },
      { label: "resolvers", tone: "neutral" },
      { label: "typed clients", tone: "neutral" },
    ],
    href: "/platform/build",
    Illu: BuildIllu,
  },
  {
    index: "02",
    eyebrow: "Agentic coding",
    headline: "Give coding agents a feedback loop.",
    blurb:
      "Ground agents in the operations your clients already use, gate the risky calls, and turn every fast edit into feedback you can trust before it touches production.",
    proofs: [
      "agents grounded in the operations clients already use",
      "risky calls gated before they touch production",
    ],
    tags: [
      { label: "published ops", tone: "neutral" },
      { label: "gated calls", tone: "neutral" },
      { label: "trusted feedback", tone: "neutral" },
    ],
    href: "/platform/agentic-coding",
    Illu: AgenticIllu,
  },
  {
    index: "03",
    eyebrow: "Production view",
    headline: "See what the API is doing.",
    blurb:
      "Nitro turns live traffic into operation metrics, distributed traces, and impact scores, so when latency climbs you debug from evidence instead of starting another dashboard project.",
    proofs: [
      "live traffic becomes operation metrics and traces",
      "impact scores rank what is worth fixing first",
    ],
    tags: [
      { label: "operation metrics", tone: "neutral" },
      { label: "distributed traces", tone: "neutral" },
      { label: "impact scores", tone: "neutral" },
    ],
    href: "/platform/observability",
    Illu: ObserveIllu,
  },
  {
    index: "04",
    eyebrow: "Workflow",
    headline: "Let work continue after the request.",
    blurb:
      "Hand the slow, fan-out, cross-service work to Mocha as commands and events, so the user gets a response now while the rest keeps moving on its own.",
    proofs: [
      "the user gets a response while the rest keeps moving",
      "slow, cross-service work runs as commands and events",
    ],
    tags: [
      { label: "commands", tone: "neutral" },
      { label: "events", tone: "neutral" },
      { label: "fan-out", tone: "neutral" },
    ],
    href: "/platform/workflows",
    Illu: WorkflowsIllu,
  },
  {
    index: "05",
    eyebrow: "Release safety",
    headline: "Change contracts with a safety net.",
    blurb:
      "Every schema change is classified safe, dangerous, or breaking and checked against the clients you have actually published, so unsafe releases stop at the gate, not in production.",
    proofs: [
      "every change classified before it can ship",
      "checked against the clients you have published",
    ],
    tags: [
      { label: "safe", tone: "safe" },
      { label: "dangerous", tone: "dangerous" },
      { label: "breaking", tone: "breaking" },
    ],
    href: "/platform/release-safety",
    Illu: GuardrailsIllu,
  },
];

const TAG_TONE: Record<TagTone, string> = {
  neutral: "border-cc-card-border text-cc-ink-dim",
  safe: "border-cc-status-healthy/40 text-cc-status-healthy",
  dangerous: "border-cc-status-investigating/40 text-cc-status-investigating",
  breaking: "border-cc-status-firing/40 text-cc-status-firing",
};

function CheckGlyph() {
  return (
    <svg
      viewBox="0 0 16 16"
      fill="none"
      stroke="currentColor"
      strokeWidth="1.75"
      strokeLinecap="round"
      strokeLinejoin="round"
      aria-hidden="true"
      className="text-cc-accent mt-0.5 size-3.5 shrink-0"
    >
      <path d="M3.5 8.5l3 3 6-7" />
    </svg>
  );
}

function SpecRow({ row }: { readonly row: Row }) {
  const { Illu } = row;

  return (
    <div className="border-cc-card-border grid grid-cols-1 items-center gap-8 border-t p-6 sm:p-8 lg:grid-cols-[1.3fr_1fr]">
      {/* spec entry */}
      <div>
        <div className="flex items-baseline gap-3">
          <span className="text-cc-nav-label font-mono text-sm tracking-[0.12em] tabular-nums">
            {row.index}
          </span>
          <span
            aria-hidden="true"
            className="bg-cc-card-border h-px w-6 flex-none"
          />
          <span className="text-cc-nav-label font-mono text-[0.65rem] tracking-[0.12em] uppercase">
            {row.eyebrow}
          </span>
        </div>

        <h3 className="font-heading text-cc-heading text-h5 mt-3 leading-[1.1] font-semibold text-balance">
          {row.headline}
        </h3>

        <p className="text-cc-ink mt-3 max-w-2xl text-sm/relaxed text-pretty sm:text-base/relaxed">
          {row.blurb}
        </p>

        <ul className="mt-4 space-y-2">
          {row.proofs.map((proof) => (
            <li key={proof} className="flex items-start gap-2.5">
              <CheckGlyph />
              <span className="text-cc-ink-dim font-mono text-xs/relaxed">
                {proof}
              </span>
            </li>
          ))}
        </ul>

        <div className="mt-5 flex flex-wrap items-center gap-1.5">
          {row.tags.map((tag) => (
            <span
              key={tag.label}
              className={[
                "rounded-md border px-2 py-0.5 font-mono text-[0.6rem] tracking-[0.08em] whitespace-nowrap",
                TAG_TONE[tag.tone],
              ].join(" ")}
            >
              {tag.label}
            </span>
          ))}
        </div>

        <Link
          href={row.href}
          className="text-cc-accent hover:text-cc-accent-hover mt-5 inline-flex items-center gap-1.5 text-sm font-medium transition-colors"
        >
          Open {row.eyebrow}
          <span aria-hidden="true">&rarr;</span>
        </Link>
      </div>

      {/* topic illustration */}
      <div className="lg:justify-self-end">
        <Illu className="w-full max-w-sm" />
      </div>
    </div>
  );
}

/**
 * Platform section, take v4 "Spec Sheet". A single framed specification table
 * whose five capability rows are all expanded at once: index, eyebrow,
 * headline, blurb, two proof bullets, a tag row, a jump link, and the topic
 * illustration. Static and scannable, with no tabs, stepper, or accordion.
 */
export function PlatformSectionV4() {
  return (
    <section className="mx-auto max-w-7xl px-5 pt-16 sm:px-12 sm:pt-24">
      <RevealOnScroll>
        <div className="max-w-3xl">
          <p className="text-cc-nav-label font-mono text-xs tracking-[0.2em] uppercase">
            The platform
          </p>
          <h2 className="font-heading text-cc-heading text-h3 sm:text-h2 mt-5 leading-[1.1] font-semibold text-balance">
            One platform around the whole API loop.
          </h2>
          <p className="text-cc-ink mt-6 max-w-3xl text-base text-pretty sm:text-lg">
            From the code you write, to the agents that extend it, the telemetry
            that watches it, the workflows behind it, and the checks that keep
            releases safe, ChilliCream keeps every feedback loop close. Here is
            the whole loop, before you pick a plan.
          </p>
        </div>

        <div className="border-cc-card-border bg-cc-card-bg/60 mt-12 overflow-hidden rounded-2xl border backdrop-blur-sm">
          {/* mono header bar */}
          <div className="border-cc-card-border flex items-center justify-between gap-4 border-b px-6 py-4 sm:px-8">
            <span className="text-cc-nav-label font-mono text-[0.65rem] tracking-[0.16em] uppercase">
              Platform / Specification
            </span>
            <span className="text-cc-ink-dim font-mono text-[0.65rem] tracking-[0.16em] uppercase">
              5 capabilities
            </span>
          </div>

          {/* the first row carries no top border so it sits flush under the bar */}
          <div className="[&>div:first-child]:border-t-0">
            {ROWS.map((row) => (
              <SpecRow key={row.index} row={row} />
            ))}
          </div>
        </div>
      </RevealOnScroll>
    </section>
  );
}

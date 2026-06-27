import type { Metadata } from "next";
import Link from "next/link";
import type { ReactNode } from "react";

export const metadata: Metadata = {
  title: "Combined Sections",
  description:
    "Each topic's three best takes merged into one compacted section.",
  robots: { index: false, follow: false },
};

const SPECTRUM = "linear-gradient(100deg, #16b9e4, #7c92c6, #f0786a)";

interface Take {
  readonly v: number;
  readonly name: string;
  readonly summary: string;
}

const TAKES: readonly Take[] = [
  {
    v: 1,
    name: "Platform",
    summary:
      "The whole API loop in one compact block: build, agentic coding, production view, workflow, and release safety.",
  },
  {
    v: 2,
    name: "Messaging (Mocha)",
    summary:
      "Events, side effects, and the simple-and-scales architecture, compacted into one section.",
  },
  {
    v: 3,
    name: "Agentic coding",
    summary:
      "Review made cheap, the patterns the agent follows, and the any-agent logo wall, in one section.",
  },
  {
    v: 4,
    name: "Governance",
    summary:
      "Break the build, lint the schema, and trace every change, compacted into one section.",
  },
  {
    v: 5,
    name: "Observability",
    summary:
      "Rank by impact, see where time is lost, and follow a symptom to its cause, in one section.",
  },
  {
    v: 6,
    name: "Nitro",
    summary:
      "The cockpit reel plus its surfaces and live gauges, compacted into one Nitro section.",
  },
];

function Eyebrow({ children }: { readonly children: ReactNode }) {
  return (
    <p className="text-cc-nav-label font-mono text-[0.6rem] tracking-[0.22em] uppercase">
      {children}
    </p>
  );
}

export default function CombinedSectionHubPage() {
  return (
    <div className="mx-auto flex max-w-6xl flex-col gap-16 px-5 py-16 sm:px-12 sm:py-24">
      <header>
        <Eyebrow>Internal · combined sections</Eyebrow>
        <h1 className="font-heading text-h2 text-cc-heading mt-5 font-semibold tracking-tight">
          Combined{" "}
          <span
            className="bg-clip-text text-transparent"
            style={{ backgroundImage: SPECTRUM }}
          >
            Sections
          </span>
        </h1>
        <p className="text-cc-ink mt-6 max-w-2xl text-[1.1rem] leading-relaxed">
          Each topic&apos;s three best takes merged into one compacted section,
          one shared header and the three facets shrunk to fit. Previewed full
          width with the real chrome and pricing below.
        </p>
      </header>

      <section className="grid gap-5 md:grid-cols-3">
        {TAKES.map((take) => (
          <Link
            key={take.v}
            href={`/combined-section/v${take.v}`}
            className="group border-cc-card-border bg-cc-card-bg hover:border-cc-accent flex flex-col gap-5 rounded-xl border p-6 no-underline backdrop-blur-sm transition-colors"
          >
            <div className="flex items-center gap-3">
              <span className="border-cc-card-border bg-cc-surface text-cc-heading flex h-9 w-9 shrink-0 items-center justify-center rounded-full border font-mono text-[0.82rem] font-semibold tabular-nums">
                v{take.v}
              </span>
              <Eyebrow>Section {take.v}</Eyebrow>
            </div>
            <p className="text-cc-heading group-hover:text-cc-accent font-heading text-h5 font-semibold tracking-tight transition-colors">
              {take.name}
            </p>
            <p className="text-cc-ink text-[0.95rem] leading-relaxed">
              {take.summary}
            </p>
            <span className="text-cc-accent mt-auto text-[0.82rem] font-medium">
              Open section →
            </span>
          </Link>
        ))}
      </section>
    </div>
  );
}

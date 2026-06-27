import type { Metadata } from "next";
import Link from "next/link";
import type { ReactNode } from "react";

export const metadata: Metadata = {
  title: "Platform Section Takes",
  description:
    "Five completely different designs of the landing-page platform section that sits above pricing.",
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
    name: "Live Console",
    summary:
      "One Nitro-style framed panel: a left rail of the five topics, a right console pane that swaps a small live-looking artifact per topic. Stateful, single viewport.",
  },
  {
    v: 2,
    name: "The Loop",
    summary:
      "The five topics strung on one horizontal rail as the developer loop; click a stop to render its detail beneath. The horizontal-axis take.",
  },
  {
    v: 3,
    name: "Bento",
    summary:
      "An asymmetric mosaic with one large hero tile and four mixed-span tiles, breaking the site's uniform equal-grid habit.",
  },
  {
    v: 4,
    name: "Spec Ledger",
    summary:
      "Five calm full-width rows that expand in place, one at a time, like a technical spec's section list. Tight footprint right above pricing.",
  },
  {
    v: 5,
    name: "Index 01-05",
    summary:
      "A typeset numbered table of contents with dotted leaders to each destination route. Pure type, no cards, the lightest jump-off block.",
  },
];

function Eyebrow({ children }: { readonly children: ReactNode }) {
  return (
    <p className="text-cc-nav-label font-mono text-[0.6rem] tracking-[0.22em] uppercase">
      {children}
    </p>
  );
}

export default function PlatformSectionHubPage() {
  return (
    <div className="mx-auto flex max-w-6xl flex-col gap-16 px-5 py-16 sm:px-12 sm:py-24">
      <header>
        <Eyebrow>Internal · landing section</Eyebrow>
        <h1 className="font-heading text-h2 text-cc-heading mt-5 font-semibold tracking-tight">
          Platform Section{" "}
          <span
            className="bg-clip-text text-transparent"
            style={{ backgroundImage: SPECTRUM }}
          >
            Takes
          </span>
        </h1>
        <p className="text-cc-ink mt-6 max-w-2xl text-[1.1rem] leading-relaxed">
          Five completely different designs of the section that sits above
          pricing on the landing page. Each presents the same five platform
          topics as jump-off points into the rest of the site. Every take is
          previewed full width with the real chrome and the pricing section
          right below it.
        </p>
      </header>

      <section className="grid gap-5 md:grid-cols-2 lg:grid-cols-3">
        {TAKES.map((take) => (
          <Link
            key={take.v}
            href={`/platform-section/v${take.v}`}
            className="group border-cc-card-border bg-cc-card-bg hover:border-cc-accent flex flex-col gap-5 rounded-xl border p-6 no-underline backdrop-blur-sm transition-colors"
          >
            <div className="flex items-center gap-3">
              <span className="border-cc-card-border bg-cc-surface text-cc-heading flex h-9 w-9 shrink-0 items-center justify-center rounded-full border font-mono text-[0.82rem] font-semibold tabular-nums">
                v{take.v}
              </span>
              <Eyebrow>Take {take.v}</Eyebrow>
            </div>
            <p className="text-cc-heading group-hover:text-cc-accent font-heading text-h5 font-semibold tracking-tight transition-colors">
              {take.name}
            </p>
            <p className="text-cc-ink text-[0.95rem] leading-relaxed">
              {take.summary}
            </p>
            <span className="text-cc-accent mt-auto text-[0.82rem] font-medium">
              Open take →
            </span>
          </Link>
        ))}
      </section>
    </div>
  );
}

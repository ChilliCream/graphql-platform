import type { Metadata } from "next";
import Link from "next/link";
import type { ReactNode } from "react";

export const metadata: Metadata = {
  title: "Nitro Section Takes",
  description:
    "Three landing takes of the Nitro section that sits after Different Protocols, above pricing.",
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
    name: "The platform, with wheels attached",
    summary:
      "One big app window: the NitroReel cockpit cycling Author, Observe, Diagnose, Schema, and Fusion. The marquee take.",
  },
  {
    v: 2,
    name: "One app, every surface",
    summary:
      "Four small looping surfaces side by side: the GraphQL IDE, telemetry, schema registry, and the Fusion query plan.",
  },
  {
    v: 3,
    name: "Your platform, with the gauges on",
    summary:
      "One live dashboard breathing in real time: latency, throughput, traces, and schema-change signals.",
  },
];

function Eyebrow({ children }: { readonly children: ReactNode }) {
  return (
    <p className="text-cc-nav-label font-mono text-[0.6rem] tracking-[0.22em] uppercase">
      {children}
    </p>
  );
}

export default function NitroSectionHubPage() {
  return (
    <div className="mx-auto flex max-w-6xl flex-col gap-16 px-5 py-16 sm:px-12 sm:py-24">
      <header>
        <Eyebrow>Internal · nitro section</Eyebrow>
        <h1 className="font-heading text-h2 text-cc-heading mt-5 font-semibold tracking-tight">
          Nitro{" "}
          <span
            className="bg-clip-text text-transparent"
            style={{ backgroundImage: SPECTRUM }}
          >
            Takes
          </span>
        </h1>
        <p className="text-cc-ink mt-6 max-w-2xl text-[1.1rem] leading-relaxed">
          Three takes of the Nitro section that sits after Different Protocols
          and above pricing. Nitro is the app you drive the platform from: the
          GraphQL IDE, telemetry, registry, and Fusion management in one place.
          Each take reuses the vendored Nitro animations. Previewed full width
          with the real chrome and pricing below.
        </p>
      </header>

      <section className="grid gap-5 md:grid-cols-3">
        {TAKES.map((take) => (
          <Link
            key={take.v}
            href={`/nitro-section/v${take.v}`}
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

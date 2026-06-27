import type { Metadata } from "next";
import Link from "next/link";

export const metadata: Metadata = {
  title: "Messaging Graphic",
  description: "Three candidates for the Mocha messaging-flow graphic.",
  robots: { index: false, follow: false },
};

const VERSIONS = [
  { v: 1, name: "Centered flow" },
  { v: 2, name: "Left-to-right pipeline" },
  { v: 3, name: "Numbered bands" },
  { v: 4, name: "Message bus" },
  { v: 5, name: "Broker hub" },
];

export default function MessagingGraphicHubPage() {
  return (
    <div className="mx-auto flex max-w-6xl flex-col gap-12 px-5 py-16 sm:px-12 sm:py-24">
      <header>
        <p className="text-cc-nav-label font-mono text-[0.6rem] tracking-[0.22em] uppercase">
          Internal · messaging graphic
        </p>
        <h1 className="font-heading text-h2 text-cc-heading mt-5 font-semibold tracking-tight">
          Messaging flow graphic
        </h1>
        <p className="text-cc-ink mt-6 max-w-2xl text-[1.1rem] leading-relaxed">
          Three candidates for the redesigned Mocha messaging-flow graphic, each
          a different layout of the same flow: request to 200 OK, emit,
          pluggable transport, the messaging patterns, and the saga.
        </p>
      </header>

      <section className="grid gap-5 md:grid-cols-3">
        {VERSIONS.map((version) => (
          <Link
            key={version.v}
            href={`/messaging-graphic/v${version.v}`}
            className="group border-cc-card-border bg-cc-card-bg hover:border-cc-accent flex flex-col gap-4 rounded-xl border p-6 no-underline backdrop-blur-sm transition-colors"
          >
            <span className="border-cc-card-border bg-cc-surface text-cc-heading flex h-9 w-9 items-center justify-center rounded-full border font-mono text-[0.82rem] font-semibold tabular-nums">
              v{version.v}
            </span>
            <p className="text-cc-heading group-hover:text-cc-accent font-heading text-h5 font-semibold tracking-tight transition-colors">
              {version.name}
            </p>
            <span className="text-cc-accent mt-auto text-[0.82rem] font-medium">
              Open graphic →
            </span>
          </Link>
        ))}
      </section>
    </div>
  );
}

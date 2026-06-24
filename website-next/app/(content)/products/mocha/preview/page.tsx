import type { Metadata } from "next";
import NextLink from "next/link";

export const metadata: Metadata = {
  title: "Mocha Page Variations",
  description: "Preview variations for the ChilliCream Mocha product page.",
  robots: { index: false, follow: false },
};

/* -------------------------------------------------------------------------- */
/*  Manifest                                                                   */
/*  Each variation is verified to exist under                                  */
/*  app/(content)/products/mocha/preview/v<n>/page.tsx. `present` records that */
/*  check; a missing variation renders a disabled placeholder row.             */
/* -------------------------------------------------------------------------- */

interface Variation {
  readonly slug: "v1" | "v2" | "v3" | "v4" | "v5" | "v6" | "v7" | "v8" | "v9";
  readonly stance: string;
  readonly summary: string;
  readonly accent: string;
  readonly present: boolean;
}

const VARIATIONS: readonly Variation[] = [
  {
    slug: "v1",
    stance: "Mediator-AND-Bus Catalogue",
    summary:
      "Mocha framed as a single framework that catalogues every shape, in-process mediator and cross-service bus, side by side.",
    accent: "#16b9e4",
    present: true,
  },
  {
    slug: "v2",
    stance: "Message-Topology Story",
    summary:
      "Mocha told as a story about message flow: handlers, sagas, outbox, and inbox traced across services in one topology.",
    accent: "#7c92c6",
    present: true,
  },
  {
    slug: "v3",
    stance: "Open-Modern Positioning",
    summary:
      "Mocha as the open, modern .NET messaging stack: open source, OpenTelemetry-first, and built for current runtime defaults.",
    accent: "#f0786a",
    present: true,
  },
  {
    slug: "v4",
    stance: "The Constellation",
    summary:
      "Mocha as a visual-hero layout where handlers, sagas, outbox, and inbox map out as a constellation of connected primitives.",
    accent: "var(--cc-accent)",
    present: true,
  },
  {
    slug: "v5",
    stance: "The Dispatch Essay",
    summary:
      "Mocha read as a centered narrative essay on dispatch, with prose-led sections that walk through messaging in .NET, one beat at a time.",
    accent: "var(--cc-accent)",
    present: true,
  },
  {
    slug: "v6",
    stance: "Order Up",
    summary:
      "Mocha staged as a barista counter: requests come in, handlers prepare them, and sagas, outbox, and inbox keep the queue flowing.",
    accent: "var(--cc-accent)",
    present: true,
  },
  {
    slug: "v7",
    stance: "Message in Flight",
    summary:
      "Mocha shown as a motion showcase, with messages traced in flight across handlers, sagas, and the outbox and inbox in continuous animation.",
    accent: "var(--cc-accent)",
    present: true,
  },
  {
    slug: "v8",
    stance: "Signal on the Wire",
    summary:
      "Mocha read as signal on the wire, with the mediator, bus, validated sagas, and the outbox and inbox traced as one waveform across every hop.",
    accent: "var(--cc-accent)",
    present: true,
  },
  {
    slug: "v9",
    stance: "Tilt: a pinball table for messages",
    summary:
      "Mocha staged as a pinball table, where commands bounce through the mediator, bus, sagas, and the outbox and inbox like a ball in play.",
    accent: "#f0786a",
    present: true,
  },
];

/* -------------------------------------------------------------------------- */
/*  Brand spectrum: the single gradient event on this screen                   */
/* -------------------------------------------------------------------------- */

const SPECTRUM = "linear-gradient(100deg, #16b9e4, #7c92c6, #f0786a)";

/* -------------------------------------------------------------------------- */
/*  Page                                                                       */
/* -------------------------------------------------------------------------- */

export default function MochaPreviewIndexPage() {
  return (
    <div className="py-6">
      <header className="grid gap-10 md:grid-cols-[minmax(0,1fr)_auto] md:items-end">
        <div>
          <p className="text-cc-nav-label font-mono text-[0.65rem] tracking-[0.22em] uppercase">
            Products / Mocha / Preview
          </p>
          <h1 className="font-heading text-cc-heading text-hero mt-4 font-semibold tracking-tight">
            Nine stances on the same{" "}
            <span
              className="bg-clip-text text-transparent"
              style={{ backgroundImage: SPECTRUM }}
            >
              Mocha
            </span>{" "}
            product page.
          </h1>
          <p className="lead text-cc-ink mt-6 max-w-2xl">
            Internal preview, not indexed. Pick a variation to see how the same
            framework reads when the page takes a different posture.
          </p>
        </div>
        <ul className="text-cc-ink-dim flex flex-col gap-1 font-mono text-[0.72rem] tracking-tight md:text-right">
          <li>v1 / Mediator-AND-Bus Catalogue</li>
          <li>v2 / Message-Topology Story</li>
          <li>v3 / Open-Modern Positioning</li>
          <li>v4 / The Constellation</li>
          <li>v5 / The Dispatch Essay</li>
          <li>v6 / Order Up</li>
          <li>v7 / Message in Flight</li>
          <li>v8 / Signal on the Wire</li>
          <li>v9 / Tilt: a pinball table for messages</li>
        </ul>
      </header>

      <div
        aria-hidden="true"
        className="mt-12 h-px w-full"
        style={{
          background:
            "linear-gradient(90deg, transparent, rgba(94,234,212,0.35), transparent)",
        }}
      />

      <section className="mt-12 flex flex-col gap-5">
        {VARIATIONS.map((variation, index) => (
          <VariationRow
            key={variation.slug}
            variation={variation}
            index={index}
            total={VARIATIONS.length}
          />
        ))}
      </section>
    </div>
  );
}

/* -------------------------------------------------------------------------- */
/*  Variation row                                                              */
/* -------------------------------------------------------------------------- */

interface VariationRowProps {
  readonly variation: Variation;
  readonly index: number;
  readonly total: number;
}

function VariationRow({ variation, index, total }: VariationRowProps) {
  const counter = `${String(index + 1).padStart(2, "0")} / ${String(total).padStart(2, "0")}`;

  if (!variation.present) {
    return (
      <div className="border-cc-card-border bg-cc-card-bg/40 grid gap-6 rounded-2xl border border-dashed p-6 md:grid-cols-[6rem_minmax(0,1fr)_auto] md:items-center md:p-7">
        <div className="flex items-center gap-3 md:flex-col md:items-start md:gap-2">
          <span className="border-cc-card-border text-cc-nav-label flex h-10 w-10 items-center justify-center rounded-full border border-dashed font-mono text-[0.78rem] tabular-nums">
            {variation.slug.toUpperCase()}
          </span>
          <span className="text-cc-nav-label font-mono text-[0.6rem] tracking-[0.22em] uppercase">
            {counter}
          </span>
        </div>
        <div>
          <p className="font-heading text-cc-ink-dim text-h4 font-semibold tracking-tight">
            {variation.stance}
          </p>
          <p className="text-cc-ink-dim mt-2 text-[0.95rem] leading-relaxed">
            {variation.summary}
          </p>
        </div>
        <span className="text-cc-nav-label font-mono text-[0.65rem] tracking-[0.18em] uppercase">
          Not generated
        </span>
      </div>
    );
  }

  return (
    <NextLink
      href={`/products/mocha/preview/${variation.slug}`}
      className="group border-cc-card-border bg-cc-card-bg hover:border-cc-card-border-hover relative grid gap-6 overflow-hidden rounded-2xl border p-6 no-underline transition-colors md:grid-cols-[6rem_minmax(0,1fr)_auto] md:items-center md:p-7"
    >
      <span
        aria-hidden="true"
        className="absolute inset-y-0 left-0 w-px opacity-70"
        style={{
          background: `linear-gradient(180deg, transparent, ${variation.accent}, transparent)`,
        }}
      />
      <div className="flex items-center gap-3 md:flex-col md:items-start md:gap-2">
        <span
          className="border-cc-card-border bg-cc-surface text-cc-heading flex h-10 w-10 items-center justify-center rounded-full border font-mono text-[0.78rem] font-semibold tabular-nums"
          style={{ boxShadow: `inset 0 0 0 1px ${variation.accent}33` }}
        >
          {variation.slug.toUpperCase()}
        </span>
        <span className="text-cc-nav-label font-mono text-[0.6rem] tracking-[0.22em] uppercase">
          {counter}
        </span>
      </div>
      <div>
        <h2 className="font-heading text-cc-heading group-hover:text-cc-accent text-h4 font-semibold tracking-tight transition-colors">
          {variation.stance}
        </h2>
        <p className="text-cc-ink mt-2 text-[0.95rem] leading-relaxed">
          {variation.summary}
        </p>
        <span className="text-cc-ink-dim mt-3 inline-block font-mono text-[0.66rem] tracking-tight">
          /products/mocha/preview/{variation.slug}
        </span>
      </div>
      <span className="text-cc-accent group-hover:text-cc-heading inline-flex items-center gap-2 font-mono text-[0.72rem] tracking-[0.18em] uppercase transition-colors">
        Open variation
        <ArrowGlyph />
      </span>
    </NextLink>
  );
}

function ArrowGlyph() {
  return (
    <svg aria-hidden="true" viewBox="0 0 16 8" className="h-2 w-4" fill="none">
      <path
        d="M1 4 H14 M10 1 L14 4 L10 7"
        stroke="currentColor"
        strokeWidth="1"
        strokeLinecap="round"
        strokeLinejoin="round"
      />
    </svg>
  );
}

import type { Metadata } from "next";
import type { ComponentType, CSSProperties, ReactNode } from "react";

import { RevealOnScroll } from "@/src/components/RevealOnScroll";
import { OutlineButton, SolidButton } from "@/src/design-system/Button";
import { CoffeeTray } from "@/src/icons/CoffeeTray";
import { DripBrewer } from "@/src/icons/DripBrewer";
import { Espresso } from "@/src/icons/Espresso";
import { FrenchPress } from "@/src/icons/FrenchPress";
import { PourOver } from "@/src/icons/PourOver";
import {
  NitroDiagnose,
  NitroFusion,
  NitroMonitoring,
  NitroReel,
  NitroSchema,
  NitroTrace,
} from "@/src/nitro";

export const metadata: Metadata = {
  title: "Nitro: the Cold Brew Control Bar for GraphQL",
  description:
    "Nitro is ChilliCream's GraphQL control plane, a cold brew control bar where every operation is poured, traced, diagnosed, and quality checked before it leaves.",
  keywords: [
    "Nitro",
    "Nitro GraphQL control plane",
    "GraphQL IDE",
    "GraphQL control plane",
    "OpenTelemetry",
    "distributed tracing",
    "schema registry",
    "Fusion gateway",
    "API observability",
    "ChilliCream",
    ".NET observability",
  ],
  robots: { index: false, follow: false },
  openGraph: {
    title: "Nitro: the Cold Brew Control Bar for GraphQL",
    description:
      "Behind the bar at ChilliCream. Pour, trace, diagnose, and evolve your GraphQL and .NET APIs from one cockpit, OpenTelemetry native and schema safe.",
    type: "website",
  },
};

// Brand spectrum gradient, used exactly once on this page (hero hairline).
const SPECTRUM =
  "linear-gradient(90deg, #16b9e4 0%, #7c92c6 50%, #f0786a 100%)";

interface EyebrowProps {
  readonly children: ReactNode;
}

function Eyebrow({ children }: EyebrowProps) {
  return (
    <span className="text-cc-accent text-caption font-medium tracking-[0.2em] uppercase">
      {children}
    </span>
  );
}

interface DrinkIconProps {
  readonly className?: string;
  readonly style?: CSSProperties;
}

interface MenuStop {
  readonly id: string;
  readonly label: string;
  readonly tag: string;
  readonly note: string;
  readonly Icon: ComponentType<DrinkIconProps>;
}

const MENU_STOPS: readonly MenuStop[] = [
  {
    id: "observe",
    label: "Observe",
    tag: "Slow pour",
    note: "Steady read on latency, throughput, and impact.",
    Icon: DripBrewer,
  },
  {
    id: "trace",
    label: "Trace",
    tag: "Follow the stream",
    note: "One request, all the way through the bar.",
    Icon: FrenchPress,
  },
  {
    id: "diagnose",
    label: "Diagnose",
    tag: "Quality control",
    note: "From an error spike to the line that threw it.",
    Icon: PourOver,
  },
  {
    id: "schema",
    label: "Evolve",
    tag: "Re-blend",
    note: "Change the recipe without spoiling the batch.",
    Icon: Espresso,
  },
  {
    id: "fusion",
    label: "Compose",
    tag: "One order",
    note: "Many stations, one cup at the pickup.",
    Icon: CoffeeTray,
  },
];

interface MenuChipProps {
  readonly stop: MenuStop;
  readonly index: number;
}

function MenuChip({ stop, index }: MenuChipProps) {
  const { id, label, tag, note, Icon } = stop;
  const number = String(index + 1).padStart(2, "0");
  return (
    <a
      href={`#${id}`}
      className="border-cc-card-border hover:border-cc-card-border-hover bg-cc-card-bg group relative flex h-full flex-col gap-3 rounded-xl border p-5 transition-colors"
    >
      <div className="flex items-center justify-between">
        <span className="text-cc-ink-dim text-caption font-mono tabular-nums">
          {number}
        </span>
        <Icon className="text-cc-accent h-8 w-8 opacity-80 transition-opacity group-hover:opacity-100" />
      </div>
      <div className="flex flex-col gap-1">
        <span className="text-cc-nav-label text-caption font-mono tracking-[0.18em] uppercase">
          {tag}
        </span>
        <span className="text-cc-heading font-heading text-h6">{label}</span>
        <span className="text-cc-ink-dim text-sm leading-relaxed">{note}</span>
      </div>
    </a>
  );
}

interface ChalkboardStripProps {
  readonly children: ReactNode;
}

/**
 * Single chalkboard style menu strip. Sticks to cc-* tokens (no warm beige or
 * paper), the coffee voice lives in the copy, not the palette.
 */
function ChalkboardStrip({ children }: ChalkboardStripProps) {
  return (
    <div className="border-cc-card-border bg-cc-surface flex items-center gap-4 rounded-lg border px-5 py-3">
      <span className="text-cc-accent text-caption font-mono tracking-[0.22em] uppercase">
        Today&apos;s pour
      </span>
      <span aria-hidden="true" className="bg-cc-card-border h-px flex-1" />
      <span className="text-cc-ink-dim text-caption font-mono">{children}</span>
    </div>
  );
}

interface SteamingCupProps {
  readonly className?: string;
}

/**
 * Single small steaming-cup glyph, used once on the CTA. Inherits currentColor.
 */
function SteamingCup({ className }: SteamingCupProps) {
  return (
    <svg
      viewBox="0 0 64 72"
      fill="none"
      stroke="currentColor"
      strokeWidth={1.6}
      strokeLinecap="round"
      strokeLinejoin="round"
      aria-hidden="true"
      className={className}
    >
      <path d="M 22 6 Q 18 14 22 22" opacity={0.7} />
      <path d="M 32 4 Q 28 14 32 24" opacity={0.7} />
      <path d="M 42 6 Q 38 14 42 22" opacity={0.7} />
      <path d="M 12 30 L 52 30 L 48 60 Q 48 64 44 64 L 20 64 Q 16 64 16 60 Z" />
      <path d="M 52 36 Q 60 36 60 46 Q 60 56 52 56" />
      <line x1="14" y1="38" x2="50" y2="38" opacity={0.4} />
    </svg>
  );
}

interface FeatureSectionProps {
  readonly id: string;
  readonly index: string;
  readonly eyebrow: string;
  readonly title: string;
  readonly body: string;
  readonly visual: ReactNode;
  /** When true, the copy sits on the right and the visual on the left (lg+). */
  readonly reverse?: boolean;
}

/**
 * One brew-step feature beat. Coffee voice rides on the eyebrow string only;
 * the body keeps the real product fact straight.
 */
function FeatureSection({
  id,
  index,
  eyebrow,
  title,
  body,
  visual,
  reverse = false,
}: FeatureSectionProps) {
  return (
    <section
      id={id}
      className="border-cc-card-border scroll-mt-24 border-t py-20 sm:py-28"
    >
      <div className="grid items-center gap-12 lg:grid-cols-12 lg:gap-16">
        <RevealOnScroll
          className={[
            "lg:col-span-5",
            reverse ? "lg:order-2" : "lg:order-1",
          ].join(" ")}
        >
          <div className="flex flex-col gap-5">
            <div className="flex items-center gap-3">
              <span className="text-cc-ink-dim text-caption font-mono tabular-nums">
                {index}
              </span>
              <Eyebrow>{eyebrow}</Eyebrow>
            </div>
            <h2 className="text-cc-heading font-heading text-h3 text-balance">
              {title}
            </h2>
            <p className="text-cc-ink max-w-md text-base leading-relaxed text-pretty sm:text-lg">
              {body}
            </p>
          </div>
        </RevealOnScroll>

        <RevealOnScroll
          className={[
            "lg:col-span-7",
            reverse ? "lg:order-1" : "lg:order-2",
          ].join(" ")}
          hiddenClassName="translate-y-8 opacity-0"
        >
          <FramedVisual>{visual}</FramedVisual>
        </RevealOnScroll>
      </div>
    </section>
  );
}

interface FramedVisualProps {
  readonly children: ReactNode;
}

/** Frames an animated Nitro screen like an embedded product screenshot. */
function FramedVisual({ children }: FramedVisualProps) {
  return (
    <div className="relative">
      <div
        aria-hidden="true"
        className="absolute -inset-x-6 -inset-y-4 -z-10 rounded-[2rem] opacity-40 blur-3xl"
        style={{
          background:
            "radial-gradient(60% 60% at 50% 40%, rgba(94,234,212,0.18), transparent 70%)",
        }}
      />
      <div className="border-cc-card-border bg-cc-surface overflow-hidden rounded-xl border shadow-2xl shadow-black/40">
        {children}
      </div>
    </div>
  );
}

export default function NitroPreviewV6Page() {
  return (
    <>
      {/* HERO ───────────────────────────────────────────────── */}
      <section className="pt-6 pb-16 text-center sm:pt-12">
        <RevealOnScroll className="mx-auto flex max-w-3xl flex-col items-center gap-6">
          <div className="flex flex-col items-center gap-4">
            <span
              aria-hidden="true"
              className="h-px w-24 rounded-full"
              style={{ background: SPECTRUM }}
            />
            <Eyebrow>Behind the bar at ChilliCream</Eyebrow>
          </div>
          <h1 className="text-cc-heading font-heading text-h1 text-balance">
            Your API, in motion.
          </h1>
          <p className="lead text-cc-ink mx-auto max-w-2xl">
            Nitro is the GraphQL control plane for your .NET backend. A steady,
            low-temperature cockpit where every operation is poured, traced, and
            quality checked before it leaves the counter.
          </p>
          <div className="mt-2 flex flex-wrap items-center justify-center gap-4">
            <SolidButton href="/get-started">Start for Free</SolidButton>
            <OutlineButton href="https://nitro.chillicream.com">
              Launch Nitro
            </OutlineButton>
          </div>
        </RevealOnScroll>

        {/* Centerpiece reel, the main station. */}
        <RevealOnScroll
          className="mt-16 sm:mt-20"
          hiddenClassName="translate-y-10 opacity-0 scale-[0.98]"
          shownClassName="translate-y-0 opacity-100 scale-100"
        >
          <div className="relative mx-auto w-full max-w-6xl">
            <div
              aria-hidden="true"
              className="absolute -inset-x-10 -inset-y-8 -z-10 rounded-[2.5rem] opacity-50 blur-3xl"
              style={{
                background:
                  "radial-gradient(50% 50% at 50% 30%, rgba(94,234,212,0.16), transparent 70%)",
              }}
            />
            <div className="border-cc-card-border bg-cc-surface overflow-hidden rounded-2xl border shadow-2xl shadow-black/50">
              <NitroReel />
            </div>
            <p className="text-cc-ink-dim mt-4 text-center text-sm">
              The main station. Five tabs, one bar.
            </p>
          </div>
        </RevealOnScroll>
      </section>

      {/* TODAY'S POUR MENU ──────────────────────────────────── */}
      <section
        aria-label="Today's pour menu"
        className="border-cc-card-border border-t py-16 sm:py-20"
      >
        <RevealOnScroll className="mx-auto flex max-w-6xl flex-col gap-8">
          <div className="flex flex-col gap-3">
            <Eyebrow>On the menu</Eyebrow>
            <h2 className="text-cc-heading font-heading text-h3 text-balance">
              Today&apos;s pour, five stops along the bar.
            </h2>
            <p className="text-cc-ink max-w-2xl text-base leading-relaxed sm:text-lg">
              Each stop is a tab inside Nitro. Same surface, same shortcuts, one
              cockpit for your GraphQL and .NET API.
            </p>
          </div>

          <ChalkboardStrip>
            Observe · Trace · Diagnose · Evolve · Compose
          </ChalkboardStrip>

          <ul className="grid gap-4 sm:grid-cols-2 lg:grid-cols-5">
            {MENU_STOPS.map((stop, index) => (
              <li key={stop.id}>
                <MenuChip stop={stop} index={index} />
              </li>
            ))}
          </ul>
        </RevealOnScroll>
      </section>

      {/* FEATURE BEATS ──────────────────────────────────────── */}
      <FeatureSection
        id="observe"
        index="01"
        eyebrow="Observe . Slow pour, steady read"
        title="See exactly how your API behaves in production."
        body="Wire up Nitro and OpenTelemetry to watch latency, throughput, and error rate per operation, with p95 and p99, per-client usage, and an impact score that ranks what hurts the system most."
        visual={<NitroMonitoring className="w-full" />}
      />

      <FeatureSection
        id="trace"
        index="02"
        eyebrow="Trace . Follow the stream"
        title="Follow one request across your whole backend."
        body="Distributed tracing stitches a single operation across GraphQL, REST, gRPC, and background jobs. Walk the span waterfall down to the resolver that ran slow."
        visual={<NitroTrace className="w-full" />}
        reverse
      />

      <FeatureSection
        id="diagnose"
        index="03"
        eyebrow="Diagnose . Quality control on the bar"
        title="From an error spike to the line that threw it."
        body="When errors climb, Nitro takes you from the spike to the exact failing operation and the server-side stack trace behind it, with no log spelunking required."
        visual={<NitroDiagnose className="w-full" />}
      />

      <FeatureSection
        id="schema"
        index="04"
        eyebrow="Evolve . Re-blend without spoiling the batch"
        title="Change your schema without breaking your clients."
        body="The schema registry classifies every change as safe, dangerous, or breaking and checks it against published clients in CI, so you validate on a PR and publish only when it is safe to ship."
        visual={<NitroSchema className="w-full" />}
        reverse
      />

      <FeatureSection
        id="fusion"
        index="05"
        eyebrow="Compose . One order, many stations"
        title="One graph, executed across every subgraph."
        body="With Fusion, Nitro shows the distributed query plan: how a single operation fans out into parallel, batched fetches across your subgraphs and folds back into one response."
        visual={<NitroFusion className="w-full" />}
      />

      {/* CTA ────────────────────────────────────────────────── */}
      <section className="border-cc-card-border border-t py-24 text-center sm:py-32">
        <RevealOnScroll className="mx-auto flex max-w-2xl flex-col items-center gap-6">
          <SteamingCup className="text-cc-accent h-12 w-12 opacity-90" />
          <Eyebrow>Ready when you are</Eyebrow>
          <h2 className="text-cc-heading font-heading text-h2 text-balance">
            Put your API on the control plane.
          </h2>
          <p className="text-cc-ink max-w-xl text-lg leading-relaxed">
            Start in the GraphQL IDE in seconds, then grow into observability,
            tracing, and a registry that keeps your schema and clients in sync.
          </p>
          <div className="mt-2 flex flex-wrap items-center justify-center gap-4">
            <SolidButton href="/get-started">Start for Free</SolidButton>
            <OutlineButton href="https://nitro.chillicream.com">
              Launch Nitro
            </OutlineButton>
          </div>
        </RevealOnScroll>
      </section>
    </>
  );
}

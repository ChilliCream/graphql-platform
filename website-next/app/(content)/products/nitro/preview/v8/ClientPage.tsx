"use client";

import { motion, useReducedMotion } from "motion/react";
import type { CSSProperties, ReactNode } from "react";

import { OutlineButton, SolidButton } from "@/src/design-system/Button";
import {
  NitroDiagnose,
  NitroFusion,
  NitroMonitoring,
  NitroReel,
  NitroSchema,
  NitroTrace,
} from "@/src/nitro";

// ─── Blueprint Cockpit (v8) ─────────────────────────────────────────────────
// The page is rendered as a top-down architectural floor plan. A faint grid
// substrate sits behind the whole canvas; every section is a "Room" with a
// hairline cc-card-border, a notched corner plaque (mono caps room label),
// and a small "door" gap in the top edge. Motion is enter-view-once draft-pen
// border draws + a slow legend dot pulse. No scroll coupling.

// Brand spectrum, used exactly once on this page (the title underline rule).
const SPECTRUM =
  "linear-gradient(90deg, #16b9e4 0%, #7c92c6 50%, #f0786a 100%)";

// Single ChilliCream accent for the page.
const ACCENT = "#5eead4";

// ─── BLUEPRINT SUBSTRATE ────────────────────────────────────────────────────
// Fixed behind everything: a faint teal grid (fine + coarse) on cc-bg, plus
// two decorative corner "dimension tick" labels. Pointer-events-none so it
// never blocks the page.
function BlueprintSubstrate() {
  const fine = "48px";
  const coarse = "240px";
  const grid: CSSProperties = {
    backgroundImage: [
      // Fine grid (vertical)
      `linear-gradient(to right, rgba(94,234,212,0.04) 1px, transparent 1px)`,
      // Fine grid (horizontal)
      `linear-gradient(to bottom, rgba(94,234,212,0.04) 1px, transparent 1px)`,
      // Coarse grid (vertical)
      `linear-gradient(to right, rgba(94,234,212,0.06) 1px, transparent 1px)`,
      // Coarse grid (horizontal)
      `linear-gradient(to bottom, rgba(94,234,212,0.06) 1px, transparent 1px)`,
    ].join(", "),
    backgroundSize: `${fine} ${fine}, ${fine} ${fine}, ${coarse} ${coarse}, ${coarse} ${coarse}`,
    backgroundPosition: "0 0",
  };
  return (
    <div aria-hidden="true" className="pointer-events-none fixed inset-0 -z-10">
      <div className="absolute inset-0" style={grid} />
      {/* Decorative corner anchors, sheet-style coordinate labels. */}
      <span className="text-cc-nav-label/70 absolute top-4 left-4 font-mono text-[10px] tracking-[0.25em] uppercase">
        A-1
      </span>
      <span className="text-cc-nav-label/70 absolute top-4 right-4 font-mono text-[10px] tracking-[0.25em] uppercase">
        B-2
      </span>
      <span className="text-cc-nav-label/70 absolute bottom-4 left-4 font-mono text-[10px] tracking-[0.25em] uppercase">
        C-3
      </span>
      <span className="text-cc-nav-label/70 absolute right-4 bottom-4 font-mono text-[10px] tracking-[0.25em] uppercase">
        D-4
      </span>
    </div>
  );
}

// ─── ROOM PRIMITIVE ─────────────────────────────────────────────────────────
// A wrapping div with a hairline border drawn by an SVG path (so we can
// stroke-dash animate it on enter-view-once). A notched corner holds the room
// label (mono caps tile sitting in cc-bg). The "door" is a 24px break in the
// top edge with two short vertical ticks.

interface RoomProps {
  readonly label: string;
  readonly number?: string;
  readonly children: ReactNode;
  readonly className?: string;
  /** Width of the doorway gap (CSS px) measured from `doorX`. */
  readonly doorX?: number;
  readonly id?: string;
}

function Room({
  label,
  number,
  children,
  className = "",
  doorX = 120,
  id,
}: RoomProps) {
  const reduce = useReducedMotion();

  return (
    <section
      id={id}
      className={["bg-cc-surface relative scroll-mt-24", className].join(" ")}
    >
      {/* Draft-pen hairline border, drawn as a single SVG rect path that
          animates from strokeDasharray 0 to full length on first reveal. The
          door gap is rendered by overlaying a small bg-cc-surface band that
          masks the top edge between two short ticks. */}
      <svg
        aria-hidden="true"
        className="pointer-events-none absolute inset-0 h-full w-full"
        preserveAspectRatio="none"
        viewBox="0 0 100 100"
      >
        <motion.rect
          x="0.5"
          y="0.5"
          width="99"
          height="99"
          fill="none"
          stroke="rgba(245, 241, 234, 0.16)"
          strokeWidth="0.25"
          vectorEffect="non-scaling-stroke"
          initial={reduce ? false : { pathLength: 0, opacity: 0.4 }}
          whileInView={{ pathLength: 1, opacity: 1 }}
          viewport={{ once: true, amount: 0.15 }}
          transition={{
            duration: reduce ? 0 : 0.6,
            ease: "easeOut",
          }}
        />
      </svg>

      {/* Doorway gap: a small piece of cc-bg covers the top border, flanked by
          two short cc-accent ticks acting as the door jambs. */}
      <div
        aria-hidden="true"
        className="bg-cc-bg absolute top-0 z-[1] h-px"
        style={{ left: doorX, width: 24 }}
      />
      <div
        aria-hidden="true"
        className="bg-cc-accent/60 absolute top-0 z-[1] h-2 w-px"
        style={{ left: doorX }}
      />
      <div
        aria-hidden="true"
        className="bg-cc-accent/60 absolute top-0 z-[1] h-2 w-px"
        style={{ left: doorX + 24 }}
      />

      {/* Notched corner plaque: a small inset cc-bg tile with the room label
          and number in mono caps, sitting at the top-left like an architect's
          door label. */}
      <motion.div
        className="bg-cc-bg border-cc-card-border absolute -top-px -left-px z-[2] flex items-center gap-3 border-r border-b px-3 py-1.5"
        initial={reduce ? false : { opacity: 0, y: 6 }}
        whileInView={{ opacity: 1, y: 0 }}
        viewport={{ once: true, amount: 0.2 }}
        transition={{ duration: reduce ? 0 : 0.35, delay: 0.15 }}
      >
        {number ? (
          <span className="text-cc-accent font-mono text-[10px] tracking-[0.25em] tabular-nums">
            {number}
          </span>
        ) : null}
        <span className="text-cc-nav-label font-mono text-[10px] tracking-[0.3em] uppercase">
          {label}
        </span>
      </motion.div>

      {/* Room interior. Generous padding so labels do not collide with the
          plaque/doorway and visuals breathe inside the hairline walls. */}
      <div className="relative z-0 px-6 py-12 sm:px-10 sm:py-14 lg:px-14 lg:py-16">
        {children}
      </div>
    </section>
  );
}

// ─── CORRIDOR CROSSHAIR ─────────────────────────────────────────────────────
// Small inline SVG "+" joint used between rooms / at corners. Decorative only.

interface CrosshairProps {
  readonly className?: string;
}

function Crosshair({ className = "" }: CrosshairProps) {
  return (
    <svg
      aria-hidden="true"
      viewBox="0 0 16 16"
      className={["text-cc-card-border-hover h-4 w-4", className].join(" ")}
    >
      <line
        x1="8"
        y1="1"
        x2="8"
        y2="15"
        stroke="currentColor"
        strokeWidth="1"
      />
      <line
        x1="1"
        y1="8"
        x2="15"
        y2="8"
        stroke="currentColor"
        strokeWidth="1"
      />
    </svg>
  );
}

// ─── CORRIDOR TICK ──────────────────────────────────────────────────────────
// A single vertical hairline + caption acting as a "corridor" connector
// between two stacked rooms. Decorative.

interface CorridorProps {
  readonly label: string;
}

function Corridor({ label }: CorridorProps) {
  return (
    <div
      aria-hidden="true"
      className="relative flex items-center justify-center py-8"
    >
      <div className="bg-cc-card-border h-12 w-px" />
      <span className="text-cc-nav-label absolute right-1/2 mr-3 font-mono text-[10px] tracking-[0.25em] uppercase">
        {label}
      </span>
      <Crosshair className="absolute -bottom-2" />
    </div>
  );
}

// ─── EYEBROW ────────────────────────────────────────────────────────────────

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

// ─── FRAMED VISUAL ──────────────────────────────────────────────────────────
// Same pattern as v1: each Nitro screen sits in a hairline card so it reads as
// the "occupant" of the room.

interface FramedVisualProps {
  readonly children: ReactNode;
}

function FramedVisual({ children }: FramedVisualProps) {
  return (
    <div className="relative">
      <div
        aria-hidden="true"
        className="absolute -inset-x-4 -inset-y-3 -z-10 rounded-[1.5rem] opacity-30 blur-3xl"
        style={{
          background:
            "radial-gradient(60% 60% at 50% 40%, rgba(94,234,212,0.16), transparent 70%)",
        }}
      />
      <div className="border-cc-card-border bg-cc-surface overflow-hidden rounded-lg border shadow-2xl shadow-black/40">
        {children}
      </div>
    </div>
  );
}

// ─── DIMENSION TICK ─────────────────────────────────────────────────────────
// Architectural-style measurement annotation, e.g. "8.4m" in mono.

interface DimensionTickProps {
  readonly value: string;
  readonly className?: string;
}

function DimensionTick({ value, className = "" }: DimensionTickProps) {
  return (
    <div
      className={[
        "text-cc-nav-label inline-flex items-center gap-2 font-mono text-[10px] tracking-[0.2em] uppercase",
        className,
      ].join(" ")}
    >
      <span className="bg-cc-card-border h-px w-6" />
      <span>{value}</span>
      <span className="bg-cc-card-border h-px w-6" />
    </div>
  );
}

// ─── LEGEND DOT (pulsing) ───────────────────────────────────────────────────
// A small circular swatch. The cc-accent variant pulses continuously (time
// driven, no scroll coupling). Others are static neutral swatches.

interface LegendDotProps {
  readonly color: string;
  readonly pulse?: boolean;
}

function LegendDot({ color, pulse = false }: LegendDotProps) {
  const reduce = useReducedMotion();
  return (
    <motion.span
      aria-hidden="true"
      className="inline-block h-2 w-2 rounded-full"
      style={{ background: color }}
      animate={
        pulse && !reduce
          ? { opacity: [1, 0.4, 1], scale: [1, 1.2, 1] }
          : undefined
      }
      transition={{ duration: 2, repeat: Infinity, ease: "easeInOut" }}
    />
  );
}

// ─── DOOR ARROW ─────────────────────────────────────────────────────────────
// Inline glyph for the Exit room CTA: a rectangle with a small arrow leaving
// through one side.

function DoorArrow() {
  return (
    <svg
      aria-hidden="true"
      viewBox="0 0 24 24"
      className="text-cc-accent h-5 w-5"
      fill="none"
      stroke="currentColor"
      strokeWidth="1.5"
      strokeLinecap="round"
      strokeLinejoin="round"
    >
      <path d="M14 4h5v16h-5" />
      <path d="M3 12h12" />
      <path d="M11 8l4 4-4 4" />
    </svg>
  );
}

// ─── PAGE ───────────────────────────────────────────────────────────────────

export function ClientPage() {
  return (
    <>
      <BlueprintSubstrate />

      <div className="mx-auto w-full max-w-6xl px-4 pt-10 pb-24 sm:px-6 lg:px-8">
        {/* TITLE BLOCK ─────────────────────────────────────────────────── */}
        <header className="mb-10">
          <div className="flex items-baseline justify-between gap-6">
            <div className="flex flex-col gap-2">
              <span className="text-cc-nav-label font-mono text-[10px] tracking-[0.3em] uppercase">
                Floor Plan
              </span>
              <h2 className="text-cc-heading font-heading text-h4 sm:text-h3">
                Nitro Control Plane
              </h2>
            </div>
            <div className="flex flex-col items-end gap-2">
              <span className="text-cc-nav-label font-mono text-[10px] tracking-[0.3em] uppercase">
                Sheet
              </span>
              <span className="text-cc-ink font-mono text-sm tracking-[0.2em] tabular-nums">
                CC-N-001
              </span>
            </div>
          </div>
          {/* The one spectrum use on this page: a 1px brand band under the title. */}
          <div
            aria-hidden="true"
            className="mt-6 h-px w-full rounded-full"
            style={{ background: SPECTRUM }}
          />

          {/* Plan legend strip. Three swatches: safe / dangerous / breaking. */}
          <div className="mt-6 flex flex-wrap items-center gap-x-6 gap-y-2 text-xs">
            <span className="text-cc-nav-label font-mono tracking-[0.25em] uppercase">
              Legend
            </span>
            <span className="text-cc-ink-dim inline-flex items-center gap-2 font-mono tracking-[0.2em] uppercase">
              <LegendDot color={ACCENT} pulse />
              Safe
            </span>
            <span className="text-cc-ink-dim inline-flex items-center gap-2 font-mono tracking-[0.2em] uppercase">
              <LegendDot color="rgba(245, 241, 234, 0.62)" />
              Dangerous
            </span>
            <span className="text-cc-ink-dim inline-flex items-center gap-2 font-mono tracking-[0.2em] uppercase">
              <LegendDot color="#f0786a" />
              Breaking
            </span>
          </div>
        </header>

        {/* LOBBY (HERO) ────────────────────────────────────────────────── */}
        <Room label="Lobby" number="00" doorX={160}>
          <div className="flex flex-col items-center gap-6 text-center">
            <Eyebrow>The Control Plane for GraphQL</Eyebrow>
            <h1 className="text-cc-heading font-heading text-h1 text-balance">
              Your API, in motion.
            </h1>
            <p className="lead text-cc-ink mx-auto max-w-2xl !text-xl !leading-relaxed">
              Nitro is the cockpit for your GraphQL and .NET backend. Author
              operations, watch them run, trace every request, and evolve your
              schema without breaking the clients you ship to.
            </p>
            <div className="mt-2 flex flex-wrap items-center justify-center gap-4">
              <SolidButton href="/get-started">Start for Free</SolidButton>
              <OutlineButton href="https://nitro.chillicream.com">
                Launch Nitro
              </OutlineButton>
            </div>
            <DimensionTick value="12.0m x 4.8m" className="mt-2" />
            <div className="mt-6 w-full">
              <FramedVisual>
                <NitroReel />
              </FramedVisual>
            </div>
          </div>
        </Room>

        {/* CORRIDOR ↓ */}
        <Corridor label="Hall A" />

        {/* ROOM 01 OBSERVE ─────────────────────────────────────────────── */}
        <Room label="Observe" number="01" id="observe" doorX={180}>
          <div className="grid items-center gap-10 lg:grid-cols-12 lg:gap-12">
            <div className="lg:col-span-8">
              <FramedVisual>
                <NitroMonitoring className="w-full" />
              </FramedVisual>
            </div>
            <div className="lg:col-span-4">
              <div className="flex flex-col gap-5">
                <div className="flex items-center gap-3">
                  <span className="text-cc-ink-dim text-caption font-mono tabular-nums">
                    01
                  </span>
                  <Eyebrow>Observe</Eyebrow>
                </div>
                <h2 className="text-cc-heading font-heading text-h3 text-balance">
                  See exactly how your API behaves in production.
                </h2>
                <p className="text-cc-ink max-w-md text-base leading-relaxed text-pretty sm:text-lg">
                  Wire up Nitro and OpenTelemetry to watch latency, throughput,
                  and error rate per operation, with p95 and p99, per-client
                  usage, and an impact score that ranks what hurts the system
                  most.
                </p>
                <DimensionTick value="8.4m" className="mt-2" />
              </div>
            </div>
          </div>
        </Room>

        {/* CORRIDOR ↓ */}
        <Corridor label="Hall B" />

        {/* ROOM 02 TRACE ──────────────────────────────────────────────── */}
        <Room label="Trace" number="02" id="trace" doorX={140}>
          <div className="flex flex-col gap-8">
            <div className="flex flex-col gap-5">
              <div className="flex items-center gap-3">
                <span className="text-cc-ink-dim text-caption font-mono tabular-nums">
                  02
                </span>
                <Eyebrow>Trace</Eyebrow>
              </div>
              <h2 className="text-cc-heading font-heading text-h3 max-w-2xl text-balance">
                Follow one request across your whole backend.
              </h2>
              <p className="text-cc-ink max-w-2xl text-base leading-relaxed text-pretty sm:text-lg">
                Distributed tracing stitches a single operation across GraphQL,
                REST, gRPC, and background jobs. Walk the span waterfall down to
                the resolver that ran slow.
              </p>
            </div>
            <div className="grid items-end gap-8 lg:grid-cols-12">
              <div className="lg:col-span-7">
                <FramedVisual>
                  <NitroTrace className="w-full" />
                </FramedVisual>
              </div>
              <div className="text-cc-ink-dim flex flex-col gap-3 font-mono text-[10px] tracking-[0.2em] uppercase lg:col-span-5">
                <span className="text-cc-nav-label">Span depth</span>
                <DimensionTick value="6 levels" />
                <span className="text-cc-nav-label mt-4">Across</span>
                <DimensionTick value="GraphQL / REST / gRPC" />
              </div>
            </div>
          </div>
        </Room>

        {/* CORRIDOR with junction */}
        <div
          aria-hidden="true"
          className="relative flex items-center justify-center py-8"
        >
          <div className="bg-cc-card-border h-px w-32" />
          <Crosshair />
          <div className="bg-cc-card-border h-px w-32" />
          <span className="text-cc-nav-label absolute -top-1 font-mono text-[10px] tracking-[0.25em] uppercase">
            Junction J-1
          </span>
        </div>

        {/* ROW: ROOM 03 DIAGNOSE (compact) + ROOM 04 EVOLVE ────────────── */}
        <div className="grid gap-8 lg:grid-cols-12">
          {/* ROOM 03 DIAGNOSE ─ compact square */}
          <div className="lg:col-span-6">
            <Room label="Diagnose" number="03" id="diagnose" doorX={120}>
              <div className="flex flex-col gap-6">
                <div className="flex items-center gap-3">
                  <span className="text-cc-ink-dim text-caption font-mono tabular-nums">
                    03
                  </span>
                  <Eyebrow>Diagnose</Eyebrow>
                </div>
                <FramedVisual>
                  <NitroDiagnose className="w-full" />
                </FramedVisual>
                <h2 className="text-cc-heading font-heading text-h4 text-balance">
                  From an error spike to the line that threw it.
                </h2>
                <p className="text-cc-ink text-base leading-relaxed text-pretty">
                  When errors climb, Nitro takes you from the spike to the exact
                  failing operation and the server-side stack trace behind it,
                  with no log spelunking required.
                </p>
              </div>
              {/* SW corner crosshair joint */}
              <Crosshair className="absolute -bottom-2 -left-2 z-[3]" />
            </Room>
          </div>

          {/* ROOM 04 EVOLVE ─ wider, with the typed safe/dangerous/breaking legend */}
          <div className="lg:col-span-6">
            <Room label="Evolve" number="04" id="schema" doorX={140}>
              <div className="grid items-center gap-8 lg:grid-cols-12">
                <div className="lg:col-span-7">
                  <FramedVisual>
                    <NitroSchema className="w-full" />
                  </FramedVisual>
                </div>
                <div className="lg:col-span-5">
                  <div className="flex flex-col gap-5">
                    <div className="flex items-center gap-3">
                      <span className="text-cc-ink-dim text-caption font-mono tabular-nums">
                        04
                      </span>
                      <Eyebrow>Evolve</Eyebrow>
                    </div>
                    <h2 className="text-cc-heading font-heading text-h4 text-balance">
                      Change your schema without breaking your clients.
                    </h2>
                    <p className="text-cc-ink text-base leading-relaxed text-pretty">
                      The schema registry classifies every change as safe,
                      dangerous, or breaking and checks it against published
                      clients in CI, so you validate on a PR and publish only
                      when it is safe to ship.
                    </p>
                    <ul className="text-cc-ink-dim mt-2 flex flex-col gap-2 font-mono text-[10px] tracking-[0.2em] uppercase">
                      <li className="flex items-center gap-2">
                        <LegendDot color={ACCENT} pulse />
                        <span>Safe</span>
                      </li>
                      <li className="flex items-center gap-2">
                        <LegendDot color="rgba(245, 241, 234, 0.62)" />
                        <span>Dangerous</span>
                      </li>
                      <li className="flex items-center gap-2">
                        <LegendDot color="#f0786a" />
                        <span>Breaking (#f0786a)</span>
                      </li>
                    </ul>
                  </div>
                </div>
              </div>
            </Room>
          </div>
        </div>

        {/* CORRIDOR ↓ */}
        <Corridor label="Hall C" />

        {/* ROOM 05 COMPOSE ─────────────────────────────────────────────── */}
        <Room label="Compose" number="05" id="fusion" doorX={200}>
          <div className="grid items-center gap-10 lg:grid-cols-12 lg:gap-12">
            <div className="lg:col-span-4">
              <div className="flex flex-col gap-5">
                <div className="flex items-center gap-3">
                  <span className="text-cc-ink-dim text-caption font-mono tabular-nums">
                    05
                  </span>
                  <Eyebrow>Compose</Eyebrow>
                </div>
                <h2 className="text-cc-heading font-heading text-h3 text-balance">
                  One graph, executed across every subgraph.
                </h2>
                <p className="text-cc-ink max-w-md text-base leading-relaxed text-pretty sm:text-lg">
                  With Fusion, Nitro shows the distributed query plan: how a
                  single operation fans out into parallel, batched fetches
                  across your subgraphs and folds back into one response.
                </p>
                <DimensionTick value="anchor display" />
              </div>
            </div>
            <div className="lg:col-span-8">
              <FramedVisual>
                <NitroFusion className="w-full" />
              </FramedVisual>
            </div>
          </div>
        </Room>

        {/* CORRIDOR ↓ */}
        <Corridor label="To Exit" />

        {/* EXIT ROOM (CTA) ────────────────────────────────────────────── */}
        <Room label="Exit" doorX={100}>
          <div className="flex flex-col items-center gap-6 text-center">
            <div className="flex items-center gap-3">
              <DoorArrow />
              <Eyebrow>Ready when you are</Eyebrow>
            </div>
            <h2 className="text-cc-heading font-heading text-h2 text-balance">
              Put your API on the control plane.
            </h2>
            <p className="text-cc-ink max-w-xl text-lg leading-relaxed">
              Start in the GraphQL IDE in seconds, then grow into observability,
              tracing, and a registry that keeps your schema and clients in
              sync.
            </p>
            <div className="mt-2 flex flex-wrap items-center justify-center gap-4">
              <SolidButton href="/get-started">Start for Free</SolidButton>
              <OutlineButton href="https://nitro.chillicream.com">
                Launch Nitro
              </OutlineButton>
            </div>
          </div>
        </Room>

        {/* SHEET FOOTER STRIP ─────────────────────────────────────────── */}
        <footer className="border-cc-card-border mt-10 border-t pt-4">
          <div className="text-cc-nav-label flex flex-wrap items-center justify-between gap-x-6 gap-y-2 font-mono text-[10px] tracking-[0.3em] uppercase">
            <span>Sheet 1 of 1</span>
            <span>Scale 1:1</span>
            <span>Drawn by ChilliCream</span>
          </div>
        </footer>
      </div>
    </>
  );
}

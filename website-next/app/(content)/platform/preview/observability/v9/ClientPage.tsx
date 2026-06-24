"use client";

import type { ReactNode } from "react";
import { MotionConfig, motion, useReducedMotion } from "motion/react";

import { CheckIcon } from "@/src/components/CheckIcon";
import { OutlineButton, SolidButton } from "@/src/design-system/Button";

/* ------------------------------------------------------------------ *
 * Constellation of Signals.
 *
 * The page treats telemetry as a calm starfield. Each "signal" is a
 * soft bokeh orb (radial-gradient halo + sharp center dot) suspended
 * in the dark. Status is rationed as data (green / amber / coral),
 * teal is the one page accent, and the brand spectrum appears exactly
 * once on the closing CTA hairline.
 * ------------------------------------------------------------------ */

const TEAL = "#5eead4";
const GREEN = "#34d399";
const AMBER = "#fbbf24";
const CORAL = "#f0786a";
const VIOLET = "#7c92c6";
const CYAN = "#16b9e4";

const SPECTRUM = `linear-gradient(100deg, ${CYAN} 0%, ${VIOLET} 52%, ${CORAL} 100%)`;

const EASE: [number, number, number, number] = [0.22, 1, 0.36, 1];

// The trace id is the literal thread between the dashboard and the slow span.
const TRACE_ID = "4b1c8f2a9e07";

export function ClientPage() {
  return (
    <MotionConfig reducedMotion="user" transition={{ ease: EASE }}>
      <main className="flex flex-col gap-28 pb-16">
        <Hero />
        <FourSignalsStrip />
        <TraceOrbitSection />
        <LensesSection />
        <WaterfallSection />
        <HonestySection />
        <ClosingCta />
      </main>
    </MotionConfig>
  );
}

/* ================================================================== *
 * HERO
 * Centered eyebrow, h1, lead, dual CTA. Beneath it a wide "signal
 * field" panel with overlapping bokeh orbs in the brand spectrum at
 * low opacity, with the focal "checkout p99" orb in teal.
 * ================================================================== */

function Hero() {
  return (
    <section className="relative isolate pt-8">
      <HeroBokeh />
      <div className="relative mx-auto max-w-3xl text-center">
        <span className="text-cc-nav-label font-mono text-xs tracking-[0.28em] uppercase">
          Production view
        </span>
        <h1 className="font-heading text-h2 text-cc-heading sm:text-h1 mt-6">
          See what the API is doing.
        </h1>
        <p className="lead text-cc-prose mx-auto mt-6 max-w-2xl">
          The moment latency climbs, you already know which operation hurts, who
          it reaches, and exactly which hop is slow.
        </p>
        <p className="text-body text-cc-ink-dim mx-auto mt-5 max-w-2xl">
          Nitro is OpenTelemetry-native: operation, service, and client views
          with p95 / p99, throughput, error rate, and an impact score. Every
          request is a distributed trace across GraphQL, REST, gRPC, and
          background jobs, so debugging starts from evidence, not another
          dashboard project.
        </p>
        <div className="mt-9 flex flex-wrap items-center justify-center gap-4">
          <SolidButton href="/get-started">Start for Free</SolidButton>
          <OutlineButton href="/docs/nitro/open-telemetry/operation-monitoring">
            Read the Docs
          </OutlineButton>
        </div>
        <div className="text-cc-nav-label mt-8 flex flex-wrap items-center justify-center gap-3 font-mono text-[11px]">
          <StatusDot color={AMBER} pulse />
          <span className="tracking-wide uppercase">
            Live incident on this page
          </span>
          <span className="text-cc-ink-faint">·</span>
          <span>
            trace <span className="text-cc-ink-dim">{TRACE_ID}</span>
          </span>
        </div>
      </div>
      <SignalField />
    </section>
  );
}

/* Five overlapping bokeh orbs behind the hero copy. Fixed in place,
   not scroll-coupled. Brand spectrum at 6 to 14 percent opacity. */
function HeroBokeh() {
  return (
    <div
      aria-hidden
      className="pointer-events-none absolute inset-0 -top-24 -z-10 overflow-hidden"
    >
      <BokehOrb color={TEAL} size={520} opacity={0.14} left="22%" top="6%" />
      <BokehOrb color={CYAN} size={420} opacity={0.1} left="72%" top="10%" />
      <BokehOrb color={VIOLET} size={460} opacity={0.09} left="58%" top="55%" />
      <BokehOrb color={CORAL} size={380} opacity={0.07} left="12%" top="62%" />
      <BokehOrb color={TEAL} size={680} opacity={0.06} left="44%" top="38%" />
    </div>
  );
}

interface BokehOrbProps {
  readonly color: string;
  readonly size: number;
  readonly opacity: number;
  readonly left: string;
  readonly top: string;
}

function BokehOrb({ color, size, opacity, left, top }: BokehOrbProps) {
  return (
    <div
      className="absolute -translate-x-1/2 -translate-y-1/2 rounded-full blur-3xl"
      style={{
        width: size,
        height: size,
        left,
        top,
        background: `radial-gradient(circle at center, ${color} 0%, transparent 70%)`,
        opacity,
      }}
    />
  );
}

/* The wide "signal field" panel directly below the hero copy. Six
   named bokeh orbs of varying size sit on dark cc-surface; the focal
   "checkout p99 318ms" orb softly pulses opacity over ~3.2s. */
function SignalField() {
  return (
    <div className="relative mt-14">
      <div className="border-cc-card-border bg-cc-surface/80 relative overflow-hidden rounded-2xl border backdrop-blur-md">
        <div className="border-cc-card-border/60 bg-cc-code-header/70 flex items-center gap-2 border-b px-4 py-2.5">
          <span className="bg-cc-ink-faint h-2.5 w-2.5 rounded-full" />
          <span className="bg-cc-ink-faint h-2.5 w-2.5 rounded-full" />
          <span className="bg-cc-ink-faint h-2.5 w-2.5 rounded-full" />
          <span className="text-cc-nav-label ml-2 font-mono text-[11px]">
            nitro · signal field
          </span>
          <span className="border-cc-card-border/70 text-cc-nav-label ml-auto inline-flex items-center gap-1.5 rounded-full border px-2 py-0.5 font-mono text-[10px] tracking-wide uppercase">
            <StatusDot color={GREEN} />
            ingesting
          </span>
        </div>
        <div className="relative h-[360px] w-full sm:h-[420px]">
          {/* ambient teal wash behind the focal orb */}
          <div
            aria-hidden
            className="pointer-events-none absolute top-1/2 left-1/2 h-[420px] w-[640px] -translate-x-1/2 -translate-y-1/2 rounded-full blur-3xl"
            style={{
              background: `radial-gradient(circle at center, ${TEAL} 0%, transparent 65%)`,
              opacity: 0.08,
            }}
          />
          <DataOrb
            label="operations"
            value="142"
            sub="tracked"
            color={TEAL}
            size={108}
            left="14%"
            top="28%"
          />
          <DataOrb
            label="services"
            value="9"
            sub="reporting"
            color={CYAN}
            size={96}
            left="80%"
            top="22%"
          />
          <DataOrb
            label="clients"
            value="14"
            sub="published"
            color={VIOLET}
            size={92}
            left="86%"
            top="72%"
          />
          <DataOrb
            label="traces"
            value="9.4k"
            sub="rpm"
            color={TEAL}
            size={100}
            left="18%"
            top="74%"
          />
          <DataOrb
            label="errors"
            value="0.3%"
            sub="5xx"
            color={AMBER}
            size={84}
            left="34%"
            top="14%"
          />
          <DataOrb
            label="throughput"
            value="steady"
            sub="±2%"
            color={GREEN}
            size={84}
            left="68%"
            top="80%"
          />
          {/* The focal orb: checkout p99 spiking. Slowly pulses. */}
          <FocalOrb />
        </div>
      </div>
    </div>
  );
}

interface DataOrbProps {
  readonly label: string;
  readonly value: string;
  readonly sub: string;
  readonly color: string;
  readonly size: number;
  readonly left: string;
  readonly top: string;
}

function DataOrb({ label, value, sub, color, size, left, top }: DataOrbProps) {
  return (
    <div
      className="absolute -translate-x-1/2 -translate-y-1/2"
      style={{ left, top }}
    >
      <div className="relative flex items-center justify-center">
        <div
          aria-hidden
          className="absolute rounded-full blur-2xl"
          style={{
            width: size,
            height: size,
            background: `radial-gradient(circle at center, ${color} 0%, transparent 70%)`,
            opacity: 0.32,
          }}
        />
        <div
          className="relative h-1.5 w-1.5 rounded-full"
          style={{
            backgroundColor: color,
            boxShadow: `0 0 8px ${color}`,
          }}
        />
      </div>
      <div className="mt-3 text-center">
        <div className="text-cc-nav-label font-mono text-[10px] tracking-wide uppercase">
          {label}
        </div>
        <div className="text-cc-heading mt-0.5 font-mono text-sm">{value}</div>
        <div className="text-cc-ink-faint font-mono text-[10px]">{sub}</div>
      </div>
    </div>
  );
}

/* The focal orb is the centered teal bokeh with the checkout p99 number.
   Opacity oscillates between 0.55 and 0.85 over 3.2s on the halo; the
   inner number and label stay constant. Respects reduced motion. */
function FocalOrb() {
  const reduced = useReducedMotion();
  return (
    <div className="absolute top-1/2 left-1/2 -translate-x-1/2 -translate-y-1/2">
      <div className="relative flex flex-col items-center">
        <div className="relative flex items-center justify-center">
          <motion.div
            aria-hidden
            className="absolute rounded-full blur-3xl"
            style={{
              width: 280,
              height: 280,
              background: `radial-gradient(circle at center, ${TEAL} 0%, transparent 65%)`,
            }}
            animate={
              reduced ? { opacity: 0.7 } : { opacity: [0.55, 0.85, 0.55] }
            }
            transition={
              reduced
                ? undefined
                : { duration: 3.2, repeat: Infinity, ease: "easeInOut" }
            }
          />
          <div
            aria-hidden
            className="absolute h-[140px] w-[140px] rounded-full blur-xl"
            style={{
              background: `radial-gradient(circle at center, ${TEAL} 0%, transparent 70%)`,
              opacity: 0.4,
            }}
          />
          <div
            className="relative h-2.5 w-2.5 rounded-full"
            style={{
              backgroundColor: TEAL,
              boxShadow: `0 0 14px ${TEAL}, 0 0 28px ${TEAL}88`,
            }}
          />
        </div>
        <div className="mt-5 text-center">
          <div
            className="font-mono text-[11px] tracking-[0.24em] uppercase"
            style={{ color: TEAL }}
          >
            checkout · p99
          </div>
          <div className="text-cc-heading font-heading mt-1 text-3xl sm:text-4xl">
            318
            <span className="text-cc-ink-dim ml-1 font-mono text-sm">ms</span>
          </div>
          <div className="text-cc-nav-label mt-1 inline-flex items-center gap-2 font-mono text-[10px] tracking-wide uppercase">
            <StatusDot color={AMBER} pulse />
            investigating
          </div>
        </div>
      </div>
    </div>
  );
}

/* ================================================================== *
 * FOUR SIGNALS STRIP
 * Single rounded row split into p95 / p99 / error rate / throughput.
 * Each value framed by a tiny inset bokeh halo in green / coral /
 * amber / teal to keep the orb motif consistent.
 * ================================================================== */

function FourSignalsStrip() {
  const signals = [
    {
      label: "p95 latency",
      value: "42",
      unit: "ms",
      color: GREEN,
      note: "within budget",
    },
    {
      label: "p99 latency",
      value: "318",
      unit: "ms",
      color: CORAL,
      note: "spiking",
    },
    {
      label: "error rate",
      value: "0.3",
      unit: "%",
      color: AMBER,
      note: "5xx on billing",
    },
    {
      label: "throughput",
      value: "9.4",
      unit: "k rpm",
      color: TEAL,
      note: "steady",
    },
  ];
  return (
    <section>
      <div className="text-center">
        <SectionEyebrow>The four signals that matter</SectionEyebrow>
      </div>
      <div className="border-cc-card-border bg-cc-card-border mt-6 grid gap-px overflow-hidden rounded-2xl border sm:grid-cols-2 lg:grid-cols-4">
        {signals.map((s) => (
          <div
            key={s.label}
            className="bg-cc-surface/85 relative overflow-hidden px-6 py-7"
          >
            <div
              aria-hidden
              className="pointer-events-none absolute -top-12 -right-12 h-44 w-44 rounded-full blur-3xl"
              style={{
                background: `radial-gradient(circle at center, ${s.color} 0%, transparent 70%)`,
                opacity: 0.14,
              }}
            />
            <div className="relative">
              <div className="text-cc-nav-label font-mono text-[11px] tracking-wide uppercase">
                {s.label}
              </div>
              <div className="mt-3 flex items-baseline gap-1">
                <span
                  className="font-heading text-h3 leading-none"
                  style={{ color: s.color }}
                >
                  {s.value}
                </span>
                <span className="text-cc-ink-dim font-mono text-sm">
                  {s.unit}
                </span>
              </div>
              <div className="mt-3 flex items-center gap-2">
                <StatusDot color={s.color} />
                <span className="text-cc-nav-label font-mono text-[11px]">
                  {s.note}
                </span>
              </div>
            </div>
          </div>
        ))}
      </div>
      <p className="text-caption text-cc-nav-label mt-4 text-center">
        The same numbers behind the focal orb above, held side by side. Impact
        score names the rest:{" "}
        <span className="text-cc-ink-dim">#1 checkout</span>.
      </p>
    </section>
  );
}

/* ================================================================== *
 * TRACE ORBIT
 * A centered card showing the GraphQL root orb with concentric soft
 * rings; smaller orbs (REST, gRPC, DB, Job) sit on the rings at
 * angles. The billing gRPC orb glows coral.
 * ================================================================== */

function TraceOrbitSection() {
  return (
    <section className="text-center">
      <SectionEyebrow>One trace, every hop</SectionEyebrow>
      <h2 className="font-heading text-h4 text-cc-heading sm:text-h3 mx-auto mt-5 max-w-2xl">
        The graph is the entry. The trace goes all the way down.
      </h2>
      <p className="text-body text-cc-ink-dim mx-auto mt-5 max-w-2xl">
        A distributed trace does not stop at the GraphQL boundary. Nitro
        monitors REST APIs, gRPC services, and background jobs through{" "}
        <code className="text-cc-ink font-mono">
          ChilliCream.Nitro.OpenTelemetry
        </code>
        , so the same trace that opens on{" "}
        <code className="font-mono" style={{ color: TEAL }}>
          checkout
        </code>{" "}
        follows the call down to the hop that is actually slow.
      </p>
      <div className="mt-10">
        <TraceOrbitCard />
      </div>
      <p className="text-caption text-cc-nav-label mx-auto mt-5 max-w-xl">
        The shared trace id{" "}
        <code className="text-cc-ink font-mono">{TRACE_ID}</code> stitches the
        dashboard to the span.
      </p>
    </section>
  );
}

interface OrbitNode {
  readonly id: string;
  readonly label: string;
  readonly kind: string;
  readonly ring: 1 | 2;
  readonly angle: number; // degrees, 0 = right, 90 = bottom
  readonly color: string;
  readonly hot?: boolean;
  readonly tooltip?: string;
}

const ORBIT_NODES: readonly OrbitNode[] = [
  {
    id: "rest",
    label: "users-svc",
    kind: "REST",
    ring: 1,
    angle: 200,
    color: VIOLET,
  },
  {
    id: "grpc",
    label: "billing",
    kind: "gRPC",
    ring: 1,
    angle: 340,
    color: CORAL,
    hot: true,
    tooltip: "201 ms · 63% of trace",
  },
  {
    id: "db",
    label: "accounts",
    kind: "DB",
    ring: 2,
    angle: 60,
    color: "#7dd3fc",
  },
  {
    id: "job",
    label: "worker",
    kind: "Job",
    ring: 2,
    angle: 130,
    color: "#8b9bd4",
  },
];

function TraceOrbitCard() {
  const reduced = useReducedMotion();
  const cx = 260;
  const cy = 200;
  const r1 = 92;
  const r2 = 160;

  return (
    <div className="border-cc-card-border bg-cc-card-bg relative mx-auto max-w-3xl overflow-hidden rounded-2xl border p-6 backdrop-blur-md">
      <div
        aria-hidden
        className="pointer-events-none absolute top-1/2 left-1/2 h-72 w-72 -translate-x-1/2 -translate-y-1/2 rounded-full blur-3xl"
        style={{
          background: `radial-gradient(circle at center, ${TEAL} 0%, transparent 65%)`,
          opacity: 0.08,
        }}
      />
      <div className="relative h-[400px] w-full">
        <svg
          viewBox="0 0 520 400"
          className="absolute inset-0 h-full w-full"
          aria-label="Trace orbit: GraphQL root with REST, gRPC, DB, and Job orbs on concentric rings."
        >
          <circle
            cx={cx}
            cy={cy}
            r={r1}
            fill="none"
            stroke="var(--color-cc-card-border)"
            strokeDasharray="2 4"
            strokeWidth="1"
          />
          <circle
            cx={cx}
            cy={cy}
            r={r2}
            fill="none"
            stroke="var(--color-cc-card-border)"
            strokeDasharray="2 4"
            strokeWidth="1"
          />
          {ORBIT_NODES.map((n) => {
            const r = n.ring === 1 ? r1 : r2;
            const rad = (n.angle * Math.PI) / 180;
            const nx = cx + r * Math.cos(rad);
            const ny = cy + r * Math.sin(rad);
            return (
              <line
                key={`edge-${n.id}`}
                x1={cx}
                y1={cy}
                x2={nx}
                y2={ny}
                stroke={n.hot ? n.color : "var(--color-cc-card-border)"}
                strokeOpacity={n.hot ? 0.7 : 0.5}
                strokeWidth={n.hot ? 1.5 : 1}
              />
            );
          })}
        </svg>

        {/* Root orb: GraphQL */}
        <OrbitOrb
          label="api"
          kind="GraphQL"
          color={TEAL}
          left={`${(cx / 520) * 100}%`}
          top={`${(cy / 400) * 100}%`}
          size={120}
          root
        />

        {/* Satellite orbs with enter-view stagger */}
        {ORBIT_NODES.map((n, i) => {
          const r = n.ring === 1 ? r1 : r2;
          const rad = (n.angle * Math.PI) / 180;
          const nx = cx + r * Math.cos(rad);
          const ny = cy + r * Math.sin(rad);
          return (
            <motion.div
              key={n.id}
              className="absolute"
              style={{
                left: `${(nx / 520) * 100}%`,
                top: `${(ny / 400) * 100}%`,
              }}
              initial={
                reduced ? { opacity: 1, scale: 1 } : { opacity: 0, scale: 0 }
              }
              whileInView={{ opacity: 1, scale: 1 }}
              viewport={{ once: true, margin: "-50px" }}
              transition={{ duration: 0.6, delay: i * 0.12 }}
            >
              <OrbitOrb
                label={n.label}
                kind={n.kind}
                color={n.color}
                size={n.hot ? 92 : 72}
                hot={n.hot}
                tooltip={n.tooltip}
              />
            </motion.div>
          );
        })}
      </div>
    </div>
  );
}

interface OrbitOrbProps {
  readonly label: string;
  readonly kind: string;
  readonly color: string;
  readonly size: number;
  readonly left?: string;
  readonly top?: string;
  readonly root?: boolean;
  readonly hot?: boolean;
  readonly tooltip?: string;
}

function OrbitOrb({
  label,
  kind,
  color,
  size,
  left,
  top,
  root,
  hot,
  tooltip,
}: OrbitOrbProps) {
  const positioned = left !== undefined && top !== undefined;
  return (
    <div
      className={
        positioned
          ? "absolute -translate-x-1/2 -translate-y-1/2"
          : "-translate-x-1/2 -translate-y-1/2"
      }
      style={positioned ? { left, top } : undefined}
    >
      <div className="relative flex flex-col items-center">
        <div className="relative flex items-center justify-center">
          <div
            aria-hidden
            className="absolute rounded-full blur-2xl"
            style={{
              width: size,
              height: size,
              background: `radial-gradient(circle at center, ${color} 0%, transparent 70%)`,
              opacity: hot ? 0.55 : root ? 0.45 : 0.35,
            }}
          />
          <div
            className={`relative rounded-full ${root ? "h-3 w-3" : "h-2 w-2"}`}
            style={{
              backgroundColor: color,
              boxShadow: `0 0 ${hot ? 16 : root ? 12 : 8}px ${color}`,
            }}
          />
        </div>
        <div className="mt-2 text-center">
          <div
            className="font-mono text-[9px] tracking-[0.16em] uppercase"
            style={{ color }}
          >
            {kind}
          </div>
          <div
            className={`mt-0.5 font-mono text-[11px] ${root ? "text-cc-heading" : "text-cc-ink-dim"}`}
          >
            {label}
          </div>
          {tooltip && (
            <div
              className="mt-1.5 inline-block rounded px-1.5 py-0.5 font-mono text-[10px] tracking-wide"
              style={{ color, backgroundColor: `${color}1a` }}
            >
              {tooltip}
            </div>
          )}
        </div>
      </div>
    </div>
  );
}

/* ================================================================== *
 * LENSES SECTION
 * Three lenses row: operation / service / client cards in a 3-up grid.
 * Each card is titled with a small orb glyph in its dominant signal
 * color.
 * ================================================================== */

function LensesSection() {
  return (
    <section className="text-center">
      <SectionEyebrow>Many lenses, same incident</SectionEyebrow>
      <h2 className="font-heading text-h4 text-cc-heading sm:text-h3 mx-auto mt-5 max-w-2xl">
        Operation, service, client. Pick the angle the question asks for.
      </h2>
      <p className="text-body text-cc-ink-dim mx-auto mt-5 max-w-2xl">
        Telemetry is the same stream, sliced three ways. Rank operations by
        impact to find what hurts most, drop into the service that is degraded,
        or check which published clients are affected before you ship a fix.
      </p>
      <div className="mt-10 grid gap-5 text-left lg:grid-cols-3">
        <OperationLens />
        <ServiceLens />
        <ClientLens />
      </div>
    </section>
  );
}

interface LensCardProps {
  readonly title: string;
  readonly tab: string;
  readonly glyphColor: string;
  readonly children: ReactNode;
}

function LensCard({ title, tab, glyphColor, children }: LensCardProps) {
  return (
    <div className="border-cc-card-border bg-cc-card-bg relative overflow-hidden rounded-xl border backdrop-blur-md">
      <div
        aria-hidden
        className="pointer-events-none absolute -top-10 -left-10 h-32 w-32 rounded-full blur-3xl"
        style={{
          background: `radial-gradient(circle at center, ${glyphColor} 0%, transparent 70%)`,
          opacity: 0.18,
        }}
      />
      <div className="border-cc-card-border/60 bg-cc-code-header/60 relative flex items-center justify-between border-b px-4 py-2.5">
        <span className="text-cc-nav-label font-mono text-[11px] tracking-wide uppercase">
          {tab}
        </span>
        <span className="text-cc-ink-faint font-mono text-[10px]">nitro</span>
      </div>
      <div className="relative px-4 py-4">
        <div className="flex items-center gap-2.5">
          <OrbGlyph color={glyphColor} />
          <h3 className="font-heading text-h6 text-cc-heading">{title}</h3>
        </div>
        <div className="mt-3">{children}</div>
      </div>
    </div>
  );
}

interface OrbGlyphProps {
  readonly color: string;
  readonly size?: number;
}

function OrbGlyph({ color, size = 28 }: OrbGlyphProps) {
  return (
    <span
      className="relative inline-flex items-center justify-center"
      style={{ width: size, height: size }}
    >
      <span
        aria-hidden
        className="absolute inset-0 rounded-full blur-md"
        style={{
          background: `radial-gradient(circle at center, ${color} 0%, transparent 70%)`,
          opacity: 0.55,
        }}
      />
      <span
        className="relative h-1.5 w-1.5 rounded-full"
        style={{
          backgroundColor: color,
          boxShadow: `0 0 8px ${color}`,
        }}
      />
    </span>
  );
}

interface OpRow {
  readonly name: string;
  readonly impact: number;
  readonly p95: string;
  readonly status: "ok" | "warn" | "fire";
}

const OP_ROWS: readonly OpRow[] = [
  { name: "checkout", impact: 1, p95: "42ms", status: "fire" },
  { name: "cartSummary", impact: 2, p95: "31ms", status: "warn" },
  { name: "productList", impact: 3, p95: "12ms", status: "ok" },
  { name: "userProfile", impact: 4, p95: "8ms", status: "ok" },
];

const STATUS_COLOR: Record<OpRow["status"], string> = {
  ok: GREEN,
  warn: AMBER,
  fire: CORAL,
};

function OperationLens() {
  return (
    <LensCard title="Ranked by impact" tab="operations" glyphColor={CORAL}>
      <div className="space-y-1.5">
        {OP_ROWS.map((row) => (
          <div
            key={row.name}
            className={`flex items-center gap-3 rounded-lg px-2.5 py-2 ${
              row.status === "fire" ? "bg-cc-surface/80" : "bg-cc-surface/40"
            }`}
            style={
              row.status === "fire"
                ? { boxShadow: `inset 0 0 0 1px ${CORAL}33` }
                : undefined
            }
          >
            <span className="text-cc-nav-label w-5 font-mono text-[11px]">
              #{row.impact}
            </span>
            <StatusDot
              color={STATUS_COLOR[row.status]}
              pulse={row.status === "fire"}
            />
            <span
              className={`flex-1 font-mono text-[12px] ${
                row.status === "fire" ? "text-cc-heading" : "text-cc-ink-dim"
              }`}
            >
              {row.name}
            </span>
            <span className="text-cc-nav-label font-mono text-[11px]">
              {row.p95}
            </span>
          </div>
        ))}
      </div>
      <p className="text-cc-nav-label mt-3 text-[11px]">
        Impact score ranks by what hurts the system, not raw call count.
      </p>
    </LensCard>
  );
}

function ServiceLens() {
  return (
    <LensCard title="billing · degraded" tab="services" glyphColor={AMBER}>
      <div className="grid grid-cols-2 gap-2">
        <MiniStat label="p95" value="42ms" />
        <MiniStat label="p99" value="318ms" tone={CORAL} />
        <MiniStat label="errors" value="0.3%" />
        <MiniStat label="rpm" value="9.4k" />
      </div>
      <div className="bg-cc-surface/50 mt-3 rounded-lg px-3 py-2.5">
        <div className="text-cc-nav-label mb-1.5 font-mono text-[10px] tracking-wide uppercase">
          status codes · 5m
        </div>
        <StatusBar />
        <div className="mt-2 flex items-center gap-4 font-mono text-[10px]">
          <span style={{ color: GREEN }}>2xx 96.4%</span>
          <span style={{ color: AMBER }}>4xx 3.3%</span>
          <span style={{ color: CORAL }}>5xx 0.3%</span>
        </div>
      </div>
    </LensCard>
  );
}

function ClientLens() {
  const clients = [
    { name: "web-storefront@4.2.0", share: "61%", status: "fire" as const },
    { name: "ios-app@3.8.1", share: "27%", status: "warn" as const },
    { name: "partner-api@1.0", share: "12%", status: "ok" as const },
  ];
  return (
    <LensCard
      title="Published clients affected"
      tab="clients"
      glyphColor={VIOLET}
    >
      <div className="space-y-2">
        {clients.map((c) => (
          <div key={c.name} className="flex items-center gap-2.5">
            <StatusDot color={STATUS_COLOR[c.status]} />
            <span className="text-cc-ink-dim flex-1 truncate font-mono text-[12px]">
              {c.name}
            </span>
            <span className="text-cc-nav-label font-mono text-[11px]">
              {c.share}
            </span>
          </div>
        ))}
      </div>
      <p className="text-cc-nav-label mt-4 text-[11px]">
        See which published clients are affected before you ship the fix.
      </p>
    </LensCard>
  );
}

interface MiniStatProps {
  readonly label: string;
  readonly value: string;
  readonly tone?: string;
}

function MiniStat({ label, value, tone }: MiniStatProps) {
  return (
    <div className="bg-cc-surface/50 rounded-lg px-3 py-2.5">
      <div className="text-cc-nav-label font-mono text-[10px] tracking-wide uppercase">
        {label}
      </div>
      <div
        className="mt-1 font-mono text-sm"
        style={{ color: tone ?? "var(--color-cc-heading)" }}
      >
        {value}
      </div>
    </div>
  );
}

function StatusBar() {
  return (
    <div className="flex h-2 overflow-hidden rounded-full">
      <span style={{ width: "96.4%", backgroundColor: GREEN }} />
      <span style={{ width: "3.3%", backgroundColor: AMBER }} />
      <span style={{ width: "0.3%", backgroundColor: CORAL }} />
    </div>
  );
}

/* ================================================================== *
 * WATERFALL SECTION
 * A single wide card showing a compact 5-row trace waterfall. The
 * slow gRPC bar glows coral; left-side prose explains how OTel spans
 * cross GraphQL, REST, gRPC, and background jobs.
 * ================================================================== */

interface Span {
  readonly id: string;
  readonly label: string;
  readonly kind: "graphql" | "rest" | "grpc" | "job" | "db";
  readonly start: number;
  readonly width: number;
  readonly ms: string;
  readonly slow?: boolean;
}

const KIND_LABEL: Record<Span["kind"], string> = {
  graphql: "GraphQL",
  rest: "REST",
  grpc: "gRPC",
  job: "Job",
  db: "DB",
};

const KIND_COLOR: Record<Span["kind"], string> = {
  graphql: TEAL,
  rest: VIOLET,
  grpc: CORAL,
  job: "#8b9bd4",
  db: "#7dd3fc",
};

const SPANS: readonly Span[] = [
  {
    id: "s0",
    label: "mutation checkout",
    kind: "graphql",
    start: 0,
    width: 100,
    ms: "318ms",
  },
  {
    id: "s1",
    label: "api → users-svc · GET /me",
    kind: "rest",
    start: 4,
    width: 11,
    ms: "21ms",
  },
  {
    id: "s2",
    label: "users-svc → billing · Charge()",
    kind: "grpc",
    start: 16,
    width: 64,
    ms: "201ms",
    slow: true,
  },
  {
    id: "s3",
    label: "billing → db · SELECT account",
    kind: "db",
    start: 20,
    width: 12,
    ms: "9ms",
  },
  {
    id: "s4",
    label: "billing → worker · enqueue receipt",
    kind: "job",
    start: 82,
    width: 13,
    ms: "37ms",
  },
];

const SPAN_DEPTH: readonly number[] = [0, 1, 1, 2, 2];

function WaterfallSection() {
  return (
    <section className="grid gap-10 lg:grid-cols-[1fr_1.6fr] lg:items-start">
      <div>
        <SectionEyebrow>Waterfall in context</SectionEyebrow>
        <h2 className="font-heading text-h4 text-cc-heading sm:text-h3 mt-5">
          Follow the slow span, not the dashboard.
        </h2>
        <p className="text-body text-cc-ink-dim mt-5">
          A single request fans out across your graph and the services behind
          it, every hop a real OpenTelemetry span. OTel spans cross GraphQL,
          REST, gRPC, and background jobs without a proprietary agent in the
          way.
        </p>
        <p className="text-body text-cc-ink-dim mt-4">
          The shared{" "}
          <code className="font-mono" style={{ color: TEAL }}>
            trace {TRACE_ID}
          </code>{" "}
          that surfaced on the signal field stitches straight to the gRPC span
          that is actually slow.
        </p>
        <ul className="text-caption text-cc-ink-dim mt-7 space-y-3">
          <LegendRow
            kind="graphql"
            text="The GraphQL operation, root of the trace"
          />
          <LegendRow kind="rest" text="A REST hop to users-svc" />
          <LegendRow
            kind="grpc"
            text="The slow gRPC charge to billing"
            highlight
          />
          <LegendRow kind="db" text="A fast database read" />
          <LegendRow
            kind="job"
            text="A background job enqueued for the receipt"
          />
        </ul>
      </div>
      <TraceWaterfall />
    </section>
  );
}

interface LegendRowProps {
  readonly kind: Span["kind"];
  readonly text: string;
  readonly highlight?: boolean;
}

function LegendRow({ kind, text, highlight }: LegendRowProps) {
  return (
    <li className="flex items-center gap-3">
      <span
        className="h-2.5 w-2.5 shrink-0 rounded-[3px]"
        style={{ backgroundColor: KIND_COLOR[kind] }}
      />
      <span className={highlight ? "text-cc-prose" : undefined}>
        {text}
        {highlight && (
          <span
            className="ml-2 rounded px-1.5 py-0.5 font-mono text-[10px] tracking-wide uppercase"
            style={{ color: CORAL, backgroundColor: `${CORAL}1a` }}
          >
            201 ms · 63%
          </span>
        )}
      </span>
    </li>
  );
}

function TraceWaterfall() {
  return (
    <div className="border-cc-card-border bg-cc-card-bg relative overflow-hidden rounded-2xl border backdrop-blur-md">
      <div
        aria-hidden
        className="pointer-events-none absolute -top-12 right-10 h-40 w-40 rounded-full blur-3xl"
        style={{
          background: `radial-gradient(circle at center, ${CORAL} 0%, transparent 70%)`,
          opacity: 0.12,
        }}
      />
      <div className="border-cc-card-border/60 bg-cc-code-header/70 relative flex flex-wrap items-center gap-x-3 gap-y-1 border-b px-5 py-3">
        <span className="text-cc-nav-label font-mono text-[11px]">trace</span>
        <span className="font-mono text-[11px]" style={{ color: TEAL }}>
          {TRACE_ID}
        </span>
        <span className="text-cc-ink-faint font-mono text-[11px]">·</span>
        <span className="text-cc-ink-dim font-mono text-[11px]">
          mutation checkout
        </span>
        <span className="text-cc-nav-label ml-auto inline-flex items-center gap-1.5 font-mono text-[11px]">
          duration <span className="text-cc-heading">318ms</span>
        </span>
      </div>
      <div className="relative px-5 py-5">
        <div className="space-y-2.5">
          {SPANS.map((span, i) => (
            <SpanRow key={span.id} span={span} depth={SPAN_DEPTH[i] ?? 0} />
          ))}
        </div>
        <div className="border-cc-card-border/50 text-cc-nav-label mt-5 ml-[38%] flex items-center justify-between border-t pt-2 font-mono text-[10px]">
          <span>0ms</span>
          <span>100ms</span>
          <span>200ms</span>
          <span>318ms</span>
        </div>
      </div>
    </div>
  );
}

interface SpanRowProps {
  readonly span: Span;
  readonly depth: number;
}

function SpanRow({ span, depth }: SpanRowProps) {
  const color = KIND_COLOR[span.kind];
  const isRoot = span.kind === "graphql";
  return (
    <div className="flex items-center gap-3">
      <div
        className="flex w-[38%] shrink-0 items-center gap-2 truncate"
        style={{ paddingLeft: depth * 14 }}
      >
        <span
          className="rounded px-1.5 py-0.5 font-mono text-[9px] font-semibold tracking-wide uppercase"
          style={{ color, backgroundColor: `${color}1a` }}
        >
          {KIND_LABEL[span.kind]}
        </span>
        <span
          className={`truncate font-mono text-[12px] ${isRoot ? "text-cc-heading" : "text-cc-ink-dim"}`}
        >
          {span.label}
        </span>
      </div>
      <div className="bg-cc-surface/60 relative h-6 flex-1 rounded">
        <div
          className="absolute top-1/2 flex h-4 -translate-y-1/2 items-center rounded-[3px]"
          style={{
            left: `${span.start}%`,
            width: `${span.width}%`,
            backgroundColor: span.slow ? CORAL : color,
            opacity: span.slow ? 1 : 0.78,
            boxShadow: span.slow ? `0 0 16px ${CORAL}66` : undefined,
          }}
        >
          {span.slow && (
            <span className="text-cc-surface ml-2 font-mono text-[10px] font-semibold">
              billing.Charge()
            </span>
          )}
        </div>
        <span
          className="text-cc-nav-label absolute top-1/2 -translate-y-1/2 font-mono text-[10px]"
          style={{ left: `calc(${span.start + span.width}% + 8px)` }}
        >
          {span.ms}
        </span>
      </div>
    </div>
  );
}

/* ================================================================== *
 * HONESTY TRIO
 * Three short cards: telemetry is configured, the IDE is separate,
 * OpenTelemetry end to end. Each prefaced with a teal CheckIcon and
 * a faint orb behind the title.
 * ================================================================== */

function HonestySection() {
  return (
    <section className="text-center">
      <SectionEyebrow>Straight about what it is</SectionEyebrow>
      <h2 className="font-heading text-h4 text-cc-heading sm:text-h3 mx-auto mt-5 max-w-2xl">
        Honest about the setup, precise about the payoff.
      </h2>
      <div className="mt-9 grid gap-5 text-left md:grid-cols-3">
        <HonestyCard title="Telemetry is configured, not magic">
          The dashboards above come from telemetry you point at Nitro. It is a
          configuration step, deliberate and documented, not something that
          turns on by itself.
        </HonestyCard>
        <HonestyCard title="The IDE is a separate thing">
          The GraphQL IDE can be served from your Hot Chocolate endpoint. That
          is independent of the telemetry dashboards here. Two facts, kept
          apart.
        </HonestyCard>
        <HonestyCard title="OpenTelemetry end to end">
          Vendor-neutral spans mean your data is yours, and there is no
          proprietary agent locking the trace in.
        </HonestyCard>
      </div>
    </section>
  );
}

interface HonestyCardProps {
  readonly title: string;
  readonly children: ReactNode;
}

function HonestyCard({ title, children }: HonestyCardProps) {
  return (
    <div className="border-cc-card-border bg-cc-card-bg relative overflow-hidden rounded-xl border px-5 py-5 backdrop-blur-md">
      <div
        aria-hidden
        className="pointer-events-none absolute -top-6 -left-6 h-24 w-24 rounded-full blur-2xl"
        style={{
          background: `radial-gradient(circle at center, ${TEAL} 0%, transparent 70%)`,
          opacity: 0.18,
        }}
      />
      <div className="relative flex items-center gap-2">
        <span style={{ color: TEAL }}>
          <CheckIcon size={15} />
        </span>
        <h3 className="font-heading text-h6 text-cc-heading">{title}</h3>
      </div>
      <p className="text-caption text-cc-ink-dim relative mt-3">{children}</p>
    </div>
  );
}

/* ================================================================== *
 * CLOSING CTA
 * Centered, single thin spectrum hairline at top (the page's one
 * spectrum event), low-opacity spectrum blur far below the buttons.
 * ================================================================== */

function ClosingCta() {
  return (
    <section className="border-cc-card-border bg-cc-surface/80 relative overflow-hidden rounded-2xl border px-6 py-14 text-center backdrop-blur-md sm:px-12">
      <div
        aria-hidden
        className="pointer-events-none absolute inset-x-0 top-0 h-px"
        style={{ background: SPECTRUM }}
      />
      <div
        aria-hidden
        className="pointer-events-none absolute -bottom-24 left-1/2 h-64 w-[680px] -translate-x-1/2 opacity-25 blur-3xl"
        style={{ background: SPECTRUM }}
      />
      <span className="text-cc-nav-label font-mono text-xs tracking-[0.28em] uppercase">
        Production view
      </span>
      <h2 className="font-heading text-h3 text-cc-heading sm:text-h2 mt-5">
        Stop guessing. See it.
      </h2>
      <p className="text-body text-cc-ink-dim mx-auto mt-5 max-w-xl">
        Wire your services to OpenTelemetry once and every request becomes
        evidence: ranked by impact, traced end to end, slow span already
        highlighted.
      </p>
      <div className="mt-9 flex flex-wrap items-center justify-center gap-4">
        <SolidButton href="/get-started">Start for Free</SolidButton>
        <OutlineButton href="/docs/nitro/open-telemetry/operation-monitoring">
          Read the Docs
        </OutlineButton>
      </div>
    </section>
  );
}

/* ================================================================== *
 * Shared primitives
 * ================================================================== */

interface SectionEyebrowProps {
  readonly children: ReactNode;
}

function SectionEyebrow({ children }: SectionEyebrowProps) {
  return (
    <span className="text-cc-nav-label font-mono text-xs tracking-[0.28em] uppercase">
      {children}
    </span>
  );
}

interface StatusDotProps {
  readonly color: string;
  readonly pulse?: boolean;
}

function StatusDot({ color, pulse }: StatusDotProps) {
  return (
    <span className="relative inline-flex h-2 w-2 shrink-0">
      {pulse && (
        <span
          className="absolute inline-flex h-full w-full rounded-full opacity-60 motion-safe:animate-ping"
          style={{ backgroundColor: color }}
        />
      )}
      <span
        className="relative inline-flex h-2 w-2 rounded-full"
        style={{ backgroundColor: color }}
      />
    </span>
  );
}

"use client";

import { useEffect, useRef, useState, type ReactNode } from "react";
import {
  MotionConfig,
  motion,
  useInView,
  useReducedMotion,
} from "motion/react";

import { CheckIcon } from "@/src/components/CheckIcon";
import { OutlineButton, SolidButton } from "@/src/design-system/Button";

/* ------------------------------------------------------------------ */
/* Metadata                                                            */
/* ------------------------------------------------------------------ */

/* ------------------------------------------------------------------ */
/* Shared constants and data (sourced verbatim from v1)                */
/* ------------------------------------------------------------------ */

const BOOKING_URL = "https://calendly.com/chillicream/60min";
const CONTACT_MAILTO = "mailto:contact@chillicream.com";
const ENTERPRISE_MAILTO =
  "mailto:contact@chillicream.com?subject=Enterprise%20Services";
const SUPPORT_CONTACT = "/services/support/contact";

type TrackId = "advisory" | "support" | "training";

interface ServiceTrack {
  readonly id: TrackId;
  readonly eyebrow: string;
  readonly name: string;
  readonly tagline: string;
  readonly priceLine: string;
  readonly priceNote: string;
  readonly bullets: readonly string[];
  readonly learnMoreHref: string;
  readonly learnMoreLabel: string;
  readonly highlight?: boolean;
  readonly highlightLabel?: string;
}

const TRACKS: readonly ServiceTrack[] = [
  {
    id: "advisory",
    eyebrow: "Consulting & Contracting",
    name: "Advisory",
    tagline:
      "Hourly consulting or scoped contracting from senior engineers. Bring a question, a design, or a deadline.",
    priceLine: "Hourly",
    priceNote: "or scoped contracting",
    bullets: [
      "Architecture, schema review, troubleshooting",
      "Proof of concept and full implementation",
      "Direct line to the core engineering team",
      "Start with a single 60-minute call",
    ],
    learnMoreHref: "/services/advisory",
    learnMoreLabel: "Explore Advisory",
  },
  {
    id: "support",
    eyebrow: "Community, Startup, Business, Enterprise",
    name: "Support",
    tagline:
      "Tiered support plans with private channels, defined response times, and an escalation path you can rely on.",
    priceLine: "From $450",
    priceNote: "per month",
    bullets: [
      "Free Community plan, paid tiers from $450",
      "Business at $1,300 with email and incident handling",
      "Enterprise with phone support and a dedicated account manager",
      "Same engineers who ship Hot Chocolate, Fusion, and Nitro",
    ],
    learnMoreHref: "/services/support",
    learnMoreLabel: "Compare Support",
    highlight: true,
    highlightLabel: "Most teams start here",
  },
  {
    id: "training",
    eyebrow: "Corporate Training & Workshop",
    name: "Training",
    tagline:
      "Hands-on training and workshops for your team. Levels and pacing tuned to where your engineers are today.",
    priceLine: "Custom",
    priceNote: "tailored to team size",
    bullets: [
      "Corporate Training tuned to beginner, advanced, or mixed teams",
      "Corporate Workshop covering Hot Chocolate, ASP.NET Core, React, Relay",
      "Real-project exercises and production quirks",
      "Designed to lift the whole team at once",
    ],
    learnMoreHref: "/services/training",
    learnMoreLabel: "Explore Training",
  },
];

interface SwitchInput {
  readonly id: TrackId;
  readonly need: string;
  readonly destinationLabel: string;
}

const SWITCH_INPUTS: readonly SwitchInput[] = [
  {
    id: "support",
    need: "Need help right now",
    destinationLabel: "Routes to Support",
  },
  {
    id: "advisory",
    need: "Need expert delivery",
    destinationLabel: "Routes to Advisory",
  },
  {
    id: "training",
    need: "Need your team trained",
    destinationLabel: "Routes to Training",
  },
];

interface DecisionRow {
  readonly id: string;
  readonly need: string;
  readonly route: string;
  readonly destinations: readonly {
    readonly label: string;
    readonly href: string;
  }[];
}

const DECISION_ROWS: readonly DecisionRow[] = [
  {
    id: "right-now",
    need: "Need help right now",
    route: "A single call, or an ongoing Support plan with a defined SLA.",
    destinations: [
      { label: "Advisory consult", href: "/services/advisory" },
      { label: "Support plans", href: "/services/support" },
    ],
  },
  {
    id: "expert-delivery",
    need: "Need expert delivery",
    route:
      "A scoped statement of work where our engineers ship the result with you.",
    destinations: [
      { label: "Advisory: Contracting", href: "/services/advisory" },
    ],
  },
  {
    id: "team-trained",
    need: "Need your team trained",
    route:
      "Corporate training or a hands-on workshop tuned to your team and stack.",
    destinations: [{ label: "Training", href: "/services/training" }],
  },
];

interface EnterpriseBullet {
  readonly label: string;
  readonly value: string;
}

const ENTERPRISE_BULLETS: readonly EnterpriseBullet[] = [
  {
    label: "Coverage",
    value: "Phone support and unlimited critical incidents",
  },
  { label: "Account", value: "Dedicated account manager and status reviews" },
  { label: "Delivery", value: "Embedded engineers across teams and units" },
  { label: "Contract", value: "Custom SLAs and procurement-ready paperwork" },
];

interface TrustFact {
  readonly label: string;
  readonly value: string;
}

const TRUST_FACTS: readonly TrustFact[] = [
  {
    label: "Team",
    value: "Same engineers who ship Hot Chocolate, Fusion, and Nitro",
  },
  {
    label: "Seniority",
    value: "Hourly consulting or scoped contracting from senior engineers",
  },
  { label: "Response", value: "Defined SLAs across Business and Enterprise" },
];

/* ------------------------------------------------------------------ */
/* Page                                                                */
/* ------------------------------------------------------------------ */

export function ClientPage() {
  const [routedId, setRoutedId] = useState<TrackId>("support");

  return (
    <MotionConfig reducedMotion="user">
      <Hero />
      <SwitchboardSection onRouted={setRoutedId} />
      <TrackGrid routedId={routedId} />
      <DecisionStrip />
      <EnterpriseBand />
      <TrustStrip />
      <ClosingCta />
    </MotionConfig>
  );
}

/* ------------------------------------------------------------------ */
/* Hero                                                                */
/* ------------------------------------------------------------------ */

function Hero() {
  return (
    <section className="pt-10 pb-12 text-center sm:pt-16 sm:pb-16">
      <p className="text-cc-nav-label font-mono text-xs tracking-[0.18em] uppercase">
        ChilliCream Services
      </p>
      <h1 className="font-heading text-cc-heading text-h2 sm:text-h1 mt-5 font-semibold tracking-tight">
        Pick the right level of help for your GraphQL stack.
      </h1>
      <p className="text-cc-ink mx-auto mt-6 max-w-2xl text-base text-pretty sm:text-lg">
        Three ways to work with the team behind Hot Chocolate, Fusion, and
        Nitro: hands-on Advisory, ongoing Support plans, or Corporate Training.
        Tell us where you are and the switchboard below will route you.
      </p>
      <div className="mt-9 flex flex-wrap items-center justify-center gap-3">
        <SolidButton href={BOOKING_URL}>Book a 60-min call</SolidButton>
        <OutlineButton href="#switchboard">Route me</OutlineButton>
      </div>
    </section>
  );
}

/* ------------------------------------------------------------------ */
/* Switchboard centerpiece                                             */
/* ------------------------------------------------------------------ */

// Colors are sourced from cc-* tokens defined in app/globals.css so any token
// change propagates here. The brand cyan does not have its own token; it is
// the documented brand cyan hex from the design system.
const CYAN = "#16b9e4";
const ACCENT = "var(--color-cc-accent)";
const SURFACE = "var(--color-cc-surface)";
const NAV_LABEL = "var(--color-cc-nav-label)";
const HEADING = "var(--color-cc-heading)";
const INK_DIM = "var(--color-cc-ink)";
const WIRE_DIM = "rgba(124, 146, 198, 0.18)";
const NODE_DIM = "rgba(124, 146, 198, 0.45)";

// SVG geometry (viewBox 800x420)
const SVG_W = 800;
const SVG_H = 420;
const INPUT_X = 130;
const INPUT_YS = [90, 210, 330];
const JUNCTION_X = 400;
const JUNCTION_Y = 210;
const OUTPUT_X = 670;
// Outputs are ordered to match TrackGrid order: advisory, support, training.
const OUTPUTS: readonly { readonly id: TrackId; readonly y: number }[] = [
  { id: "advisory", y: 90 },
  { id: "support", y: 210 },
  { id: "training", y: 330 },
];

interface SwitchboardSectionProps {
  readonly onRouted: (id: TrackId) => void;
}

function SwitchboardSection({ onRouted }: SwitchboardSectionProps) {
  const wrapperRef = useRef<HTMLDivElement | null>(null);
  const inView = useInView(wrapperRef, { once: false, margin: "-15%" });
  const reduced = useReducedMotion();

  const [activeInputIndex, setActiveInputIndex] = useState(0);
  const [forced, setForced] = useState(false);

  // Sync routedId from current input index.
  useEffect(() => {
    onRouted(SWITCH_INPUTS[activeInputIndex].id);
  }, [activeInputIndex, onRouted]);

  // Auto-cycle while in view, unless user forced or reduced motion.
  useEffect(() => {
    if (reduced) {
      return;
    }
    if (!inView) {
      return;
    }
    if (forced) {
      return;
    }
    const id = window.setTimeout(() => {
      setActiveInputIndex((i) => (i + 1) % SWITCH_INPUTS.length);
    }, 2600);
    return () => window.clearTimeout(id);
  }, [activeInputIndex, inView, forced, reduced]);

  const handleSelect = (idx: number) => {
    setForced(true);
    setActiveInputIndex(idx);
  };

  const activeInput = SWITCH_INPUTS[activeInputIndex];
  const activeOutputIndex = OUTPUTS.findIndex((o) => o.id === activeInput.id);

  return (
    <section
      id="switchboard"
      aria-labelledby="switchboard-heading"
      className="border-cc-card-border bg-cc-card-bg/40 mt-2 rounded-3xl border p-6 sm:p-10"
    >
      <div className="text-center">
        <p className="text-cc-nav-label font-mono text-xs tracking-[0.18em] uppercase">
          Routing Switchboard
        </p>
        <h2
          id="switchboard-heading"
          className="font-heading text-cc-heading text-h4 sm:text-h3 mt-3 font-semibold"
        >
          Tell us where you are. We will route you.
        </h2>
        <p className="text-cc-ink mx-auto mt-4 max-w-2xl text-base">
          Pick an input on the left, or let the switchboard cycle. The active
          line lights up the destination card below.
        </p>
      </div>

      <div
        ref={wrapperRef}
        className="bg-cc-surface border-cc-card-border mt-8 grid gap-6 rounded-2xl border p-4 sm:p-6 md:grid-cols-[1fr_auto] md:items-stretch"
      >
        <Switchboard
          activeInputIndex={activeInputIndex}
          activeOutputIndex={activeOutputIndex}
          inView={inView}
          reduced={reduced ?? false}
        />
        <ol className="flex flex-col justify-center gap-3 md:w-56">
          {SWITCH_INPUTS.map((input, idx) => {
            const isActive = idx === activeInputIndex;
            return (
              <li key={input.id}>
                <button
                  type="button"
                  onClick={() => handleSelect(idx)}
                  aria-pressed={isActive}
                  className={[
                    "group block w-full rounded-xl border px-4 py-3 text-left transition-colors",
                    isActive
                      ? "border-cc-accent bg-cc-accent/5"
                      : "border-cc-card-border hover:border-cc-card-border-hover bg-cc-card-bg/30",
                  ].join(" ")}
                >
                  <span className="text-cc-nav-label font-mono text-[0.6rem] tracking-[0.18em] uppercase">
                    {String(idx + 1).padStart(2, "0")} /{" "}
                    {input.destinationLabel}
                  </span>
                  <span className="font-heading text-cc-heading mt-1 block text-sm font-semibold">
                    {input.need}
                  </span>
                </button>
              </li>
            );
          })}
        </ol>
      </div>
      <p className="text-cc-nav-label mt-4 text-center font-mono text-[0.65rem] tracking-[0.18em] uppercase">
        Routing to {activeInput.id}
      </p>
      <p className="text-cc-ink-dim mt-2 text-center text-xs">
        Right-now also overlaps with Advisory. See the decision strip below.
      </p>
    </section>
  );
}

interface SwitchboardProps {
  readonly activeInputIndex: number;
  readonly activeOutputIndex: number;
  readonly inView: boolean;
  readonly reduced: boolean;
}

function Switchboard({
  activeInputIndex,
  activeOutputIndex,
  inView,
  reduced,
}: SwitchboardProps) {
  // Each wire is composed of two cubic curves: input -> junction, junction -> output.
  // We render all six segments and only light the active pair.
  return (
    <div className="relative aspect-[800/420] w-full">
      <svg
        viewBox={`0 0 ${SVG_W} ${SVG_H}`}
        className="absolute inset-0 h-full w-full"
        role="img"
        aria-label="Animated routing switchboard from inputs to Advisory, Support, and Training"
      >
        {/* Dimmed wires (all) */}
        {INPUT_YS.map((y, i) => (
          <path
            key={`in-dim-${i}`}
            d={inputToJunctionPath(y)}
            stroke={WIRE_DIM}
            strokeWidth={2}
            fill="none"
          />
        ))}
        {OUTPUTS.map((o, i) => (
          <path
            key={`out-dim-${i}`}
            d={junctionToOutputPath(o.y)}
            stroke={WIRE_DIM}
            strokeWidth={2}
            fill="none"
          />
        ))}

        {/* Active input -> junction */}
        <ActiveWire
          d={inputToJunctionPath(INPUT_YS[activeInputIndex])}
          phase="in"
          reduced={reduced}
          inView={inView}
          activeInputIndex={activeInputIndex}
        />
        {/* Active junction -> output */}
        <ActiveWire
          d={junctionToOutputPath(OUTPUTS[activeOutputIndex].y)}
          phase="out"
          reduced={reduced}
          inView={inView}
          activeInputIndex={activeInputIndex}
        />

        {/* Input nodes */}
        {INPUT_YS.map((y, i) => {
          const active = i === activeInputIndex;
          return (
            <g key={`input-${i}`}>
              <circle
                cx={INPUT_X}
                cy={y}
                r={10}
                fill={SURFACE}
                stroke={active ? CYAN : NODE_DIM}
                strokeWidth={2}
              />
              {active && !reduced ? (
                <motion.circle
                  cx={INPUT_X}
                  cy={y}
                  r={10}
                  fill="none"
                  stroke={CYAN}
                  strokeWidth={2}
                  initial={{ scale: 1, opacity: 0.6 }}
                  animate={{ scale: 2.4, opacity: 0 }}
                  transition={{
                    duration: 1.6,
                    repeat: Infinity,
                    ease: "easeOut",
                  }}
                  style={{ transformOrigin: `${INPUT_X}px ${y}px` }}
                />
              ) : null}
              <text
                x={INPUT_X - 18}
                y={y + 4}
                textAnchor="end"
                fontSize={11}
                fontFamily="var(--font-mono)"
                fill={active ? CYAN : NAV_LABEL}
                style={{ letterSpacing: "0.12em", textTransform: "uppercase" }}
              >
                In 0{i + 1}
              </text>
            </g>
          );
        })}

        {/* Junction */}
        <g>
          <circle
            cx={JUNCTION_X}
            cy={JUNCTION_Y}
            r={22}
            fill={SURFACE}
            stroke={CYAN}
            strokeWidth={2}
          />
          <circle cx={JUNCTION_X} cy={JUNCTION_Y} r={6} fill={CYAN} />
          <text
            x={JUNCTION_X}
            y={JUNCTION_Y + 46}
            textAnchor="middle"
            fontSize={11}
            fontFamily="var(--font-mono)"
            fill={NAV_LABEL}
            style={{ letterSpacing: "0.18em", textTransform: "uppercase" }}
          >
            Junction
          </text>
        </g>

        {/* Output nodes */}
        {OUTPUTS.map((o, i) => {
          const active = i === activeOutputIndex;
          return (
            <g key={`out-${o.id}`}>
              <circle
                cx={OUTPUT_X}
                cy={o.y}
                r={12}
                fill={SURFACE}
                stroke={active ? CYAN : NODE_DIM}
                strokeWidth={2}
              />
              {active ? (
                <circle cx={OUTPUT_X} cy={o.y} r={5} fill={CYAN} />
              ) : null}
              <text
                x={OUTPUT_X + 22}
                y={o.y - 6}
                fontSize={11}
                fontFamily="var(--font-mono)"
                fill={active ? CYAN : NAV_LABEL}
                style={{ letterSpacing: "0.18em", textTransform: "uppercase" }}
              >
                Out 0{i + 1}
              </text>
              <text
                x={OUTPUT_X + 22}
                y={o.y + 12}
                fontSize={13}
                fontFamily="var(--font-heading)"
                fontWeight={600}
                fill={active ? HEADING : INK_DIM}
                style={{ textTransform: "capitalize" }}
              >
                {o.id}
              </text>
            </g>
          );
        })}
      </svg>
    </div>
  );
}

interface ActiveWireProps {
  readonly d: string;
  readonly phase: "in" | "out";
  readonly reduced: boolean;
  readonly inView: boolean;
  readonly activeInputIndex: number;
}

function ActiveWire({
  d,
  phase,
  reduced,
  inView,
  activeInputIndex,
}: ActiveWireProps) {
  // Key on activeInputIndex so the path redraw replays when routing changes.
  const key = `${phase}-${activeInputIndex}`;

  if (reduced) {
    return <path d={d} stroke={CYAN} strokeWidth={2.5} fill="none" />;
  }

  return (
    <g key={key}>
      <motion.path
        d={d}
        stroke={CYAN}
        strokeWidth={2.5}
        fill="none"
        initial={{ pathLength: 0, opacity: 0.7 }}
        animate={inView ? { pathLength: 1, opacity: 1 } : { pathLength: 0 }}
        transition={{
          duration: 0.7,
          delay: phase === "in" ? 0 : 0.55,
          ease: "easeInOut",
        }}
      />
      <motion.path
        d={d}
        stroke={CYAN}
        strokeWidth={6}
        fill="none"
        strokeLinecap="round"
        style={{ filter: "blur(6px)" }}
        initial={{ pathLength: 0, opacity: 0 }}
        animate={
          inView
            ? { pathLength: 1, opacity: 0.45 }
            : { pathLength: 0, opacity: 0 }
        }
        transition={{
          duration: 0.7,
          delay: phase === "in" ? 0 : 0.55,
          ease: "easeInOut",
        }}
      />
    </g>
  );
}

function inputToJunctionPath(y: number): string {
  const cp1x = INPUT_X + 110;
  const cp1y = y;
  const cp2x = JUNCTION_X - 110;
  const cp2y = JUNCTION_Y;
  return `M ${INPUT_X} ${y} C ${cp1x} ${cp1y}, ${cp2x} ${cp2y}, ${JUNCTION_X} ${JUNCTION_Y}`;
}

function junctionToOutputPath(y: number): string {
  const cp1x = JUNCTION_X + 110;
  const cp1y = JUNCTION_Y;
  const cp2x = OUTPUT_X - 110;
  const cp2y = y;
  return `M ${JUNCTION_X} ${JUNCTION_Y} C ${cp1x} ${cp1y}, ${cp2x} ${cp2y}, ${OUTPUT_X} ${y}`;
}

/* ------------------------------------------------------------------ */
/* Track grid                                                          */
/* ------------------------------------------------------------------ */

interface TrackGridProps {
  readonly routedId: TrackId;
}

function TrackGrid({ routedId }: TrackGridProps) {
  return (
    <section
      aria-labelledby="tracks-heading"
      className="pt-10 pb-16 sm:pt-14 sm:pb-20"
    >
      <h2 id="tracks-heading" className="sr-only">
        Service tracks
      </h2>
      <div className="grid gap-6 lg:grid-cols-3 lg:items-stretch">
        {TRACKS.map((track) => (
          <TrackCard
            key={track.id}
            track={track}
            routed={track.id === routedId}
          />
        ))}
      </div>
    </section>
  );
}

interface TrackCardProps {
  readonly track: ServiceTrack;
  readonly routed: boolean;
}

function TrackCard({ track, routed }: TrackCardProps) {
  const routedBadge = routed ? <RoutedHereBadge /> : null;

  if (track.highlight) {
    return (
      <motion.div
        className="relative isolate rounded-3xl p-[1.5px]"
        style={{
          background:
            "linear-gradient(140deg, #16b9e4 0%, #7c92c6 50%, #f0786a 100%)",
        }}
        animate={
          routed
            ? { boxShadow: "0 0 0 3px rgba(22, 185, 228, 0.35)" }
            : { boxShadow: "0 0 0 0 rgba(22, 185, 228, 0)" }
        }
        transition={{ duration: 0.4, ease: "easeOut" }}
      >
        <HighlightPill label={track.highlightLabel ?? "Recommended"} />
        {routedBadge}
        <div className="bg-cc-surface flex h-full flex-col rounded-[calc(1.5rem-1.5px)] p-7 sm:p-9">
          <RoutedTopEdge active={routed} />
          <TrackCardBody track={track} />
        </div>
      </motion.div>
    );
  }

  return (
    <motion.div
      className="bg-cc-card-bg border-cc-card-border relative flex h-full flex-col rounded-3xl border p-7 transition-colors sm:p-9"
      animate={{
        borderColor: routed
          ? "rgba(22, 185, 228, 0.55)"
          : "rgba(245, 241, 234, 0.12)",
        boxShadow: routed
          ? "0 0 0 1px rgba(22, 185, 228, 0.35)"
          : "0 0 0 0 rgba(22, 185, 228, 0)",
      }}
      transition={{ duration: 0.4, ease: "easeOut" }}
    >
      {routedBadge}
      <RoutedTopEdge active={routed} />
      <TrackCardBody track={track} />
    </motion.div>
  );
}

function RoutedTopEdge({ active }: { readonly active: boolean }) {
  return (
    <motion.span
      aria-hidden="true"
      className="pointer-events-none absolute inset-x-6 top-0 h-px rounded-full"
      style={{
        background: `linear-gradient(90deg, transparent 0%, ${CYAN} 50%, transparent 100%)`,
      }}
      animate={{ opacity: active ? 1 : 0 }}
      transition={{ duration: 0.4, ease: "easeOut" }}
    />
  );
}

function RoutedHereBadge() {
  return (
    <motion.span
      initial={{ opacity: 0, y: -6 }}
      animate={{ opacity: 1, y: 0 }}
      transition={{ duration: 0.3, ease: "easeOut" }}
      className="border-cc-accent text-cc-accent bg-cc-surface absolute top-0 right-9 z-10 -translate-y-1/2 rounded-full border px-3 py-1 font-mono text-[0.6rem] tracking-[0.18em] whitespace-nowrap uppercase"
    >
      Routed here
    </motion.span>
  );
}

function TrackCardBody({ track }: { readonly track: ServiceTrack }) {
  return (
    <>
      <p className="text-cc-nav-label font-mono text-[0.65rem] tracking-[0.18em] uppercase">
        {track.eyebrow}
      </p>
      <h3 className="font-heading text-cc-heading text-h3 mt-3 font-semibold">
        {track.name}
      </h3>
      <p className="text-cc-ink-dim mt-3 text-sm leading-relaxed sm:text-base">
        {track.tagline}
      </p>

      <div className="mt-6 flex items-baseline gap-2">
        <span className="font-heading text-cc-heading text-h4 font-semibold">
          {track.priceLine}
        </span>
        <span className="text-cc-nav-label font-mono text-xs">
          {track.priceNote}
        </span>
      </div>

      <div
        aria-hidden="true"
        className="border-cc-ink-faint my-6 border-t border-dashed"
      />

      <p className="text-cc-nav-label font-mono text-[0.65rem] tracking-[0.18em] uppercase">
        What you get
      </p>
      <ul className="mt-3 flex flex-1 flex-col gap-3">
        {track.bullets.map((bullet) => (
          <li key={bullet} className="flex items-start gap-3">
            <span className="text-cc-accent mt-[5px] flex-none">
              <CheckIcon />
            </span>
            <span className="text-cc-ink text-sm">{bullet}</span>
          </li>
        ))}
      </ul>

      <div className="mt-8">
        <SolidButton href={track.learnMoreHref} className="w-full">
          {track.learnMoreLabel}
        </SolidButton>
      </div>
    </>
  );
}

function HighlightPill({ label }: { readonly label: string }) {
  return (
    <span className="bg-cc-surface text-cc-accent border-cc-accent absolute top-0 left-9 z-10 -translate-y-1/2 rounded-full border px-4 py-1 font-mono text-[0.65rem] tracking-[0.18em] whitespace-nowrap uppercase">
      {label}
    </span>
  );
}

/* ------------------------------------------------------------------ */
/* Decision strip                                                      */
/* ------------------------------------------------------------------ */

function DecisionStrip() {
  return (
    <section
      id="decide"
      aria-labelledby="decision-heading"
      className="border-cc-card-border bg-cc-card-bg/40 rounded-3xl border p-6 sm:p-10"
    >
      <div className="text-center">
        <p className="text-cc-nav-label font-mono text-xs tracking-[0.18em] uppercase">
          Help me choose
        </p>
        <h2
          id="decision-heading"
          className="font-heading text-cc-heading text-h4 sm:text-h3 mt-3 font-semibold"
        >
          Which one is right for me?
        </h2>
        <p className="text-cc-ink mx-auto mt-4 max-w-2xl text-base">
          Three common starting points. Pick the row that sounds like you and
          follow the link, or book a call and we will sort it out together.
        </p>
      </div>

      <ol className="mt-10 flex flex-col gap-4">
        {DECISION_ROWS.map((row, index) => (
          <DecisionItem
            key={row.id}
            row={row}
            index={String(index + 1).padStart(2, "0")}
            order={index}
          />
        ))}
      </ol>
    </section>
  );
}

interface DecisionItemProps {
  readonly row: DecisionRow;
  readonly index: string;
  readonly order: number;
}

function DecisionItem({ row, index, order }: DecisionItemProps) {
  return (
    <motion.li
      initial={{ opacity: 0, y: 18 }}
      whileInView={{ opacity: 1, y: 0 }}
      viewport={{ once: true, margin: "-10%" }}
      transition={{ duration: 0.45, ease: "easeOut", delay: order * 0.08 }}
      className="bg-cc-surface border-cc-card-border grid gap-4 rounded-2xl border p-6 md:grid-cols-[auto_1fr] md:items-start lg:grid-cols-[auto_1fr_1.2fr_auto] lg:items-center"
    >
      <motion.span
        initial={{ opacity: 0, scale: 0.85 }}
        whileInView={{ opacity: 1, scale: 1 }}
        viewport={{ once: true, margin: "-10%" }}
        transition={{
          duration: 0.35,
          ease: "easeOut",
          delay: order * 0.08 + 0.05,
        }}
        className="text-cc-accent font-mono text-xs tracking-[0.18em] uppercase"
      >
        {index}
      </motion.span>
      <div>
        <p className="text-cc-nav-label font-mono text-[0.65rem] tracking-[0.18em] uppercase">
          You
        </p>
        <p className="font-heading text-cc-heading mt-1 text-lg font-semibold">
          {row.need}
        </p>
      </div>
      <div className="md:col-span-2 lg:col-span-1">
        <p className="text-cc-nav-label font-mono text-[0.65rem] tracking-[0.18em] uppercase">
          We point you at
        </p>
        <p className="text-cc-ink mt-1 text-sm leading-relaxed">{row.route}</p>
      </div>
      <div className="md:col-span-2 lg:col-span-1">
        <div className="flex flex-wrap gap-2 lg:justify-end">
          {row.destinations.map((destination) => (
            <OutlineButton key={destination.href} href={destination.href}>
              {destination.label}
            </OutlineButton>
          ))}
        </div>
      </div>
    </motion.li>
  );
}

/* ------------------------------------------------------------------ */
/* Enterprise band                                                     */
/* ------------------------------------------------------------------ */

function EnterpriseBand() {
  return (
    <section aria-labelledby="enterprise-heading" className="mt-20 sm:mt-28">
      <div className="border-cc-card-border bg-cc-card-bg/60 grid gap-10 rounded-3xl border p-8 sm:p-12 md:grid-cols-[1.2fr_1fr] md:items-start">
        <div>
          <p className="text-cc-nav-label font-mono text-xs tracking-[0.18em] uppercase">
            Enterprise
          </p>
          <h2
            id="enterprise-heading"
            className="font-heading text-cc-heading text-h4 sm:text-h3 mt-3 font-semibold"
          >
            One contract, every team, an SLA you wrote together.
          </h2>
          <p className="text-cc-ink mt-4 text-base leading-relaxed">
            For organizations standardizing on Hot Chocolate, Fusion, and Nitro
            across business units. We will bundle Advisory hours, an Enterprise
            Support plan, and on-site training into one agreement that matches
            how procurement actually buys.
          </p>
          <div className="mt-7 flex flex-wrap gap-3">
            <SolidButton href={ENTERPRISE_MAILTO}>Talk to sales</SolidButton>
            <OutlineButton href={SUPPORT_CONTACT}>
              Enterprise Support details
            </OutlineButton>
          </div>
        </div>
        <ul className="grid gap-4 sm:grid-cols-2">
          {ENTERPRISE_BULLETS.map((bullet, idx) => (
            <EnterpriseBulletCard
              key={bullet.label}
              bullet={bullet}
              order={idx}
            />
          ))}
        </ul>
      </div>
    </section>
  );
}

interface EnterpriseBulletCardProps {
  readonly bullet: EnterpriseBullet;
  readonly order: number;
}

function EnterpriseBulletCard({ bullet, order }: EnterpriseBulletCardProps) {
  return (
    <motion.li
      initial={{ opacity: 0, y: 12 }}
      whileInView={{ opacity: 1, y: 0 }}
      viewport={{ once: true, margin: "-10%" }}
      transition={{ duration: 0.4, ease: "easeOut", delay: order * 0.08 }}
      className="bg-cc-surface border-cc-card-border flex h-full flex-col rounded-2xl border p-5"
    >
      <span className="text-cc-nav-label font-mono text-[0.65rem] tracking-[0.18em] uppercase">
        {bullet.label}
      </span>
      <span className="text-cc-ink mt-2 text-sm leading-relaxed">
        {bullet.value}
      </span>
    </motion.li>
  );
}

/* ------------------------------------------------------------------ */
/* Trust strip                                                         */
/* ------------------------------------------------------------------ */

function TrustStrip() {
  return (
    <section aria-labelledby="trust-heading" className="mt-20 sm:mt-24">
      <h2 id="trust-heading" className="sr-only">
        Who you are working with
      </h2>
      <div className="border-cc-card-border bg-cc-card-bg/40 rounded-3xl border p-6 sm:p-8">
        <ul className="grid gap-4 sm:grid-cols-3">
          {TRUST_FACTS.map((fact, idx) => (
            <TrustFactItem key={fact.label} fact={fact} order={idx} />
          ))}
        </ul>
      </div>
    </section>
  );
}

interface TrustFactItemProps {
  readonly fact: TrustFact;
  readonly order: number;
}

function TrustFactItem({ fact, order }: TrustFactItemProps) {
  return (
    <li className="relative pb-2">
      <p className="text-cc-nav-label font-mono text-[0.65rem] tracking-[0.18em] uppercase">
        {fact.label}
      </p>
      <p className="text-cc-ink mt-2 font-mono text-sm leading-relaxed">
        {fact.value}
      </p>
      <motion.span
        aria-hidden="true"
        className="absolute bottom-0 left-0 block h-px"
        style={{
          background: `linear-gradient(90deg, ${CYAN} 0%, ${ACCENT} 100%)`,
          transformOrigin: "left center",
        }}
        initial={{ scaleX: 0, width: "0%" }}
        whileInView={{ scaleX: 1, width: "60%" }}
        viewport={{ once: true, margin: "-10%" }}
        transition={{ duration: 0.7, ease: "easeOut", delay: order * 0.12 }}
      />
    </li>
  );
}

/* ------------------------------------------------------------------ */
/* Closing CTA                                                         */
/* ------------------------------------------------------------------ */

function ClosingCta() {
  return (
    <section aria-labelledby="closing-heading" className="mt-20 mb-8 sm:mt-28">
      <div className="border-cc-card-border bg-cc-card-bg/70 grid gap-8 rounded-3xl border p-8 sm:p-12 md:grid-cols-[1.4fr_1fr] md:items-center">
        <div>
          <p className="text-cc-nav-label font-mono text-xs tracking-[0.18em] uppercase">
            Still not sure
          </p>
          <h2
            id="closing-heading"
            className="font-heading text-cc-heading text-h4 mt-3 font-semibold"
          >
            One call is usually enough to know.
          </h2>
          <p className="text-cc-ink mt-4 text-base">
            Book a 60-minute call with an engineer. Walk us through the project,
            and you will leave with a clear next step: an Advisory engagement, a
            Support plan, a Training plan, or a candid no when we are not the
            right fit.
          </p>
          <ClosingSpec />
        </div>
        <div className="flex flex-col gap-3 md:items-end">
          <SolidButton href={BOOKING_URL}>Book a 60-min call</SolidButton>
          <OutlineButton href={CONTACT_MAILTO}>Email us</OutlineButton>
        </div>
      </div>
    </section>
  );
}

function ClosingSpec() {
  const items: readonly {
    readonly label: string;
    readonly value: ReactNode;
  }[] = [
    { label: "Advisory", value: "Hourly or scoped contracting" },
    { label: "Support", value: "From $450 per month" },
    { label: "Training", value: "Custom, tailored to your team" },
    { label: "Enterprise", value: "Custom SLAs and procurement" },
  ];
  return (
    <ul className="mt-6 grid gap-3 sm:grid-cols-2">
      {items.map((item) => (
        <li key={item.label} className="flex items-start gap-3">
          <span className="text-cc-accent mt-[5px] flex-none">
            <CheckIcon />
          </span>
          <span className="text-cc-ink text-sm">
            <span className="text-cc-nav-label font-mono text-[0.65rem] tracking-[0.18em] uppercase">
              {item.label}
            </span>
            <br />
            {item.value}
          </span>
        </li>
      ))}
    </ul>
  );
}

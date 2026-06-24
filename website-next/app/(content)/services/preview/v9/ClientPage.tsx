"use client";

import {
  MotionConfig,
  motion,
  useInView,
  useMotionValue,
  useReducedMotion,
  useSpring,
  useTransform,
  type MotionValue,
} from "motion/react";
import type { ReactNode } from "react";
import { useEffect, useRef } from "react";

import { CheckIcon } from "@/src/components/CheckIcon";
import { OutlineButton, SolidButton } from "@/src/design-system/Button";

// This page is a "use client" file because the magnetic dot field relies on
// motion hooks (useMotionValue, useSpring, useTransform, useInView,
// useReducedMotion) plus a window pointermove listener. Next.js does not allow
// `export const metadata` from a client component, so robots/no-index is
// declared on the sibling server page.tsx. The route lives at
// /services/preview/v9/, an internal preview path not surfaced in navigation.
// Primary keyword: ChilliCream services. The single page accent is cyan
// (#16b9e4); the brand spectrum appears at most once, as the thin band under
// the routing matrix heading.

// The lone accent for this page (cyan). cc-* tokens carry every other color.
const ACCENT = "#16b9e4";

const BOOKING_URL = "https://calendly.com/chillicream/60min";
const CONTACT_MAILTO = "mailto:contact@chillicream.com";
const ENTERPRISE_MAILTO =
  "mailto:contact@chillicream.com?subject=Enterprise%20Services";
const SUPPORT_CONTACT = "/services/support/contact";

const EASE_OUT_QUART: readonly [number, number, number, number] = [
  0.22, 1, 0.36, 1,
];

interface ServiceTrack {
  readonly id: "advisory" | "support" | "training";
  readonly eyebrow: string;
  readonly name: string;
  readonly tagline: string;
  readonly priceLine: string;
  readonly priceNote: string;
  readonly bullets: readonly string[];
  readonly learnMoreHref: string;
  readonly learnMoreLabel: string;
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

interface DecisionRow {
  readonly id: string;
  readonly condition: string;
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
    condition: "help_right_now",
    need: "Need help right now",
    route: "A single call, or an ongoing Support plan with a defined SLA.",
    destinations: [
      { label: "Advisory consult", href: "/services/advisory" },
      { label: "Support plans", href: "/services/support" },
    ],
  },
  {
    id: "expert-delivery",
    condition: "expert_delivery",
    need: "Need expert delivery",
    route:
      "A scoped statement of work where our engineers ship the result with you.",
    destinations: [
      { label: "Advisory: Contracting", href: "/services/advisory" },
    ],
  },
  {
    id: "team-trained",
    condition: "team_trained",
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

// Shared cursor position in viewport coordinates. Off-screen until the pointer
// moves so the lattice starts perfectly at rest. Provided to the global field
// and every per-card mini-lattice so they all react to one cursor.
interface Cursor {
  readonly x: MotionValue<number>;
  readonly y: MotionValue<number>;
  readonly active: MotionValue<number>;
}

const FALLOFF_RADIUS = 180;
const MAX_PULL = 18;

export function ClientPage() {
  const cursorX = useMotionValue(-9999);
  const cursorY = useMotionValue(-9999);
  const cursorActive = useMotionValue(0);
  const reduce = useReducedMotion();

  useEffect(() => {
    if (reduce) {
      return;
    }
    const onMove = (event: PointerEvent) => {
      cursorX.set(event.clientX);
      cursorY.set(event.clientY);
      cursorActive.set(1);
    };
    const onLeave = () => {
      cursorX.set(-9999);
      cursorY.set(-9999);
      cursorActive.set(0);
    };
    window.addEventListener("pointermove", onMove);
    window.addEventListener("pointerleave", onLeave);
    return () => {
      window.removeEventListener("pointermove", onMove);
      window.removeEventListener("pointerleave", onLeave);
    };
  }, [cursorX, cursorY, cursorActive, reduce]);

  const cursor: Cursor = { x: cursorX, y: cursorY, active: cursorActive };

  return (
    <MotionConfig reducedMotion="user">
      <MagneticDotField cursor={cursor} reduce={Boolean(reduce)} />
      <div className="relative">
        <Hero cursor={cursor} reduce={Boolean(reduce)} />
        <TrackRow cursor={cursor} reduce={Boolean(reduce)} />
        <RoutingMatrix />
        <EnterpriseBand />
        <ClosingCta />
      </div>
    </MotionConfig>
  );
}

// --- Global magnetic dot field ------------------------------------------------

const FIELD_COLS = 40;
const FIELD_ROWS = 24;
const FIELD_STEP = 32;

function MagneticDotField({
  cursor,
  reduce,
}: {
  readonly cursor: Cursor;
  readonly reduce: boolean;
}) {
  const width = FIELD_COLS * FIELD_STEP;
  const height = FIELD_ROWS * FIELD_STEP;

  const dots: { col: number; row: number }[] = [];
  for (let row = 0; row < FIELD_ROWS; row++) {
    for (let col = 0; col < FIELD_COLS; col++) {
      dots.push({ col, row });
    }
  }

  return (
    <div
      aria-hidden="true"
      className="pointer-events-none fixed inset-0 -z-10 overflow-hidden"
    >
      <svg
        className="h-full w-full"
        viewBox={`0 0 ${width} ${height}`}
        preserveAspectRatio="xMidYMid slice"
      >
        {dots.map((dot) =>
          reduce ? (
            <circle
              key={`${dot.col}-${dot.row}`}
              cx={dot.col * FIELD_STEP + FIELD_STEP / 2}
              cy={dot.row * FIELD_STEP + FIELD_STEP / 2}
              r={1.6}
              fill="rgba(245, 241, 234, 0.12)"
              opacity={0.18}
            />
          ) : (
            <FieldDot
              key={`${dot.col}-${dot.row}`}
              homeX={dot.col * FIELD_STEP + FIELD_STEP / 2}
              homeY={dot.row * FIELD_STEP + FIELD_STEP / 2}
              viewWidth={width}
              viewHeight={height}
              cursor={cursor}
            />
          ),
        )}
      </svg>
    </div>
  );
}

// One spring-driven dot. It maps the shared cursor (viewport pixels) into the
// SVG's own coordinate space, then drifts toward the cursor with an
// inverse-square pull that softens to zero past the falloff radius.
function FieldDot({
  homeX,
  homeY,
  viewWidth,
  viewHeight,
  cursor,
}: {
  readonly homeX: number;
  readonly homeY: number;
  readonly viewWidth: number;
  readonly viewHeight: number;
  readonly cursor: Cursor;
}) {
  const targetX = useMotionValue(homeX);
  const targetY = useMotionValue(homeY);
  const pull = useMotionValue(0);

  const x = useSpring(targetX, { stiffness: 140, damping: 18, mass: 0.4 });
  const y = useSpring(targetY, { stiffness: 140, damping: 18, mass: 0.4 });
  const fill = useTransform(
    pull,
    [0, 1],
    ["rgba(245, 241, 234, 0.12)", ACCENT],
  );
  const opacity = useTransform(pull, [0, 1], [0.18, 0.55]);

  useEffect(() => {
    const compute = () => {
      // The field is rendered with preserveAspectRatio "slice", so map the
      // viewport cursor into SVG units using the larger of the two scales.
      const vw = window.innerWidth;
      const vh = window.innerHeight;
      const scale = Math.max(viewWidth / vw, viewHeight / vh);
      const offsetX = (viewWidth - vw * scale) / 2;
      const offsetY = (viewHeight - vh * scale) / 2;
      const cx = cursor.x.get() * scale + offsetX;
      const cy = cursor.y.get() * scale + offsetY;

      const dx = cx - homeX;
      const dy = cy - homeY;
      const dist = Math.hypot(dx, dy);

      if (cursor.active.get() === 0 || dist > FALLOFF_RADIUS || dist === 0) {
        targetX.set(homeX);
        targetY.set(homeY);
        pull.set(0);
        return;
      }

      const falloff = 1 - dist / FALLOFF_RADIUS;
      const strength = falloff * falloff;
      const move = strength * MAX_PULL;
      targetX.set(homeX + (dx / dist) * move);
      targetY.set(homeY + (dy / dist) * move);
      pull.set(strength);
    };

    const unsubX = cursor.x.on("change", compute);
    const unsubY = cursor.y.on("change", compute);
    const unsubActive = cursor.active.on("change", compute);
    return () => {
      unsubX();
      unsubY();
      unsubActive();
    };
  }, [homeX, homeY, viewWidth, viewHeight, cursor, targetX, targetY, pull]);

  return <motion.circle cx={x} cy={y} r={1.6} fill={fill} opacity={opacity} />;
}

// --- Per-card mini-lattice ----------------------------------------------------

const MINI_COLS = 9;
const MINI_ROWS = 4;
const MINI_STEP = 9;
const MINI_RADIUS = 26;

// A 9x4 anchor lattice rendered above each card corner. It reacts to the same
// global cursor: when the pointer is over the card, its dots drift toward the
// cursor and glow cyan, echoing the field at card scale.
function MiniLattice({ cursor }: { readonly cursor: Cursor }) {
  const wrapRef = useRef<SVGSVGElement>(null);
  const width = (MINI_COLS - 1) * MINI_STEP;
  const height = (MINI_ROWS - 1) * MINI_STEP;

  const dots: { col: number; row: number }[] = [];
  for (let row = 0; row < MINI_ROWS; row++) {
    for (let col = 0; col < MINI_COLS; col++) {
      dots.push({ col, row });
    }
  }

  return (
    <svg
      ref={wrapRef}
      aria-hidden="true"
      width={width + 8}
      height={height + 8}
      viewBox={`-4 -4 ${width + 8} ${height + 8}`}
      className="overflow-visible"
    >
      {dots.map((dot) => (
        <MiniDot
          key={`${dot.col}-${dot.row}`}
          homeX={dot.col * MINI_STEP}
          homeY={dot.row * MINI_STEP}
          wrapRef={wrapRef}
          cursor={cursor}
        />
      ))}
    </svg>
  );
}

function MiniDot({
  homeX,
  homeY,
  wrapRef,
  cursor,
}: {
  readonly homeX: number;
  readonly homeY: number;
  readonly wrapRef: React.RefObject<SVGSVGElement | null>;
  readonly cursor: Cursor;
}) {
  const targetX = useMotionValue(homeX);
  const targetY = useMotionValue(homeY);
  const pull = useMotionValue(0);

  const x = useSpring(targetX, { stiffness: 180, damping: 16, mass: 0.3 });
  const y = useSpring(targetY, { stiffness: 180, damping: 16, mass: 0.3 });
  const fill = useTransform(
    pull,
    [0, 1],
    ["rgba(245, 241, 234, 0.16)", ACCENT],
  );
  const opacity = useTransform(pull, [0, 1], [0.22, 0.6]);

  useEffect(() => {
    const compute = () => {
      const svg = wrapRef.current;
      if (svg === null || cursor.active.get() === 0) {
        targetX.set(homeX);
        targetY.set(homeY);
        pull.set(0);
        return;
      }
      const rect = svg.getBoundingClientRect();
      // Map cursor into the mini lattice's local units (origin at dot 0,0,
      // which sits 4px inside the padded viewBox).
      const cx = cursor.x.get() - rect.left - 4;
      const cy = cursor.y.get() - rect.top - 4;
      const dx = cx - homeX;
      const dy = cy - homeY;
      const dist = Math.hypot(dx, dy);

      if (dist > MINI_RADIUS || dist === 0) {
        targetX.set(homeX);
        targetY.set(homeY);
        pull.set(0);
        return;
      }

      const falloff = 1 - dist / MINI_RADIUS;
      const strength = falloff * falloff;
      const move = strength * 5;
      targetX.set(homeX + (dx / dist) * move);
      targetY.set(homeY + (dy / dist) * move);
      pull.set(strength);
    };

    const unsubX = cursor.x.on("change", compute);
    const unsubY = cursor.y.on("change", compute);
    const unsubActive = cursor.active.on("change", compute);
    return () => {
      unsubX();
      unsubY();
      unsubActive();
    };
  }, [homeX, homeY, wrapRef, cursor, targetX, targetY, pull]);

  return <motion.circle cx={x} cy={y} r={1.4} fill={fill} opacity={opacity} />;
}

// --- Sections -----------------------------------------------------------------

function Hero({
  cursor,
  reduce,
}: {
  readonly cursor: Cursor;
  readonly reduce: boolean;
}) {
  const ref = useRef<HTMLDivElement>(null);
  const inView = useInView(ref, { once: true, amount: 0.4 });

  return (
    <section ref={ref} className="pt-10 pb-16 sm:pt-16 sm:pb-20">
      <div className="grid gap-10 md:grid-cols-[1.5fr_1fr] md:items-center">
        <div>
          <p className="text-cc-nav-label font-mono text-xs tracking-[0.18em] uppercase">
            ChilliCream GraphQL services
          </p>
          <h1 className="font-heading text-cc-heading text-h2 sm:text-h1 mt-5 max-w-xl font-semibold tracking-tight text-balance">
            Tell us where you are. The right service comes to meet you.
          </h1>
          <motion.p
            initial={reduce ? { opacity: 1 } : { opacity: 0, y: 12 }}
            animate={inView ? { opacity: 1, y: 0 } : undefined}
            transition={{ duration: 0.5, ease: EASE_OUT_QUART, delay: 0.1 }}
            className="text-cc-ink mt-6 max-w-xl text-base text-pretty sm:text-lg"
          >
            Three ways to work with the team behind Hot Chocolate, Fusion, and
            Nitro: hands-on Advisory, ongoing Support plans, or Corporate
            Training. Move your cursor and the field gathers, the same way we
            route you to the answer.
          </motion.p>
          <div className="mt-9 flex flex-wrap items-center gap-3">
            <SolidButton href={BOOKING_URL}>Book a 60-min call</SolidButton>
            <OutlineButton href="#decide">Help me choose</OutlineButton>
          </div>
        </div>

        <div className="flex justify-center md:justify-end">
          <HeroCluster cursor={cursor} reduce={reduce} inView={inView} />
        </div>
      </div>
    </section>
  );
}

// A small "what we offer" stub. On first view its dots condense from a loose
// ring into a tight cluster, demonstrating the page's magnetic interaction.
function HeroCluster({
  cursor,
  reduce,
  inView,
}: {
  readonly cursor: Cursor;
  readonly reduce: boolean;
  readonly inView: boolean;
}) {
  return (
    <div className="border-cc-card-border bg-cc-card-bg/50 w-full max-w-[15rem] rounded-3xl border p-6">
      <p className="text-cc-nav-label font-mono text-[0.65rem] tracking-[0.18em] uppercase">
        What we offer
      </p>
      <div className="mt-4 flex h-28 items-center justify-center">
        <ClusterMotif cursor={cursor} reduce={reduce} inView={inView} />
      </div>
      <ul className="mt-4 flex flex-col gap-1.5">
        {TRACKS.map((track) => (
          <li
            key={track.id}
            className="text-cc-ink font-heading flex items-center gap-2 text-sm"
          >
            <span
              className="h-1.5 w-1.5 flex-none rounded-full"
              style={{ backgroundColor: ACCENT }}
            />
            {track.name}
          </li>
        ))}
      </ul>
    </div>
  );
}

function ClusterMotif({
  cursor,
  reduce,
  inView,
}: {
  readonly cursor: Cursor;
  readonly reduce: boolean;
  readonly inView: boolean;
}) {
  const wrapRef = useRef<SVGSVGElement>(null);
  const count = 14;
  const dots = Array.from({ length: count }, (_, i) => {
    const angle = (i / count) * Math.PI * 2;
    return {
      i,
      restX: 50 + Math.cos(angle) * 34,
      restY: 28 + Math.sin(angle) * 22,
      clusterX: 50 + Math.cos(angle) * 9,
      clusterY: 28 + Math.sin(angle) * 6,
    };
  });

  return (
    <svg
      ref={wrapRef}
      aria-hidden="true"
      viewBox="0 0 100 56"
      className="h-full w-full overflow-visible"
    >
      {dots.map((dot) => (
        <ClusterDot
          key={dot.i}
          dot={dot}
          wrapRef={wrapRef}
          cursor={cursor}
          reduce={reduce}
          inView={inView}
        />
      ))}
    </svg>
  );
}

function ClusterDot({
  dot,
  wrapRef,
  cursor,
  reduce,
  inView,
}: {
  readonly dot: {
    readonly restX: number;
    readonly restY: number;
    readonly clusterX: number;
    readonly clusterY: number;
  };
  readonly wrapRef: React.RefObject<SVGSVGElement | null>;
  readonly cursor: Cursor;
  readonly reduce: boolean;
  readonly inView: boolean;
}) {
  const targetX = useMotionValue(dot.restX);
  const targetY = useMotionValue(dot.restY);
  const x = useSpring(targetX, { stiffness: 120, damping: 20, mass: 0.5 });
  const y = useSpring(targetY, { stiffness: 120, damping: 20, mass: 0.5 });

  // Enter-view-once condense into the cluster.
  useEffect(() => {
    if (reduce) {
      targetX.set(dot.clusterX);
      targetY.set(dot.clusterY);
      return;
    }
    if (inView) {
      targetX.set(dot.clusterX);
      targetY.set(dot.clusterY);
    }
  }, [inView, reduce, dot, targetX, targetY]);

  // Hover pull, once the cluster has condensed.
  useEffect(() => {
    if (reduce) {
      return;
    }
    const compute = () => {
      const svg = wrapRef.current;
      if (svg === null || cursor.active.get() === 0 || !inView) {
        targetX.set(dot.clusterX);
        targetY.set(dot.clusterY);
        return;
      }
      const rect = svg.getBoundingClientRect();
      const scaleX = 100 / rect.width;
      const scaleY = 56 / rect.height;
      const cx = (cursor.x.get() - rect.left) * scaleX;
      const cy = (cursor.y.get() - rect.top) * scaleY;
      const dx = cx - dot.clusterX;
      const dy = cy - dot.clusterY;
      const dist = Math.hypot(dx, dy);
      if (dist > 60 || dist === 0) {
        targetX.set(dot.clusterX);
        targetY.set(dot.clusterY);
        return;
      }
      const strength = (1 - dist / 60) * (1 - dist / 60);
      targetX.set(dot.clusterX + (dx / dist) * strength * 10);
      targetY.set(dot.clusterY + (dy / dist) * strength * 7);
    };
    const unsubX = cursor.x.on("change", compute);
    const unsubActive = cursor.active.on("change", compute);
    return () => {
      unsubX();
      unsubActive();
    };
  }, [inView, reduce, dot, wrapRef, cursor, targetX, targetY]);

  return <motion.circle cx={x} cy={y} r={1.8} fill={ACCENT} opacity={0.55} />;
}

function TrackRow({
  cursor,
  reduce,
}: {
  readonly cursor: Cursor;
  readonly reduce: boolean;
}) {
  const ref = useRef<HTMLDivElement>(null);
  const inView = useInView(ref, { once: true, amount: 0.2 });

  return (
    <section aria-labelledby="tracks-heading" className="pb-16 sm:pb-20">
      <h2 id="tracks-heading" className="sr-only">
        Service tracks
      </h2>
      <div ref={ref} className="grid gap-6 lg:grid-cols-3 lg:items-stretch">
        {TRACKS.map((track, index) => (
          <TrackCard
            key={track.id}
            track={track}
            index={index}
            cursor={cursor}
            reduce={reduce}
            inView={inView}
          />
        ))}
      </div>
    </section>
  );
}

function TrackCard({
  track,
  index,
  cursor,
  reduce,
  inView,
}: {
  readonly track: ServiceTrack;
  readonly index: number;
  readonly cursor: Cursor;
  readonly reduce: boolean;
  readonly inView: boolean;
}) {
  return (
    <motion.div
      initial={reduce ? { opacity: 1 } : { opacity: 0, y: 18 }}
      animate={inView ? { opacity: 1, y: 0 } : undefined}
      transition={{
        duration: 0.5,
        ease: EASE_OUT_QUART,
        delay: Math.min(index * 0.08, 0.24),
      }}
      className="bg-cc-card-bg border-cc-card-border hover:border-cc-card-border-hover relative flex h-full flex-col rounded-3xl border p-7 transition-colors sm:p-9"
    >
      <div className="absolute top-6 right-6">
        {reduce ? <StaticMiniLattice /> : <MiniLattice cursor={cursor} />}
      </div>

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
            <span className="mt-[5px] flex-none" style={{ color: ACCENT }}>
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
    </motion.div>
  );
}

function StaticMiniLattice() {
  const width = (MINI_COLS - 1) * MINI_STEP;
  const height = (MINI_ROWS - 1) * MINI_STEP;
  const dots: { col: number; row: number }[] = [];
  for (let row = 0; row < MINI_ROWS; row++) {
    for (let col = 0; col < MINI_COLS; col++) {
      dots.push({ col, row });
    }
  }
  return (
    <svg
      aria-hidden="true"
      width={width + 8}
      height={height + 8}
      viewBox={`-4 -4 ${width + 8} ${height + 8}`}
    >
      {dots.map((dot) => (
        <circle
          key={`${dot.col}-${dot.row}`}
          cx={dot.col * MINI_STEP}
          cy={dot.row * MINI_STEP}
          r={1.4}
          fill="rgba(245, 241, 234, 0.16)"
          opacity={0.22}
        />
      ))}
    </svg>
  );
}

function RoutingMatrix() {
  return (
    <section
      id="decide"
      aria-labelledby="routing-heading"
      className="border-cc-card-border bg-cc-card-bg/40 rounded-3xl border p-6 sm:p-10"
    >
      <div className="text-center">
        <p className="text-cc-nav-label font-mono text-xs tracking-[0.18em] uppercase">
          Help me choose
        </p>
        <h2
          id="routing-heading"
          className="font-heading text-cc-heading text-h4 sm:text-h3 mt-3 font-semibold"
        >
          Tell us where you are.
        </h2>
        <div
          aria-hidden="true"
          className="mx-auto mt-5 h-px w-32 rounded-full"
          style={{
            background:
              "linear-gradient(90deg, #16b9e4 0%, #7c92c6 50%, #f0786a 100%)",
          }}
        />
        <p className="text-cc-ink mx-auto mt-5 max-w-2xl text-base">
          Three common starting points. Pick the row that sounds like you and
          follow the link, or book a call and we will sort it out together.
        </p>
      </div>

      <div className="mt-10 hidden grid-cols-[auto_1fr_auto] gap-x-8 gap-y-6 md:grid">
        <div className="text-cc-nav-label border-cc-ink-faint col-span-3 grid grid-cols-subgrid border-b pb-3 font-mono text-[0.65rem] tracking-[0.18em] uppercase">
          <span>If</span>
          <span>Then</span>
          <span>Go</span>
        </div>
        {DECISION_ROWS.map((row) => (
          <div
            key={row.id}
            className="border-cc-ink-faint col-span-3 grid grid-cols-subgrid items-center border-b pb-6 last:border-b-0 last:pb-0"
          >
            <code className="font-mono text-sm" style={{ color: ACCENT }}>
              {row.condition}
            </code>
            <p className="text-cc-ink text-sm leading-relaxed">
              <span className="text-cc-heading font-heading mr-2 font-semibold">
                {row.need}.
              </span>
              {row.route}
            </p>
            <div className="flex flex-wrap justify-end gap-2">
              {row.destinations.map((destination) => (
                <OutlineButton key={destination.href} href={destination.href}>
                  {destination.label}
                </OutlineButton>
              ))}
            </div>
          </div>
        ))}
      </div>

      <ol className="mt-10 flex flex-col gap-4 md:hidden">
        {DECISION_ROWS.map((row) => (
          <li
            key={row.id}
            className="bg-cc-surface border-cc-card-border rounded-2xl border p-6"
          >
            <code className="font-mono text-xs" style={{ color: ACCENT }}>
              {row.condition}
            </code>
            <p className="font-heading text-cc-heading mt-2 text-lg font-semibold">
              {row.need}
            </p>
            <p className="text-cc-ink mt-2 text-sm leading-relaxed">
              {row.route}
            </p>
            <div className="mt-4 flex flex-wrap gap-2">
              {row.destinations.map((destination) => (
                <OutlineButton key={destination.href} href={destination.href}>
                  {destination.label}
                </OutlineButton>
              ))}
            </div>
          </li>
        ))}
      </ol>
    </section>
  );
}

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
          {ENTERPRISE_BULLETS.map((bullet) => (
            <li
              key={bullet.label}
              className="bg-cc-surface border-cc-card-border flex h-full flex-col rounded-2xl border p-5"
            >
              <span className="text-cc-nav-label font-mono text-[0.65rem] tracking-[0.18em] uppercase">
                {bullet.label}
              </span>
              <span className="text-cc-ink mt-2 text-sm leading-relaxed">
                {bullet.value}
              </span>
            </li>
          ))}
        </ul>
      </div>
    </section>
  );
}

function ClosingCta() {
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
          <ul className="mt-6 grid gap-3 sm:grid-cols-2">
            {items.map((item) => (
              <li key={item.label} className="flex items-start gap-3">
                <span className="mt-[5px] flex-none" style={{ color: ACCENT }}>
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
        </div>
        <div className="flex flex-col gap-3 md:items-end">
          <SolidButton href={BOOKING_URL}>Book a 60-min call</SolidButton>
          <OutlineButton href={CONTACT_MAILTO}>Email us</OutlineButton>
        </div>
      </div>
    </section>
  );
}

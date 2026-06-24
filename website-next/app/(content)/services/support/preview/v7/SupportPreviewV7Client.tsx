"use client";

import { animate, motion, useInView, useReducedMotion } from "motion/react";
import type { ReactNode } from "react";
import { useEffect, useRef } from "react";

import { CheckIcon } from "@/src/components/CheckIcon";
import { OutlineButton, SolidButton } from "@/src/design-system/Button";

type PlanName = "Community" | "Startup" | "Business" | "Enterprise";

interface Plan {
  readonly name: PlanName;
  readonly price: string;
  readonly priceNumeric?: number;
  readonly priceNote?: string;
  readonly tagline: string;
  readonly description: string;
  readonly perks: readonly string[];
  readonly cta: { readonly label: string; readonly href: string };
  readonly highlight?: boolean;
}

const PLANS: readonly Plan[] = [
  {
    name: "Community",
    price: "Free",
    tagline: "For hackers and side projects",
    description: "For personal or non-commercial projects, to start hacking.",
    perks: ["Public Slack Channel"],
    cta: { label: "Join Slack", href: "https://slack.chillicream.com/" },
  },
  {
    name: "Startup",
    price: "$450",
    priceNumeric: 450,
    priceNote: "per month",
    tagline: "Small teams, steady cadence",
    description:
      "For small teams with moderate bandwidth and projects of low to medium complexity.",
    perks: ["Private Slack Channel", "2 critical incidents"],
    cta: {
      label: "Contact Us",
      href: "/services/support/contact?plan=Startup",
    },
  },
  {
    name: "Business",
    price: "$1,300",
    priceNumeric: 1300,
    priceNote: "per month",
    tagline: "Most popular",
    description: "For larger teams with business-critical projects.",
    perks: [
      "Private Slack Channel",
      "5 critical incidents",
      "2 non-critical incidents",
      "Email support",
    ],
    cta: {
      label: "Contact Us",
      href: "/services/support/contact?plan=Business",
    },
    highlight: true,
  },
  {
    name: "Enterprise",
    price: "Custom",
    tagline: "Whole-org coverage with SLAs",
    description:
      "For the whole organization, all your teams and business units, and with tailor made SLAs.",
    perks: [
      "Private Slack Channel",
      "Unlimited critical incidents",
      "10 non-critical incidents",
      "Phone support",
      "Dedicated account manager",
      "Status reviews",
    ],
    cta: {
      label: "Contact Us",
      href: "/services/support/contact?plan=Enterprise",
    },
  },
];

const PLAN_NAMES: readonly PlanName[] = [
  "Community",
  "Startup",
  "Business",
  "Enterprise",
];

type CellValue = boolean | string;

interface ComparisonRow {
  readonly title: string;
  readonly hint?: string;
  readonly values: readonly [CellValue, CellValue, CellValue, CellValue];
}

interface ComparisonGroup {
  readonly title: string;
  readonly rows: readonly ComparisonRow[];
}

const COMPARISON: readonly ComparisonGroup[] = [
  {
    title: "Response & incidents",
    rows: [
      {
        title: "Critical Incidents",
        hint: "Production is impacted: down, data loss, or hard outage.",
        values: [
          false,
          "2 (next business day)",
          "5 (next business day)",
          "Unlimited (24 hours)",
        ],
      },
      {
        title: "Non-critical Incidents",
        hint: "Bugs and questions that block work but not production.",
        values: [false, false, "5 (3 business days)", "10 (next business day)"],
      },
    ],
  },
  {
    title: "Channels",
    rows: [
      { title: "Public Slack Channel", values: [true, true, true, true] },
      { title: "Private Slack Channel", values: [false, true, true, true] },
      {
        title: "Private Issue Tracking Board",
        values: [false, false, true, true],
      },
      { title: "Email Support", values: [false, false, true, true] },
      { title: "Phone Support", values: [false, false, false, true] },
    ],
  },
  {
    title: "Strategic",
    rows: [
      {
        title: "Dedicated Account Manager",
        values: [false, false, false, true],
      },
      {
        title: "Status Reviews",
        hint: "Recurring check-ins on roadmap, upgrades, and posture.",
        values: [false, false, false, true],
      },
    ],
  },
];

interface FaqItem {
  readonly q: string;
  readonly a: string;
}

const FAQ: readonly FaqItem[] = [
  {
    q: "What counts as a critical incident?",
    a: "An incident is critical when a production system you run on Hot Chocolate, Fusion, or Nitro is down, returning wrong data, or otherwise hard-blocked. Anything that degrades a live user experience qualifies. Local dev issues and questions are non-critical.",
  },
  {
    q: "How fast do you respond?",
    a: "Startup and Business respond to critical incidents the next business day. Enterprise responds to critical incidents within 24 hours, any day. Non-critical incidents are 3 business days on Business and next business day on Enterprise. The Community plan is best-effort in public Slack with no guarantee.",
  },
  {
    q: "How is an incident opened and tracked?",
    a: "Paid plans get a private Slack channel staffed by ChilliCream engineers. Business and Enterprise additionally get a private issue tracking board so every incident has a ticket, an owner, and a written history you can audit.",
  },
  {
    q: "What happens when I use up my incidents in a month?",
    a: "We do not cut you off mid-fire. We keep working the incident and reach out to discuss either a one-time top-up or moving to the next plan. Incidents do not roll over month to month.",
  },
  {
    q: "Do you support self-hosted Nitro and on-prem deployments?",
    a: "Yes. Business and Enterprise both support self-hosted Nitro, Fusion gateways, and Hot Chocolate services running in your own cloud or on-prem. Enterprise adds tailored SLAs and a named account manager who knows your topology.",
  },
  {
    q: "Can we change plans later?",
    a: "Yes. You can upgrade at any time and the new SLA takes effect immediately. Downgrades take effect at the start of the next billing month so an in-flight incident never falls between plans.",
  },
];

export function SupportPreviewV7Client() {
  return (
    <>
      <Hero />
      <ResponseRace />
      <PlanGrid />
      <CoverageStrip />
      <ComparisonMatrix />
      <EnterpriseBand />
      <FaqSection />
      <ClosingCta />
    </>
  );
}

/* ============================== Hero ============================== */

function Hero() {
  return (
    <section className="py-20 text-center sm:py-28">
      <div className="text-cc-nav-label mb-5 font-mono text-xs font-semibold tracking-widest uppercase">
        Support
      </div>
      <h1 className="font-heading text-cc-heading mx-auto max-w-3xl text-5xl leading-tight font-semibold tracking-tight sm:text-6xl lg:text-7xl">
        GraphQL support plans, on a clock you can see.
      </h1>
      <p className="text-cc-prose lead mx-auto mt-6 max-w-2xl text-lg sm:text-xl">
        Watch the SLA before you read the table. Four plans for Hot Chocolate,
        Fusion, and Nitro, from community Slack to a 24 hour critical clock.
      </p>
      <div className="mt-10 flex flex-col items-center justify-center gap-3 sm:flex-row sm:gap-4">
        <SolidButton href="#plans">Compare plans</SolidButton>
        <OutlineButton href="/services/support/contact">
          Talk to us
        </OutlineButton>
      </div>
      <HeroAxis />
    </section>
  );
}

function HeroAxis() {
  const ref = useRef<SVGSVGElement>(null);
  const isInView = useInView(ref, { once: true, margin: "-10%" });
  const reduce = useReducedMotion();
  const animated = !reduce && isInView;

  return (
    <div className="mx-auto mt-12 max-w-xl">
      <svg
        ref={ref}
        viewBox="0 0 600 40"
        className="w-full"
        aria-hidden
        role="img"
      >
        <motion.line
          x1="12"
          x2="588"
          y1="22"
          y2="22"
          stroke="currentColor"
          strokeWidth="1"
          strokeLinecap="round"
          className="text-cc-card-border-hover"
          initial={reduce ? false : { pathLength: 0 }}
          animate={
            animated
              ? { pathLength: 1 }
              : reduce
                ? { pathLength: 1 }
                : { pathLength: 0 }
          }
          transition={{ duration: 1.4, ease: [0.22, 1, 0.36, 1] }}
        />
        {[0, 24, 48, 72].map((h) => {
          const x = 12 + (h / 72) * 576;
          return (
            <g key={h}>
              <line
                x1={x}
                x2={x}
                y1="18"
                y2="26"
                stroke="currentColor"
                strokeWidth="1"
                className="text-cc-ink-faint"
              />
              <text
                x={x}
                y="38"
                textAnchor="middle"
                className="fill-cc-ink-dim font-mono"
                style={{ fontSize: 9 }}
              >
                {h}h
              </text>
            </g>
          );
        })}
        <motion.circle
          cx="12"
          cy="22"
          r="5"
          className="fill-cc-accent"
          initial={reduce ? false : { opacity: 0, scale: 0.5 }}
          animate={
            animated
              ? { opacity: [0, 1, 0.6, 1], scale: [0.5, 1.2, 1, 1.15] }
              : reduce
                ? { opacity: 1, scale: 1 }
                : { opacity: 0, scale: 0.5 }
          }
          transition={{
            duration: 2,
            ease: "easeInOut",
            repeat: animated ? Infinity : 0,
            repeatType: "loop",
          }}
        />
      </svg>
      <p className="text-cc-ink-dim mt-3 text-center font-mono text-[11px] tracking-widest uppercase">
        Incident opened, first engineer response
      </p>
    </div>
  );
}

/* ========================== Response Race ========================= */

interface Lane {
  readonly id: PlanName;
  readonly label: string;
  readonly hours: number; // first response, clamped to axis 0-72
  readonly badge: string;
  readonly tone: "ink" | "accent";
  readonly variant: "solid" | "dashed";
  // When true, render the badge as a numeric tick-count ("24h"). When false,
  // render the badge text verbatim (v1 fact: "next business day"). Only set
  // for lanes whose hour count is itself a contractual figure from v1.
  readonly tickCount: boolean;
}

const AXIS_MAX = 72;

const LANES: readonly Lane[] = [
  {
    id: "Community",
    label: "Community",
    hours: AXIS_MAX, // runs past the edge, best-effort
    badge: "best-effort",
    tone: "ink",
    variant: "dashed",
    tickCount: false,
  },
  {
    id: "Startup",
    label: "Startup",
    hours: 32,
    badge: "next business day",
    tone: "ink",
    variant: "solid",
    tickCount: false,
  },
  {
    id: "Business",
    label: "Business",
    hours: 32,
    badge: "next business day",
    tone: "ink",
    variant: "solid",
    tickCount: false,
  },
  {
    id: "Enterprise",
    label: "Enterprise",
    hours: 24,
    badge: "within 24 hours",
    tone: "accent",
    variant: "solid",
    tickCount: true,
  },
];

function ResponseRace() {
  const ref = useRef<HTMLDivElement>(null);
  // One-shot reveal on first entry. Replaying on every re-entry coupled
  // perceived motion to scroll position and read as jank.
  const isInView = useInView(ref, { once: true, margin: "-15%" });
  const reduce = useReducedMotion();
  const animated = !reduce && isInView;

  return (
    <section ref={ref} className="py-16">
      <div className="mx-auto mb-10 max-w-2xl text-center">
        <div className="text-cc-nav-label mb-3 font-mono text-xs font-semibold tracking-widest uppercase">
          Response Race
        </div>
        <h2 className="font-heading text-cc-heading text-3xl font-semibold tracking-tight sm:text-4xl">
          From incident opened to engineer responding.
        </h2>
        <p className="text-cc-ink-dim mt-3 text-base sm:text-lg">
          A critical incident lands at t=0. Watch how far each plan runs before
          the first engineer answers.
        </p>
      </div>

      <div className="border-cc-card-border bg-cc-card-bg overflow-hidden rounded-2xl border p-6 sm:p-8">
        <div className="space-y-4">
          {LANES.map((lane, i) => (
            <LaneRow
              key={lane.id}
              lane={lane}
              index={i}
              animated={animated}
              reduce={!!reduce}
            />
          ))}
        </div>

        <TimeAxis />

        <div className="text-cc-ink-dim mt-4 flex flex-wrap items-center gap-3 font-mono text-[11px] tracking-widest uppercase">
          <span className="inline-flex items-center gap-2">
            <span className="bg-cc-accent inline-block h-2 w-2 rounded-full" />
            Critical incident, first engineer response
          </span>
        </div>
      </div>
    </section>
  );
}

interface LaneRowProps {
  readonly lane: Lane;
  readonly index: number;
  readonly animated: boolean;
  readonly reduce: boolean;
}

function LaneRow({ lane, index, animated, reduce }: LaneRowProps) {
  const widthPct = Math.min(100, (lane.hours / AXIS_MAX) * 100);
  const isAccent = lane.tone === "accent";
  const isDashed = lane.variant === "dashed";
  const delay = 0.15 + index * 0.2;
  // Community runs past the edge as a fading dashed line.
  const duration = isDashed ? 2.4 : 1.6;

  const enterTransition = {
    duration,
    delay,
    ease: [0.22, 1, 0.36, 1] as const,
  };

  return (
    <div className="grid grid-cols-[7rem_1fr_5.5rem] items-center gap-3 sm:grid-cols-[8rem_1fr_7rem] sm:gap-4">
      <div
        className={`font-heading text-sm font-medium sm:text-base ${
          isAccent ? "text-cc-accent" : "text-cc-heading"
        }`}
      >
        {lane.label}
      </div>

      <div className="relative h-7">
        <div className="border-cc-card-border bg-cc-surface/40 absolute inset-0 rounded-full border" />
        <motion.div
          className={`absolute inset-y-0 left-0 rounded-full ${
            isAccent
              ? "bg-cc-accent/80"
              : isDashed
                ? "bg-cc-ink-faint"
                : "bg-cc-ink-faint/80"
          } ${isDashed ? "[mask-image:linear-gradient(90deg,#000,#000_70%,transparent)]" : ""}`}
          style={{
            backgroundImage: isDashed
              ? "repeating-linear-gradient(90deg, currentColor 0 6px, transparent 6px 12px)"
              : undefined,
            color: isDashed ? "rgba(245, 241, 234, 0.35)" : undefined,
            backgroundColor: isDashed ? "transparent" : undefined,
          }}
          initial={reduce ? false : { width: "0%", opacity: isDashed ? 0 : 1 }}
          animate={
            animated || reduce
              ? { width: `${widthPct}%`, opacity: 1 }
              : { width: "0%", opacity: isDashed ? 0 : 1 }
          }
          transition={enterTransition}
        />
        <LaneMarker
          hours={lane.hours}
          animated={animated}
          reduce={reduce}
          delay={delay + duration * 0.85}
          isAccent={isAccent}
          isDashed={isDashed}
        />
      </div>

      <div className="text-right">
        <HourBadge
          lane={lane}
          animated={animated}
          reduce={reduce}
          delay={delay + duration * 0.6}
        />
        <div className="text-cc-ink-dim mt-1 font-mono text-[10px] tracking-wider uppercase">
          {lane.badge}
        </div>
      </div>
    </div>
  );
}

interface LaneMarkerProps {
  readonly hours: number;
  readonly animated: boolean;
  readonly reduce: boolean;
  readonly delay: number;
  readonly isAccent: boolean;
  readonly isDashed: boolean;
}

function LaneMarker({
  hours,
  animated,
  reduce,
  delay,
  isAccent,
  isDashed,
}: LaneMarkerProps) {
  if (isDashed) {
    return null;
  }
  const leftPct = Math.min(100, (hours / AXIS_MAX) * 100);
  return (
    <motion.span
      className={`absolute top-1/2 -translate-x-1/2 -translate-y-1/2 ${
        isAccent ? "bg-cc-accent" : "bg-cc-heading/80"
      } block h-3 w-3 rounded-full ring-2 ${
        isAccent ? "ring-cc-accent/30" : "ring-cc-card-border"
      }`}
      style={{ left: `${leftPct}%` }}
      initial={reduce ? false : { scale: 0, opacity: 0 }}
      animate={
        animated
          ? { scale: [0, 1.4, 1], opacity: 1 }
          : reduce
            ? { scale: 1, opacity: 1 }
            : { scale: 0, opacity: 0 }
      }
      transition={{ duration: 0.5, delay, ease: "easeOut" }}
    />
  );
}

interface HourBadgeProps {
  readonly lane: Lane;
  readonly animated: boolean;
  readonly reduce: boolean;
  readonly delay: number;
}

// Renders the lane's response figure. The numeric tick-count is reserved for
// lanes whose hour count is itself a v1 figure (Enterprise's 24h). Other
// lanes render the v1 phrase verbatim ("NBD" with the badge supplying the
// full "next business day" text), so we never present an invented hour count
// next to real SLA copy.
function HourBadge({ lane, animated, reduce, delay }: HourBadgeProps) {
  const spanRef = useRef<HTMLSpanElement>(null);
  const isDashed = lane.variant === "dashed";
  const isAccent = lane.tone === "accent";
  const tickCount = lane.tickCount;

  let staticLabel: string;
  if (isDashed) {
    staticLabel = "best-effort";
  } else if (!tickCount) {
    staticLabel = "NBD";
  } else {
    staticLabel = `${lane.hours}h`;
  }

  const initial = tickCount && !reduce ? "0h" : staticLabel;

  useEffect(() => {
    const el = spanRef.current;
    if (!el) {
      return;
    }
    if (!tickCount) {
      el.textContent = staticLabel;
      return;
    }
    if (reduce) {
      el.textContent = staticLabel;
      return;
    }
    if (!animated) {
      el.textContent = "0h";
      return;
    }
    const controls = animate(0, lane.hours, {
      duration: 1.2,
      delay,
      ease: [0.22, 1, 0.36, 1],
      onUpdate: (v) => {
        if (spanRef.current) {
          spanRef.current.textContent = `${Math.round(v)}h`;
        }
      },
    });
    return () => controls.stop();
  }, [animated, reduce, tickCount, lane.hours, delay, staticLabel]);

  return (
    <span
      ref={spanRef}
      className={`font-heading text-base font-semibold tabular-nums sm:text-lg ${
        isAccent
          ? "text-cc-accent"
          : isDashed
            ? "text-cc-ink-dim"
            : "text-cc-heading"
      }`}
    >
      {initial}
    </span>
  );
}

function TimeAxis() {
  const ticks = [0, 24, 48, 72];
  return (
    <div className="mt-6 grid grid-cols-[7rem_1fr_5.5rem] items-center gap-3 sm:grid-cols-[8rem_1fr_7rem] sm:gap-4">
      <div />
      <div className="relative h-6">
        <div className="bg-cc-card-border absolute top-1/2 right-0 left-0 h-px -translate-y-1/2" />
        {ticks.map((h) => {
          const pct = (h / AXIS_MAX) * 100;
          return (
            <div
              key={h}
              className="absolute top-0 bottom-0 flex flex-col items-center"
              style={{ left: `${pct}%`, transform: "translateX(-50%)" }}
            >
              <div className="bg-cc-card-border-hover h-2 w-px" />
              <div className="text-cc-ink-dim mt-1 font-mono text-[10px] tracking-wider">
                {h === 0 ? "t=0" : `${h}h`}
              </div>
            </div>
          );
        })}
      </div>
      <div className="text-cc-ink-dim text-right font-mono text-[10px] tracking-widest uppercase">
        hours
      </div>
    </div>
  );
}

/* ============================ Plan Grid =========================== */

function PlanGrid() {
  return (
    <section id="plans" className="py-16">
      <div className="mx-auto mb-10 max-w-2xl text-center">
        <h2 className="font-heading text-cc-heading text-3xl font-semibold tracking-tight sm:text-4xl">
          Four plans. Pick the one that fits.
        </h2>
        <p className="text-cc-ink-dim mt-3 text-base sm:text-lg">
          Every paid plan is staffed by ChilliCream engineers, with flat monthly
          pricing.
        </p>
      </div>
      <div className="grid gap-5 lg:grid-cols-4">
        {PLANS.map((plan, i) => (
          <PlanCard key={plan.name} plan={plan} index={i} />
        ))}
      </div>
      <p className="text-cc-ink-dim mt-6 text-center text-sm">
        Prices in USD. Excludes applicable taxes.
      </p>
    </section>
  );
}

function PlanCard({
  plan,
  index,
}: {
  readonly plan: Plan;
  readonly index: number;
}) {
  const cardBase =
    "relative flex h-full flex-col rounded-2xl border p-6 transition-colors";
  const cardSkin = plan.highlight
    ? "border-cc-accent/60 bg-cc-card-bg shadow-[0_0_0_1px_rgba(94,234,212,0.25)]"
    : "border-cc-card-border bg-cc-card-bg hover:border-cc-card-border-hover";

  return (
    <motion.article
      className={`${cardBase} ${cardSkin}`}
      initial={{ opacity: 0, y: 24 }}
      whileInView={{ opacity: 1, y: 0 }}
      viewport={{ once: true, margin: "-15%" }}
      transition={{
        duration: 0.6,
        delay: index * 0.08,
        ease: [0.22, 1, 0.36, 1],
      }}
    >
      {plan.highlight && (
        <span className="bg-cc-accent text-cc-surface absolute -top-3 left-6 rounded-full px-3 py-1 font-mono text-[10px] font-semibold tracking-widest uppercase">
          Most popular
        </span>
      )}

      <header>
        <h3 className="font-heading text-cc-heading text-2xl font-semibold tracking-tight">
          {plan.name}
        </h3>
        <p className="text-cc-ink-dim mt-1 text-sm">{plan.tagline}</p>
      </header>

      <div className="mt-5 flex items-baseline gap-2">
        <PriceLabel plan={plan} />
        {plan.priceNote && (
          <span className="text-cc-ink-dim text-sm">{plan.priceNote}</span>
        )}
      </div>

      <p className="text-cc-prose mt-4 text-sm leading-relaxed">
        {plan.description}
      </p>

      <ul className="mt-6 flex-1 space-y-3">
        {plan.perks.map((perk) => (
          <li key={perk} className="flex items-start gap-3 text-sm">
            <span
              className="text-cc-accent mt-[3px] inline-flex shrink-0"
              aria-hidden
            >
              <CheckIcon />
            </span>
            <span className="text-cc-prose">{perk}</span>
          </li>
        ))}
      </ul>

      <div className="mt-7">
        {plan.highlight ? (
          <SolidButton href={plan.cta.href} className="w-full">
            {plan.cta.label}
          </SolidButton>
        ) : (
          <OutlineButton href={plan.cta.href} className="w-full">
            {plan.cta.label}
          </OutlineButton>
        )}
      </div>
    </motion.article>
  );
}

function PriceLabel({ plan }: { readonly plan: Plan }) {
  const ref = useRef<HTMLSpanElement>(null);
  const isInView = useInView(ref, { once: true, margin: "-10%" });
  const reduce = useReducedMotion();
  const initial = reduce || plan.priceNumeric === undefined ? plan.price : "$0";

  useEffect(() => {
    const el = ref.current;
    if (!el) {
      return;
    }
    if (plan.priceNumeric === undefined) {
      el.textContent = plan.price;
      return;
    }
    if (reduce) {
      el.textContent = plan.price;
      return;
    }
    if (!isInView) {
      return;
    }
    const target = plan.priceNumeric;
    const controls = animate(0, target, {
      duration: 1.4,
      ease: [0.22, 1, 0.36, 1],
      onUpdate: (v) => {
        if (ref.current) {
          ref.current.textContent = `$${Math.round(v).toLocaleString("en-US")}`;
        }
      },
    });
    return () => controls.stop();
  }, [isInView, reduce, plan]);

  return (
    <span
      ref={ref}
      className="font-heading text-cc-heading text-4xl font-semibold tracking-tight tabular-nums"
    >
      {initial}
    </span>
  );
}

/* ========================= Coverage Strip ========================= */

const COVERAGE: readonly { readonly label: string; readonly detail: string }[] =
  [
    {
      label: "Engineers, not a queue",
      detail: "The people who wrote Hot Chocolate, Fusion, and Nitro reply.",
    },
    {
      label: "Private Slack on every paid plan",
      detail: "From Startup up, a named channel with ChilliCream engineers.",
    },
    {
      label: "Written SLAs from Business up",
      detail: "Critical and non-critical clocks, in writing, in your contract.",
    },
    {
      label: "Self-host supported",
      detail:
        "Nitro, Fusion gateway, and Hot Chocolate on your infrastructure.",
    },
  ];

function CoverageStrip() {
  return (
    <section className="py-12">
      <div className="grid gap-4 sm:grid-cols-2 lg:grid-cols-4">
        {COVERAGE.map((item, i) => (
          <motion.div
            key={item.label}
            className="border-cc-card-border bg-cc-card-bg rounded-xl border p-5"
            initial={{ opacity: 0, y: 16 }}
            whileInView={{ opacity: 1, y: 0 }}
            viewport={{ once: true, margin: "-10%" }}
            transition={{
              duration: 0.5,
              delay: i * 0.06,
              ease: [0.22, 1, 0.36, 1],
            }}
          >
            <div className="flex items-start gap-3">
              <span
                className="text-cc-accent mt-[3px] inline-flex shrink-0"
                aria-hidden
              >
                <CheckIcon size={16} />
              </span>
              <div>
                <div className="text-cc-heading font-medium">{item.label}</div>
                <p className="text-cc-ink-dim mt-1 text-sm leading-relaxed">
                  {item.detail}
                </p>
              </div>
            </div>
          </motion.div>
        ))}
      </div>
    </section>
  );
}

/* ======================= Comparison Matrix ======================== */

function ComparisonMatrix() {
  return (
    <section className="py-16">
      <div className="mx-auto mb-8 max-w-2xl text-center">
        <h2 className="font-heading text-cc-heading text-3xl font-semibold tracking-tight sm:text-4xl">
          Compare every plan, line by line.
        </h2>
        <p className="text-cc-ink-dim mt-3 text-base sm:text-lg">
          Response times, channels, and the strategic perks Enterprise teams ask
          for.
        </p>
      </div>

      <div className="border-cc-card-border bg-cc-card-bg overflow-x-auto rounded-2xl border">
        <table className="w-full border-collapse text-sm">
          <thead>
            <tr className="border-cc-card-border border-b">
              <th
                scope="col"
                className="text-cc-ink-dim w-[34%] px-5 py-4 text-left font-mono text-xs font-semibold tracking-widest uppercase"
              >
                Feature
              </th>
              {PLAN_NAMES.map((name) => {
                const isHighlight = name === "Business";
                return (
                  <th
                    key={name}
                    scope="col"
                    className={`px-5 py-4 text-center font-semibold ${
                      isHighlight ? "text-cc-accent" : "text-cc-heading"
                    }`}
                  >
                    {name}
                  </th>
                );
              })}
            </tr>
          </thead>
          <tbody>
            {COMPARISON.map((group) => (
              <ComparisonGroupRows key={group.title} group={group} />
            ))}
          </tbody>
        </table>
      </div>
    </section>
  );
}

function ComparisonGroupRows({ group }: { readonly group: ComparisonGroup }) {
  return (
    <>
      <tr className="bg-cc-surface/60">
        <th
          scope="rowgroup"
          colSpan={5}
          className="text-cc-nav-label border-cc-card-border border-y px-5 py-2 text-left font-mono text-[11px] font-semibold tracking-widest uppercase"
        >
          {group.title}
        </th>
      </tr>
      {group.rows.map((row, i) => (
        <motion.tr
          key={row.title}
          className="border-cc-card-border border-b"
          initial={{ opacity: 0, y: 8 }}
          whileInView={{ opacity: 1, y: 0 }}
          viewport={{ once: true, margin: "-10%" }}
          transition={{
            duration: 0.45,
            delay: i * 0.05,
            ease: [0.22, 1, 0.36, 1],
          }}
        >
          <th scope="row" className="px-5 py-4 text-left align-top">
            <div className="text-cc-heading font-medium">{row.title}</div>
            {row.hint && (
              <div className="text-cc-ink-dim mt-1 text-xs leading-relaxed">
                {row.hint}
              </div>
            )}
          </th>
          {row.values.map((value, index) => {
            const planName = PLAN_NAMES[index];
            const isHighlight = planName === "Business";
            return (
              <td
                key={planName}
                className={`px-5 py-4 text-center align-top ${
                  isHighlight ? "bg-cc-accent/[0.04]" : ""
                }`}
              >
                <ComparisonValue value={value} />
              </td>
            );
          })}
        </motion.tr>
      ))}
    </>
  );
}

function ComparisonValue({ value }: { readonly value: CellValue }) {
  if (value === true) {
    return (
      <span
        className="text-cc-accent inline-flex items-center justify-center"
        aria-label="Included"
      >
        <CheckIcon size={16} />
      </span>
    );
  }
  if (value === false) {
    return (
      <span
        className="text-cc-ink-faint inline-flex items-center justify-center"
        aria-label="Not included"
      >
        <svg viewBox="0 0 16 16" width={12} height={2} aria-hidden>
          <line
            x1="2"
            y1="1"
            x2="14"
            y2="1"
            stroke="currentColor"
            strokeWidth="1.5"
            strokeLinecap="round"
          />
        </svg>
      </span>
    );
  }
  return <span className="text-cc-prose">{value}</span>;
}

/* ========================= Enterprise Band ======================== */

function EnterpriseBand() {
  return (
    <section className="py-16">
      <div className="border-cc-card-border bg-cc-card-bg relative overflow-hidden rounded-2xl border p-8 sm:p-12">
        <EnterpriseArc />
        <div className="relative grid gap-10 lg:grid-cols-[1.2fr_1fr] lg:items-center">
          <div>
            <div className="text-cc-accent mb-3 font-mono text-xs font-semibold tracking-widest uppercase">
              Enterprise
            </div>
            <h2 className="font-heading text-cc-heading text-3xl font-semibold tracking-tight sm:text-4xl">
              Whole-org coverage, tailored SLAs, one named contact.
            </h2>
            <p className="text-cc-prose mt-4 max-w-xl text-base leading-relaxed sm:text-lg">
              For organizations running ChilliCream across multiple teams and
              business units. We shape the SLA around how you ship, and a
              dedicated account manager owns the relationship end to end.
            </p>
            <div className="mt-7 flex flex-col gap-3 sm:flex-row">
              <SolidButton href="/services/support/contact?plan=Enterprise">
                Contact Us
              </SolidButton>
              <OutlineButton href="/services/advisory">
                Pair with Advisory
              </OutlineButton>
            </div>
          </div>
          <ul className="grid gap-5 sm:grid-cols-2">
            <EnterpriseFact index={0} title="24-hour critical SLA">
              Any day. Unlimited critical incidents under one ceiling.
            </EnterpriseFact>
            <EnterpriseFact index={1} title="Dedicated account manager">
              One named contact who knows your stack and your roadmap.
            </EnterpriseFact>
            <EnterpriseFact index={2} title="Phone support">
              For the calls that should not wait for a ticket.
            </EnterpriseFact>
            <EnterpriseFact index={3} title="Status reviews">
              Recurring check-ins on roadmap, upgrades, and posture.
            </EnterpriseFact>
          </ul>
        </div>
      </div>
    </section>
  );
}

function EnterpriseArc() {
  const ref = useRef<SVGSVGElement>(null);
  const isInView = useInView(ref, { once: true, margin: "-10%" });
  const reduce = useReducedMotion();
  const draw = reduce ? 1 : isInView ? 1 : 0;

  return (
    <svg
      ref={ref}
      className="pointer-events-none absolute -top-24 -right-24 h-[420px] w-[420px] opacity-80"
      viewBox="0 0 400 400"
      aria-hidden
    >
      <defs>
        <radialGradient id="cc-support-v7-glow" cx="50%" cy="50%" r="50%">
          <stop offset="0%" stopColor="#5eead4" stopOpacity="0.18" />
          <stop offset="60%" stopColor="#5eead4" stopOpacity="0.03" />
          <stop offset="100%" stopColor="#5eead4" stopOpacity="0" />
        </radialGradient>
      </defs>
      <circle cx="200" cy="200" r="200" fill="url(#cc-support-v7-glow)" />
      <motion.path
        d="M 60 200 A 140 140 0 1 1 340 200"
        fill="none"
        stroke="#5eead4"
        strokeOpacity="0.55"
        strokeWidth="2"
        strokeLinecap="round"
        initial={reduce ? false : { pathLength: 0 }}
        animate={{ pathLength: draw }}
        transition={{ duration: 1.6, ease: [0.22, 1, 0.36, 1] }}
      />
      <motion.text
        x="200"
        y="208"
        textAnchor="middle"
        className="fill-cc-accent font-mono"
        style={{ fontSize: 18, letterSpacing: 2 }}
        initial={reduce ? false : { opacity: 0 }}
        animate={{ opacity: draw }}
        transition={{ duration: 0.5, delay: 1.4 }}
      >
        24h
      </motion.text>
    </svg>
  );
}

function EnterpriseFact({
  index,
  title,
  children,
}: {
  readonly index: number;
  readonly title: string;
  readonly children: ReactNode;
}) {
  return (
    <motion.li
      className="border-cc-card-border rounded-xl border p-4"
      initial={{ opacity: 0, y: 12 }}
      whileInView={{ opacity: 1, y: 0 }}
      viewport={{ once: true, margin: "-10%" }}
      transition={{
        duration: 0.5,
        delay: index * 0.08,
        ease: [0.22, 1, 0.36, 1],
      }}
    >
      <div className="flex items-start gap-3">
        <span
          className="text-cc-accent mt-[3px] inline-flex shrink-0"
          aria-hidden
        >
          <CheckIcon size={16} />
        </span>
        <div>
          <div className="text-cc-heading font-medium">{title}</div>
          <p className="text-cc-ink-dim mt-1 text-sm leading-relaxed">
            {children}
          </p>
        </div>
      </div>
    </motion.li>
  );
}

/* =============================== FAQ ============================== */

function FaqSection() {
  return (
    <section className="py-16">
      <div className="mx-auto mb-10 max-w-2xl text-center">
        <h2 className="font-heading text-cc-heading text-3xl font-semibold tracking-tight sm:text-4xl">
          Frequently asked questions
        </h2>
        <p className="text-cc-ink-dim mt-3 text-base sm:text-lg">
          The questions buyers ask before they sign, answered straight.
        </p>
      </div>
      <div className="border-cc-card-border bg-cc-card-bg divide-cc-card-border divide-y rounded-2xl border">
        {FAQ.map((item) => (
          <details
            key={item.q}
            className="group px-5 py-5 sm:px-6"
            name="support-faq"
          >
            <summary className="flex cursor-pointer list-none items-start justify-between gap-6">
              <span className="text-cc-heading text-base font-medium sm:text-lg">
                {item.q}
              </span>
              <span
                className="text-cc-ink-dim mt-1 inline-flex shrink-0 transition-transform group-open:rotate-45"
                aria-hidden
              >
                <PlusGlyph />
              </span>
            </summary>
            <p className="text-cc-prose mt-3 pr-10 text-sm leading-relaxed sm:text-base">
              {item.a}
            </p>
          </details>
        ))}
      </div>
    </section>
  );
}

function PlusGlyph() {
  return (
    <svg viewBox="0 0 16 16" width={16} height={16} aria-hidden>
      <path
        d="M8 3v10M3 8h10"
        stroke="currentColor"
        strokeWidth="1.5"
        strokeLinecap="round"
      />
    </svg>
  );
}

/* =========================== Closing CTA ========================== */

function ClosingCta() {
  return (
    <section className="py-20 text-center">
      <h2 className="font-heading text-cc-heading mx-auto max-w-2xl text-3xl font-semibold tracking-tight sm:text-4xl">
        Ready when you are.
      </h2>
      <p className="text-cc-prose mx-auto mt-4 max-w-xl text-base sm:text-lg">
        Join the community Slack to talk to other ChilliCream users, or get in
        touch to size a paid plan for your team.
      </p>
      <div className="mt-8 flex flex-col items-center justify-center gap-3 sm:flex-row sm:gap-4">
        <SolidButton href="/services/support/contact">
          Contact sales
        </SolidButton>
        <OutlineButton href="https://slack.chillicream.com/">
          Join community Slack
        </OutlineButton>
      </div>
    </section>
  );
}

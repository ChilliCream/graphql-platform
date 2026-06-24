"use client";

import type { ReactNode } from "react";
import { useEffect, useRef, useState } from "react";
import {
  MotionConfig,
  motion,
  useInView,
  useReducedMotion,
} from "motion/react";

import { CheckIcon } from "@/src/components/CheckIcon";
import { OutlineButton, SolidButton } from "@/src/design-system/Button";

/* V7 "First Hour, Live": the 60-minute intro call rendered as a scroll-driven
   timeline. Same cc-* dark palette as the canonical page, brand-spectrum
   gradient appears once on the highlighted Consulting tier ring, cc-accent
   teal is the running color for the sweep/markers/counter. */

const BOOKING_URL = "https://calendly.com/chillicream/60min";
const CONSULTING_MAILTO = "mailto:contact@chillicream.com?subject=Consulting";
const CONTRACTING_MAILTO = "mailto:contact@chillicream.com?subject=Contracting";

/* ============================== Data ===================================== */

interface Tier {
  readonly id: "consulting" | "contracting";
  readonly eyebrow: string;
  readonly name: string;
  readonly tagline: string;
  readonly priceLine: string;
  readonly priceNote: string;
  readonly bestFor: string;
  readonly perks: readonly string[];
  readonly primaryCta: { readonly label: string; readonly href: string };
  readonly secondaryCta: { readonly label: string; readonly href: string };
  readonly highlight?: boolean;
}

const TIERS: readonly Tier[] = [
  {
    id: "consulting",
    eyebrow: "Hourly engagements",
    name: "Consulting",
    tagline:
      "Hourly consulting services to get the help you need at any stage of your project. This is the best way to get started.",
    priceLine: "$300",
    priceNote: "per hour",
    bestFor:
      "Teams that already own the build and need a senior GraphQL engineer on call for design, troubleshooting, and review.",
    perks: [
      "Mentoring and guidance",
      "Architecture",
      "Troubleshooting",
      "Code Review",
      "Best practices education",
    ],
    primaryCta: { label: "Book a 60-min call", href: BOOKING_URL },
    secondaryCta: { label: "Email us", href: CONSULTING_MAILTO },
    highlight: true,
  },
  {
    id: "contracting",
    eyebrow: "Scoped engagements",
    name: "Contracting",
    tagline:
      "Options for teams who do not have the time, bandwidth, and/or expertise to implement their own GraphQL solutions.",
    priceLine: "Custom",
    priceNote: "scope & timeline",
    bestFor:
      "Teams that want our engineers to deliver a working result, from a proof of concept to a production rollout.",
    perks: ["Proof of concept", "Implementation"],
    primaryCta: { label: "Talk to an Expert", href: CONTRACTING_MAILTO },
    secondaryCta: { label: "Book an intro call", href: BOOKING_URL },
  },
];

interface EngagementStep {
  readonly index: string;
  readonly title: string;
  readonly description: string;
  readonly bullets: readonly string[];
}

const ENGAGEMENT_STEPS: readonly EngagementStep[] = [
  {
    index: "01",
    title: "Introductory call",
    description:
      "A 60-minute working call. You walk us through the system, the goal, and the constraints. We ask the hard questions and tell you whether we are the right fit.",
    bullets: [
      "Senior engineer, no sales handoff",
      "Mutual NDA available on request",
      "Written recap with next steps",
    ],
  },
  {
    index: "02",
    title: "Proposal",
    description:
      "A written proposal that matches your need: hourly retainer for consulting, or a scoped statement of work for contracting with deliverables, milestones, and a target timeline.",
    bullets: [
      "Hourly retainer or fixed scope",
      "Clear deliverables and milestones",
      "Written, scoped, and signable",
    ],
  },
  {
    index: "03",
    title: "Kickoff",
    description:
      "Contract signed, channel opened, work starts. You get a direct line to the engineers doing the work, a shared backlog, and a weekly checkpoint.",
    bullets: [
      "Shared Slack or Teams channel",
      "Weekly checkpoint and written status",
      "Direct access to the engineers doing the work",
    ],
  },
];

interface FaqItem {
  readonly question: string;
  readonly answer: string;
}

const FAQ: readonly FaqItem[] = [
  {
    question: "What is the hourly rate for consulting?",
    answer:
      "Consulting is billed at $300 per hour. We bill in blocks of time agreed up front, on a retainer or a small purchase order, so you never get a surprise invoice. Contracting engagements are scoped separately as a statement of work.",
  },
  {
    question: "How small is too small for an engagement?",
    answer:
      "A single 60-minute call is fine. Many teams start with one or two hours to unblock a specific decision (schema shape, Fusion composition, auth model) and only return when the next question lands. There is no minimum retainer to talk to us.",
  },
  {
    question: "Do you sign an NDA?",
    answer:
      "Yes. We will sign a mutual NDA before the introductory call when you ask, and we are comfortable with most standard agreements. Customer code, schemas, and traces never leave the engagement.",
  },
  {
    question: "How quickly can you start?",
    answer:
      "Consulting hours usually start within the same week, sometimes the same day. Contracting engagements depend on scope and current bandwidth, and we will tell you the realistic start date on the introductory call rather than promise a slot we cannot honor.",
  },
  {
    question: "What outcomes can I expect?",
    answer:
      "Concrete, written deliverables tied to your goal: an architecture decision record, a schema review with line-level comments, a working proof of concept, or a production-ready implementation. We do not bill for slideware.",
  },
  {
    question: "Who actually does the work?",
    answer:
      "The engineers who build Hot Chocolate, Fusion, and Nitro. The same people who write the framework code, review the pull requests, and answer the hard issues on GitHub are the people on your call.",
  },
];

interface CredentialColumn {
  readonly title: string;
  readonly body: string;
  readonly bullets: readonly string[];
}

const CREDENTIALS: readonly CredentialColumn[] = [
  {
    title: "Who you work with",
    body: "Senior engineers from the core team. The same people who maintain Hot Chocolate, design Fusion, and ship Nitro.",
    bullets: [
      "Direct line to the maintainers",
      "Pull-request authors on the core code",
      "Same team across consulting and contracting",
    ],
  },
  {
    title: "What we work on",
    body: "The full GraphQL stack we build: schema design, federation with Fusion, ASP.NET Core integration, performance and resolver tuning, MCP, and Nitro observability and CI.",
    bullets: [
      "Schema and federation design",
      "Fusion composition and rollout",
      "Nitro observability, CI, and persisted ops",
    ],
  },
  {
    title: "How we work",
    body: "In your repo, in your channels, in your timezone window. Written status every week. Honest answers when something is not the right fit, even when that means a smaller engagement.",
    bullets: [
      "Embedded in your codebase",
      "Written weekly status reports",
      "We will say no when no is the right answer",
    ],
  },
];

interface CallMoment {
  readonly at: number; // minute offset 0..60
  readonly label: string;
  readonly note: string;
}

const CALL_MOMENTS: readonly CallMoment[] = [
  {
    at: 2,
    label: "Hello",
    note: "Five minutes of context. Who is on the call and what brought you here.",
  },
  {
    at: 12,
    label: "Map the system",
    note: "You walk us through the schema, the topology, the team, and the deadline.",
  },
  {
    at: 24,
    label: "Hard questions",
    note: "We probe the assumptions you are quietly worried about. No slide deck.",
  },
  {
    at: 36,
    label: "Risks named",
    note: "We name the failure modes we see on the way to the result you want.",
  },
  {
    at: 48,
    label: "Sketch the path",
    note: "Two or three viable approaches, with the trade-offs we would weigh on each.",
  },
  {
    at: 58,
    label: "Written recap",
    note: "You leave with a written summary and a clear next step. No commitment required.",
  },
];

interface StatItem {
  readonly label: string;
  readonly value: string;
  readonly target: number;
  readonly prefix?: string;
  readonly suffix?: string;
}

const HERO_STATS: readonly StatItem[] = [
  { label: "Hourly rate", value: "$300", target: 300, prefix: "$" },
  { label: "Intro call", value: "60 min", target: 60, suffix: " min" },
  { label: "Engagements", value: "2 tiers", target: 2, suffix: " tiers" },
];

/* ============================== Page ===================================== */

export function ClientPage() {
  return (
    <MotionConfig reducedMotion="user">
      <Hero />
      <HourZeroCenterpiece />
      <TierGrid />
      <EngagementStrip />
      <TeamCard />
      <Faq />
      <ContactBand />
    </MotionConfig>
  );
}

/* ============================== Hero ===================================== */

function Hero() {
  return (
    <section className="pt-10 pb-12 text-center sm:pt-16 sm:pb-16">
      <p className="text-cc-nav-label font-mono text-xs tracking-[0.18em] uppercase">
        ChilliCream Advisory
      </p>
      <h1 className="font-heading text-cc-heading text-h2 sm:text-h1 mt-5 font-semibold tracking-tight">
        Talk to the engineers who built your GraphQL stack.
      </h1>
      <p className="text-cc-ink mx-auto mt-6 max-w-2xl text-base text-pretty sm:text-lg">
        Hourly consulting at $300 per hour, or full contracting engagements,
        delivered by the team behind Hot Chocolate, Fusion, and Nitro. Bring a
        question, a design, or a deadline. We meet you where the work is.
      </p>
      <div className="mt-9 flex flex-wrap items-center justify-center gap-3">
        <HoverLiftButton>
          <SolidButton href={BOOKING_URL}>Book a 60-min call</SolidButton>
        </HoverLiftButton>
        <HoverLiftButton>
          <OutlineButton href={CONSULTING_MAILTO}>Email us</OutlineButton>
        </HoverLiftButton>
      </div>
      <HeroStats />
    </section>
  );
}

function HeroStats() {
  return (
    <dl className="border-cc-card-border bg-cc-card-border mx-auto mt-12 grid max-w-2xl grid-cols-3 gap-px overflow-hidden rounded-2xl border">
      {HERO_STATS.map((item) => (
        <div
          key={item.label}
          className="bg-cc-surface px-4 py-5 text-center sm:px-6"
        >
          <dt className="text-cc-nav-label font-mono text-[0.65rem] tracking-[0.18em] uppercase">
            {item.label}
          </dt>
          <dd className="font-heading text-cc-heading mt-2 text-xl font-semibold sm:text-2xl">
            <TickUpNumber
              target={item.target}
              prefix={item.prefix}
              suffix={item.suffix}
              finalLabel={item.value}
            />
          </dd>
        </div>
      ))}
    </dl>
  );
}

interface TickUpNumberProps {
  readonly target: number;
  readonly prefix?: string;
  readonly suffix?: string;
  readonly finalLabel: string;
  readonly durationMs?: number;
}

function TickUpNumber({
  target,
  prefix = "",
  suffix = "",
  finalLabel,
  durationMs = 900,
}: TickUpNumberProps) {
  const reduce = useReducedMotion();
  const ref = useRef<HTMLSpanElement | null>(null);
  const inView = useInView(ref, { once: true, amount: 0.6 });
  const [value, setValue] = useState<number>(reduce ? target : 0);

  useEffect(() => {
    if (reduce || !inView) return;
    const start = performance.now();
    let raf = 0;
    const tick = (now: number) => {
      const t = Math.min(1, (now - start) / durationMs);
      const eased = 1 - Math.pow(1 - t, 3);
      setValue(Math.round(eased * target));
      if (t < 1) raf = requestAnimationFrame(tick);
    };
    raf = requestAnimationFrame(tick);
    return () => cancelAnimationFrame(raf);
  }, [inView, reduce, target, durationMs]);

  if (reduce) {
    return <span ref={ref}>{finalLabel}</span>;
  }
  return (
    <span ref={ref}>
      {prefix}
      {value}
      {suffix}
    </span>
  );
}

/* ====================== Centerpiece: Hour Zero =========================== */

function HourZeroCenterpiece() {
  const reduce = useReducedMotion();
  const sectionRef = useRef<HTMLDivElement | null>(null);
  const inView = useInView(sectionRef, { once: true, amount: 0.3 });

  const [progress, setProgress] = useState<number>(reduce ? 1 : 0);

  useEffect(() => {
    if (reduce || !inView) return;
    const durationMs = 4200;
    const start = performance.now();
    let raf = 0;
    const tick = (now: number) => {
      const t = Math.min(1, (now - start) / durationMs);
      const eased = 1 - Math.pow(1 - t, 3);
      setProgress(eased);
      if (t < 1) raf = requestAnimationFrame(tick);
    };
    raf = requestAnimationFrame(tick);
    return () => cancelAnimationFrame(raf);
  }, [inView, reduce]);

  const counterDisplay = Math.round(progress * 300);
  const clampedMinute = Math.max(0, Math.min(60, progress * 60));
  const mm = Math.floor(clampedMinute);
  const ss = Math.floor((clampedMinute - mm) * 60);
  const timeDisplay = `${String(mm).padStart(2, "0")}:${String(ss).padStart(2, "0")}`;
  let activeIndex = -1;
  for (let i = 0; i < CALL_MOMENTS.length; i++) {
    if (clampedMinute >= CALL_MOMENTS[i].at) activeIndex = i;
    else break;
  }
  const fillPercent = `${Math.max(0, Math.min(1, progress)) * 100}%`;
  const fillWidth = reduce ? "100%" : fillPercent;

  return (
    <section
      aria-labelledby="hour-zero-heading"
      className="mt-16 sm:mt-24"
      ref={sectionRef}
    >
      <div className="text-center">
        <p className="text-cc-nav-label font-mono text-xs tracking-[0.18em] uppercase">
          The hour, in motion
        </p>
        <h2
          id="hour-zero-heading"
          className="font-heading text-cc-heading text-h4 sm:text-h3 mt-3 font-semibold"
        >
          What sixty minutes with us actually looks like.
        </h2>
        <p className="text-cc-ink mx-auto mt-4 max-w-2xl text-base">
          Scroll the call. The clock sweeps from 00:00 to 60:00, six moments
          light up, and the $300 you spend on the hour assembles in front of
          you.
        </p>
      </div>

      <div className="border-cc-card-border bg-cc-card-bg/60 mt-10 rounded-3xl border p-6 sm:p-10">
        {/* Top bar: time + counter */}
        <div className="flex items-center justify-between gap-4">
          <div className="flex items-center gap-3">
            <span
              aria-hidden="true"
              className="bg-cc-accent inline-block size-2 rounded-full"
            />
            <span className="text-cc-nav-label font-mono text-[0.65rem] tracking-[0.18em] uppercase">
              Hour Zero
            </span>
          </div>
          <div className="flex items-baseline gap-6">
            <div className="text-right">
              <div className="text-cc-nav-label font-mono text-[0.6rem] tracking-[0.18em] uppercase">
                Elapsed
              </div>
              <div
                className="text-cc-accent mt-1 font-mono text-lg sm:text-xl"
                aria-live="polite"
              >
                {timeDisplay}
              </div>
            </div>
            <div className="text-right">
              <div className="text-cc-nav-label font-mono text-[0.6rem] tracking-[0.18em] uppercase">
                Value
              </div>
              <div
                className="font-heading text-cc-accent mt-1 text-xl font-semibold sm:text-2xl"
                aria-live="polite"
              >
                ${counterDisplay}
              </div>
            </div>
          </div>
        </div>

        {/* Track */}
        <div className="mt-8 sm:mt-10">
          <div className="relative">
            {/* base track */}
            <div className="bg-cc-card-border relative h-[2px] w-full rounded-full">
              {/* progress fill */}
              <div
                className="bg-cc-accent absolute inset-y-0 left-0 rounded-full"
                style={{ width: fillWidth }}
              />
            </div>

            {/* moment markers */}
            <ul className="pointer-events-none absolute inset-x-0 top-1/2 m-0 -translate-y-1/2 p-0">
              {CALL_MOMENTS.map((moment, i) => {
                const left = `${(moment.at / 60) * 100}%`;
                const lit = reduce || i <= activeIndex;
                return (
                  <li
                    key={moment.label}
                    className="absolute"
                    style={{ left, transform: "translate(-50%, -50%)" }}
                  >
                    <MarkerDot lit={lit} />
                  </li>
                );
              })}
            </ul>

            {/* tick labels */}
            <div className="text-cc-nav-label mt-3 flex justify-between font-mono text-[0.6rem] tracking-[0.18em] uppercase">
              <span>00:00</span>
              <span>15:00</span>
              <span>30:00</span>
              <span>45:00</span>
              <span>60:00</span>
            </div>
          </div>

          {/* moment notes grid */}
          <ol className="mt-8 grid gap-3 sm:grid-cols-2 lg:grid-cols-3">
            {CALL_MOMENTS.map((moment, i) => {
              const lit = reduce || i <= activeIndex;
              return (
                <MomentNote key={moment.label} moment={moment} lit={lit} />
              );
            })}
          </ol>
        </div>
      </div>
    </section>
  );
}

function MarkerDot({ lit }: { readonly lit: boolean }) {
  return (
    <span
      aria-hidden="true"
      className={
        lit
          ? "bg-cc-accent ring-cc-surface block size-3 rounded-full ring-4"
          : "bg-cc-card-border ring-cc-surface block size-3 rounded-full ring-4"
      }
      style={lit ? { boxShadow: "0 0 0 1px rgba(94,234,212,0.35)" } : undefined}
    />
  );
}

function MomentNote({
  moment,
  lit,
}: {
  readonly moment: CallMoment;
  readonly lit: boolean;
}) {
  return (
    <motion.li
      initial={false}
      animate={{ opacity: lit ? 1 : 0.45, y: lit ? 0 : 4 }}
      transition={{ duration: 0.35, ease: "easeOut" }}
      className={
        "bg-cc-surface flex h-full flex-col rounded-2xl border p-4 " +
        (lit ? "border-cc-accent/40" : "border-cc-card-border")
      }
    >
      <div className="flex items-center justify-between">
        <span
          className={
            lit
              ? "text-cc-accent font-mono text-[0.6rem] tracking-[0.18em] uppercase"
              : "text-cc-nav-label font-mono text-[0.6rem] tracking-[0.18em] uppercase"
          }
        >
          {String(moment.at).padStart(2, "0")}:00
        </span>
        <span className="text-cc-nav-label font-mono text-[0.6rem] tracking-[0.18em] uppercase">
          {lit ? "On" : "Up next"}
        </span>
      </div>
      <h3 className="font-heading text-cc-heading mt-3 text-base font-semibold">
        {moment.label}
      </h3>
      <p className="text-cc-ink-dim mt-2 text-sm leading-relaxed">
        {moment.note}
      </p>
    </motion.li>
  );
}

/* ============================== Tiers ==================================== */

function TierGrid() {
  return (
    <section
      aria-labelledby="tiers-heading"
      className="mt-20 pb-16 sm:mt-28 sm:pb-24"
    >
      <h2 id="tiers-heading" className="sr-only">
        Engagement tiers
      </h2>
      <div className="grid gap-6 lg:grid-cols-2 lg:items-stretch">
        {TIERS.map((tier, i) => (
          <RevealOnView key={tier.id} delay={i * 0.08}>
            <TierCard tier={tier} />
          </RevealOnView>
        ))}
      </div>
    </section>
  );
}

function TierCard({ tier }: { readonly tier: Tier }) {
  if (tier.highlight) {
    return (
      <div
        className="relative isolate h-full rounded-3xl p-[1.5px]"
        style={{
          background:
            "linear-gradient(140deg, #16b9e4 0%, #7c92c6 50%, #f0786a 100%)",
        }}
      >
        <StartHerePill />
        <div className="bg-cc-surface flex h-full flex-col rounded-[calc(1.5rem-1.5px)] p-7 sm:p-9">
          <TierCardBody tier={tier} />
        </div>
      </div>
    );
  }

  return (
    <div className="bg-cc-card-bg border-cc-card-border hover:border-cc-card-border-hover flex h-full flex-col rounded-3xl border p-7 transition-colors sm:p-9">
      <TierCardBody tier={tier} />
    </div>
  );
}

function TierCardBody({ tier }: { readonly tier: Tier }) {
  return (
    <>
      <p className="text-cc-nav-label font-mono text-[0.65rem] tracking-[0.18em] uppercase">
        {tier.eyebrow}
      </p>
      <h3 className="font-heading text-cc-heading text-h3 mt-3 font-semibold">
        {tier.name}
      </h3>
      <p className="text-cc-ink-dim mt-3 text-sm leading-relaxed sm:text-base">
        {tier.tagline}
      </p>

      <div className="mt-6 flex items-baseline gap-2">
        <span className="font-heading text-cc-heading text-h4 font-semibold">
          {tier.priceLine}
        </span>
        <span className="text-cc-nav-label font-mono text-xs">
          {tier.priceNote}
        </span>
      </div>

      <div
        aria-hidden="true"
        className="border-cc-ink-faint my-6 border-t border-dashed"
      />

      <p className="text-cc-nav-label font-mono text-[0.65rem] tracking-[0.18em] uppercase">
        Best for
      </p>
      <p className="text-cc-ink mt-2 text-sm leading-relaxed">{tier.bestFor}</p>

      <p className="text-cc-nav-label mt-6 font-mono text-[0.65rem] tracking-[0.18em] uppercase">
        What is included
      </p>
      <ul className="mt-3 flex flex-1 flex-col gap-3">
        {tier.perks.map((perk, i) => (
          <StaggerItem key={perk} index={i}>
            <span className="text-cc-accent mt-[5px] flex-none">
              <CheckIcon />
            </span>
            <span className="text-cc-ink text-sm">{perk}</span>
          </StaggerItem>
        ))}
      </ul>

      <div className="mt-8 flex flex-col gap-3 sm:flex-row">
        <HoverLiftButton className="w-full sm:flex-1">
          <SolidButton href={tier.primaryCta.href} className="w-full">
            {tier.primaryCta.label}
          </SolidButton>
        </HoverLiftButton>
        <HoverLiftButton className="w-full sm:flex-1">
          <OutlineButton href={tier.secondaryCta.href} className="w-full">
            {tier.secondaryCta.label}
          </OutlineButton>
        </HoverLiftButton>
      </div>
    </>
  );
}

function StartHerePill() {
  return (
    <span className="bg-cc-surface text-cc-accent border-cc-accent absolute top-0 left-9 z-10 -translate-y-1/2 rounded-full border px-4 py-1 font-mono text-[0.65rem] tracking-[0.18em] whitespace-nowrap uppercase">
      Start here
    </span>
  );
}

function StaggerItem({
  index,
  children,
}: {
  readonly index: number;
  readonly children: ReactNode;
}) {
  const reduce = useReducedMotion();
  if (reduce) {
    return <li className="flex items-start gap-3">{children}</li>;
  }
  return (
    <motion.li
      initial={{ opacity: 0, x: -6 }}
      whileInView={{ opacity: 1, x: 0 }}
      viewport={{ once: true, amount: 0.5 }}
      transition={{ duration: 0.35, delay: 0.05 * index, ease: "easeOut" }}
      className="flex items-start gap-3"
    >
      {children}
    </motion.li>
  );
}

/* ====================== Engagement strip ================================= */

function EngagementStrip() {
  const ref = useRef<HTMLDivElement | null>(null);
  const inView = useInView(ref, { once: true, amount: 0.3 });
  const reduce = useReducedMotion();
  const drawn = reduce || inView;

  return (
    <section
      aria-labelledby="engagement-heading"
      className="border-cc-card-border bg-cc-card-bg/40 rounded-3xl border p-6 sm:p-10"
      ref={ref}
    >
      <div className="text-center">
        <p className="text-cc-nav-label font-mono text-xs tracking-[0.18em] uppercase">
          How an engagement starts
        </p>
        <h2
          id="engagement-heading"
          className="font-heading text-cc-heading text-h4 sm:text-h3 mt-3 font-semibold"
        >
          From first call to first commit in three steps.
        </h2>
        <p className="text-cc-ink mx-auto mt-4 max-w-2xl text-base">
          No long sales cycle. You speak to an engineer, you get a written
          proposal, you kick off.
        </p>
      </div>

      {/* Connecting rail */}
      <div className="relative mt-10">
        <svg
          aria-hidden="true"
          className="pointer-events-none absolute top-[2.25rem] right-6 left-6 hidden h-[2px] w-auto md:block"
          viewBox="0 0 100 1"
          preserveAspectRatio="none"
        >
          <line
            x1="0"
            y1="0.5"
            x2="100"
            y2="0.5"
            stroke="var(--color-cc-card-border)"
            strokeWidth="1"
            vectorEffect="non-scaling-stroke"
          />
          <motion.line
            x1="0"
            y1="0.5"
            x2="100"
            y2="0.5"
            stroke="var(--color-cc-accent)"
            strokeWidth="1.5"
            vectorEffect="non-scaling-stroke"
            initial={{ pathLength: reduce ? 1 : 0 }}
            animate={{ pathLength: drawn ? 1 : 0 }}
            transition={{ duration: 1.1, ease: "easeInOut" }}
          />
        </svg>

        <ol className="grid gap-6 md:grid-cols-3">
          {ENGAGEMENT_STEPS.map((step, i) => (
            <RevealOnView key={step.index} delay={0.15 + i * 0.12}>
              <EngagementCard step={step} />
            </RevealOnView>
          ))}
        </ol>
      </div>
    </section>
  );
}

function EngagementCard({ step }: { readonly step: EngagementStep }) {
  return (
    <li className="bg-cc-surface border-cc-card-border relative flex h-full list-none flex-col rounded-2xl border p-6">
      <span className="text-cc-accent font-mono text-xs tracking-[0.18em] uppercase">
        Step {step.index}
      </span>
      <h3 className="font-heading text-cc-heading mt-2 text-lg font-semibold">
        {step.title}
      </h3>
      <p className="text-cc-ink mt-3 text-sm leading-relaxed">
        {step.description}
      </p>
      <ul className="mt-5 flex flex-col gap-2">
        {step.bullets.map((bullet) => (
          <li
            key={bullet}
            className="text-cc-ink-dim flex items-start gap-2 text-xs"
          >
            <span className="text-cc-accent mt-[4px] flex-none">
              <CheckIcon size={12} />
            </span>
            <span>{bullet}</span>
          </li>
        ))}
      </ul>
    </li>
  );
}

/* ============================== Team ===================================== */

function TeamCard() {
  return (
    <section aria-labelledby="team-heading" className="mt-20 sm:mt-28">
      <div className="text-center">
        <p className="text-cc-nav-label font-mono text-xs tracking-[0.18em] uppercase">
          Who you work with
        </p>
        <h2
          id="team-heading"
          className="font-heading text-cc-heading text-h4 sm:text-h3 mt-3 font-semibold"
        >
          The team behind Hot Chocolate, Fusion, and Nitro.
        </h2>
        <p className="text-cc-ink mx-auto mt-4 max-w-2xl text-base">
          ChilliCream advisory is not a generalist consultancy that learned
          GraphQL last quarter. The engineers on the call are the ones who write
          the framework you depend on.
        </p>
      </div>

      <div className="mt-10 grid gap-6 md:grid-cols-3">
        {CREDENTIALS.map((column, i) => (
          <RevealOnView key={column.title} delay={i * 0.08}>
            <CredentialColumnCard column={column} />
          </RevealOnView>
        ))}
      </div>

      <RevealOnView delay={0.2}>
        <div className="border-cc-card-border bg-cc-card-bg/60 mt-10 grid gap-6 rounded-3xl border p-6 sm:grid-cols-3 sm:p-8">
          <ProductBadge
            name="Hot Chocolate"
            tagline="The .NET GraphQL server we maintain."
          />
          <ProductBadge
            name="Fusion"
            tagline="Federation and composition for GraphQL subgraphs."
          />
          <ProductBadge
            name="Nitro"
            tagline="The observability and CI platform for the stack."
          />
        </div>
      </RevealOnView>
    </section>
  );
}

function CredentialColumnCard({
  column,
}: {
  readonly column: CredentialColumn;
}) {
  return (
    <div className="border-cc-card-border bg-cc-card-bg/60 flex h-full flex-col rounded-2xl border p-6">
      <h3 className="font-heading text-cc-heading text-base font-semibold">
        {column.title}
      </h3>
      <p className="text-cc-ink mt-3 text-sm leading-relaxed">{column.body}</p>
      <ul className="mt-5 flex flex-col gap-2">
        {column.bullets.map((bullet) => (
          <li
            key={bullet}
            className="text-cc-ink-dim flex items-start gap-2 text-xs"
          >
            <span className="text-cc-accent mt-[4px] flex-none">
              <CheckIcon size={12} />
            </span>
            <span>{bullet}</span>
          </li>
        ))}
      </ul>
    </div>
  );
}

function ProductBadge({
  name,
  tagline,
}: {
  readonly name: string;
  readonly tagline: string;
}) {
  return (
    <div className="flex flex-col">
      <span className="text-cc-nav-label font-mono text-[0.65rem] tracking-[0.18em] uppercase">
        Product
      </span>
      <span className="font-heading text-cc-heading mt-2 text-lg font-semibold">
        {name}
      </span>
      <span className="text-cc-ink-dim mt-1 text-sm">{tagline}</span>
    </div>
  );
}

/* ============================== FAQ ====================================== */

function Faq() {
  return (
    <section aria-labelledby="faq-heading" className="mt-20 sm:mt-28">
      <div className="text-center">
        <p className="text-cc-nav-label font-mono text-xs tracking-[0.18em] uppercase">
          Frequently asked
        </p>
        <h2
          id="faq-heading"
          className="font-heading text-cc-heading text-h4 sm:text-h3 mt-3 font-semibold"
        >
          Honest answers before you book a call.
        </h2>
      </div>

      <dl className="mt-10 grid gap-4 md:grid-cols-2">
        {FAQ.map((item, i) => (
          <RevealOnView key={item.question} delay={i * 0.05}>
            <FaqEntry item={item} />
          </RevealOnView>
        ))}
      </dl>
    </section>
  );
}

function FaqEntry({ item }: { readonly item: FaqItem }) {
  return (
    <div className="border-cc-card-border bg-cc-card-bg/60 rounded-2xl border p-6">
      <dt className="font-heading text-cc-heading text-base font-semibold">
        {item.question}
      </dt>
      <dd className="text-cc-ink mt-3 text-sm leading-relaxed">
        {item.answer}
      </dd>
    </div>
  );
}

/* ============================== Contact ================================== */

function ContactBand() {
  return (
    <section aria-labelledby="contact-heading" className="mt-20 mb-8 sm:mt-28">
      <RevealOnView>
        <div className="border-cc-card-border bg-cc-card-bg/70 grid gap-8 rounded-3xl border p-8 sm:p-12 md:grid-cols-[1.4fr_1fr] md:items-center">
          <div>
            <p className="text-cc-nav-label font-mono text-xs tracking-[0.18em] uppercase">
              Ready when you are
            </p>
            <h2
              id="contact-heading"
              className="font-heading text-cc-heading text-h4 mt-3 font-semibold"
            >
              One call is usually enough to know.
            </h2>
            <p className="text-cc-ink mt-4 text-base">
              Book a 60-minute call. You walk us through what you are building,
              we ask the questions, and you leave with a clear next step. No
              commitment beyond the hour. If we are not the right fit, we will
              tell you on the call.
            </p>
            <ContactSpec />
          </div>
          <div className="flex flex-col gap-3 md:items-end">
            <HoverLiftButton>
              <SolidButton href={BOOKING_URL}>Book a 60-min call</SolidButton>
            </HoverLiftButton>
            <HoverLiftButton>
              <OutlineButton href={CONSULTING_MAILTO}>Email us</OutlineButton>
            </HoverLiftButton>
          </div>
        </div>
      </RevealOnView>
    </section>
  );
}

function ContactSpec() {
  const items: readonly {
    readonly label: string;
    readonly value: ReactNode;
  }[] = [
    { label: "Rate", value: "$300 / hour for consulting" },
    { label: "Contracting", value: "Scoped statement of work" },
    { label: "NDA", value: "Mutual NDA on request" },
    { label: "Start", value: "Often the same week" },
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

/* ============================== Motion helpers =========================== */

function RevealOnView({
  children,
  delay = 0,
}: {
  readonly children: ReactNode;
  readonly delay?: number;
}) {
  const reduce = useReducedMotion();
  if (reduce) {
    return <div className="h-full">{children}</div>;
  }
  return (
    <motion.div
      className="h-full"
      initial={{ opacity: 0, y: 14 }}
      whileInView={{ opacity: 1, y: 0 }}
      viewport={{ once: true, amount: 0.3 }}
      transition={{ duration: 0.5, ease: "easeOut", delay }}
    >
      {children}
    </motion.div>
  );
}

function HoverLiftButton({
  children,
  className,
}: {
  readonly children: ReactNode;
  readonly className?: string;
}) {
  const reduce = useReducedMotion();
  if (reduce) {
    return <div className={className}>{children}</div>;
  }
  return (
    <motion.div
      className={className}
      whileHover={{ y: -2 }}
      transition={{ type: "spring", stiffness: 320, damping: 22 }}
    >
      {children}
    </motion.div>
  );
}

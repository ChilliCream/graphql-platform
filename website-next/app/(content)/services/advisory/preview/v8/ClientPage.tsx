"use client";

import { motion, useInView, useReducedMotion } from "motion/react";
import { useRef } from "react";
import type { ReactNode } from "react";

import { CheckIcon } from "@/src/components/CheckIcon";
import { OutlineButton, SolidButton } from "@/src/design-system/Button";

/* -------------------------------------------------------------------------- */
/*  Canonical facts. Reused verbatim from v1.                                  */
/* -------------------------------------------------------------------------- */

const BOOKING_URL = "https://calendly.com/chillicream/60min";
const CONSULTING_MAILTO = "mailto:contact@chillicream.com?subject=Consulting";
const CONTRACTING_MAILTO = "mailto:contact@chillicream.com?subject=Contracting";

// The brand spectrum appears exactly once on the page, as the hairline border
// of the highlighted Consulting tier.
const SPECTRUM =
  "linear-gradient(140deg, #16b9e4 0%, #7c92c6 50%, #f0786a 100%)";

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

/* -------------------------------------------------------------------------- */
/*  The advisory line.                                                         */
/*  A single hand-authored SVG stroke that traces the whole page from the      */
/*  hero entry waypoint down to the closing CTA, where it resolves into an     */
/*  arrowhead. It draws itself in once on enter (strokeDasharray /             */
/*  strokeDashoffset), then stays. Waypoint nodes fade and scale in on timed   */
/*  delays roughly matched to the moment the stroke passes their y position.   */
/*  No scroll coupling anywhere.                                               */
/* -------------------------------------------------------------------------- */

// viewBox is a tall, narrow strip. The stroke lives near the left edge so the
// content (padded left) reads as sitting just to its right. All waypoint nodes
// are placed at coordinates the path passes through.
const SPINE_W = 120;
const SPINE_H = 1600;

// Waypoint coordinates on the path, top to bottom. Kept in one place so the
// dots and the path agree.
const WP = {
  entry: { x: 96, y: 70 }, // hero, line enters from the right edge
  consulting: { x: 40, y: 340 }, // first stacked tier
  contracting: { x: 40, y: 520 }, // second stacked tier
  call: { x: 30, y: 720 }, // engagement station 1
  proposal: { x: 60, y: 780 }, // engagement station 2
  kickoff: { x: 90, y: 720 }, // engagement station 3
  team: { x: 96, y: 1000 }, // credentials, line hugs the right margin
  faq: { x: 28, y: 1300 }, // faq anchored on the left
  cta: { x: 60, y: 1540 }, // closing CTA, terminal point
} as const;

// One continuous path passing through the waypoints, with gentle curves and
// doglegs as described in the concept. The final short segment plus the small
// triangle below form the resolving arrowhead at the CTA.
const SPINE_PATH = [
  `M ${WP.entry.x} ${WP.entry.y}`,
  // enter from the right, curve down and left into the consulting tier
  `C 96 180, 40 200, ${WP.consulting.x} ${WP.consulting.y}`,
  // straight down the left margin into contracting
  `L ${WP.contracting.x} ${WP.contracting.y}`,
  // dogleg across and down toward the engagement stations
  `C 40 600, 30 640, ${WP.call.x} ${WP.call.y}`,
  // weave through the three stations: call -> proposal (dip) -> kickoff
  `Q 30 800, ${WP.proposal.x} ${WP.proposal.y}`,
  `Q 90 800, ${WP.kickoff.x} ${WP.kickoff.y}`,
  // curve out to the right margin for the dense credentials block
  `C 90 850, 96 900, ${WP.team.x} ${WP.team.y}`,
  // sweep back to the left for the faq stops
  `C 96 1120, 28 1180, ${WP.faq.x} ${WP.faq.y}`,
  // resolve down into the closing CTA pad
  `C 28 1420, 60 1460, ${WP.cta.x} ${WP.cta.y}`,
].join(" ");

const DRAW_DURATION = 1.8;

// Each waypoint node fades in at the fraction of DRAW_DURATION that roughly
// matches when the stroke reaches it. Pure time, no scroll, no progress.
const NODE_DELAYS: Record<keyof typeof WP, number> = {
  entry: 0.05,
  consulting: 0.32,
  contracting: 0.46,
  call: 0.62,
  proposal: 0.68,
  kickoff: 0.74,
  team: 0.84,
  faq: 1.18,
  cta: 1.5,
};

interface SpineProps {
  readonly active: boolean;
  readonly reduce: boolean;
}

function AdvisoryLine({ active, reduce }: SpineProps) {
  const drawn = reduce || active;

  return (
    <svg
      aria-hidden="true"
      viewBox={`0 0 ${SPINE_W} ${SPINE_H}`}
      preserveAspectRatio="xMidYMin slice"
      className="absolute inset-0 -z-10 h-full w-full"
    >
      <motion.path
        d={SPINE_PATH}
        fill="none"
        stroke="var(--color-cc-accent, #5eead4)"
        strokeWidth={1.5}
        strokeLinecap="round"
        strokeLinejoin="round"
        style={{ pathLength: 1 }}
        initial={reduce ? false : { pathLength: 0, opacity: 0.9 }}
        animate={drawn ? { pathLength: 1, opacity: 0.9 } : undefined}
        transition={{ duration: DRAW_DURATION, ease: [0.22, 1, 0.36, 1] }}
      />
      {/* Terminal arrowhead at the CTA. Drawn as the resolving final mark, it
          appears once the stroke has reached the bottom. */}
      <motion.path
        d={`M ${WP.cta.x - 5} ${WP.cta.y - 8} L ${WP.cta.x} ${WP.cta.y} L ${
          WP.cta.x + 5
        } ${WP.cta.y - 8}`}
        fill="none"
        stroke="var(--color-cc-accent, #5eead4)"
        strokeWidth={1.5}
        strokeLinecap="round"
        strokeLinejoin="round"
        initial={reduce ? false : { opacity: 0 }}
        animate={drawn ? { opacity: 0.9 } : undefined}
        transition={{ duration: 0.3, delay: reduce ? 0 : DRAW_DURATION - 0.1 }}
      />
      {/* Waypoint nodes: a teal dot with a faint ring, sitting on the line. */}
      {(Object.keys(WP) as (keyof typeof WP)[]).map((key) => {
        const { x, y } = WP[key];
        const delay = reduce ? 0 : NODE_DELAYS[key];
        return (
          <motion.g
            key={key}
            initial={reduce ? false : { opacity: 0, scale: 0 }}
            animate={drawn ? { opacity: 1, scale: 1 } : undefined}
            transition={{ duration: 0.4, delay, ease: "easeOut" }}
            style={{ transformOrigin: `${x} ${y}` }}
          >
            <circle
              cx={x}
              cy={y}
              r={6}
              fill="none"
              stroke="var(--color-cc-accent, #5eead4)"
              strokeWidth={1}
              opacity={0.4}
            />
            <circle
              cx={x}
              cy={y}
              r={2.6}
              fill="var(--color-cc-accent, #5eead4)"
            />
          </motion.g>
        );
      })}
    </svg>
  );
}

/* -------------------------------------------------------------------------- */
/*  Page                                                                       */
/* -------------------------------------------------------------------------- */

export function ClientPage() {
  const reduce = useReducedMotion() ?? false;
  const rootRef = useRef<HTMLDivElement>(null);
  // Commit the draw once when the spine wrapper enters the viewport.
  const inView = useInView(rootRef, { once: true, amount: 0.05 });

  return (
    <div ref={rootRef} className="relative isolate">
      {/* Faint radial glows at the entry point (top) and terminal point
          (bottom). The line itself is the only other ambient texture. */}
      <div
        aria-hidden="true"
        className="pointer-events-none absolute -z-20 blur-2xl"
        style={{
          top: 0,
          right: 0,
          width: 480,
          height: 480,
          background:
            "radial-gradient(circle, rgba(94,234,212,0.06) 0%, transparent 70%)",
        }}
      />
      <div
        aria-hidden="true"
        className="pointer-events-none absolute -z-20 blur-2xl"
        style={{
          bottom: 0,
          left: "30%",
          width: 480,
          height: 480,
          background:
            "radial-gradient(circle, rgba(94,234,212,0.06) 0%, transparent 70%)",
        }}
      />

      {/* The spine layer spans the whole content stack. */}
      <div
        aria-hidden="true"
        className="pointer-events-none absolute inset-0 -z-10"
      >
        <AdvisoryLine active={inView} reduce={reduce} />
      </div>

      <div className="mx-auto flex max-w-6xl flex-col">
        <Hero />
        <Tiers />
        <Engagement />
        <Team />
        <Faq />
        <ContactBand />
      </div>
    </div>
  );
}

/* -------------------------------------------------------------------------- */
/*  Sections. Left-padded so the spine reads in the left/right gutters.        */
/* -------------------------------------------------------------------------- */

function Hero() {
  return (
    <section className="border-cc-card-border border-b pt-10 pb-14 pl-0 sm:pt-16 sm:pl-12">
      <p className="text-cc-nav-label font-mono text-xs tracking-[0.18em] uppercase">
        ChilliCream Advisory
      </p>
      <h1 className="font-heading text-cc-heading text-h2 sm:text-h1 mt-5 max-w-3xl font-semibold tracking-tight text-balance">
        Talk to the engineers who built your GraphQL stack.
      </h1>
      <p className="text-cc-ink mt-6 max-w-2xl text-base text-pretty sm:text-lg">
        Hourly consulting at $300 per hour, or full contracting engagements,
        delivered by the team behind Hot Chocolate, Fusion, and Nitro. Bring a
        question, a design, or a deadline. We meet you where the work is.
      </p>
      <div className="mt-9 flex flex-wrap items-center gap-3">
        <SolidButton href={BOOKING_URL}>Book a 60-min call</SolidButton>
        <OutlineButton href={CONSULTING_MAILTO}>Email us</OutlineButton>
      </div>
      <p className="text-cc-nav-label mt-10 font-mono text-[0.65rem] tracking-[0.18em] uppercase">
        Entry waypoint · $300 per hour
      </p>
    </section>
  );
}

function Tiers() {
  return (
    <section
      aria-labelledby="tiers-heading"
      className="flex flex-col gap-6 py-14 pl-0 sm:pl-12"
    >
      <h2 id="tiers-heading" className="sr-only">
        Engagement tiers
      </h2>
      {TIERS.map((tier) => (
        <TierCard key={tier.id} tier={tier} />
      ))}
    </section>
  );
}

function TierCard({ tier }: { readonly tier: Tier }) {
  if (tier.highlight) {
    return (
      <div className="relative">
        <ConnectorTick />
        <div
          className="relative isolate rounded-3xl p-[1.5px]"
          style={{ background: SPECTRUM }}
        >
          <StartHerePill />
          <div className="bg-cc-surface flex flex-col rounded-[calc(1.5rem-1.5px)] p-7 sm:p-9">
            <TierCardBody tier={tier} />
          </div>
        </div>
      </div>
    );
  }

  return (
    <div className="relative">
      <ConnectorTick />
      <div className="bg-cc-card-bg border-cc-card-border hover:border-cc-card-border-hover flex flex-col rounded-3xl border p-7 transition-colors sm:p-9">
        <TierCardBody tier={tier} />
      </div>
    </div>
  );
}

// Short tick that ties a card back to the spine in the left gutter.
function ConnectorTick() {
  return (
    <span
      aria-hidden="true"
      className="bg-cc-accent/60 absolute top-12 -left-12 hidden h-px w-12 sm:block"
    />
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
      <ul className="mt-3 flex flex-col gap-3">
        {tier.perks.map((perk) => (
          <li key={perk} className="flex items-start gap-3">
            <span className="text-cc-accent mt-[5px] flex-none">
              <CheckIcon />
            </span>
            <span className="text-cc-ink text-sm">{perk}</span>
          </li>
        ))}
      </ul>

      <div className="mt-8 flex flex-col gap-3 sm:flex-row">
        <SolidButton href={tier.primaryCta.href} className="w-full sm:flex-1">
          {tier.primaryCta.label}
        </SolidButton>
        <OutlineButton
          href={tier.secondaryCta.href}
          className="w-full sm:flex-1"
        >
          {tier.secondaryCta.label}
        </OutlineButton>
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

function Engagement() {
  return (
    <section
      aria-labelledby="engagement-heading"
      className="border-cc-card-border bg-cc-card-bg/40 rounded-3xl border p-6 sm:p-10"
    >
      <div>
        <p className="text-cc-nav-label font-mono text-xs tracking-[0.18em] uppercase">
          How an engagement starts
        </p>
        <h2
          id="engagement-heading"
          className="font-heading text-cc-heading text-h4 sm:text-h3 mt-3 font-semibold"
        >
          From first call to first commit in three steps.
        </h2>
        <p className="text-cc-ink mt-4 max-w-2xl text-base">
          No long sales cycle. You speak to an engineer, you get a written
          proposal, you kick off.
        </p>
      </div>

      <ol className="mt-10 grid gap-6 md:grid-cols-3">
        {ENGAGEMENT_STEPS.map((step) => (
          <EngagementCard key={step.index} step={step} />
        ))}
      </ol>
    </section>
  );
}

function EngagementCard({ step }: { readonly step: EngagementStep }) {
  return (
    <li className="bg-cc-surface border-cc-card-border relative flex h-full flex-col rounded-2xl border p-6">
      {/* Connector tick binding the station to the line that curves below. */}
      <span
        aria-hidden="true"
        className="bg-cc-accent/60 absolute -bottom-4 left-8 hidden h-4 w-px md:block"
      />
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

function Team() {
  return (
    <section
      aria-labelledby="team-heading"
      className="mt-20 pr-0 sm:mt-28 sm:pr-12"
    >
      <div>
        <p className="text-cc-nav-label font-mono text-xs tracking-[0.18em] uppercase">
          Who you work with
        </p>
        <h2
          id="team-heading"
          className="font-heading text-cc-heading text-h4 sm:text-h3 mt-3 max-w-3xl font-semibold"
        >
          The team behind Hot Chocolate, Fusion, and Nitro.
        </h2>
        <p className="text-cc-ink mt-4 max-w-2xl text-base">
          ChilliCream advisory is not a generalist consultancy that learned
          GraphQL last quarter. The engineers on the call are the ones who write
          the framework you depend on.
        </p>
      </div>

      <div className="mt-10 grid gap-6 md:grid-cols-3">
        {CREDENTIALS.map((column) => (
          <CredentialColumnCard key={column.title} column={column} />
        ))}
      </div>

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

function Faq() {
  return (
    <section
      aria-labelledby="faq-heading"
      className="mt-20 pl-0 sm:mt-28 sm:pl-12"
    >
      <div>
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
        {FAQ.map((item, index) => (
          <FaqEntry key={item.question} item={item} index={index} />
        ))}
      </dl>
    </section>
  );
}

function FaqEntry({
  item,
  index,
}: {
  readonly item: FaqItem;
  readonly index: number;
}) {
  return (
    <div className="border-cc-card-border bg-cc-card-bg/60 relative rounded-2xl border p-6">
      <dt className="flex items-start gap-3">
        {/* Tiny teal index marker, reading as a labelled stop on the line. */}
        <span
          aria-hidden="true"
          className="text-cc-accent border-cc-accent/50 mt-[2px] flex h-5 w-5 flex-none items-center justify-center rounded-full border font-mono text-[0.6rem] tabular-nums"
        >
          {String(index + 1).padStart(2, "0")}
        </span>
        <span className="font-heading text-cc-heading text-base font-semibold">
          {item.question}
        </span>
      </dt>
      <dd className="text-cc-ink mt-3 text-sm leading-relaxed">
        {item.answer}
      </dd>
    </div>
  );
}

function ContactBand() {
  return (
    <section
      aria-labelledby="contact-heading"
      className="mt-20 mb-8 pl-0 sm:mt-28 sm:pl-12"
    >
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
            Book a 60-minute call. You walk us through what you are building, we
            ask the questions, and you leave with a clear next step. No
            commitment beyond the hour. If we are not the right fit, we will
            tell you on the call.
          </p>
          <ContactSpec />
        </div>
        <div className="flex flex-col gap-3 md:items-end">
          <SolidButton href={BOOKING_URL}>Book a 60-min call</SolidButton>
          <OutlineButton href={CONSULTING_MAILTO}>Email us</OutlineButton>
        </div>
      </div>
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

"use client";

import NextLink from "next/link";
import { useId, type ReactNode } from "react";
import { MotionConfig, motion, useReducedMotion } from "motion/react";

import { CheckIcon } from "@/src/components/CheckIcon";
import { OutlineButton, SolidButton } from "@/src/design-system/Button";

/* -------------------------------------------------------------------------- */
/*  The Triage Schematic                                                      */
/*  Help reads as a single annotated engineering diagram: a developer with a  */
/*  GraphQL problem is routed through a central triage hub into three labeled */
/*  outputs (Community, Consultancy, Support). Static layout, one time-driven */
/*  motion (dots traveling the connectors), no scroll coupling.               */
/* -------------------------------------------------------------------------- */

type PathKey = "community" | "consultancy" | "support";

interface PathDef {
  readonly key: PathKey;
  readonly name: string;
  readonly price: string;
  readonly priceNote?: string;
  readonly pitch: string;
  readonly responseTime: string;
  readonly bestFor: string;
  readonly features: ReadonlyArray<string>;
  readonly ctaLabel: string;
  readonly ctaHref: string;
}

const PATHS: ReadonlyArray<PathDef> = [
  {
    key: "community",
    name: "Community",
    price: "Free",
    pitch:
      "Ask in public, learn from thousands of GraphQL practitioners, and pay it forward when you can.",
    responseTime: "Best effort, community paced",
    bestFor: "Learning, sanity checks, sharing a repro",
    features: [
      "Public Slack channel",
      "7000+ individuals",
      "Open GitHub discussions",
      "Searchable history",
    ],
    ctaLabel: "Join Slack",
    ctaHref: "https://slack.chillicream.com/",
  },
  {
    key: "consultancy",
    name: "Consultancy",
    price: "$300",
    priceNote: "per hour",
    pitch:
      "Book a 60 minute session with a ChilliCream expert. Bring a problem, leave with a direction.",
    responseTime: "Usually within a few business days",
    bestFor: "Urgent unblock, design review, second opinion",
    features: [
      "Dedicated 60 min session",
      "One on one with an expert",
      "Architecture and review",
      "No long term contract",
    ],
    ctaLabel: "Book a session",
    ctaHref: "https://calendly.com/chillicream/60min",
  },
  {
    key: "support",
    name: "Support",
    price: "Custom",
    pitch:
      "An ongoing relationship for teams that ship GraphQL in production and need a partner on call.",
    responseTime: "Defined in your plan SLA",
    bestFor: "Production systems, regulated industries, scale",
    features: [
      "Dedicated account manager",
      "Private Slack channel",
      "Email support",
      "Plan tailored to your team",
    ],
    ctaLabel: "Check out plans",
    ctaHref: "/services/support",
  },
];

interface SelfServeLink {
  readonly label: string;
  readonly href: string;
  readonly external: boolean;
}

const SELF_SERVE: ReadonlyArray<SelfServeLink> = [
  { label: "Docs", href: "/docs", external: false },
  { label: "Blog", href: "/blog", external: false },
  {
    label: "YouTube",
    href: "https://www.youtube.com/c/ChilliCream",
    external: true,
  },
  {
    label: "GitHub",
    href: "https://github.com/ChilliCream/graphql-platform",
    external: true,
  },
];

interface FaqItem {
  readonly question: string;
  readonly answer: ReactNode;
}

const FAQ: ReadonlyArray<FaqItem> = [
  {
    question: "Which option gets me unblocked fastest?",
    answer:
      "For a defined problem with a deadline, Consultancy is the most reliable path. You book a 60 minute slot with a ChilliCream expert and walk in with a question. For lighter or open ended questions, Slack is faster than you might expect because the community is large and active.",
  },
  {
    question: "What response time can I expect on Slack?",
    answer:
      "Slack is best effort. Answers are usually quick during European working hours, but there is no guarantee. If your team has a hard deadline, do not rely on Slack alone. Book a Consultancy hour or move to a Support plan with a contractual SLA.",
  },
  {
    question: "When should we move from Consultancy to a Support plan?",
    answer:
      "Consultancy is great for one off questions and design reviews. A Support plan makes sense once GraphQL is on a critical path in production, you need a named contact, a private Slack channel, and a response time you can write into an internal runbook.",
  },
  {
    question: "How do I escalate something urgent in production?",
    answer:
      "Customers on a Support plan escalate through their dedicated account manager and private Slack channel, following the SLA in their plan. Without a Support plan, the fastest path is to book the next available Consultancy slot and post a clear repro in the public Slack in parallel.",
  },
];

export function ClientPage() {
  const prefersReducedMotion = useReducedMotion();

  return (
    <MotionConfig reducedMotion="user">
      <div className="mx-auto max-w-6xl space-y-24 px-4 pb-24 sm:px-6">
        <Hero />
        <Schematic animate={!prefersReducedMotion} />
        <PathDetails />
        <SelfServeStrip />
        <TriageFaq />
        <ClosingBar />
      </div>
    </MotionConfig>
  );
}

/* -------------------------------------------------------------------------- */
/*  Hero                                                                      */
/* -------------------------------------------------------------------------- */

function Hero() {
  return (
    <section className="pt-12 sm:pt-20">
      <p className="text-cc-nav-label font-mono text-xs font-semibold tracking-widest uppercase">
        Help / schematic v1.0
      </p>
      <h1 className="text-cc-heading font-heading text-hero mt-4 max-w-3xl">
        GraphQL help, <span className="text-cc-accent">routed</span>
      </h1>
      <p className="text-cc-ink-dim lead mt-6 max-w-2xl">
        One question, three exits. Drop your problem into triage and follow the
        line to the path that fits the urgency.
      </p>
      <div className="mt-10 flex flex-wrap gap-3">
        <SolidButton href="https://calendly.com/chillicream/60min">
          Book a consultancy hour
        </SolidButton>
        <OutlineButton href="https://slack.chillicream.com/">
          Ask in Slack
        </OutlineButton>
      </div>
    </section>
  );
}

/* -------------------------------------------------------------------------- */
/*  Schematic centerpiece                                                     */
/* -------------------------------------------------------------------------- */

interface Annotation {
  readonly label: string;
  readonly detail: string;
  readonly anchor: { readonly x: number; readonly y: number };
  readonly box: { readonly x: number; readonly y: number };
}

/* All coordinates live in the 1200x720 SVG viewBox so connector lines and the
   absolutely positioned callouts share one space. Callout boxes are placed in
   percentages derived from these same coordinates at md+. */
const ANNOTATIONS: ReadonlyArray<Annotation> = [
  {
    label: "Best effort response",
    detail: "Community paced, no SLA",
    anchor: { x: 980, y: 168 },
    box: { x: 1004, y: 96 },
  },
  {
    label: "7000+ practitioners",
    detail: "Public Slack and GitHub",
    anchor: { x: 980, y: 208 },
    box: { x: 1004, y: 208 },
  },
  {
    label: "60 minutes, one expert",
    detail: "Bring a problem, leave with a direction",
    anchor: { x: 980, y: 360 },
    box: { x: 1004, y: 320 },
  },
  {
    label: "Architecture and review",
    detail: "Design audits and migration planning",
    anchor: { x: 980, y: 400 },
    box: { x: 1004, y: 432 },
  },
  {
    label: "Private Slack + SLA",
    detail: "A response time you can write into a runbook",
    anchor: { x: 980, y: 552 },
    box: { x: 1004, y: 520 },
  },
  {
    label: "Dedicated account manager",
    detail: "A named partner on call",
    anchor: { x: 980, y: 592 },
    box: { x: 1004, y: 632 },
  },
];

interface SchematicProps {
  readonly animate: boolean;
}

function Schematic({ animate }: SchematicProps) {
  return (
    <section aria-labelledby="schematic-heading">
      <h2 id="schematic-heading" className="sr-only">
        How GraphQL help is routed
      </h2>

      <div className="border-cc-card-border bg-cc-surface relative overflow-hidden rounded-2xl border p-4 sm:p-8">
        <SchematicGrid />
        <div className="relative">
          <SchematicDiagram animate={animate} />

          {/* Fixed-coordinate callout boxes, fanned out at lg+ from the same
              1200x720 coordinate space the connector lines terminate in. The
              connector path stops at box.x - 8, so each box's left edge sits at
              box.x as a percentage of the 1200 wide viewBox. */}
          <div aria-hidden className="absolute inset-0 hidden lg:block">
            {ANNOTATIONS.map((annotation) => (
              <div
                key={annotation.label}
                className="border-cc-card-border bg-cc-card-bg absolute w-44 -translate-y-1/2 rounded-lg border px-3 py-2"
                style={{
                  left: `${(annotation.box.x / 1200) * 100}%`,
                  top: `${(annotation.box.y / 720) * 100}%`,
                }}
              >
                <p className="text-cc-heading font-mono text-[11px] leading-tight font-semibold tracking-wide">
                  {annotation.label}
                </p>
                <p className="text-cc-ink-dim mt-1 text-xs leading-snug">
                  {annotation.detail}
                </p>
              </div>
            ))}
          </div>
        </div>
      </div>

      {/* Stacked annotation list below the diagram for small screens, where the
          fixed-coordinate callouts cannot fan out. */}
      <ul className="mt-6 grid gap-3 sm:grid-cols-2 lg:hidden">
        {ANNOTATIONS.map((annotation) => (
          <li
            key={annotation.label}
            className="border-cc-card-border bg-cc-card-bg rounded-lg border p-4"
          >
            <p className="text-cc-heading font-mono text-xs font-semibold tracking-wide">
              {annotation.label}
            </p>
            <p className="text-cc-ink-dim mt-1 text-sm">{annotation.detail}</p>
          </li>
        ))}
      </ul>
    </section>
  );
}

/* The faint grid sits behind the schematic card only, two repeating gradients
   at 32px intervals, faded at the edges with a radial mask. */
function SchematicGrid() {
  return (
    <div
      aria-hidden
      className="pointer-events-none absolute inset-0"
      style={{
        backgroundImage:
          "repeating-linear-gradient(to right, rgba(255,255,255,0.025) 0, rgba(255,255,255,0.025) 1px, transparent 1px, transparent 32px), repeating-linear-gradient(to bottom, rgba(255,255,255,0.025) 0, rgba(255,255,255,0.025) 1px, transparent 1px, transparent 32px)",
        WebkitMaskImage:
          "radial-gradient(ellipse at center, black 55%, transparent 100%)",
        maskImage:
          "radial-gradient(ellipse at center, black 55%, transparent 100%)",
      }}
    />
  );
}

interface SchematicDiagramProps {
  readonly animate: boolean;
}

function SchematicDiagram({ animate }: SchematicDiagramProps) {
  const railId = useId();
  const railUrl = `url(#${railId})`;

  /* Node geometry shared by the rendered SVG and the connector paths. */
  const hub = { x: 600, y: 360 };
  const terminals: ReadonlyArray<{
    readonly key: PathKey;
    readonly title: string;
    readonly price: string;
    readonly y: number;
  }> = [
    { key: "community", title: "Community", price: "Free", y: 188 },
    { key: "consultancy", title: "Consultancy", price: "$300/hr", y: 380 },
    { key: "support", title: "Support", price: "Custom", y: 572 },
  ];
  const terminalX = 820;

  /* Connector paths from the hub out to each terminal node, used both as the
     visible stroke and as the offset-path for the traveling dots. */
  const connectorD = (targetY: number) =>
    `M ${hub.x + 90} ${hub.y} C ${hub.x + 150} ${hub.y}, ${terminalX - 60} ${targetY}, ${terminalX} ${targetY}`;

  return (
    <svg
      viewBox="0 0 1200 720"
      className="h-auto w-full"
      role="img"
      aria-label="Schematic routing a GraphQL question through a central triage hub into Community, Consultancy, and Support"
    >
      <defs>
        <linearGradient id={railId} x1="0%" y1="0%" x2="100%" y2="0%">
          <stop offset="0%" stopColor="#16b9e4" />
          <stop offset="50%" stopColor="#7c92c6" />
          <stop offset="100%" stopColor="#f0786a" />
        </linearGradient>
      </defs>

      {/* Developer / source node on the left */}
      <g>
        <rect
          x="60"
          y="312"
          width="200"
          height="96"
          rx="14"
          fill="#0b0f1a"
          stroke="rgba(245,241,234,0.12)"
          strokeWidth="1"
        />
        <text
          x="160"
          y="352"
          textAnchor="middle"
          className="font-heading"
          fill="#f5f0ea"
          fontSize="20"
        >
          Stuck on GraphQL
        </text>
        <text
          x="160"
          y="378"
          textAnchor="middle"
          fontFamily="var(--font-mono)"
          fill="#62748e"
          fontSize="12"
          letterSpacing="2"
        >
          INPUT
        </text>
      </g>

      {/* Source to hub feeder line */}
      <path
        d={`M 260 360 H ${hub.x - 90}`}
        fill="none"
        stroke="#5eead4"
        strokeWidth="1"
        strokeOpacity="0.55"
      />

      {/* Central Triage hub pill */}
      <g>
        <rect
          x={hub.x - 90}
          y={hub.y - 40}
          width="180"
          height="80"
          rx="40"
          fill="rgba(94,234,212,0.08)"
          stroke="#5eead4"
          strokeWidth="1.5"
        />
        <text
          x={hub.x}
          y={hub.y - 2}
          textAnchor="middle"
          className="font-heading"
          fill="#5eead4"
          fontSize="22"
        >
          Triage
        </text>
        <text
          x={hub.x}
          y={hub.y + 22}
          textAnchor="middle"
          fontFamily="var(--font-mono)"
          fill="#62748e"
          fontSize="11"
          letterSpacing="2"
        >
          ROUTER
        </text>
      </g>

      {/* Connector lines + terminal nodes */}
      {terminals.map((terminal) => (
        <g key={terminal.key} className="group">
          <path
            d={connectorD(terminal.y)}
            fill="none"
            stroke="#5eead4"
            strokeWidth="1"
            strokeOpacity="0.55"
            className="transition-[stroke-width,stroke-opacity] duration-200 group-hover:[stroke-width:2.5] group-hover:[stroke-opacity:1]"
          />
          <rect
            x={terminalX}
            y={terminal.y - 40}
            width="220"
            height="80"
            rx="14"
            fill="rgba(94,234,212,0.06)"
            stroke="#5eead4"
            strokeWidth="1"
            className="transition-[stroke-width] duration-200 group-hover:[stroke-width:2]"
          />
          <text
            x={terminalX + 24}
            y={terminal.y - 6}
            className="font-heading"
            fill="#f5f0ea"
            fontSize="20"
          >
            {terminal.title}
          </text>
          <text
            x={terminalX + 24}
            y={terminal.y + 20}
            fontFamily="var(--font-mono)"
            fill="#5eead4"
            fontSize="13"
          >
            {terminal.price}
          </text>
        </g>
      ))}

      {/* Annotation connector lines + box anchors (lines drawn inside the SVG
          so they share the diagram coordinate space) */}
      {ANNOTATIONS.map((annotation) => (
        <g key={annotation.label}>
          <path
            d={`M ${annotation.anchor.x} ${annotation.anchor.y} H ${annotation.box.x - 8}`}
            fill="none"
            stroke="rgba(245,241,234,0.32)"
            strokeWidth="1"
          />
          <circle
            cx={annotation.anchor.x}
            cy={annotation.anchor.y}
            r="2.5"
            fill="#5eead4"
          />
        </g>
      ))}

      {/* Brand spectrum bus rail under the three output nodes: the single
          spectrum element on the page */}
      <rect x={terminalX} y="648" width="380" height="2" fill={railUrl} />
      <path
        d={`M ${terminalX + 110} 612 V 648 M ${terminalX + 110} 380 V 648`}
        fill="none"
        stroke="rgba(245,241,234,0.12)"
        strokeWidth="1"
      />

      {/* Traveling dots: one per connector, time-driven only, looping with a
          per-path offset. Hidden entirely when reduced motion is requested. */}
      {animate
        ? terminals.map((terminal, index) => (
            <TravelingDot
              key={terminal.key}
              d={connectorD(terminal.y)}
              delay={index * 1.3}
            />
          ))
        : null}
    </svg>
  );
}

interface TravelingDotProps {
  readonly d: string;
  readonly delay: number;
}

function TravelingDot({ d, delay }: TravelingDotProps) {
  const pathId = useId();

  return (
    <>
      <path id={pathId} d={d} fill="none" stroke="none" />
      <motion.circle
        r="4"
        fill="#5eead4"
        initial={{ offsetDistance: "0%", opacity: 0 }}
        animate={{
          offsetDistance: ["0%", "100%"],
          opacity: [0, 1, 1, 0],
        }}
        transition={{
          duration: 4,
          delay,
          repeat: Infinity,
          ease: "linear",
        }}
        style={{ offsetPath: `path('${d}')` }}
      />
    </>
  );
}

/* -------------------------------------------------------------------------- */
/*  Path detail cards                                                         */
/* -------------------------------------------------------------------------- */

function PathDetails() {
  return (
    <section aria-labelledby="paths-heading">
      <h2 id="paths-heading" className="sr-only">
        The three help paths in detail
      </h2>
      <div className="grid gap-6 lg:grid-cols-3">
        {PATHS.map((path) => (
          <PathCard key={path.key} path={path} />
        ))}
      </div>
    </section>
  );
}

function PathCard({ path }: { readonly path: PathDef }) {
  const Button = path.key === "consultancy" ? SolidButton : OutlineButton;

  return (
    <article className="border-cc-card-border bg-cc-card-bg hover:border-cc-card-border-hover flex flex-col rounded-2xl border p-8 transition-colors">
      <header>
        <h3 className="text-cc-heading font-heading text-h4">{path.name}</h3>
        <div className="mt-3 flex items-baseline gap-2">
          <span className="text-cc-heading text-4xl font-semibold tracking-tight">
            {path.price}
          </span>
          {path.priceNote ? (
            <span className="text-cc-ink-dim text-sm">{path.priceNote}</span>
          ) : null}
        </div>
        <p className="text-cc-ink mt-4 text-sm leading-relaxed">{path.pitch}</p>
      </header>

      <dl className="border-cc-card-border mt-6 grid gap-3 border-y py-5 text-sm">
        <div>
          <dt className="text-cc-nav-label font-mono text-[10px] tracking-widest uppercase">
            Response
          </dt>
          <dd className="text-cc-ink mt-1">{path.responseTime}</dd>
        </div>
        <div>
          <dt className="text-cc-nav-label font-mono text-[10px] tracking-widest uppercase">
            Best for
          </dt>
          <dd className="text-cc-ink mt-1">{path.bestFor}</dd>
        </div>
      </dl>

      <ul className="mt-6 space-y-3 text-sm">
        {path.features.map((feature) => (
          <li key={feature} className="text-cc-ink flex items-start gap-3">
            <span className="text-cc-accent mt-1">
              <CheckIcon />
            </span>
            <span>{feature}</span>
          </li>
        ))}
      </ul>

      <div className="mt-8 pt-2">
        <Button href={path.ctaHref} className="w-full">
          {path.ctaLabel}
        </Button>
      </div>
    </article>
  );
}

/* -------------------------------------------------------------------------- */
/*  Self serve strip                                                          */
/* -------------------------------------------------------------------------- */

function SelfServeStrip() {
  return (
    <section aria-labelledby="self-serve-heading">
      <h2
        id="self-serve-heading"
        className="text-cc-nav-label font-mono text-xs font-semibold tracking-widest uppercase"
      >
        See also
      </h2>
      <div className="border-cc-card-border mt-4 flex flex-wrap items-center gap-3 border-y py-6">
        {SELF_SERVE.map((link) =>
          link.external ? (
            <a
              key={link.label}
              href={link.href}
              target="_blank"
              rel="noopener noreferrer"
              className="text-cc-ink hover:text-cc-accent hover:border-cc-card-border-hover border-cc-card-border bg-cc-card-bg rounded-full border px-4 py-2 font-mono text-sm transition-colors"
            >
              {link.label}
            </a>
          ) : (
            <NextLink
              key={link.label}
              href={link.href}
              className="text-cc-ink hover:text-cc-accent hover:border-cc-card-border-hover border-cc-card-border bg-cc-card-bg rounded-full border px-4 py-2 font-mono text-sm transition-colors"
            >
              {link.label}
            </NextLink>
          ),
        )}
      </div>
    </section>
  );
}

/* -------------------------------------------------------------------------- */
/*  Triage FAQ                                                                */
/* -------------------------------------------------------------------------- */

function TriageFaq() {
  return (
    <section aria-labelledby="faq-heading">
      <h2
        id="faq-heading"
        className="text-cc-heading font-heading text-h2 max-w-2xl"
      >
        Triage notes
      </h2>
      <dl className="mt-10 grid gap-x-12 gap-y-10 md:grid-cols-2">
        {FAQ.map((item) => (
          <div key={item.question}>
            <dt className="text-cc-heading font-heading text-h6">
              {item.question}
            </dt>
            <dd className="text-cc-ink-dim mt-3 text-sm leading-relaxed">
              {item.answer}
            </dd>
          </div>
        ))}
      </dl>
    </section>
  );
}

/* -------------------------------------------------------------------------- */
/*  Closing CTA bar                                                           */
/* -------------------------------------------------------------------------- */

function ClosingBar() {
  return (
    <section className="border-cc-card-border bg-cc-card-bg rounded-2xl border px-8 py-12 sm:px-12">
      <div className="flex flex-col gap-6 md:flex-row md:items-center md:justify-between">
        <div>
          <h2 className="text-cc-heading font-heading text-h4 max-w-xl">
            Book a consultancy hour. If we are not the right help, we will say
            so.
          </h2>
          <p className="text-cc-ink-dim mt-2 max-w-xl text-sm">
            Bring whatever you have, walk out with a direction.
          </p>
        </div>
        <div className="shrink-0">
          <SolidButton href="https://calendly.com/chillicream/60min">
            Book a consultancy hour
          </SolidButton>
        </div>
      </div>
    </section>
  );
}

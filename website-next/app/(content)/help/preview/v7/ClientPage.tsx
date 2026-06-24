"use client";

import NextLink from "next/link";
import { useEffect, useId, useRef, useState, type ReactNode } from "react";
import {
  AnimatePresence,
  MotionConfig,
  motion,
  useInView,
  useReducedMotion,
} from "motion/react";

import { CheckIcon } from "@/src/components/CheckIcon";
import { OutlineButton, SolidButton } from "@/src/design-system/Button";

type RouteKey = "community" | "consultancy" | "support";

interface RouteDef {
  readonly key: RouteKey;
  readonly toggleLabel: string;
  readonly toggleSub: string;
  readonly leafLabel: string;
  readonly tierName: string;
}

const ROUTES: ReadonlyArray<RouteDef> = [
  {
    key: "community",
    toggleLabel: "Learning",
    toggleSub: "No deadline",
    leafLabel: "Community",
    tierName: "Community",
  },
  {
    key: "consultancy",
    toggleLabel: "Unblock this week",
    toggleSub: "Has deadline",
    leafLabel: "Consultancy",
    tierName: "Consultancy",
  },
  {
    key: "support",
    toggleLabel: "Production critical",
    toggleSub: "Need named contact",
    leafLabel: "Support",
    tierName: "Support",
  },
];

interface TierFeature {
  readonly label: string;
}

interface Tier {
  readonly name: string;
  readonly price: string;
  readonly priceNote?: string;
  readonly pitch: string;
  readonly responseTime: string;
  readonly bestFor: string;
  readonly features: ReadonlyArray<TierFeature>;
  readonly ctaLabel: string;
  readonly ctaHref: string;
  readonly highlight?: boolean;
  readonly badge?: string;
}

const TIERS: ReadonlyArray<Tier> = [
  {
    name: "Community",
    price: "Free",
    pitch:
      "Ask in public, learn from thousands of GraphQL practitioners, and pay it forward when you can.",
    responseTime: "Best effort, community paced",
    bestFor: "Learning, sanity checks, sharing a repro",
    features: [
      { label: "Public Slack channel" },
      { label: "7000+ individuals" },
      { label: "Open GitHub discussions" },
      { label: "Searchable history" },
    ],
    ctaLabel: "Join Slack",
    ctaHref: "https://slack.chillicream.com/",
  },
  {
    name: "Consultancy",
    price: "$300",
    priceNote: "per hour",
    pitch:
      "Book a 60 minute session with a ChilliCream expert. Bring a problem, leave with a direction.",
    responseTime: "Usually within a few business days",
    bestFor: "Urgent unblock, design review, second opinion",
    features: [
      { label: "Dedicated 60 min session" },
      { label: "One on one with an expert" },
      { label: "Architecture and review" },
      { label: "No long term contract" },
    ],
    ctaLabel: "Book a session",
    ctaHref: "https://calendly.com/chillicream/60min",
    highlight: true,
    badge: "Fastest answer",
  },
  {
    name: "Support",
    price: "Custom",
    pitch:
      "An ongoing relationship for teams that ship GraphQL in production and need a partner on call.",
    responseTime: "Defined in your plan SLA",
    bestFor: "Production systems, regulated industries, scale",
    features: [
      { label: "Dedicated account manager" },
      { label: "Private Slack channel" },
      { label: "Email support" },
      { label: "Plan tailored to your team" },
    ],
    ctaLabel: "Check out plans",
    ctaHref: "/services/support",
  },
];

interface SelfServeChannel {
  readonly title: string;
  readonly description: string;
  readonly href: string;
  readonly external: boolean;
  readonly icon: ReactNode;
}

const SELF_SERVE: ReadonlyArray<SelfServeChannel> = [
  {
    title: "Docs",
    description:
      "Guides, recipes, and the full Hot Chocolate and Fusion reference.",
    href: "/docs",
    external: false,
    icon: <DocsGlyph />,
  },
  {
    title: "Blog",
    description: "Release notes, deep dives, and patterns from the team.",
    href: "/blog",
    external: false,
    icon: <BlogGlyph />,
  },
  {
    title: "Slack",
    description: "Live conversation with maintainers and 7000+ developers.",
    href: "https://slack.chillicream.com/",
    external: true,
    icon: <SlackGlyph />,
  },
  {
    title: "YouTube",
    description:
      "Workshops, talks, and walkthroughs from the ChilliCream team.",
    href: "https://www.youtube.com/c/ChilliCream",
    external: true,
    icon: <PlayGlyph />,
  },
  {
    title: "GitHub",
    description:
      "Source, issues, and discussions for graphql-platform on GitHub.",
    href: "https://github.com/ChilliCream/graphql-platform",
    external: true,
    icon: <GitHubGlyph />,
  },
];

interface FaqItem {
  readonly question: string;
  readonly answer: ReactNode;
}

const FAQ: ReadonlyArray<FaqItem> = [
  {
    question: "Which option gets me unblocked fastest?",
    answer: (
      <>
        For a defined problem with a deadline, Consultancy is the most reliable
        path. You book a 60 minute slot with a ChilliCream expert and walk in
        with a question. For lighter or open ended questions, Slack is faster
        than you might expect because the community is large and active.
      </>
    ),
  },
  {
    question: "What response time can I expect on Slack?",
    answer: (
      <>
        Slack is best effort. Answers are usually quick during European working
        hours, but there is no guarantee. If your team has a hard deadline, do
        not rely on Slack alone. Book a Consultancy hour or move to a Support
        plan with a contractual SLA.
      </>
    ),
  },
  {
    question: "When should we move from Consultancy to a Support plan?",
    answer: (
      <>
        Consultancy is great for one off questions and design reviews. A Support
        plan makes sense once GraphQL is on a critical path in production, you
        need a named contact, a private Slack channel, and a response time you
        can write into an internal runbook.
      </>
    ),
  },
  {
    question: "How do I escalate something urgent in production?",
    answer: (
      <>
        Customers on a Support plan escalate through their dedicated account
        manager and private Slack channel, following the SLA in their plan.
        Without a Support plan, the fastest path is to book the next available
        Consultancy slot and post a clear repro in the public Slack in parallel.
      </>
    ),
  },
  {
    question: "Can ChilliCream help us design a schema or migration?",
    answer: (
      <>
        Yes. Design reviews, schema audits, and migration planning are common
        Consultancy topics. For larger engagements, our{" "}
        <NextLink
          href="/services/advisory"
          className="text-cc-accent hover:text-cc-accent-hover underline"
        >
          advisory service
        </NextLink>{" "}
        wraps that work in a structured engagement with deliverables.
      </>
    ),
  },
  {
    question: "Is the community Slack the right place for bug reports?",
    answer: (
      <>
        Slack is good for triage and reproductions. Once a bug is confirmed,
        please file it on{" "}
        <a
          href="https://github.com/ChilliCream/graphql-platform"
          target="_blank"
          rel="noopener noreferrer"
          className="text-cc-accent hover:text-cc-accent-hover underline"
        >
          GitHub
        </a>{" "}
        so it gets a tracking issue, a label, and a place to land the fix.
      </>
    ),
  },
];

export function ClientPage() {
  const reduce = useReducedMotion();
  // When reduced motion is on we lock to the production route so both the
  // centerpiece and the tier highlight resolve to a static final frame.
  const initial: RouteKey = reduce ? "support" : "consultancy";
  const [activeRoute, setActiveRoute] = useState<RouteKey>(initial);
  const displayedRoute: RouteKey = reduce ? "support" : activeRoute;

  return (
    <MotionConfig reducedMotion="user">
      <div className="space-y-24 pb-24">
        <HelpHero />
        <RoutingTreeCenterpiece
          activeRoute={displayedRoute}
          onRouteChange={setActiveRoute}
        />
        <TiersSection activeRoute={displayedRoute} />
        <ResponseTimeline />
        <SelfServeSection />
        <FaqSection />
        <ClosingCta />
      </div>
    </MotionConfig>
  );
}

function HelpHero() {
  return (
    <section className="pt-12 pb-4 text-center sm:pt-20">
      <div className="text-cc-nav-label mb-4 font-mono text-xs font-semibold tracking-widest uppercase">
        Help
      </div>
      <h1 className="text-cc-heading font-heading text-hero mx-auto max-w-4xl">
        Get unblocked, <span className="text-cc-accent">on your timeline</span>.
      </h1>
      <p className="text-cc-ink-dim lead mx-auto mt-6 max-w-2xl">
        Three honest paths to GraphQL help: a free community of 7000+
        practitioners, expert consultancy by the hour, and tailored support
        plans for production teams. Pick the one that matches the urgency.
      </p>
      <div className="mt-10 flex flex-wrap justify-center gap-3">
        <SolidButton href="https://calendly.com/chillicream/60min">
          Book a consultancy hour
        </SolidButton>
        <OutlineButton href="https://slack.chillicream.com/">
          Ask in Slack
        </OutlineButton>
      </div>
      <div className="text-cc-ink-dim mx-auto mt-8 inline-flex items-center gap-2 text-xs">
        <span aria-hidden>
          <ArrowDownGlyph />
        </span>
        <span>Watch how a question finds its tier</span>
      </div>
    </section>
  );
}

/* Centerpiece geometry. The SVG is 720 wide x 360 tall (viewBox).
 * Root at (360, 30). First branch splits left/right at (180, 120) / (540, 120).
 * Second branch from (540, 120) splits to (420, 210) / (660, 210).
 * Leaves: Community at (180, 310), Consultancy at (420, 310), Support at (660, 310).
 */
const ROOT = { x: 360, y: 30 } as const;
const Q1_LEARN = { x: 180, y: 120 } as const; // No deadline branch label point
const Q1_DEADLINE = { x: 540, y: 120 } as const; // Has deadline branch label point
const Q2_CONSULT = { x: 420, y: 210 } as const; // Production critical? -> No
const Q2_SUPPORT = { x: 660, y: 210 } as const; // Production critical? -> Yes
const LEAF_COMMUNITY = { x: 180, y: 310 } as const;
const LEAF_CONSULTANCY = { x: 420, y: 310 } as const;
const LEAF_SUPPORT = { x: 660, y: 310 } as const;

interface Point {
  readonly x: number;
  readonly y: number;
}

interface RoutePath {
  readonly d: string;
  readonly leaf: Point;
}

const ROUTE_PATHS: Record<RouteKey, RoutePath> = {
  community: {
    d: `M ${ROOT.x} ${ROOT.y} L ${Q1_LEARN.x} ${Q1_LEARN.y} L ${LEAF_COMMUNITY.x} ${LEAF_COMMUNITY.y}`,
    leaf: LEAF_COMMUNITY,
  },
  consultancy: {
    d: `M ${ROOT.x} ${ROOT.y} L ${Q1_DEADLINE.x} ${Q1_DEADLINE.y} L ${Q2_CONSULT.x} ${Q2_CONSULT.y} L ${LEAF_CONSULTANCY.x} ${LEAF_CONSULTANCY.y}`,
    leaf: LEAF_CONSULTANCY,
  },
  support: {
    d: `M ${ROOT.x} ${ROOT.y} L ${Q1_DEADLINE.x} ${Q1_DEADLINE.y} L ${Q2_SUPPORT.x} ${Q2_SUPPORT.y} L ${LEAF_SUPPORT.x} ${LEAF_SUPPORT.y}`,
    leaf: LEAF_SUPPORT,
  },
};

interface RoutingTreeProps {
  readonly activeRoute: RouteKey;
  readonly onRouteChange: (route: RouteKey) => void;
}

function RoutingTreeCenterpiece({
  activeRoute,
  onRouteChange,
}: RoutingTreeProps) {
  const reduce = useReducedMotion();
  const containerRef = useRef<HTMLDivElement>(null);
  const inView = useInView(containerRef, { once: false, margin: "-15%" });
  // Auto-play unless reduced motion is on. When user toggles a pill we also
  // pause via the click handler below.
  const [playing, setPlaying] = useState<boolean>(() => !reduce);
  const headingId = useId();
  const offsetPathId = useId();

  // Auto-rotate routes only while in view and not reduced-motion.
  useEffect(() => {
    if (reduce || !inView || !playing) {
      return;
    }
    const order: ReadonlyArray<RouteKey> = [
      "community",
      "consultancy",
      "support",
    ];
    const interval = window.setInterval(() => {
      const idx = order.indexOf(activeRoute);
      const next = order[(idx + 1) % order.length];
      onRouteChange(next);
    }, 3200);
    return () => window.clearInterval(interval);
  }, [reduce, inView, playing, activeRoute, onRouteChange]);

  const route = ROUTE_PATHS[activeRoute];

  return (
    <section aria-labelledby={headingId}>
      <div className="mx-auto max-w-2xl text-center">
        <div className="text-cc-nav-label mb-3 font-mono text-xs font-semibold tracking-widest uppercase">
          Routing tree
        </div>
        <h2 id={headingId} className="text-cc-heading font-heading text-h2">
          Watch a question find its tier.
        </h2>
        <p className="text-cc-ink-dim mt-4 text-base sm:text-lg">
          A token enters at the question, walks the urgency branches, and lands
          on the tier that matches.
        </p>
      </div>

      <div
        ref={containerRef}
        className="border-cc-card-border bg-cc-card-bg mx-auto mt-10 max-w-4xl overflow-hidden rounded-2xl border p-6 sm:p-8"
      >
        <div className="flex flex-col gap-4 sm:flex-row sm:items-center sm:justify-between">
          <div
            role="group"
            aria-label="Force a route"
            className="border-cc-card-border bg-cc-surface/40 inline-flex flex-wrap gap-1 rounded-full border p-1"
          >
            {ROUTES.map((r) => {
              const active = r.key === activeRoute;
              return (
                <button
                  key={r.key}
                  type="button"
                  onClick={() => {
                    onRouteChange(r.key);
                    setPlaying(false);
                  }}
                  aria-pressed={active}
                  className={
                    active
                      ? "bg-cc-accent text-cc-surface rounded-full px-3 py-1.5 text-xs font-semibold transition-colors"
                      : "text-cc-ink-dim hover:text-cc-ink rounded-full px-3 py-1.5 text-xs font-medium transition-colors"
                  }
                >
                  <span className="block leading-tight">{r.toggleLabel}</span>
                  <span className="block text-[10px] leading-tight opacity-70">
                    {r.toggleSub}
                  </span>
                </button>
              );
            })}
          </div>

          <button
            type="button"
            onClick={() => setPlaying((p) => !p)}
            aria-pressed={playing}
            aria-label={playing ? "Pause auto-rotate" : "Play auto-rotate"}
            className="text-cc-ink-dim hover:text-cc-ink border-cc-card-border hover:border-cc-card-border-hover inline-flex items-center gap-2 self-start rounded-full border px-3 py-1.5 text-xs font-medium transition-colors sm:self-auto"
          >
            {playing ? <PauseGlyph /> : <PlayMiniGlyph />}
            <span>{playing ? "Pause" : "Play"}</span>
          </button>
        </div>

        <div className="relative mt-6">
          <svg
            viewBox="0 0 720 360"
            role="img"
            aria-label="Decision tree routing a question to the matching help tier."
            className="text-cc-ink-dim w-full"
          >
            <defs>
              <linearGradient
                id={`${offsetPathId}-glow`}
                x1="0%"
                y1="0%"
                x2="100%"
                y2="100%"
              >
                <stop offset="0%" stopColor="#5eead4" stopOpacity="0.9" />
                <stop offset="100%" stopColor="#5eead4" stopOpacity="0.55" />
              </linearGradient>
            </defs>

            {/* Static branch skeleton (dim). */}
            <BranchSkeleton activeRoute={activeRoute} />

            {/* Decision node labels. */}
            <NodeLabel
              x={ROOT.x}
              y={ROOT.y}
              label="Stuck on GraphQL?"
              kind="root"
            />
            <NodeLabel
              x={Q1_LEARN.x}
              y={Q1_LEARN.y}
              label="No deadline"
              kind="branch"
            />
            <NodeLabel
              x={Q1_DEADLINE.x}
              y={Q1_DEADLINE.y}
              label="Has deadline"
              kind="branch"
            />
            <NodeLabel
              x={Q2_CONSULT.x}
              y={Q2_CONSULT.y}
              label="Not production yet"
              kind="branch"
            />
            <NodeLabel
              x={Q2_SUPPORT.x}
              y={Q2_SUPPORT.y}
              label="Production critical"
              kind="branch"
            />

            {/* Leaf nodes. */}
            <LeafNode
              x={LEAF_COMMUNITY.x}
              y={LEAF_COMMUNITY.y}
              label="Community"
              active={activeRoute === "community"}
            />
            <LeafNode
              x={LEAF_CONSULTANCY.x}
              y={LEAF_CONSULTANCY.y}
              label="Consultancy"
              active={activeRoute === "consultancy"}
            />
            <LeafNode
              x={LEAF_SUPPORT.x}
              y={LEAF_SUPPORT.y}
              label="Support"
              active={activeRoute === "support"}
            />

            {/* Animated active path. AnimatePresence so non-active branches fade. */}
            <AnimatePresence mode="wait">
              <motion.path
                key={activeRoute}
                d={route.d}
                fill="none"
                stroke="#5eead4"
                strokeWidth={2}
                strokeLinecap="round"
                strokeLinejoin="round"
                initial={reduce ? { pathLength: 1 } : { pathLength: 0 }}
                animate={{ pathLength: 1 }}
                exit={{ opacity: 0 }}
                transition={
                  reduce
                    ? { duration: 0 }
                    : { duration: 1.6, ease: [0.4, 0, 0.2, 1] }
                }
                style={{
                  filter:
                    "drop-shadow(0 0 6px rgba(94,234,212,0.45)) drop-shadow(0 0 12px rgba(94,234,212,0.25))",
                }}
              />
            </AnimatePresence>

            {/* Traveling token. Uses CSS offsetPath via inline style. */}
            {!reduce && (
              <motion.circle
                key={`token-${activeRoute}`}
                r={5}
                cx={0}
                cy={0}
                fill="#5eead4"
                initial={{ offsetDistance: "0%" }}
                animate={{ offsetDistance: "100%" }}
                transition={{ duration: 1.6, ease: [0.4, 0, 0.2, 1] }}
                style={{
                  offsetPath: `path('${route.d}')`,
                  filter:
                    "drop-shadow(0 0 4px rgba(94,234,212,0.9)) drop-shadow(0 0 10px rgba(94,234,212,0.5))",
                }}
              />
            )}
          </svg>
        </div>

        <div className="border-cc-card-border mt-6 flex flex-wrap items-center justify-center gap-x-6 gap-y-2 border-t pt-4 text-xs">
          <LegendDot label="Community" />
          <LegendDot label="Consultancy" />
          <LegendDot label="Support" />
        </div>
      </div>
    </section>
  );
}

function BranchSkeleton({ activeRoute }: { readonly activeRoute: RouteKey }) {
  // Dim every edge; the animated path on top highlights the chosen route.
  const dim = (key: RouteKey) =>
    activeRoute === key ? "rgba(94,234,212,0.18)" : "rgba(124,146,198,0.20)";

  return (
    <g fill="none" strokeWidth={1.5} strokeLinecap="round">
      <line
        x1={ROOT.x}
        y1={ROOT.y}
        x2={Q1_LEARN.x}
        y2={Q1_LEARN.y}
        stroke={dim("community")}
      />
      <line
        x1={ROOT.x}
        y1={ROOT.y}
        x2={Q1_DEADLINE.x}
        y2={Q1_DEADLINE.y}
        stroke={dim("consultancy")}
      />
      <line
        x1={Q1_LEARN.x}
        y1={Q1_LEARN.y}
        x2={LEAF_COMMUNITY.x}
        y2={LEAF_COMMUNITY.y}
        stroke={dim("community")}
      />
      <line
        x1={Q1_DEADLINE.x}
        y1={Q1_DEADLINE.y}
        x2={Q2_CONSULT.x}
        y2={Q2_CONSULT.y}
        stroke={dim("consultancy")}
      />
      <line
        x1={Q1_DEADLINE.x}
        y1={Q1_DEADLINE.y}
        x2={Q2_SUPPORT.x}
        y2={Q2_SUPPORT.y}
        stroke={dim("support")}
      />
      <line
        x1={Q2_CONSULT.x}
        y1={Q2_CONSULT.y}
        x2={LEAF_CONSULTANCY.x}
        y2={LEAF_CONSULTANCY.y}
        stroke={dim("consultancy")}
      />
      <line
        x1={Q2_SUPPORT.x}
        y1={Q2_SUPPORT.y}
        x2={LEAF_SUPPORT.x}
        y2={LEAF_SUPPORT.y}
        stroke={dim("support")}
      />
    </g>
  );
}

interface NodeLabelProps {
  readonly x: number;
  readonly y: number;
  readonly label: string;
  readonly kind: "root" | "branch";
}

function NodeLabel({ x, y, label, kind }: NodeLabelProps) {
  const width = kind === "root" ? 160 : 130;
  const height = 28;
  return (
    <g transform={`translate(${x - width / 2}, ${y - height / 2})`}>
      <rect
        width={width}
        height={height}
        rx={14}
        fill="rgba(12,19,34,0.85)"
        stroke="rgba(124,146,198,0.25)"
        strokeWidth={1}
      />
      <text
        x={width / 2}
        y={height / 2 + 4}
        textAnchor="middle"
        fontSize={kind === "root" ? 12 : 11}
        fontWeight={kind === "root" ? 600 : 500}
        fill={kind === "root" ? "#f6f7fb" : "#c3c9d6"}
        fontFamily="var(--font-sans, system-ui, sans-serif)"
      >
        {label}
      </text>
    </g>
  );
}

interface LeafNodeProps {
  readonly x: number;
  readonly y: number;
  readonly label: string;
  readonly active: boolean;
}

function LeafNode({ x, y, label, active }: LeafNodeProps) {
  const width = 130;
  const height = 36;
  return (
    <g transform={`translate(${x - width / 2}, ${y - height / 2})`}>
      <motion.rect
        width={width}
        height={height}
        rx={18}
        animate={{
          fill: active ? "rgba(94,234,212,0.14)" : "rgba(12,19,34,0.85)",
          stroke: active ? "#5eead4" : "rgba(124,146,198,0.25)",
        }}
        transition={{ duration: 0.4 }}
        strokeWidth={1.25}
        style={{
          filter: active
            ? "drop-shadow(0 0 8px rgba(94,234,212,0.35))"
            : "none",
        }}
      />
      <text
        x={width / 2}
        y={height / 2 + 4}
        textAnchor="middle"
        fontSize={12}
        fontWeight={600}
        fill={active ? "#5eead4" : "#f6f7fb"}
        fontFamily="var(--font-sans, system-ui, sans-serif)"
      >
        {label}
      </text>
    </g>
  );
}

function LegendDot({ label }: { readonly label: string }) {
  return (
    <span className="text-cc-ink-dim inline-flex items-center gap-2">
      <span className="bg-cc-accent inline-block h-1.5 w-1.5 rounded-full" />
      <span>{label}</span>
    </span>
  );
}

function TiersSection({ activeRoute }: { readonly activeRoute: RouteKey }) {
  return (
    <section aria-labelledby="tiers-heading">
      <div className="mx-auto max-w-2xl text-center">
        <div className="text-cc-nav-label mb-3 font-mono text-xs font-semibold tracking-widest uppercase">
          Three paths
        </div>
        <h2 id="tiers-heading" className="text-cc-heading font-heading text-h2">
          Choose the help that matches the moment.
        </h2>
        <p className="text-cc-ink-dim mt-4 text-base sm:text-lg">
          Community for thinking out loud, Consultancy for getting unstuck this
          week, Support for teams that depend on GraphQL in production.
        </p>
      </div>

      <div className="mt-12 grid gap-6 md:grid-cols-3">
        {TIERS.map((tier) => (
          <TierCard
            key={tier.name}
            tier={tier}
            active={
              ROUTES.find((r) => r.key === activeRoute)?.tierName === tier.name
            }
          />
        ))}
      </div>

      <p className="text-cc-ink-dim mt-8 text-center text-sm">
        Not sure which to pick? Start in{" "}
        <a
          href="https://slack.chillicream.com/"
          target="_blank"
          rel="noopener noreferrer"
          className="text-cc-accent hover:text-cc-accent-hover underline"
        >
          Slack
        </a>
        . If the problem outgrows it, a consultancy hour is one click away.
      </p>
    </section>
  );
}

interface TierCardProps {
  readonly tier: Tier;
  readonly active: boolean;
}

function TierCard({ tier, active }: TierCardProps) {
  const Button = tier.highlight ? SolidButton : OutlineButton;

  const base = tier.highlight
    ? "relative flex flex-col rounded-2xl border border-cc-accent/40 bg-cc-card-bg p-8 shadow-[0_0_0_1px_rgba(94,234,212,0.15),0_20px_60px_-20px_rgba(94,234,212,0.35)]"
    : "relative flex flex-col rounded-2xl border border-cc-card-border bg-cc-card-bg p-8 transition-colors hover:border-cc-card-border-hover";

  return (
    <motion.article
      layoutId={`tier-${tier.name.toLowerCase()}`}
      className={base}
      animate={{
        y: active ? -6 : 0,
        boxShadow: active
          ? "0 0 0 1px rgba(94,234,212,0.9), 0 30px 80px -20px rgba(94,234,212,0.35)"
          : tier.highlight
            ? "0 0 0 1px rgba(94,234,212,0.15), 0 20px 60px -20px rgba(94,234,212,0.35)"
            : "0 0 0 1px rgba(0,0,0,0)",
      }}
      transition={{ duration: 0.45, ease: [0.4, 0, 0.2, 1] }}
    >
      {tier.badge ? (
        <div className="absolute -top-3 left-1/2 -translate-x-1/2">
          <span className="bg-cc-accent text-cc-surface rounded-full px-3 py-1 font-mono text-[10px] font-semibold tracking-widest uppercase">
            {tier.badge}
          </span>
        </div>
      ) : null}

      <header>
        <h3 className="text-cc-heading font-heading text-h4">{tier.name}</h3>
        <div className="mt-3 flex items-baseline gap-2">
          <span className="text-cc-heading text-4xl font-semibold tracking-tight">
            {tier.price}
          </span>
          {tier.priceNote ? (
            <span className="text-cc-ink-dim text-sm">{tier.priceNote}</span>
          ) : null}
        </div>
        <p className="text-cc-ink mt-4 text-sm leading-relaxed">{tier.pitch}</p>
      </header>

      <dl className="border-cc-card-border mt-6 grid gap-3 border-y py-5 text-sm">
        <div>
          <dt className="text-cc-nav-label font-mono text-[10px] tracking-widest uppercase">
            Response
          </dt>
          <dd className="text-cc-ink mt-1">{tier.responseTime}</dd>
        </div>
        <div>
          <dt className="text-cc-nav-label font-mono text-[10px] tracking-widest uppercase">
            Best for
          </dt>
          <dd className="text-cc-ink mt-1">{tier.bestFor}</dd>
        </div>
      </dl>

      <ul className="mt-6 space-y-3 text-sm">
        {tier.features.map((feature) => (
          <li
            key={feature.label}
            className="text-cc-ink flex items-start gap-3"
          >
            <span
              className={
                tier.highlight ? "text-cc-accent mt-1" : "text-cc-success mt-1"
              }
            >
              <CheckIcon />
            </span>
            <span>{feature.label}</span>
          </li>
        ))}
      </ul>

      <div className="mt-8 pt-2">
        <Button href={tier.ctaHref} className="w-full">
          {tier.ctaLabel}
        </Button>
      </div>
    </motion.article>
  );
}

interface TimelineSegment {
  readonly label: string;
  readonly tier: string;
  readonly start: number; // 0..1
  readonly end: number; // 0..1
}

const TIMELINE: ReadonlyArray<TimelineSegment> = [
  { label: "Now", tier: "Community", start: 0.0, end: 0.28 },
  { label: "Hours to days", tier: "Consultancy", start: 0.28, end: 0.66 },
  { label: "Contractual SLA", tier: "Support", start: 0.66, end: 1.0 },
];

function ResponseTimeline() {
  const reduce = useReducedMotion();
  const ref = useRef<HTMLDivElement>(null);
  const inView = useInView(ref, { once: true, margin: "-10%" });
  const headingId = useId();

  return (
    <section aria-labelledby={headingId} ref={ref}>
      <div className="mx-auto max-w-2xl text-center">
        <div className="text-cc-nav-label mb-3 font-mono text-xs font-semibold tracking-widest uppercase">
          Response time
        </div>
        <h2 id={headingId} className="text-cc-heading font-heading text-h2">
          Pick by urgency, not by feature list.
        </h2>
        <p className="text-cc-ink-dim mt-4 text-base sm:text-lg">
          The three tiers cover three time horizons. Where your question sits on
          the timeline tells you where to start.
        </p>
      </div>

      <div className="border-cc-card-border bg-cc-card-bg mx-auto mt-12 max-w-4xl rounded-2xl border p-8 sm:p-10">
        <div className="relative h-2 w-full overflow-hidden rounded-full bg-[rgba(124,146,198,0.18)]">
          {TIMELINE.map((seg) => (
            <motion.div
              key={seg.label}
              className="absolute top-0 h-2 origin-left bg-[rgba(94,234,212,0.6)]"
              style={{
                left: `${seg.start * 100}%`,
                width: `${(seg.end - seg.start) * 100}%`,
              }}
              initial={reduce ? { scaleX: 1 } : { scaleX: 0 }}
              animate={inView ? { scaleX: 1 } : { scaleX: reduce ? 1 : 0 }}
              transition={{
                duration: reduce ? 0 : 0.9,
                delay: reduce ? 0 : 0.15 + TIMELINE.indexOf(seg) * 0.25,
                ease: [0.4, 0, 0.2, 1],
              }}
            />
          ))}
        </div>

        <div className="mt-6 grid grid-cols-1 gap-6 sm:grid-cols-3">
          {TIMELINE.map((seg) => (
            <div key={seg.label}>
              <div className="text-cc-nav-label font-mono text-[10px] tracking-widest uppercase">
                {seg.label}
              </div>
              <div className="text-cc-heading mt-1 text-sm font-semibold">
                {seg.tier}
              </div>
            </div>
          ))}
        </div>
      </div>
    </section>
  );
}

function SelfServeSection() {
  const reduce = useReducedMotion();

  return (
    <section aria-labelledby="self-serve-heading">
      <div className="mx-auto max-w-2xl text-center">
        <div className="text-cc-nav-label mb-3 font-mono text-xs font-semibold tracking-widest uppercase">
          First stop
        </div>
        <h2
          id="self-serve-heading"
          className="text-cc-heading font-heading text-h2"
        >
          Try self serve before you raise a hand.
        </h2>
        <p className="text-cc-ink-dim mt-4 text-base sm:text-lg">
          Most questions have already been answered. Five places to look before
          you book a session.
        </p>
      </div>

      <div className="mt-12 grid gap-4 sm:grid-cols-2 lg:grid-cols-3">
        {SELF_SERVE.map((channel, idx) => (
          <motion.div
            key={channel.title}
            initial={reduce ? { opacity: 1, y: 0 } : { opacity: 0, y: 16 }}
            whileInView={{ opacity: 1, y: 0 }}
            viewport={{ once: true, margin: "-10%" }}
            transition={{
              duration: reduce ? 0 : 0.5,
              delay: reduce ? 0 : idx * 0.08,
              ease: [0.4, 0, 0.2, 1],
            }}
          >
            <SelfServeCard channel={channel} />
          </motion.div>
        ))}
      </div>
    </section>
  );
}

function SelfServeCard({ channel }: { readonly channel: SelfServeChannel }) {
  const className =
    "group border-cc-card-border bg-cc-card-bg hover:border-cc-card-border-hover flex h-full flex-col rounded-xl border p-6 transition-colors";

  const content = (
    <>
      <div className="text-cc-accent border-cc-card-border bg-cc-surface/60 mb-4 inline-flex h-10 w-10 items-center justify-center rounded-lg border">
        {channel.icon}
      </div>
      <h3 className="text-cc-heading font-heading text-h6">{channel.title}</h3>
      <p className="text-cc-ink-dim mt-2 text-sm leading-relaxed">
        {channel.description}
      </p>
      <span className="text-cc-accent group-hover:text-cc-accent-hover mt-6 inline-flex items-center gap-2 text-sm font-medium">
        Open
        <ArrowGlyph />
      </span>
    </>
  );

  if (channel.external) {
    return (
      <a
        href={channel.href}
        target="_blank"
        rel="noopener noreferrer"
        className={className}
      >
        {content}
      </a>
    );
  }

  return (
    <NextLink href={channel.href} className={className}>
      {content}
    </NextLink>
  );
}

function FaqSection() {
  return (
    <section aria-labelledby="faq-heading">
      <div className="mx-auto max-w-2xl text-center">
        <div className="text-cc-nav-label mb-3 font-mono text-xs font-semibold tracking-widest uppercase">
          FAQ
        </div>
        <h2 id="faq-heading" className="text-cc-heading font-heading text-h2">
          Honest answers to the obvious questions.
        </h2>
      </div>

      <div className="border-cc-card-border bg-cc-card-bg divide-cc-card-border mx-auto mt-12 max-w-3xl divide-y overflow-hidden rounded-2xl border">
        {FAQ.map((item, index) => (
          <details
            key={item.question}
            className="group"
            open={index === 0 ? true : undefined}
          >
            <summary className="text-cc-heading hover:bg-cc-surface/40 flex cursor-pointer list-none items-center justify-between gap-6 px-6 py-5 text-base font-medium transition-colors sm:text-lg">
              <span>{item.question}</span>
              <span className="text-cc-ink-dim group-open:text-cc-accent shrink-0 transition-transform group-open:rotate-45">
                <PlusGlyph />
              </span>
            </summary>
            <div className="text-cc-ink-dim px-6 pb-6 text-sm leading-relaxed sm:text-base">
              {item.answer}
            </div>
          </details>
        ))}
      </div>
    </section>
  );
}

function ClosingCta() {
  const reduce = useReducedMotion();
  const ref = useRef<HTMLDivElement>(null);
  const inView = useInView(ref, { once: true, margin: "-15%" });

  return (
    <section
      ref={ref}
      className="border-cc-card-border bg-cc-card-bg relative overflow-hidden rounded-3xl border px-8 py-16 text-center sm:px-12 sm:py-20"
    >
      <div aria-hidden className="pointer-events-none absolute inset-0">
        <svg
          viewBox="0 0 1200 400"
          preserveAspectRatio="none"
          width="100%"
          height="100%"
        >
          <motion.path
            d="M -20 320 C 200 280, 360 80, 600 200 C 840 320, 1000 120, 1220 240"
            fill="none"
            stroke="#5eead4"
            strokeOpacity={0.45}
            strokeWidth={1}
            initial={reduce ? { pathLength: 1 } : { pathLength: 0 }}
            animate={inView ? { pathLength: 1 } : { pathLength: 0 }}
            transition={
              reduce
                ? { duration: 0 }
                : { duration: 2.4, ease: [0.4, 0, 0.2, 1] }
            }
            style={{
              filter: "drop-shadow(0 0 6px rgba(94,234,212,0.35))",
            }}
          />
        </svg>
      </div>
      <div className="relative">
        <h2 className="text-cc-heading font-heading text-h2 mx-auto max-w-2xl">
          Still not sure where to start?
        </h2>
        <p className="text-cc-ink-dim mx-auto mt-4 max-w-xl text-base sm:text-lg">
          Book a consultancy hour, bring whatever you have, and walk out with a
          direction. If we are not the right help, we will say so.
        </p>
        <div className="mt-8 flex flex-wrap justify-center gap-3">
          <SolidButton href="https://calendly.com/chillicream/60min">
            Book a consultancy hour
          </SolidButton>
          <OutlineButton href="/services/support">
            Explore support plans
          </OutlineButton>
        </div>
      </div>
    </section>
  );
}

function PlusGlyph() {
  return (
    <svg
      viewBox="0 0 16 16"
      width="16"
      height="16"
      aria-hidden
      fill="none"
      stroke="currentColor"
      strokeWidth="1.75"
      strokeLinecap="round"
    >
      <path d="M3 8h10" />
      <path d="M8 3v10" />
    </svg>
  );
}

function ArrowGlyph() {
  return (
    <svg
      viewBox="0 0 16 16"
      width="14"
      height="14"
      aria-hidden
      fill="none"
      stroke="currentColor"
      strokeWidth="1.75"
      strokeLinecap="round"
      strokeLinejoin="round"
    >
      <path d="M3 8h10" />
      <path d="M9 4l4 4-4 4" />
    </svg>
  );
}

function ArrowDownGlyph() {
  return (
    <svg
      viewBox="0 0 16 16"
      width="14"
      height="14"
      aria-hidden
      fill="none"
      stroke="currentColor"
      strokeWidth="1.75"
      strokeLinecap="round"
      strokeLinejoin="round"
    >
      <path d="M8 3v10" />
      <path d="M4 9l4 4 4-4" />
    </svg>
  );
}

function PauseGlyph() {
  return (
    <svg
      viewBox="0 0 16 16"
      width="12"
      height="12"
      aria-hidden
      fill="currentColor"
    >
      <rect x="4" y="3" width="2.5" height="10" rx="0.75" />
      <rect x="9.5" y="3" width="2.5" height="10" rx="0.75" />
    </svg>
  );
}

function PlayMiniGlyph() {
  return (
    <svg
      viewBox="0 0 16 16"
      width="12"
      height="12"
      aria-hidden
      fill="currentColor"
    >
      <path d="M5 3.5v9l8-4.5z" />
    </svg>
  );
}

function DocsGlyph() {
  return (
    <svg
      viewBox="0 0 24 24"
      width="20"
      height="20"
      aria-hidden
      fill="none"
      stroke="currentColor"
      strokeWidth="1.6"
      strokeLinecap="round"
      strokeLinejoin="round"
    >
      <path d="M6 3h8l4 4v14H6z" />
      <path d="M14 3v4h4" />
      <path d="M9 12h6" />
      <path d="M9 16h6" />
    </svg>
  );
}

function BlogGlyph() {
  return (
    <svg
      viewBox="0 0 24 24"
      width="20"
      height="20"
      aria-hidden
      fill="none"
      stroke="currentColor"
      strokeWidth="1.6"
      strokeLinecap="round"
      strokeLinejoin="round"
    >
      <rect x="4" y="4" width="16" height="16" rx="2" />
      <path d="M8 9h8" />
      <path d="M8 13h8" />
      <path d="M8 17h5" />
    </svg>
  );
}

function SlackGlyph() {
  return (
    <svg
      viewBox="0 0 24 24"
      width="20"
      height="20"
      aria-hidden
      fill="none"
      stroke="currentColor"
      strokeWidth="1.6"
      strokeLinecap="round"
      strokeLinejoin="round"
    >
      <rect x="3" y="10" width="8" height="4" rx="2" />
      <rect x="13" y="10" width="8" height="4" rx="2" />
      <rect x="10" y="3" width="4" height="8" rx="2" />
      <rect x="10" y="13" width="4" height="8" rx="2" />
    </svg>
  );
}

function PlayGlyph() {
  return (
    <svg
      viewBox="0 0 24 24"
      width="20"
      height="20"
      aria-hidden
      fill="none"
      stroke="currentColor"
      strokeWidth="1.6"
      strokeLinecap="round"
      strokeLinejoin="round"
    >
      <rect x="3" y="5" width="18" height="14" rx="3" />
      <path d="M11 9.5v5l4-2.5z" fill="currentColor" stroke="none" />
    </svg>
  );
}

function GitHubGlyph() {
  return (
    <svg
      viewBox="0 0 24 24"
      width="20"
      height="20"
      aria-hidden
      fill="none"
      stroke="currentColor"
      strokeWidth="1.6"
      strokeLinecap="round"
      strokeLinejoin="round"
    >
      <path d="M9 19c-4 1.2-4-2-6-2" />
      <path d="M15 21v-3.2c0-.9-.1-1.3-.6-1.8 2.8-.3 5.6-1.4 5.6-6.1a4.7 4.7 0 0 0-1.3-3.3 4.3 4.3 0 0 0-.1-3.2s-1.1-.3-3.6 1.3a12.4 12.4 0 0 0-6.4 0C5.1 1.1 4 1.4 4 1.4a4.3 4.3 0 0 0-.1 3.2A4.7 4.7 0 0 0 2.6 7.9c0 4.6 2.8 5.7 5.5 6.1-.4.4-.6.9-.6 1.6V21" />
    </svg>
  );
}

import type { Metadata } from "next";
import type { ComponentType, ReactNode } from "react";

import { CheckIcon } from "@/src/components/CheckIcon";
import { OutlineButton, SolidButton } from "@/src/design-system/Button";

export const metadata: Metadata = {
  title: "GraphQL Advisory: Outcome-led Engagements",
  description:
    "Ship the GraphQL change you have been postponing: federation rollouts, schema cleanup, performance audits, and .NET migration with ChilliCream experts.",
  keywords: [
    "GraphQL advisory",
    "GraphQL consulting",
    "Hot Chocolate",
    "Fusion federation",
    "GraphQL performance",
    "schema cleanup",
    ".NET GraphQL",
  ],
  openGraph: {
    title: "GraphQL Advisory: Outcome-led Engagements",
    description:
      "Ship the GraphQL change you have been postponing: federation rollouts, schema cleanup, performance audits, and .NET migration with ChilliCream experts.",
  },
  robots: { index: false, follow: false },
};

const BOOKING_URL = "https://calendly.com/chillicream/60min";
const CONSULTING_MAILTO = "mailto:contact@chillicream.com?subject=Consulting";
const CONTRACTING_MAILTO = "mailto:contact@chillicream.com?subject=Contracting";

export default function AdvisoryOutcomePatternsPage() {
  return (
    <>
      <Hero />
      <Patterns />
      <HowWeEngage />
      <WhatGoodLooksLike />
      <Faq />
      <ClosingCta />
    </>
  );
}

function Hero() {
  return (
    <section className="relative pt-16 pb-12 sm:pt-24 sm:pb-16">
      <div className="text-cc-nav-label mb-5 font-mono text-xs font-semibold tracking-[0.2em] uppercase">
        Advisory / Outcome patterns
      </div>
      <h1 className="font-heading text-cc-heading max-w-4xl text-4xl leading-[1.05] font-semibold tracking-tight sm:text-5xl lg:text-6xl">
        Ship the GraphQL change you have been{" "}
        <span className="text-cc-accent">postponing</span>.
      </h1>
      <p className="text-cc-prose mt-7 max-w-2xl text-base leading-relaxed sm:text-lg">
        Most teams already know which migration, refactor, or rollout is
        overdue. We pair you with a ChilliCream expert who has done it before,
        scope it honestly, and move it forward. Hourly when you need a sounding
        board, per project when you need it built.
      </p>
      <div className="mt-9 flex flex-wrap items-center gap-3">
        <SolidButton href={BOOKING_URL}>Book a 60 minute call</SolidButton>
        <OutlineButton href="#patterns">See engagement patterns</OutlineButton>
      </div>
      <div className="text-cc-ink-dim mt-7 flex flex-wrap items-center gap-x-6 gap-y-2 font-mono text-xs">
        <span>$300 / hour, paid by the hour</span>
        <span className="text-cc-ink-faint">|</span>
        <span>NDA on request</span>
        <span className="text-cc-ink-faint">|</span>
        <span>Remote, async friendly</span>
      </div>
    </section>
  );
}

interface Pattern {
  readonly id: string;
  readonly tag: string;
  readonly title: string;
  readonly signal: string;
  readonly summary: string;
  readonly motions: readonly string[];
  readonly Glyph: ComponentType<{ readonly className?: string }>;
  readonly tone: "cyan" | "violet" | "coral" | "accent";
}

const PATTERNS: readonly Pattern[] = [
  {
    id: "federation",
    tag: "Pattern 01",
    title: "Federation rollout",
    signal:
      "Multiple services, one client surface, and a sprint that keeps growing.",
    summary:
      "You have several backends and a frontend that should not care which one owns a field. We work through the composition story, ownership boundaries, and the migration path that gets the first subgraph live without freezing the rest of the org.",
    motions: [
      "Subgraph boundaries and ownership",
      "Schema composition and contract checks",
      "Gateway rollout and traffic cutover",
    ],
    Glyph: FederationGlyph,
    tone: "cyan",
  },
  {
    id: "schema-cleanup",
    tag: "Pattern 02",
    title: "Schema cleanup and breaking-change strategy",
    signal:
      "The schema grew with the product. Field deprecations stalled. Nobody wants to ship the breaking PR.",
    summary:
      "We audit the schema against how it is actually used, surface the deprecations that are safe to retire, and put a breaking-change policy in place that your clients can follow. The goal is a schema you trust, not a rewrite.",
    motions: [
      "Usage based deprecation audit",
      "Naming, nullability, and pagination review",
      "Versioning policy and client comms plan",
    ],
    Glyph: CleanupGlyph,
    tone: "accent",
  },
  {
    id: "performance",
    tag: "Pattern 03",
    title: "Performance and N+1 audit",
    signal:
      "P95 climbs at peak. A familiar query lights up the database log. Caching has stopped paying off.",
    summary:
      "We trace the slow paths end to end, identify which resolvers are quietly fanning out, and put DataLoader, projections, and persisted operations to work where they earn their keep. Outcome is measured against your own baseline, not a brochure number.",
    motions: [
      "Trace driven hotspot analysis",
      "DataLoader and projection coverage",
      "Persisted operations and query allowlisting",
    ],
    Glyph: PerformanceGlyph,
    tone: "coral",
  },
  {
    id: "dotnet-migration",
    tag: "Pattern 04",
    title: ".NET native migration",
    signal:
      "The team is .NET first. The GraphQL layer is on another stack and the bridge is the slowest part of the stack.",
    summary:
      "We map the existing schema onto Hot Chocolate, plan the surface area that has to stay stable, and stage the cutover so published clients keep working. Where the old stack had a feature with no direct equivalent, we name it early and design the substitute.",
    motions: [
      "Schema and resolver parity inventory",
      "Auth, subscriptions, and middleware mapping",
      "Staged cutover with parallel run",
    ],
    Glyph: MigrationGlyph,
    tone: "violet",
  },
];

function Patterns() {
  return (
    <section id="patterns" className="py-16 sm:py-20">
      <SectionHeader
        eyebrow="Four ways we usually start"
        title="Outcome patterns"
        intro="Most advisory engagements land in one of these shapes. They are not packages, they are starting points. We adapt to what your team actually needs."
      />
      <ol className="mt-12 space-y-6">
        {PATTERNS.map((pattern, index) => (
          <PatternRow
            key={pattern.id}
            pattern={pattern}
            flipped={index % 2 === 1}
          />
        ))}
      </ol>
    </section>
  );
}

interface PatternRowProps {
  readonly pattern: Pattern;
  readonly flipped: boolean;
}

function PatternRow({ pattern, flipped }: PatternRowProps) {
  const { tag, title, signal, summary, motions, Glyph } = pattern;
  return (
    <li
      id={pattern.id}
      className="border-cc-card-border bg-cc-card-bg/60 hover:border-cc-card-border-hover relative overflow-hidden rounded-3xl border transition-colors"
    >
      <div
        className={`grid gap-0 md:grid-cols-[1fr_18rem] ${
          flipped ? "md:[direction:rtl]" : ""
        }`}
      >
        <div className="p-7 sm:p-9 md:[direction:ltr]">
          <div className="text-cc-nav-label flex items-center gap-3 font-mono text-xs tracking-[0.18em] uppercase">
            <span>{tag}</span>
            <span className="bg-cc-ink-faint h-px w-12" aria-hidden="true" />
          </div>
          <h3 className="font-heading text-cc-heading mt-4 text-2xl font-semibold sm:text-3xl">
            {title}
          </h3>
          <p className="text-cc-accent mt-3 text-sm leading-relaxed">
            {signal}
          </p>
          <p className="text-cc-prose mt-4 max-w-2xl text-sm leading-relaxed sm:text-base">
            {summary}
          </p>
          <ul className="mt-6 grid gap-2 sm:grid-cols-2">
            {motions.map((motion) => (
              <li
                key={motion}
                className="text-cc-ink flex items-start gap-2 text-sm"
              >
                <span className="text-cc-accent mt-1 flex-none">
                  <CheckIcon />
                </span>
                <span>{motion}</span>
              </li>
            ))}
          </ul>
        </div>
        <div
          className={`border-cc-card-border relative flex items-center justify-center border-t bg-[radial-gradient(circle_at_30%_20%,rgba(94,234,212,0.08),transparent_60%)] p-8 md:border-t-0 md:[direction:ltr] ${
            flipped ? "md:border-r" : "md:border-l"
          }`}
        >
          <Glyph className="h-32 w-32 sm:h-40 sm:w-40" />
        </div>
      </div>
    </li>
  );
}

function HowWeEngage() {
  return (
    <section id="engage" className="py-16 sm:py-20">
      <SectionHeader
        eyebrow="How we engage"
        title="Two tiers. No surprise scope."
        intro="Start hourly to scope and de-risk. If the work is large enough to need a dedicated team, we move to a per-project contract with a written statement of work."
      />
      <div className="mt-12 grid gap-6 md:grid-cols-2">
        <TierCard
          eyebrow="Tier 01"
          title="Consulting"
          billing="Hourly, $300 per hour"
          description="Hourly consulting services to get the help you need at any stage of your project. This is the best way to get started."
          perks={[
            "Mentoring and guidance",
            "Architecture",
            "Troubleshooting",
            "Code Review",
            "Best practices education",
          ]}
          ctaLabel="Start with Consulting"
          ctaHref={CONSULTING_MAILTO}
          accent={false}
        />
        <TierCard
          eyebrow="Tier 02"
          title="Contracting"
          billing="Per project, written SoW"
          description="Options for teams who do not have the time, bandwidth, and/or expertise to implement their own GraphQL solutions."
          perks={["Proof of concept", "Implementation"]}
          ctaLabel="Talk about Contracting"
          ctaHref={CONTRACTING_MAILTO}
          accent
        />
      </div>
      <p className="text-cc-ink-dim mt-8 text-center text-sm">
        Not sure which fits? Book a one hour call and we will tell you.{" "}
        <a
          href={BOOKING_URL}
          target="_blank"
          rel="noopener noreferrer"
          className="text-cc-accent hover:text-cc-accent-hover underline-offset-4 hover:underline"
        >
          Book a 60 minute call
        </a>
        .
      </p>
    </section>
  );
}

interface TierCardProps {
  readonly eyebrow: string;
  readonly title: string;
  readonly billing: string;
  readonly description: string;
  readonly perks: readonly string[];
  readonly ctaLabel: string;
  readonly ctaHref: string;
  readonly accent: boolean;
}

function TierCard({
  eyebrow,
  title,
  billing,
  description,
  perks,
  ctaLabel,
  ctaHref,
  accent,
}: TierCardProps) {
  const Cta = accent ? SolidButton : OutlineButton;
  return (
    <div
      className={`flex h-full flex-col rounded-3xl border p-7 sm:p-8 ${
        accent
          ? "border-cc-accent/70 bg-cc-card-bg"
          : "border-cc-card-border bg-cc-card-bg/60"
      }`}
    >
      <div className="text-cc-nav-label font-mono text-xs tracking-[0.18em] uppercase">
        {eyebrow}
      </div>
      <h3 className="font-heading text-cc-heading mt-3 text-2xl font-semibold sm:text-3xl">
        {title}
      </h3>
      <p className="text-cc-accent mt-1 font-mono text-xs tracking-wide">
        {billing}
      </p>
      <p className="text-cc-prose mt-4 text-sm leading-relaxed">
        {description}
      </p>
      <ul className="mt-6 flex flex-1 flex-col gap-3">
        {perks.map((perk) => (
          <li key={perk} className="flex items-start gap-3">
            <span className="text-cc-accent mt-1 flex-none">
              <CheckIcon />
            </span>
            <span className="text-cc-ink text-sm">{perk}</span>
          </li>
        ))}
      </ul>
      <Cta href={ctaHref} className="mt-8 w-full">
        {ctaLabel}
      </Cta>
    </div>
  );
}

function WhatGoodLooksLike() {
  const items: ReadonlyArray<{
    readonly step: string;
    readonly title: string;
    readonly body: ReactNode;
  }> = [
    {
      step: "01",
      title: "A 60 minute scoping call",
      body: "We listen, ask the awkward questions, and end with a written summary of what is in scope, what is not, and how we would start. No deck, no sales rep.",
    },
    {
      step: "02",
      title: "Engineers you can name",
      body: "You get a ChilliCream engineer who has shipped this kind of work, not a junior with a checklist. The same person stays with you for the engagement.",
    },
    {
      step: "03",
      title: "Short feedback loops",
      body: "We work in your repo, your tracker, and your chat. Updates are written, decisions are recorded, and you can pause or stop the clock when you need to.",
    },
    {
      step: "04",
      title: "An honest handover",
      body: "We leave behind code your team understands and a short doc that captures the why behind the choices. If a follow up belongs on your team, we will say so.",
    },
  ];

  return (
    <section id="what-good-looks-like" className="py-16 sm:py-20">
      <SectionHeader
        eyebrow="What good looks like"
        title="The engagement model, in plain language"
        intro="Advisory work goes sideways when expectations are fuzzy. Here is the shape every engagement takes, regardless of which tier you pick."
      />
      <ol className="mt-12 grid gap-5 sm:grid-cols-2">
        {items.map((item) => (
          <li
            key={item.step}
            className="border-cc-card-border bg-cc-card-bg/60 hover:border-cc-card-border-hover relative flex gap-5 rounded-2xl border p-6 transition-colors sm:p-7"
          >
            <div
              aria-hidden="true"
              className="font-heading text-cc-ink-faint shrink-0 text-4xl leading-none font-semibold sm:text-5xl"
            >
              {item.step}
            </div>
            <div>
              <h3 className="font-heading text-cc-heading text-lg font-semibold sm:text-xl">
                {item.title}
              </h3>
              <p className="text-cc-prose mt-2 text-sm leading-relaxed">
                {item.body}
              </p>
            </div>
          </li>
        ))}
      </ol>
    </section>
  );
}

function Faq() {
  const faqs: ReadonlyArray<{ readonly q: string; readonly a: ReactNode }> = [
    {
      q: "What is the hourly rate?",
      a: (
        <>
          Consulting is $300 per hour. The first 60 minute call is the easiest
          way to get started; you can{" "}
          <a
            href={BOOKING_URL}
            target="_blank"
            rel="noopener noreferrer"
            className="text-cc-accent hover:text-cc-accent-hover underline-offset-4 hover:underline"
          >
            book it directly
          </a>
          .
        </>
      ),
    },
    {
      q: "Can you sign an NDA before we share details?",
      a: "Yes. Send us your standard mutual NDA and we will sign it before the call. If you do not have one, we can send ours.",
    },
    {
      q: "How small or large can the scope be?",
      a: "Smallest sensible scope is a one hour call to unblock a single decision. Largest is a multi month contracting engagement with a written statement of work. Anything in between is fine; we will tell you which tier fits.",
    },
    {
      q: "How quickly can you start?",
      a: "Reach out and we will reply with the next available slot for an introductory call. Contracting engagements need a written scope first and then start as soon as both sides sign.",
    },
    {
      q: "What outcomes do you promise?",
      a: "We promise a named engineer, written summaries after every working session, and that we will tell you the moment we believe the scope no longer fits the budget. We do not promise specific performance percentages without measuring your system first.",
    },
    {
      q: "Do you work with non .NET stacks?",
      a: "Our deepest expertise is Hot Chocolate, Fusion, and the broader .NET ecosystem. We are happy to advise on schema design, federation, and client patterns regardless of the server language; if the work is server side and not .NET, we will say so up front.",
    },
  ];

  return (
    <section id="faq" className="py-16 sm:py-20">
      <SectionHeader
        eyebrow="FAQ"
        title="Honest answers, before you ask"
        intro="If your question is not here, ask it on the scoping call. We will not dodge it."
      />
      <dl className="border-cc-card-border bg-cc-card-bg/40 mt-12 divide-y divide-[color:var(--color-cc-card-border)] overflow-hidden rounded-2xl border">
        {faqs.map((faq) => (
          <div
            key={faq.q}
            className="grid gap-2 p-6 sm:grid-cols-[1fr_2fr] sm:gap-6 sm:p-7"
          >
            <dt className="font-heading text-cc-heading text-base font-semibold sm:text-lg">
              {faq.q}
            </dt>
            <dd className="text-cc-prose text-sm leading-relaxed sm:text-base">
              {faq.a}
            </dd>
          </div>
        ))}
      </dl>
    </section>
  );
}

function ClosingCta() {
  return (
    <section className="py-16 sm:py-20">
      <div className="border-cc-accent/40 relative overflow-hidden rounded-3xl border bg-[linear-gradient(135deg,rgba(22,185,228,0.10),rgba(124,146,198,0.08)_55%,rgba(240,120,106,0.08))] p-8 sm:p-12">
        <div className="text-cc-nav-label font-mono text-xs tracking-[0.2em] uppercase">
          Next step
        </div>
        <h2 className="font-heading text-cc-heading mt-3 max-w-3xl text-3xl font-semibold sm:text-4xl">
          Tell us which change you have been postponing.
        </h2>
        <p className="text-cc-prose mt-4 max-w-2xl text-sm leading-relaxed sm:text-base">
          One sentence is enough. We read it, decide whether we are the right
          team, and reply with a scoping call slot or a recommendation that does
          not involve us. Either way you walk away with a clearer next step.
        </p>
        <div className="mt-7 flex flex-wrap gap-3">
          <SolidButton href={BOOKING_URL}>Book a 60 minute call</SolidButton>
          <OutlineButton href={CONSULTING_MAILTO}>
            Email contact@chillicream.com
          </OutlineButton>
        </div>
      </div>
    </section>
  );
}

interface SectionHeaderProps {
  readonly eyebrow: string;
  readonly title: string;
  readonly intro: string;
}

function SectionHeader({ eyebrow, title, intro }: SectionHeaderProps) {
  return (
    <header className="max-w-3xl">
      <div className="text-cc-nav-label font-mono text-xs font-semibold tracking-[0.2em] uppercase">
        {eyebrow}
      </div>
      <h2 className="font-heading text-cc-heading mt-3 text-3xl font-semibold sm:text-4xl">
        {title}
      </h2>
      <p className="text-cc-prose mt-4 text-sm leading-relaxed sm:text-base">
        {intro}
      </p>
    </header>
  );
}

interface GlyphProps {
  readonly className?: string;
}

function FederationGlyph({ className }: GlyphProps) {
  return (
    <svg
      viewBox="0 0 160 160"
      className={className}
      role="img"
      aria-label="Federation rollout diagram"
    >
      <defs>
        <linearGradient id="adv-fed-grad" x1="0" x2="1" y1="0" y2="1">
          <stop offset="0%" stopColor="#16b9e4" stopOpacity="0.9" />
          <stop offset="100%" stopColor="#5eead4" stopOpacity="0.7" />
        </linearGradient>
      </defs>
      <circle
        cx="80"
        cy="80"
        r="22"
        fill="none"
        stroke="url(#adv-fed-grad)"
        strokeWidth="1.5"
      />
      <circle cx="80" cy="80" r="6" fill="#5eead4" />
      {[
        [80, 22],
        [138, 80],
        [80, 138],
        [22, 80],
      ].map(([cx, cy]) => (
        <g key={`${cx}-${cy}`}>
          <line
            x1="80"
            y1="80"
            x2={cx}
            y2={cy}
            stroke="rgba(245,241,234,0.25)"
            strokeWidth="1"
            strokeDasharray="3 4"
          />
          <circle
            cx={cx}
            cy={cy}
            r="10"
            fill="rgba(12,19,34,0.9)"
            stroke="#16b9e4"
            strokeWidth="1.2"
          />
        </g>
      ))}
    </svg>
  );
}

function CleanupGlyph({ className }: GlyphProps) {
  return (
    <svg
      viewBox="0 0 160 160"
      className={className}
      role="img"
      aria-label="Schema cleanup diagram"
    >
      <rect
        x="22"
        y="28"
        width="116"
        height="104"
        rx="10"
        fill="none"
        stroke="rgba(245,241,234,0.18)"
        strokeWidth="1"
      />
      {[44, 60, 76, 92, 108].map((y, index) => (
        <g key={y}>
          <rect
            x="34"
            y={y}
            width={[80, 64, 88, 52, 72][index]}
            height="6"
            rx="3"
            fill={
              index === 1 || index === 3 ? "rgba(245,241,234,0.22)" : "#5eead4"
            }
            opacity={index === 1 || index === 3 ? 0.5 : 0.85}
          />
          {(index === 1 || index === 3) && (
            <line
              x1="34"
              y1={y + 3}
              x2={34 + [80, 64, 88, 52, 72][index]}
              y2={y + 3}
              stroke="#f0786a"
              strokeWidth="1.2"
            />
          )}
        </g>
      ))}
    </svg>
  );
}

function PerformanceGlyph({ className }: GlyphProps) {
  return (
    <svg
      viewBox="0 0 160 160"
      className={className}
      role="img"
      aria-label="Performance audit chart"
    >
      <defs>
        <linearGradient id="adv-perf-grad" x1="0" x2="0" y1="0" y2="1">
          <stop offset="0%" stopColor="#f0786a" stopOpacity="0.6" />
          <stop offset="100%" stopColor="#f0786a" stopOpacity="0" />
        </linearGradient>
      </defs>
      <line
        x1="22"
        y1="130"
        x2="138"
        y2="130"
        stroke="rgba(245,241,234,0.2)"
        strokeWidth="1"
      />
      <line
        x1="22"
        y1="30"
        x2="22"
        y2="130"
        stroke="rgba(245,241,234,0.2)"
        strokeWidth="1"
      />
      <path
        d="M22 110 L42 96 L58 102 L74 70 L92 84 L108 50 L124 64 L138 38"
        fill="none"
        stroke="#f0786a"
        strokeWidth="1.8"
        strokeLinejoin="round"
      />
      <path
        d="M22 110 L42 96 L58 102 L74 70 L92 84 L108 50 L124 64 L138 38 L138 130 L22 130 Z"
        fill="url(#adv-perf-grad)"
      />
      <path
        d="M22 118 L42 112 L58 116 L74 108 L92 112 L108 104 L124 108 L138 100"
        fill="none"
        stroke="#5eead4"
        strokeWidth="1.6"
        strokeLinejoin="round"
        strokeDasharray="4 3"
      />
    </svg>
  );
}

function MigrationGlyph({ className }: GlyphProps) {
  return (
    <svg
      viewBox="0 0 160 160"
      className={className}
      role="img"
      aria-label=".NET migration diagram"
    >
      <rect
        x="14"
        y="50"
        width="48"
        height="60"
        rx="8"
        fill="rgba(12,19,34,0.9)"
        stroke="rgba(245,241,234,0.22)"
        strokeWidth="1"
      />
      <rect
        x="98"
        y="50"
        width="48"
        height="60"
        rx="8"
        fill="rgba(12,19,34,0.9)"
        stroke="#7c92c6"
        strokeWidth="1.4"
      />
      <text
        x="38"
        y="84"
        textAnchor="middle"
        fontFamily="ui-monospace, SFMono-Regular, Menlo, monospace"
        fontSize="11"
        fill="rgba(245,241,234,0.55)"
      >
        old
      </text>
      <text
        x="122"
        y="84"
        textAnchor="middle"
        fontFamily="ui-monospace, SFMono-Regular, Menlo, monospace"
        fontSize="11"
        fill="#7c92c6"
      >
        .NET
      </text>
      <line
        x1="62"
        y1="80"
        x2="98"
        y2="80"
        stroke="#5eead4"
        strokeWidth="1.5"
        strokeDasharray="3 3"
      />
      <polygon points="98,80 92,76 92,84" fill="#5eead4" />
      <circle cx="80" cy="80" r="3" fill="#5eead4" />
    </svg>
  );
}

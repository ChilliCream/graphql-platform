"use client";

import { motion, useReducedMotion } from "motion/react";
import type { ReactNode } from "react";
import { useState } from "react";

import { CheckIcon } from "@/src/components/CheckIcon";
import { OutlineButton, SolidButton } from "@/src/design-system/Button";

// ---------------------------------------------------------------------------
// Canonical data, reused verbatim from the v1 ground truth.
// ---------------------------------------------------------------------------

type PlanName = "Community" | "Startup" | "Business" | "Enterprise";

interface Plan {
  readonly name: PlanName;
  readonly price: string;
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

const PLAN_NAMES: readonly PlanName[] = [
  "Community",
  "Startup",
  "Business",
  "Enterprise",
];

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
      {
        title: "Public Slack Channel",
        values: [true, true, true, true],
      },
      {
        title: "Private Slack Channel",
        values: [false, true, true, true],
      },
      {
        title: "Private Issue Tracking Board",
        values: [false, false, true, true],
      },
      {
        title: "Email Support",
        values: [false, false, true, true],
      },
      {
        title: "Phone Support",
        values: [false, false, false, true],
      },
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

// Tier numerals stamped on each plan, repeating the spine motif.
const PLAN_NUMERALS: Record<PlanName, string> = {
  Community: "01",
  Startup: "02",
  Business: "03",
  Enterprise: "04",
};

// ---------------------------------------------------------------------------
// Page
// ---------------------------------------------------------------------------

export function ClientPage() {
  return (
    <div className="relative">
      <TickRail />
      <div className="relative mx-auto max-w-6xl px-4 sm:px-6">
        <Hero />
        <Coverage />
        <Plans />
        <Compare />
        <Faq />
        <Enterprise />
        <Closing />
      </div>
    </div>
  );
}

// ---------------------------------------------------------------------------
// The page-wide vertical tick rail running down the left edge at md+.
// A 1px hairline with short horizontal ticks at each section baseline.
// ---------------------------------------------------------------------------

function TickRail() {
  // Tick positions as a fraction of the rail height, one per numbered rung.
  const ticks = [0.06, 0.22, 0.4, 0.58, 0.72, 0.88];
  return (
    <svg
      className="text-cc-card-border pointer-events-none absolute top-0 left-[max(1rem,calc((100%-72rem)/2+1.5rem))] hidden h-full w-6 md:block"
      viewBox="0 0 24 1000"
      preserveAspectRatio="none"
      aria-hidden
    >
      <line
        x1="1"
        y1="0"
        x2="1"
        y2="1000"
        stroke="currentColor"
        strokeWidth="1"
        vectorEffect="non-scaling-stroke"
      />
      {ticks.map((t) => (
        <line
          key={t}
          x1="1"
          y1={t * 1000}
          x2="14"
          y2={t * 1000}
          stroke="currentColor"
          strokeWidth="1"
          vectorEffect="non-scaling-stroke"
        />
      ))}
    </svg>
  );
}

// ---------------------------------------------------------------------------
// A colossal outlined numeral, the architectural marker of every section.
// Outlined via -webkit-text-stroke with a transparent fill.
// ---------------------------------------------------------------------------

function GiantNumeral({
  value,
  className = "",
}: {
  readonly value: string;
  readonly className?: string;
}) {
  const reduce = useReducedMotion();
  return (
    <motion.div
      className={`font-heading pointer-events-none leading-none font-semibold select-none ${className}`}
      style={{
        fontSize: "clamp(8rem, 18vw, 16rem)",
        color: "transparent",
        WebkitTextStroke: "2px var(--color-cc-accent)",
      }}
      initial={reduce ? false : { opacity: 0, y: 12 }}
      whileInView={{ opacity: 1, y: 0 }}
      viewport={{ once: true, margin: "-15%" }}
      transition={{ duration: 0.5, ease: "easeOut" }}
      aria-hidden
    >
      {value}
    </motion.div>
  );
}

// A chapter eyebrow in mono, doubling as a numbered story label.
function Chapter({ children }: { readonly children: ReactNode }) {
  return (
    <div className="text-cc-nav-label font-mono text-xs font-semibold tracking-widest uppercase">
      {children}
    </div>
  );
}

// ---------------------------------------------------------------------------
// 00 Hero
// ---------------------------------------------------------------------------

function Hero() {
  const reduce = useReducedMotion();
  return (
    <section className="pt-20 pb-16 sm:pt-28 md:pl-16">
      <motion.div
        className="font-heading leading-none font-semibold select-none"
        style={{
          fontSize: "clamp(7rem, 16vw, 14rem)",
          color: "transparent",
          WebkitTextStroke: "2px var(--color-cc-accent)",
        }}
        initial={reduce ? false : { opacity: 0, y: 12 }}
        whileInView={{ opacity: 1, y: 0 }}
        viewport={{ once: true }}
        transition={{ duration: 0.5, ease: "easeOut" }}
        aria-hidden
      >
        00
      </motion.div>

      {/* Brand spectrum appears exactly once, a thin band that sets the story going. */}
      <SpectrumBand />

      <div className="mt-8 max-w-3xl">
        <Chapter>Support / Chapter 00</Chapter>
        <h1 className="font-heading text-cc-heading mt-5 text-5xl leading-tight font-semibold tracking-tight sm:text-6xl lg:text-7xl">
          GraphQL support plans, numbered for the way you ship.
        </h1>
        <p className="text-cc-prose lead mt-6 max-w-2xl text-lg sm:text-xl">
          Pick the rung that matches how your team ships Hot Chocolate, Fusion,
          and Nitro. Clear response times, named channels, and engineers who
          wrote the code.
        </p>
        <div className="mt-10 flex flex-col gap-3 sm:flex-row sm:gap-4">
          <SolidButton href="#plans">Compare plans</SolidButton>
          <OutlineButton href="/services/support/contact">
            Talk to us
          </OutlineButton>
        </div>
        <ul className="text-cc-ink-dim mt-10 flex flex-wrap items-center gap-x-6 gap-y-2 text-sm">
          <HeroFact>Engineers, not a queue</HeroFact>
          <HeroFact>Private Slack on every paid plan</HeroFact>
          <HeroFact>Written SLAs from Business up</HeroFact>
        </ul>
      </div>
    </section>
  );
}

function SpectrumBand() {
  return (
    <svg
      className="mt-2 h-[2px] w-40 max-w-full"
      viewBox="0 0 160 2"
      preserveAspectRatio="none"
      aria-hidden
    >
      <defs>
        <linearGradient id="cc-support-v8-spectrum" x1="0" y1="0" x2="1" y2="0">
          <stop offset="0%" stopColor="#16b9e4" />
          <stop offset="50%" stopColor="#7c92c6" />
          <stop offset="100%" stopColor="#f0786a" />
        </linearGradient>
      </defs>
      <rect
        x="0"
        y="0"
        width="160"
        height="2"
        fill="url(#cc-support-v8-spectrum)"
      />
    </svg>
  );
}

function HeroFact({ children }: { readonly children: ReactNode }) {
  return (
    <li className="inline-flex items-center gap-2">
      <span className="text-cc-accent inline-flex" aria-hidden>
        <CheckIcon />
      </span>
      <span>{children}</span>
    </li>
  );
}

// ---------------------------------------------------------------------------
// A consistent two-zone row: numeral gutter on the left, content on the right.
// On mobile the numeral stacks above the heading at a reduced size.
// ---------------------------------------------------------------------------

function NumberedSection({
  numeral,
  chapter,
  heading,
  intro,
  children,
  id,
}: {
  readonly numeral: string;
  readonly chapter: ReactNode;
  readonly heading: string;
  readonly intro?: string;
  readonly children: ReactNode;
  readonly id?: string;
}) {
  return (
    <section id={id} className="py-16 md:pl-16">
      <div className="grid gap-x-10 gap-y-6 md:grid-cols-[minmax(0,16rem)_1fr] md:items-start">
        <div className="md:pt-2">
          <GiantNumeral value={numeral} className="text-7xl sm:text-8xl" />
        </div>
        <div>
          <Chapter>{chapter}</Chapter>
          <h2 className="font-heading text-cc-heading mt-4 text-3xl font-semibold tracking-tight sm:text-4xl">
            {heading}
          </h2>
          {intro && (
            <p className="text-cc-ink-dim mt-3 max-w-2xl text-base sm:text-lg">
              {intro}
            </p>
          )}
          <div className="mt-8">{children}</div>
        </div>
      </div>
    </section>
  );
}

// ---------------------------------------------------------------------------
// 01 Coverage
// ---------------------------------------------------------------------------

interface Pillar {
  readonly numeral: string;
  readonly title: string;
  readonly body: string;
}

const PILLARS: readonly Pillar[] = [
  {
    numeral: "01.1",
    title: "Engineers, not a queue",
    body: "Every paid plan is staffed by the ChilliCream engineers who wrote Hot Chocolate, Fusion, and Nitro. You talk to the people who own the code.",
  },
  {
    numeral: "01.2",
    title: "Written SLAs from Business up",
    body: "Business and Enterprise carry response times you can plan around, in writing, so incidents have a clock and not a hope.",
  },
  {
    numeral: "01.3",
    title: "Private channels on every paid plan",
    body: "Startup, Business, and Enterprise each get a private Slack channel, so your context stays in one named place.",
  },
];

function Coverage() {
  return (
    <NumberedSection
      numeral="01"
      chapter="01 / Coverage"
      heading="What coverage actually means."
      intro="Three things hold across every paid plan, whatever rung you land on."
    >
      <div className="grid gap-5 sm:grid-cols-3">
        {PILLARS.map((pillar) => (
          <article
            key={pillar.numeral}
            className="border-cc-card-border bg-cc-card-bg hover:border-cc-card-border-hover rounded-2xl border p-6 transition-colors"
          >
            <div className="text-cc-accent font-mono text-xs font-semibold tracking-widest">
              {pillar.numeral}
            </div>
            <h3 className="font-heading text-cc-heading mt-3 text-xl font-semibold tracking-tight">
              {pillar.title}
            </h3>
            <p className="text-cc-prose mt-2 text-sm leading-relaxed">
              {pillar.body}
            </p>
          </article>
        ))}
      </div>
    </NumberedSection>
  );
}

// ---------------------------------------------------------------------------
// 02 Plans
// ---------------------------------------------------------------------------

function Plans() {
  return (
    <NumberedSection
      id="plans"
      numeral="02"
      chapter="02 / Plans"
      heading="Four plans. Pick the rung you need."
      intro="Every paid plan is staffed by ChilliCream engineers, with flat monthly pricing."
    >
      <div className="grid gap-5 lg:grid-cols-4">
        {PLANS.map((plan, index) => (
          <PlanCard key={plan.name} plan={plan} index={index} />
        ))}
      </div>
      <p className="text-cc-ink-dim mt-6 text-sm">
        Prices in USD. Excludes applicable taxes.
      </p>
    </NumberedSection>
  );
}

function PlanCard({
  plan,
  index,
}: {
  readonly plan: Plan;
  readonly index: number;
}) {
  const reduce = useReducedMotion();
  const cardBase =
    "group relative flex h-full flex-col rounded-2xl border p-6 transition-[transform,border-color] hover:-translate-y-0.5";
  const cardSkin = plan.highlight
    ? "border-cc-accent/60 bg-cc-card-bg shadow-[0_0_0_1px_rgba(240,120,106,0.25)]"
    : "border-cc-card-border bg-cc-card-bg hover:border-cc-card-border-hover";

  return (
    <motion.article
      className={`${cardBase} ${cardSkin}`}
      initial={reduce ? false : { opacity: 0, y: 12 }}
      whileInView={{ opacity: 1, y: 0 }}
      viewport={{ once: true, margin: "-10%" }}
      transition={{ duration: 0.4, ease: "easeOut", delay: index * 0.06 }}
    >
      {/* Tier numeral in the top-right corner repeats the spine motif. It is
          outlined by default and fills on hover. */}
      <span
        className="font-heading absolute top-4 right-5 text-3xl leading-none font-semibold transition-colors"
        style={{
          color: "transparent",
          WebkitTextStroke: "1px var(--color-cc-accent)",
        }}
        aria-hidden
      >
        <span className="block transition-colors group-hover:[color:var(--color-cc-accent)]">
          {PLAN_NUMERALS[plan.name]}
        </span>
      </span>

      {plan.highlight && (
        <span className="bg-cc-accent text-cc-surface absolute -top-3 left-6 rounded-full px-3 py-1 font-mono text-[10px] font-semibold tracking-widest uppercase">
          Most popular
        </span>
      )}

      <header className="pr-12">
        <h3 className="font-heading text-cc-heading text-2xl font-semibold tracking-tight">
          {plan.name}
        </h3>
        <p className="text-cc-ink-dim mt-1 text-sm">{plan.tagline}</p>
      </header>

      <div className="mt-5 flex items-baseline gap-2">
        <span className="font-heading text-cc-heading text-4xl font-semibold tracking-tight">
          {plan.price}
        </span>
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

// ---------------------------------------------------------------------------
// 03 Compare
// The matrix rearranged into four vertical tier columns. On desktop a left
// rail carries the feature labels; on mobile a tab switcher shows one column.
// ---------------------------------------------------------------------------

function Compare() {
  const [active, setActive] = useState<PlanName>("Business");

  return (
    <NumberedSection
      numeral="03"
      chapter="03 / Compare"
      heading="Compare every plan, line by line."
      intro="Response times, channels, and the strategic perks Enterprise teams ask for, read top to bottom as a single tier."
    >
      {/* Mobile: tab switcher, one tier column at a time. */}
      <div className="md:hidden">
        <div
          className="border-cc-card-border flex gap-1 overflow-x-auto rounded-full border p-1"
          role="tablist"
          aria-label="Plan tiers"
        >
          {PLAN_NAMES.map((name) => {
            const isActive = name === active;
            return (
              <button
                key={name}
                type="button"
                role="tab"
                aria-selected={isActive}
                onClick={() => setActive(name)}
                className={`flex-1 rounded-full px-3 py-2 text-xs font-medium whitespace-nowrap transition-colors ${
                  isActive
                    ? "bg-cc-accent text-cc-surface"
                    : "text-cc-ink-dim hover:text-cc-heading"
                }`}
              >
                <span className="font-mono">{PLAN_NUMERALS[name]}</span> {name}
              </button>
            );
          })}
        </div>
        <div className="mt-5">
          <TierColumn name={active} mobile />
        </div>
      </div>

      {/* Desktop: a left feature rail with four tier columns alongside. */}
      <div className="hidden gap-4 md:grid md:grid-cols-[minmax(0,1fr)_repeat(4,minmax(0,1fr))]">
        <FeatureRail />
        {PLAN_NAMES.map((name) => (
          <TierColumn key={name} name={name} />
        ))}
      </div>
    </NumberedSection>
  );
}

function FeatureRail() {
  return (
    <div className="sticky top-24 self-start">
      <div className="h-[88px]" aria-hidden />
      {COMPARISON.map((group) => (
        <div key={group.title} className="mb-2">
          <div className="text-cc-nav-label border-cc-card-border border-b py-2 font-mono text-[11px] font-semibold tracking-widest uppercase">
            {group.title}
          </div>
          {group.rows.map((row) => (
            <div
              key={row.title}
              className="border-cc-card-border flex min-h-[60px] items-center border-b py-3"
            >
              <span className="text-cc-heading text-sm font-medium">
                {row.title}
              </span>
            </div>
          ))}
        </div>
      ))}
    </div>
  );
}

function TierColumn({
  name,
  mobile = false,
}: {
  readonly name: PlanName;
  readonly mobile?: boolean;
}) {
  const index = PLAN_NAMES.indexOf(name);
  const isHighlight = name === "Business";
  const skin = isHighlight
    ? "border-cc-accent/50 bg-cc-accent/[0.04]"
    : "border-cc-card-border bg-cc-card-bg";

  return (
    <div className={`overflow-hidden rounded-2xl border ${skin}`}>
      <div className="border-cc-card-border border-b px-4 py-4 text-center">
        <div
          className="font-heading mx-auto text-2xl leading-none font-semibold"
          style={{
            color: "transparent",
            WebkitTextStroke: "1px var(--color-cc-accent)",
          }}
          aria-hidden
        >
          {PLAN_NUMERALS[name]}
        </div>
        <div
          className={`mt-2 font-semibold ${
            isHighlight ? "text-cc-accent" : "text-cc-heading"
          }`}
        >
          {name}
        </div>
      </div>

      {COMPARISON.map((group) => (
        <div key={group.title}>
          <div className="bg-cc-surface/60 text-cc-nav-label border-cc-card-border border-b px-4 py-2 font-mono text-[11px] font-semibold tracking-widest uppercase">
            {group.title}
          </div>
          {group.rows.map((row) => (
            <div
              key={row.title}
              className="border-cc-card-border flex min-h-[60px] flex-col justify-center border-b px-4 py-3 text-center text-sm"
            >
              {mobile && (
                <div className="text-cc-heading mb-1 text-left text-xs font-medium">
                  {row.title}
                </div>
              )}
              <div className={mobile ? "text-left" : ""}>
                <ComparisonValue value={row.values[index]} />
              </div>
            </div>
          ))}
        </div>
      ))}
    </div>
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

// ---------------------------------------------------------------------------
// 04 FAQ
// ---------------------------------------------------------------------------

function Faq() {
  return (
    <NumberedSection
      numeral="04"
      chapter="04 / Questions"
      heading="Questions buyers ask before they sign."
      intro="The questions buyers ask before they sign, answered straight."
    >
      <div className="border-cc-card-border bg-cc-card-bg divide-cc-card-border divide-y rounded-2xl border">
        {FAQ.map((item, index) => {
          const label = `Q.${String(index + 1).padStart(2, "0")}`;
          return (
            <details
              key={item.q}
              className="group px-5 py-5 sm:px-6"
              name="support-faq-v8"
            >
              <summary className="flex cursor-pointer list-none items-start justify-between gap-6">
                <span className="flex items-start gap-3">
                  <span className="text-cc-accent mt-0.5 font-mono text-sm font-semibold tracking-wide">
                    {label}
                  </span>
                  <span className="text-cc-heading text-base font-medium sm:text-lg">
                    {item.q}
                  </span>
                </span>
                <span
                  className="text-cc-ink-dim mt-1 inline-flex shrink-0 transition-transform group-open:rotate-45"
                  aria-hidden
                >
                  <PlusGlyph />
                </span>
              </summary>
              <p className="text-cc-prose mt-3 pr-10 pl-12 text-sm leading-relaxed sm:text-base">
                {item.a}
              </p>
            </details>
          );
        })}
      </div>
    </NumberedSection>
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

// ---------------------------------------------------------------------------
// 05 Enterprise summit
// ---------------------------------------------------------------------------

function Enterprise() {
  return (
    <section className="py-16 md:pl-16">
      <div className="border-cc-card-border bg-cc-card-bg relative overflow-hidden rounded-2xl border p-8 sm:p-12">
        <EnterpriseGlow />
        <div className="relative grid gap-x-10 gap-y-8 md:grid-cols-[minmax(0,12rem)_1fr] md:items-start">
          <div className="md:pt-1">
            <GiantNumeral value="05" className="text-7xl sm:text-8xl" />
          </div>
          <div>
            <Chapter>05 / Enterprise</Chapter>
            <h2 className="font-heading text-cc-heading mt-4 text-3xl font-semibold tracking-tight sm:text-4xl">
              Whole-org coverage. Tailored SLAs. One named contact.
            </h2>
            <p className="text-cc-prose mt-4 max-w-xl text-base leading-relaxed sm:text-lg">
              For organizations running ChilliCream across multiple teams and
              business units. We shape the SLA around how you ship, and a
              dedicated account manager owns the relationship end to end.
            </p>
            <ul className="mt-8 grid gap-5 sm:grid-cols-2">
              <EnterpriseFact title="24-hour critical SLA">
                Any day. Unlimited critical incidents under one ceiling.
              </EnterpriseFact>
              <EnterpriseFact title="Dedicated account manager">
                One named contact who knows your stack and your roadmap.
              </EnterpriseFact>
              <EnterpriseFact title="Phone support">
                For the calls that should not wait for a ticket.
              </EnterpriseFact>
              <EnterpriseFact title="Status reviews">
                Recurring check-ins on roadmap, upgrades, and posture.
              </EnterpriseFact>
            </ul>
            <div className="mt-8 flex flex-col gap-3 sm:flex-row">
              <SolidButton href="/services/support/contact?plan=Enterprise">
                Contact Us
              </SolidButton>
              <OutlineButton href="/services/advisory">
                Pair with Advisory
              </OutlineButton>
            </div>
          </div>
        </div>
      </div>
    </section>
  );
}

function EnterpriseGlow() {
  return (
    <svg
      className="pointer-events-none absolute -top-24 -right-24 h-[420px] w-[420px] opacity-60"
      viewBox="0 0 400 400"
      aria-hidden
    >
      <defs>
        <radialGradient id="cc-support-v8-glow" cx="50%" cy="50%" r="50%">
          <stop offset="0%" stopColor="#f0786a" stopOpacity="0.35" />
          <stop offset="60%" stopColor="#f0786a" stopOpacity="0.05" />
          <stop offset="100%" stopColor="#f0786a" stopOpacity="0" />
        </radialGradient>
      </defs>
      <circle cx="200" cy="200" r="200" fill="url(#cc-support-v8-glow)" />
    </svg>
  );
}

function EnterpriseFact({
  title,
  children,
}: {
  readonly title: string;
  readonly children: ReactNode;
}) {
  return (
    <li className="border-cc-card-border rounded-xl border p-4">
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
    </li>
  );
}

// ---------------------------------------------------------------------------
// Closing CTA
// ---------------------------------------------------------------------------

function Closing() {
  return (
    <section className="py-20 text-center md:pl-16">
      <div
        className="font-heading text-cc-ink-faint mx-auto text-2xl font-semibold"
        aria-hidden
      >
        06
      </div>
      <h2 className="font-heading text-cc-heading mx-auto mt-3 max-w-2xl text-3xl font-semibold tracking-tight sm:text-4xl">
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

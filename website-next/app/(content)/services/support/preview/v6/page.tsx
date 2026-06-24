import type { Metadata } from "next";
import type { ReactNode } from "react";

import { CheckIcon } from "@/src/components/CheckIcon";
import { OutlineButton, SolidButton } from "@/src/design-system/Button";
import { DripBrewer } from "@/src/icons/DripBrewer";
import { FrenchPress } from "@/src/icons/FrenchPress";
import { PourOver } from "@/src/icons/PourOver";

export const metadata: Metadata = {
  title: "GraphQL Support Plans & SLAs | ChilliCream",
  description:
    "GraphQL support plans for Hot Chocolate, Fusion, and Nitro. From community Slack to enterprise SLAs with phone support, named contact, and 24h response.",
  keywords: [
    "GraphQL support",
    "GraphQL support plans",
    "Hot Chocolate support",
    "Nitro support",
    "GraphQL SLA",
    "enterprise GraphQL support",
    "ChilliCream support plans",
  ],
  openGraph: {
    title: "GraphQL Support Plans & SLAs | ChilliCream",
    description:
      "GraphQL support plans for Hot Chocolate, Fusion, and Nitro. From community Slack to enterprise SLAs with phone support, named contact, and 24h response.",
  },
  robots: { index: false, follow: false },
};

type PlanName = "Community" | "Startup" | "Business" | "Enterprise";

type BrewIcon = "drip" | "french-press" | "pour-over" | "espresso";

interface Plan {
  readonly name: PlanName;
  readonly kicker: string;
  readonly brew: BrewIcon;
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
    kicker: "Walk-in counter",
    brew: "drip",
    price: "Free",
    tagline: "For hackers and side projects",
    description: "For personal or non-commercial projects, to start hacking.",
    perks: ["Public Slack Channel"],
    cta: { label: "Join Slack", href: "https://slack.chillicream.com/" },
  },
  {
    name: "Startup",
    kicker: "Reserved seat",
    brew: "french-press",
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
    kicker: "House table",
    brew: "pour-over",
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
    kicker: "Private room",
    brew: "espresso",
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

export default function SupportPreviewV6Page() {
  return (
    <>
      <ServiceBarHero />
      <BehindTheBar />
      <HouseSla />
      <ComparisonMatrix />
      <EnterpriseBand />
      <FaqSection />
      <ClosingCta />
    </>
  );
}

function ServiceBarHero() {
  return (
    <section className="py-20 text-center sm:py-28">
      <div className="text-cc-nav-label mb-5 font-mono text-xs font-semibold tracking-widest uppercase">
        On the menu / Support
      </div>
      <h1 className="font-heading text-cc-heading mx-auto max-w-3xl text-5xl leading-tight font-semibold tracking-tight sm:text-6xl lg:text-7xl">
        GraphQL support, brewed by the engineers who wrote the code.
      </h1>
      <p className="text-cc-prose lead mx-auto mt-6 max-w-2xl text-lg sm:text-xl">
        Coverage for Hot Chocolate, Fusion, and Nitro, from public Slack to
        whole-org SLAs. Named channels, response times you can plan around, and
        people who can read the stack trace.
      </p>
      <div className="mt-10 flex flex-col items-center justify-center gap-3 sm:flex-row sm:gap-4">
        <SolidButton href="#plans">Compare plans</SolidButton>
        <OutlineButton href="/services/support/contact">
          Talk to us
        </OutlineButton>
      </div>
      <ul className="text-cc-ink-dim mt-10 flex flex-wrap items-center justify-center gap-x-6 gap-y-2 text-sm">
        <HeroFact>Engineers, not a queue</HeroFact>
        <HeroFact>Private Slack on every paid plan</HeroFact>
        <HeroFact>Written SLAs from Business up</HeroFact>
      </ul>
    </section>
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

function BehindTheBar() {
  return (
    <section id="plans" className="py-16">
      <div className="mx-auto mb-10 max-w-2xl text-center">
        <div className="text-cc-nav-label mb-3 font-mono text-xs font-semibold tracking-widest uppercase">
          Behind the bar
        </div>
        <h2 className="font-heading text-cc-heading text-3xl font-semibold tracking-tight sm:text-4xl">
          Four ways to be served.
        </h2>
        <p className="text-cc-ink-dim mt-3 text-base sm:text-lg">
          Every paid plan is staffed by ChilliCream engineers, with flat monthly
          pricing.
        </p>
      </div>
      <div className="grid gap-5 lg:grid-cols-4">
        {PLANS.map((plan) => (
          <PlanCard key={plan.name} plan={plan} />
        ))}
      </div>
      <p className="text-cc-ink-dim mt-6 text-center text-sm">
        Prices in USD. Excludes applicable taxes.
      </p>
    </section>
  );
}

function BrewGlyph({ brew }: { readonly brew: BrewIcon }) {
  return (
    <div
      className="text-cc-accent border-cc-accent/40 bg-cc-accent/10 flex h-12 w-12 items-center justify-center rounded-xl border"
      aria-hidden
    >
      {brew === "drip" && <DripBrewer className="h-7 w-7" />}
      {brew === "french-press" && <FrenchPress className="h-7 w-7" />}
      {brew === "pour-over" && <PourOver className="h-7 w-7" />}
      {brew === "espresso" && <EspressoCupGlyph className="h-7 w-7" />}
    </div>
  );
}

function EspressoCupGlyph({ className }: { readonly className?: string }) {
  // Monoline currentColor espresso-cup-and-saucer, matched to the
  // DripBrewer / FrenchPress / PourOver line weight so it inherits text-cc-accent.
  return (
    <svg
      viewBox="0 0 24 24"
      className={className}
      fill="none"
      stroke="currentColor"
      strokeWidth="1.6"
      strokeLinecap="round"
      strokeLinejoin="round"
      aria-hidden
    >
      <path d="M5 9h11v5a4 4 0 0 1-4 4H9a4 4 0 0 1-4-4V9z" />
      <path d="M16 10.5h1.5a2.5 2.5 0 0 1 0 5H16" />
      <path d="M3.5 20.5h14" />
      <path d="M8.5 6.2c0-.9.7-1.4.7-2.2 0-.8-.7-1.3-.7-2" opacity="0.7" />
      <path d="M12 6.2c0-.9.7-1.4.7-2.2 0-.8-.7-1.3-.7-2" opacity="0.7" />
    </svg>
  );
}

function PlanCard({ plan }: { readonly plan: Plan }) {
  const cardBase =
    "relative flex h-full flex-col rounded-2xl border p-6 transition-colors";
  const cardSkin = plan.highlight
    ? "border-cc-accent/60 bg-cc-card-bg shadow-[0_0_0_1px_rgba(94,234,212,0.25)]"
    : "border-cc-card-border bg-cc-card-bg hover:border-cc-card-border-hover";

  return (
    <article className={`${cardBase} ${cardSkin}`}>
      {plan.highlight && (
        <span className="bg-cc-accent text-cc-surface absolute -top-3 left-6 rounded-full px-3 py-1 font-mono text-[10px] font-semibold tracking-widest uppercase">
          Today&rsquo;s pour
        </span>
      )}

      <header>
        <BrewGlyph brew={plan.brew} />
        <div className="text-cc-accent mt-4 font-mono text-[11px] font-semibold tracking-widest uppercase">
          {plan.kicker}
        </div>
        <h3 className="font-heading text-cc-heading mt-1 text-2xl font-semibold tracking-tight">
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
    </article>
  );
}

interface SlaPanel {
  readonly title: string;
  readonly body: string;
  readonly plans: string;
}

const SLA_PANELS: readonly SlaPanel[] = [
  {
    title: "Drip",
    body: "Best-effort answers from engineers and the community in public Slack. No guarantee, no ticket history.",
    plans: "Community",
  },
  {
    title: "Next business day",
    body: "Critical incidents on Startup and Business are picked up the next business day, with non-critical work tracked alongside.",
    plans: "Startup, Business",
  },
  {
    title: "24 hours, any day",
    body: "Enterprise commits to 24 hours on critical incidents, weekends included, with non-critical work on next business day.",
    plans: "Enterprise",
  },
];

function HouseSla() {
  return (
    <section className="py-16">
      <div className="mx-auto mb-10 max-w-2xl text-center">
        <div className="text-cc-nav-label mb-3 font-mono text-xs font-semibold tracking-widest uppercase">
          How fast we pour
        </div>
        <h2 className="font-heading text-cc-heading text-3xl font-semibold tracking-tight sm:text-4xl">
          Response times, in plain language.
        </h2>
        <p className="text-cc-ink-dim mt-3 text-base sm:text-lg">
          The same numbers as the table below, said the way a customer would say
          them.
        </p>
      </div>
      <div className="grid gap-5 md:grid-cols-3">
        {SLA_PANELS.map((panel) => (
          <article
            key={panel.title}
            className="border-cc-card-border bg-cc-card-bg flex h-full flex-col rounded-2xl border p-6"
          >
            <h3 className="font-heading text-cc-heading text-xl font-semibold tracking-tight">
              {panel.title}
            </h3>
            <p className="text-cc-prose mt-3 flex-1 text-sm leading-relaxed">
              {panel.body}
            </p>
            <div className="text-cc-ink-dim border-cc-card-border mt-4 border-t pt-3 text-xs">
              <span className="text-cc-nav-label font-mono tracking-widest uppercase">
                Plans:
              </span>{" "}
              {panel.plans}
            </div>
          </article>
        ))}
      </div>
    </section>
  );
}

function ComparisonMatrix() {
  return (
    <section className="py-16">
      <div className="mx-auto mb-8 max-w-2xl text-center">
        <div className="text-cc-nav-label mb-3 font-mono text-xs font-semibold tracking-widest uppercase">
          The full menu
        </div>
        <h2 className="font-heading text-cc-heading text-3xl font-semibold tracking-tight sm:text-4xl">
          Compare every plan, line by line.
        </h2>
        <p className="text-cc-ink-dim mt-3 text-base sm:text-lg">
          Response times, channels, and the strategic perks Enterprise teams ask
          for.
        </p>
      </div>

      <SteamLine />

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
                    className={`px-5 py-4 text-center align-bottom font-semibold ${
                      isHighlight ? "text-cc-accent" : "text-cc-heading"
                    }`}
                  >
                    {isHighlight && (
                      <span
                        className="text-cc-accent mb-2 flex items-end justify-center"
                        aria-hidden
                      >
                        <EspressoCupGlyph className="h-6 w-6" />
                      </span>
                    )}
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

function SteamLine() {
  return (
    <svg
      viewBox="0 0 200 24"
      className="text-cc-accent/40 mx-auto mb-3 h-5 w-44"
      fill="none"
      stroke="currentColor"
      strokeWidth="1.4"
      strokeLinecap="round"
      aria-hidden
    >
      <path d="M 40 22 Q 50 12 60 22 T 80 22 T 100 22 T 120 22 T 140 22 T 160 22" />
      <path
        d="M 50 14 Q 58 6 66 14 T 82 14 T 98 14 T 114 14 T 130 14 T 146 14"
        opacity="0.55"
      />
    </svg>
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
      {group.rows.map((row) => (
        <tr key={row.title} className="border-cc-card-border border-b">
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
        </tr>
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

function EnterpriseBand() {
  return (
    <section className="py-16">
      <div className="border-cc-card-border bg-cc-card-bg relative overflow-hidden rounded-2xl border p-8 sm:p-12">
        <EnterpriseGlow />
        <div className="relative grid gap-10 lg:grid-cols-[1.2fr_1fr] lg:items-center">
          <div>
            <div className="text-cc-accent mb-3 font-mono text-xs font-semibold tracking-widest uppercase">
              Private room
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
        <radialGradient id="cc-support-v6-glow" cx="50%" cy="50%" r="50%">
          <stop offset="0%" stopColor="#5eead4" stopOpacity="0.35" />
          <stop offset="60%" stopColor="#5eead4" stopOpacity="0.05" />
          <stop offset="100%" stopColor="#5eead4" stopOpacity="0" />
        </radialGradient>
      </defs>
      <circle cx="200" cy="200" r="200" fill="url(#cc-support-v6-glow)" />
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

const FAQ_GLYPHS: readonly BrewIcon[] = [
  "drip",
  "french-press",
  "pour-over",
  "espresso",
  "drip",
  "french-press",
];

function FaqSection() {
  return (
    <section className="py-16">
      <div className="mx-auto mb-10 max-w-2xl text-center">
        <div className="text-cc-nav-label mb-3 font-mono text-xs font-semibold tracking-widest uppercase">
          Ask the barista
        </div>
        <h2 className="font-heading text-cc-heading text-3xl font-semibold tracking-tight sm:text-4xl">
          Frequently asked questions
        </h2>
        <p className="text-cc-ink-dim mt-3 text-base sm:text-lg">
          The questions buyers ask before they sign, answered straight.
        </p>
      </div>
      <div className="border-cc-card-border bg-cc-card-bg divide-cc-card-border divide-y rounded-2xl border">
        {FAQ.map((item, i) => (
          <details
            key={item.q}
            className="group px-5 py-5 sm:px-6"
            name="support-faq"
          >
            <summary className="flex cursor-pointer list-none items-start justify-between gap-6">
              <span className="flex items-start gap-4">
                <FaqGlyph brew={FAQ_GLYPHS[i % FAQ_GLYPHS.length]} />
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
        ))}
      </div>
    </section>
  );
}

function FaqGlyph({ brew }: { readonly brew: BrewIcon }) {
  const baseClass =
    "text-cc-accent border-cc-card-border bg-cc-surface/60 inline-flex h-8 w-8 shrink-0 items-center justify-center rounded-lg border";
  return (
    <span className={baseClass} aria-hidden>
      {brew === "drip" && <DripBrewer className="h-5 w-5" />}
      {brew === "french-press" && <FrenchPress className="h-5 w-5" />}
      {brew === "pour-over" && <PourOver className="h-5 w-5" />}
      {brew === "espresso" && <EspressoCupGlyph className="h-5 w-5" />}
    </span>
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

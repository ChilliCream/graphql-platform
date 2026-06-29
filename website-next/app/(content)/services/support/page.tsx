import type { Metadata } from "next";
import type { ReactElement } from "react";

import { CheckIcon } from "@/src/components/CheckIcon";
import { OutlineButton, SolidButton } from "@/src/design-system/Button";

export const metadata: Metadata = {
  title: "GraphQL Support Plans",
  description:
    "Support and response-time plans from ChilliCream. Next business day on Startup and Business, 24 hours for Enterprise criticals, direct to the engineers who build Hot Chocolate, Fusion, and Nitro.",
  keywords: [
    "GraphQL support",
    "HotChocolate support",
    "Nitro support",
    "ChilliCream support",
    "enterprise GraphQL",
    "incident response",
  ],
  openGraph: {
    type: "website",
    title: "GraphQL Support Plans",
    description:
      "Response windows you can hold us to. Next business day on Startup and Business, 24 hours for Enterprise criticals, plus a direct line to the core team.",
  },
  twitter: {
    card: "summary_large_image",
    title: "GraphQL Support Plans",
    description:
      "Response windows you can hold us to. Next business day on Startup and Business, 24 hours for Enterprise criticals, plus a direct line to the core team.",
  },
};

// ---------------------------------------------------------------------------
// Data
// ---------------------------------------------------------------------------

interface SupportScenario {
  readonly label: string;
  readonly title: string;
  readonly copy: string;
  readonly Icon: () => ReactElement;
}

const SCENARIOS: readonly SupportScenario[] = [
  {
    label: "A quick question",
    title: "Message us, hear back in minutes",
    copy: "A query plan that fell over, a schema call you'd rather not make alone, an upgrade that needs a second pair of eyes. Drop it in your private channel and, more often than not, a core engineer answers within minutes.",
    Icon: ChannelIcon,
  },
  {
    label: "Production is down",
    title: "We get on a call until you're back",
    copy: "Something critical breaks. You email us and open a ticket, and a core team member jumps on a call, staying with you until production is running again.",
    Icon: CallIcon,
  },
  {
    label: "A second opinion",
    title: "Book time with the team",
    copy: "Planning a migration, reviewing a schema, sizing a rollout? Schedule a session and we'll work through it together, live, with the people who build the platform.",
    Icon: CalendarIcon,
  },
];

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
    tagline: "Whole-org coverage, tailored terms",
    description:
      "For the whole organization, all your teams and business units, and with tailor made plans.",
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
        values: [
          false,
          "2 (next business day)",
          "5 (next business day)",
          "Unlimited (24 hours)",
        ],
      },
      {
        title: "Non-critical Incidents",
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
    a: "Yes. Business and Enterprise both support self-hosted Nitro, Fusion gateways, and Hot Chocolate services running in your own cloud or on-prem. Enterprise adds tailored response windows and a named account manager who knows your topology.",
  },
  {
    q: "Can we change plans later?",
    a: "Yes. You can upgrade at any time and the new response window takes effect immediately. Downgrades take effect at the start of the next billing month so an in-flight incident never falls between plans.",
  },
];

// ---------------------------------------------------------------------------
// Hero: three ways support actually plays out (from preview v3).
// ---------------------------------------------------------------------------

function Hero() {
  return (
    <section className="pt-16 pb-12 sm:pt-24 sm:pb-16">
      <div className="text-cc-nav-label mb-4 text-center font-mono text-xs font-semibold tracking-widest uppercase">
        ChilliCream Support
      </div>
      <h1 className="text-cc-heading mx-auto max-w-3xl text-center text-5xl leading-tight font-semibold tracking-tight sm:text-6xl lg:text-7xl">
        Support from the people who build the platform.
      </h1>
      <p className="text-cc-ink-dim mx-auto mt-6 max-w-2xl text-center text-base sm:text-lg">
        However you reach us, you&rsquo;re working with the core engineers who
        build Hot Chocolate, Fusion and Nitro, not a first-line queue. Here is
        what that looks like in practice.
      </p>

      <ul className="mt-14 grid gap-4 md:grid-cols-3">
        {SCENARIOS.map((scenario) => (
          <ScenarioCard key={scenario.label} scenario={scenario} />
        ))}
      </ul>

      <div className="mt-12 flex flex-wrap items-center justify-center gap-3">
        <SolidButton href="#plans">See the four plans</SolidButton>
        <OutlineButton href="/services/support/contact">
          Talk to us
        </OutlineButton>
      </div>
    </section>
  );
}

function ScenarioCard({ scenario }: { readonly scenario: SupportScenario }) {
  const { Icon } = scenario;
  return (
    <li className="border-cc-card-border bg-cc-card-bg/60 flex flex-col gap-4 rounded-3xl border p-6 sm:p-7">
      <div className="text-cc-ink-dim font-mono text-[0.65rem] tracking-[0.18em] uppercase">
        {scenario.label}
      </div>
      <div className="text-cc-accent">
        <Icon />
      </div>
      <h2 className="font-heading text-cc-heading text-xl font-semibold">
        {scenario.title}
      </h2>
      <p className="text-cc-ink text-sm leading-relaxed">{scenario.copy}</p>
    </li>
  );
}

// ---------------------------------------------------------------------------
// Plans, comparison, FAQ, and enterprise band (from preview v1).
// ---------------------------------------------------------------------------

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
        {PLANS.map((plan) => (
          <PlanCard key={plan.name} plan={plan} />
        ))}
      </div>
    </section>
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

function ComparisonMatrix() {
  return (
    <section id="compare" className="py-16">
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

function EnterpriseBand() {
  return (
    <section className="py-16 sm:py-20">
      <div className="border-cc-accent/40 bg-cc-card-bg/70 relative overflow-hidden rounded-3xl border p-8 sm:p-12">
        <div
          aria-hidden="true"
          className="pointer-events-none absolute inset-0"
          style={{
            background:
              "radial-gradient(60% 80% at 100% 0%, rgba(94,234,212,0.12), transparent 60%), radial-gradient(50% 70% at 0% 100%, rgba(124,146,198,0.10), transparent 60%)",
          }}
        />
        <div className="relative grid gap-8 lg:grid-cols-[1.4fr_1fr] lg:items-center">
          <div>
            <div className="text-cc-nav-label mb-3 font-mono text-xs font-semibold tracking-widest uppercase">
              Enterprise
            </div>
            <h2 className="text-cc-heading text-3xl font-semibold tracking-tight sm:text-4xl">
              Your response windows, your teams, your platform.
            </h2>
            <p className="text-cc-ink mt-4 max-w-xl text-base leading-relaxed">
              Multiple business units on Hot Chocolate, Fusion or Nitro?
              Regulated workload that needs a 24 hour critical response window,
              phone cover, and status reviews? We will tailor the contract, the
              response windows, and the named contacts to fit.
            </p>
            <ul className="text-cc-ink mt-6 grid gap-2 text-sm sm:grid-cols-2">
              {[
                "Tailored response windows",
                "Dedicated account manager",
                "Phone support",
                "Status reviews",
                "Unlimited critical incidents",
                "Private issue tracking board",
              ].map((item) => (
                <li key={item} className="flex items-start gap-2">
                  <span className="text-cc-accent mt-1 flex-none">
                    <CheckIcon />
                  </span>
                  <span>{item}</span>
                </li>
              ))}
            </ul>
          </div>
          <div className="flex flex-col gap-3">
            <SolidButton
              href="/services/support/contact?plan=Enterprise"
              className="w-full"
            >
              Talk to us about Enterprise
            </SolidButton>
            <OutlineButton href="/services/advisory" className="w-full">
              See advisory engagements
            </OutlineButton>
            <p className="text-cc-ink-dim mt-2 font-mono text-xs">
              Typical response on enterprise enquiries: one business day.
            </p>
          </div>
        </div>
      </div>
    </section>
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

// ---------------------------------------------------------------------------
// Hero scenario icons.
// ---------------------------------------------------------------------------

function ChannelIcon() {
  return (
    <svg
      width="44"
      height="44"
      viewBox="0 0 44 44"
      fill="none"
      aria-hidden="true"
    >
      <rect
        x="6"
        y="10"
        width="32"
        height="22"
        rx="4"
        stroke="currentColor"
        strokeWidth="1.5"
      />
      <path
        d="M6 16h32"
        stroke="currentColor"
        strokeWidth="1.5"
        opacity="0.6"
      />
      <circle cx="10" cy="13" r="0.9" fill="currentColor" />
      <circle cx="13" cy="13" r="0.9" fill="currentColor" />
      <circle cx="16" cy="13" r="0.9" fill="currentColor" />
      <path
        d="M11 22h14M11 26h10"
        stroke="currentColor"
        strokeWidth="1.5"
        strokeLinecap="round"
      />
    </svg>
  );
}

function CallIcon() {
  return (
    <svg
      width="44"
      height="44"
      viewBox="0 0 44 44"
      fill="none"
      aria-hidden="true"
    >
      <path
        d="M12 25v-3a10 10 0 0 1 20 0v3"
        stroke="currentColor"
        strokeWidth="1.5"
        strokeLinecap="round"
      />
      <rect
        x="8"
        y="23"
        width="6"
        height="9"
        rx="2"
        stroke="currentColor"
        strokeWidth="1.5"
      />
      <rect
        x="30"
        y="23"
        width="6"
        height="9"
        rx="2"
        stroke="currentColor"
        strokeWidth="1.5"
      />
      <path
        d="M33 32v1.5a4 4 0 0 1-4 4h-5"
        stroke="currentColor"
        strokeWidth="1.5"
        strokeLinecap="round"
        opacity="0.6"
      />
      <circle cx="24" cy="37.5" r="1.4" fill="currentColor" />
    </svg>
  );
}

function CalendarIcon() {
  return (
    <svg
      width="44"
      height="44"
      viewBox="0 0 44 44"
      fill="none"
      aria-hidden="true"
    >
      <rect
        x="8"
        y="11"
        width="28"
        height="25"
        rx="4"
        stroke="currentColor"
        strokeWidth="1.5"
      />
      <path
        d="M8 18h28"
        stroke="currentColor"
        strokeWidth="1.5"
        opacity="0.6"
      />
      <path
        d="M15 8v6M29 8v6"
        stroke="currentColor"
        strokeWidth="1.8"
        strokeLinecap="round"
      />
      <rect
        x="14"
        y="23"
        width="6"
        height="6"
        rx="1.2"
        fill="currentColor"
        opacity="0.8"
      />
    </svg>
  );
}

// ---------------------------------------------------------------------------
// Page
// ---------------------------------------------------------------------------

export default function SupportPage() {
  return (
    <>
      <Hero />
      <PlanGrid />
      <ComparisonMatrix />
      <FaqSection />
      <EnterpriseBand />
      <ClosingCta />
    </>
  );
}

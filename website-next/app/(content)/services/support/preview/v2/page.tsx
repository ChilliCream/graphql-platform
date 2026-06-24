import type { Metadata } from "next";

import { CheckIcon } from "@/src/components/CheckIcon";
import { OutlineButton, SolidButton } from "@/src/design-system/Button";
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeaderCell,
  TableRow,
} from "@/src/design-system/Table";

export const metadata: Metadata = {
  title: "GraphQL Support Plans with Honest SLAs",
  description:
    "GraphQL support and SLA plans from ChilliCream. Next-business-day responses on Startup and Business, 24-hour critical SLA on Enterprise, plus Nitro support.",
  keywords: [
    "GraphQL support",
    "GraphQL SLA",
    "HotChocolate support",
    "Nitro support",
    "ChilliCream support",
    "enterprise GraphQL",
    "incident response",
  ],
  robots: { index: false, follow: false },
  openGraph: {
    type: "website",
    title: "GraphQL Support Plans with Honest SLAs",
    description:
      "Response promises you can hold us to. Next business day on Startup and Business, 24 hours for Enterprise criticals, plus dedicated channels.",
  },
  twitter: {
    card: "summary_large_image",
    title: "GraphQL Support Plans with Honest SLAs",
    description:
      "Response promises you can hold us to. Next business day on Startup and Business, 24 hours for Enterprise criticals, plus dedicated channels.",
  },
};

// ---------------------------------------------------------------------------
// Data
// ---------------------------------------------------------------------------

type PlanKey = "community" | "startup" | "business" | "enterprise";

interface Plan {
  readonly key: PlanKey;
  readonly title: string;
  readonly price: string;
  readonly priceNote?: string;
  readonly description: string;
  readonly perks: readonly string[];
  readonly cta: { readonly title: string; readonly link: string };
  readonly featured?: boolean;
}

const PLANS: readonly Plan[] = [
  {
    key: "community",
    title: "Community",
    price: "Free",
    description: "For personal or non-commercial projects, to start hacking.",
    perks: ["Public Slack Channel"],
    cta: { title: "Join Slack", link: "https://slack.chillicream.com/" },
  },
  {
    key: "startup",
    title: "Startup",
    price: "$450",
    priceNote: "per month",
    description:
      "For small teams with moderate bandwidth and projects of low to medium complexity.",
    perks: ["Private Slack Channel", "2 critical incidents"],
    cta: {
      title: "Contact Us",
      link: "/services/support/contact?plan=Startup",
    },
  },
  {
    key: "business",
    title: "Business",
    price: "$1,300",
    priceNote: "per month",
    description: "For larger teams with business-critical projects.",
    perks: [
      "Private Slack Channel",
      "5 critical incidents",
      "2 non-critical incidents",
      "Email support",
    ],
    cta: {
      title: "Contact Us",
      link: "/services/support/contact?plan=Business",
    },
    featured: true,
  },
  {
    key: "enterprise",
    title: "Enterprise",
    price: "Custom",
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
      title: "Contact Us",
      link: "/services/support/contact?plan=Enterprise",
    },
  },
];

const PROMISE_TILES: readonly {
  readonly eyebrow: string;
  readonly title: string;
  readonly subtitle: string;
  readonly rows: readonly {
    readonly plan: string;
    readonly value: string;
    readonly note?: string;
  }[];
}[] = [
  {
    eyebrow: "01 / Critical",
    title: "Production is on fire",
    subtitle:
      "A live system is down or seriously degraded and your users are feeling it.",
    rows: [
      { plan: "Community", value: "Not included" },
      { plan: "Startup", value: "Next business day", note: "2 / month" },
      { plan: "Business", value: "Next business day", note: "5 / month" },
      { plan: "Enterprise", value: "24 hours", note: "Unlimited" },
    ],
  },
  {
    eyebrow: "02 / Non-critical",
    title: "Something is wrong, not bleeding",
    subtitle:
      "A defect, regression, or blocker that does not take production offline right now.",
    rows: [
      { plan: "Community", value: "Not included" },
      { plan: "Startup", value: "Not included" },
      { plan: "Business", value: "3 business days", note: "2 / month" },
      { plan: "Enterprise", value: "Next business day", note: "10 / month" },
    ],
  },
  {
    eyebrow: "03 / Channels",
    title: "How you reach us",
    subtitle:
      "Where the conversation actually happens once you have a private line in.",
    rows: [
      { plan: "Community", value: "Public Slack" },
      { plan: "Startup", value: "Private Slack" },
      { plan: "Business", value: "Private Slack + Email" },
      { plan: "Enterprise", value: "Private Slack + Email + Phone" },
    ],
  },
];

interface FeatureValue {
  readonly title: string;
  readonly values: readonly (boolean | string)[];
}

const COMPARISON: readonly FeatureValue[] = [
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
  { title: "Public Slack Channel", values: [true, true, true, true] },
  { title: "Private Slack Channel", values: [false, true, true, true] },
  {
    title: "Private Issue Tracking Board",
    values: [false, false, true, true],
  },
  { title: "Email Support", values: [false, false, true, true] },
  { title: "Phone Support", values: [false, false, false, true] },
  { title: "Dedicated Account Manager", values: [false, false, false, true] },
  { title: "Status Reviews", values: [false, false, false, true] },
];

const PLAN_NAMES = ["Community", "Startup", "Business", "Enterprise"] as const;

const FAQ: readonly { readonly q: string; readonly a: string }[] = [
  {
    q: "What counts as a critical incident?",
    a: "A critical incident is a production system that is fully down, returning errors for most requests, or so degraded that core user journeys do not complete. If you are unsure, file it as critical. We would rather assess and downgrade together than miss a real outage.",
  },
  {
    q: "When does the response clock start?",
    a: "The clock starts when your ticket lands in your private Slack channel or, on Business and Enterprise, in email. Business-day responses follow your account's working calendar, agreed when we onboard you. The 24-hour Enterprise critical window is measured in elapsed hours against that same operating calendar.",
  },
  {
    q: "Do you guarantee a fix time?",
    a: "No, and we will not pretend otherwise. We guarantee response time, expert ownership, and a plan back to you inside the SLA. Time to fix depends on the defect and your environment. We tell you what we know, when we know it.",
  },
  {
    q: "What happens if I exceed my monthly incidents?",
    a: "Nothing breaks. We keep working the open tickets and flag the overage to you. If you regularly land above your allowance, that usually means it is time to talk about Business or Enterprise rather than absorbing surprise invoices.",
  },
  {
    q: "Do you support self-hosted HotChocolate, Fusion, and Nitro?",
    a: "Yes. All plans cover HotChocolate, Fusion gateway, Banana Cake Pop, and Nitro deployments you run yourself. We also help with upgrade paths between major versions when you hit blockers.",
  },
  {
    q: "What is not covered by these plans?",
    a: "Greenfield design work, architecture deep dives, bespoke feature development, and embedded engineering are not support. They live on our Advisory page and bill separately, so support hours stay focused on keeping you running.",
  },
];

// ---------------------------------------------------------------------------
// Section components
// ---------------------------------------------------------------------------

function Hero() {
  return (
    <section className="relative pt-16 pb-12 sm:pt-24 sm:pb-16">
      <div className="border-cc-card-border bg-cc-card-bg/40 text-cc-ink-dim mx-auto mb-8 inline-flex w-full max-w-3xl items-center justify-center gap-3 rounded-full border px-4 py-2 font-mono text-xs tracking-widest uppercase">
        <span className="bg-cc-accent inline-block h-1.5 w-1.5 rounded-full" />
        <span>Response SLAs</span>
      </div>
      <h1 className="text-cc-heading font-heading mx-auto max-w-4xl text-center text-5xl leading-tight font-bold tracking-tight sm:text-6xl lg:text-7xl">
        Critical incidents in <span className="text-cc-accent">24 hours</span>{" "}
        on Enterprise. <span className="text-cc-accent">Next business day</span>{" "}
        on Startup and Business.
      </h1>
      <p className="text-cc-prose mx-auto mt-6 max-w-2xl text-center text-base sm:text-lg">
        Non-critical incidents land in three business days on Business and next
        business day on Enterprise. Pick a plan by the SLA you need, talk to the
        engineers who wrote HotChocolate, Fusion, and Nitro.
      </p>
      <div className="mt-10 flex flex-col items-center justify-center gap-3 sm:flex-row">
        <SolidButton href="/services/support/contact?plan=Business">
          Get a plan with an SLA
        </SolidButton>
        <OutlineButton href="#plans">Compare plans</OutlineButton>
      </div>
    </section>
  );
}

function PromiseTile({
  eyebrow,
  title,
  subtitle,
  rows,
}: {
  eyebrow: string;
  title: string;
  subtitle: string;
  rows: readonly { plan: string; value: string; note?: string }[];
}) {
  return (
    <article className="bg-cc-card-bg border-cc-card-border hover:border-cc-card-border-hover relative flex flex-col rounded-2xl border p-6 transition-colors sm:p-7">
      <div className="text-cc-nav-label mb-4 font-mono text-xs tracking-widest uppercase">
        {eyebrow}
      </div>
      <h3 className="text-cc-heading font-heading text-2xl leading-tight font-semibold">
        {title}
      </h3>
      <p className="text-cc-ink-dim mt-2 text-sm">{subtitle}</p>
      <ul className="border-cc-card-border mt-6 divide-y divide-[var(--color-cc-card-border)] border-t">
        {rows.map((row) => (
          <li
            key={row.plan}
            className="flex items-baseline justify-between gap-4 py-3"
          >
            <span className="text-cc-ink-dim text-sm">{row.plan}</span>
            <span className="text-right">
              <span className="text-cc-heading text-sm font-medium">
                {row.value}
              </span>
              {row.note && (
                <span className="text-cc-ink-dim ml-2 font-mono text-xs">
                  {row.note}
                </span>
              )}
            </span>
          </li>
        ))}
      </ul>
    </article>
  );
}

function ResponsePromises() {
  return (
    <section
      id="promises"
      aria-labelledby="promises-heading"
      className="py-12 sm:py-16"
    >
      <header className="mx-auto mb-10 max-w-3xl text-center">
        <div className="text-cc-nav-label mb-3 font-mono text-xs tracking-widest uppercase">
          The promise
        </div>
        <h2
          id="promises-heading"
          className="text-cc-heading font-heading text-3xl leading-tight font-semibold tracking-tight sm:text-4xl"
        >
          Three numbers, written down, by plan.
        </h2>
        <p className="text-cc-ink-dim mt-4 text-base">
          Every plan name below maps to a concrete response window. No
          asterisks, no &ldquo;up to&rdquo;, no best-effort weasel words.
        </p>
      </header>
      <div className="grid gap-4 md:grid-cols-3">
        {PROMISE_TILES.map((tile) => (
          <PromiseTile key={tile.eyebrow} {...tile} />
        ))}
      </div>
    </section>
  );
}

function PriceTag({ price, priceNote }: { price: string; priceNote?: string }) {
  return (
    <div className="flex items-baseline gap-2">
      <span className="text-cc-heading font-heading text-4xl font-semibold tracking-tight">
        {price}
      </span>
      {priceNote && (
        <span className="text-cc-ink-dim font-mono text-xs tracking-wide uppercase">
          {priceNote}
        </span>
      )}
    </div>
  );
}

function PlanCard({ plan }: { plan: Plan }) {
  return (
    <article
      className={[
        "bg-cc-card-bg relative flex flex-col rounded-2xl border p-6 transition-colors sm:p-7",
        plan.featured
          ? "border-cc-accent/60 ring-cc-accent/10 ring-1"
          : "border-cc-card-border hover:border-cc-card-border-hover",
      ].join(" ")}
    >
      <h3 className="text-cc-heading font-heading text-2xl font-semibold">
        {plan.title}
      </h3>
      <p className="text-cc-ink-dim mt-2 min-h-[3.5rem] text-sm">
        {plan.description}
      </p>
      <div className="mt-6">
        <PriceTag price={plan.price} priceNote={plan.priceNote} />
      </div>
      <ul className="mt-6 grow space-y-3">
        {plan.perks.map((perk) => (
          <li
            key={perk}
            className="text-cc-prose flex items-start gap-3 text-sm"
          >
            <span className="text-cc-accent mt-0.5 inline-flex shrink-0 items-center justify-center">
              <CheckIcon size={14} />
            </span>
            <span>{perk}</span>
          </li>
        ))}
      </ul>
      <div className="mt-8">
        {plan.featured ? (
          <SolidButton href={plan.cta.link} className="w-full">
            {plan.cta.title}
          </SolidButton>
        ) : (
          <OutlineButton href={plan.cta.link} className="w-full">
            {plan.cta.title}
          </OutlineButton>
        )}
      </div>
    </article>
  );
}

function Plans() {
  return (
    <section id="plans" aria-labelledby="plans-heading" className="py-16">
      <header className="mx-auto mb-10 max-w-3xl text-center">
        <div className="text-cc-nav-label mb-3 font-mono text-xs tracking-widest uppercase">
          Access tiers
        </div>
        <h2
          id="plans-heading"
          className="text-cc-heading font-heading text-3xl leading-tight font-semibold tracking-tight sm:text-4xl"
        >
          Pick the plan that gives you the SLA above.
        </h2>
        <p className="text-cc-ink-dim mt-4 text-base">
          The numbers in the promise band come straight from these four plans.
          Pricing is monthly and billed in your account currency.
        </p>
      </header>
      <div className="grid gap-5 md:grid-cols-2 lg:grid-cols-4">
        {PLANS.map((plan) => (
          <PlanCard key={plan.key} plan={plan} />
        ))}
      </div>
    </section>
  );
}

function CriticalitySigns() {
  return (
    <section
      id="criticality"
      aria-labelledby="criticality-heading"
      className="py-16"
    >
      <div className="border-cc-card-border bg-cc-card-bg/60 grid gap-10 rounded-2xl border p-8 sm:p-12 lg:grid-cols-[1fr_1.2fr]">
        <header>
          <div className="text-cc-nav-label mb-3 font-mono text-xs tracking-widest uppercase">
            What we count as critical
          </div>
          <h2
            id="criticality-heading"
            className="text-cc-heading font-heading text-3xl leading-tight font-semibold tracking-tight sm:text-4xl"
          >
            No SLO inflation. Here is the line.
          </h2>
          <p className="text-cc-ink-dim mt-4 text-base">
            We publish what a critical incident is, and what it is not, so the
            24-hour and next-business-day windows above actually mean something.
            Borderline cases get talked through, not gamed.
          </p>
        </header>
        <div className="grid gap-6 sm:grid-cols-2">
          <div>
            <div className="text-cc-success mb-2 inline-flex items-center gap-2 font-mono text-xs tracking-widest uppercase">
              <span className="bg-cc-success inline-block h-1.5 w-1.5 rounded-full" />
              Counts as critical
            </div>
            <ul className="text-cc-prose space-y-2 text-sm">
              <li>Production gateway is down or returns errors at scale.</li>
              <li>Schema or subgraph composition stops a release.</li>
              <li>
                Security defect in a supported HotChocolate, Fusion, or Nitro
                release.
              </li>
              <li>Data loss or persistent corruption you can reproduce.</li>
            </ul>
          </div>
          <div>
            <div className="text-cc-warning mb-2 inline-flex items-center gap-2 font-mono text-xs tracking-widest uppercase">
              <span className="bg-cc-warning inline-block h-1.5 w-1.5 rounded-full" />
              Does not count as critical
            </div>
            <ul className="text-cc-prose space-y-2 text-sm">
              <li>
                Questions about how to do something the docs already cover.
              </li>
              <li>Performance tuning on a healthy production system.</li>
              <li>Architecture review of a system you are still designing.</li>
              <li>
                Feature requests, including ones we agree are a good idea.
              </li>
            </ul>
          </div>
        </div>
      </div>
    </section>
  );
}

function Comparison() {
  return (
    <section id="compare" aria-labelledby="compare-heading" className="py-16">
      <header className="mx-auto mb-10 max-w-3xl text-center">
        <div className="text-cc-nav-label mb-3 font-mono text-xs tracking-widest uppercase">
          Side by side
        </div>
        <h2
          id="compare-heading"
          className="text-cc-heading font-heading text-3xl leading-tight font-semibold tracking-tight sm:text-4xl"
        >
          Compare every plan in one table.
        </h2>
      </header>
      <Table>
        <TableHead>
          <TableRow>
            <TableHeaderCell>Support</TableHeaderCell>
            {PLAN_NAMES.map((name) => (
              <TableHeaderCell key={name} align="center">
                {name}
              </TableHeaderCell>
            ))}
          </TableRow>
        </TableHead>
        <TableBody>
          {COMPARISON.map((row) => (
            <TableRow key={row.title}>
              <TableCell>{row.title}</TableCell>
              {row.values.map((v, i) => (
                <TableCell key={i} align="center">
                  {v === true ? (
                    <span className="text-cc-accent inline-flex items-center justify-center">
                      <CheckIcon />
                    </span>
                  ) : v === false ? (
                    <span className="text-cc-ink-dim">Not included</span>
                  ) : (
                    v
                  )}
                </TableCell>
              ))}
            </TableRow>
          ))}
        </TableBody>
      </Table>
    </section>
  );
}

function EnterpriseBand() {
  return (
    <section aria-labelledby="enterprise-heading" className="py-16">
      <div className="border-cc-card-border bg-cc-surface relative overflow-hidden rounded-2xl border p-8 sm:p-12">
        <div
          aria-hidden
          className="pointer-events-none absolute inset-0 opacity-60"
          style={{
            background:
              "radial-gradient(60% 80% at 85% 50%, rgba(94,234,212,0.10), transparent 70%), radial-gradient(50% 80% at 10% 20%, rgba(124,146,198,0.08), transparent 70%)",
          }}
        />
        <div className="relative grid gap-10 lg:grid-cols-[1.4fr_1fr] lg:items-center">
          <div>
            <div className="text-cc-nav-label mb-3 font-mono text-xs tracking-widest uppercase">
              Enterprise
            </div>
            <h2
              id="enterprise-heading"
              className="text-cc-heading font-heading text-3xl leading-tight font-semibold tracking-tight sm:text-4xl"
            >
              A 24-hour critical SLA, a phone number, and an account manager who
              picks up.
            </h2>
            <p className="text-cc-prose mt-4 text-base">
              Enterprise wraps the whole organization, unlimited critical
              incidents, ten non-critical incidents per month, status reviews,
              and an SLA tailored to your operating calendar. Tell us how your
              business runs and we will write it into the contract.
            </p>
            <div className="mt-8 flex flex-col gap-3 sm:flex-row">
              <SolidButton href="/services/support/contact?plan=Enterprise">
                Talk to us about Enterprise
              </SolidButton>
              <OutlineButton href="#compare">See what is in it</OutlineButton>
            </div>
          </div>
          <ul className="border-cc-card-border grid grid-cols-2 gap-px overflow-hidden rounded-xl border bg-[var(--color-cc-card-border)]">
            {[
              { label: "Critical SLA", value: "24h" },
              { label: "Non-critical SLA", value: "Next BD" },
              { label: "Critical incidents", value: "Unlimited" },
              { label: "Non-critical / mo", value: "10" },
              { label: "Phone support", value: "Yes" },
              { label: "Account manager", value: "Dedicated" },
            ].map((stat) => (
              <li
                key={stat.label}
                className="bg-cc-surface flex flex-col gap-1 p-5"
              >
                <span className="text-cc-nav-label font-mono text-[10px] tracking-widest uppercase">
                  {stat.label}
                </span>
                <span className="text-cc-heading font-heading text-xl font-semibold">
                  {stat.value}
                </span>
              </li>
            ))}
          </ul>
        </div>
      </div>
    </section>
  );
}

function Faq() {
  return (
    <section id="faq" aria-labelledby="faq-heading" className="py-16">
      <header className="mx-auto mb-10 max-w-3xl text-center">
        <div className="text-cc-nav-label mb-3 font-mono text-xs tracking-widest uppercase">
          The fine print, in plain words
        </div>
        <h2
          id="faq-heading"
          className="text-cc-heading font-heading text-3xl leading-tight font-semibold tracking-tight sm:text-4xl"
        >
          Questions we get before signing.
        </h2>
      </header>
      <dl className="border-cc-card-border bg-cc-card-bg/40 divide-cc-card-border divide-y rounded-2xl border">
        {FAQ.map((item) => (
          <div
            key={item.q}
            className="grid gap-4 p-6 sm:grid-cols-[1fr_1.6fr] sm:p-8"
          >
            <dt className="text-cc-heading font-heading text-lg font-semibold">
              {item.q}
            </dt>
            <dd className="text-cc-prose text-sm sm:text-base">{item.a}</dd>
          </div>
        ))}
      </dl>
    </section>
  );
}

function ClosingCta() {
  return (
    <section aria-labelledby="closing-heading" className="py-16">
      <div className="border-cc-card-border bg-cc-card-bg/50 rounded-2xl border p-10 text-center sm:p-14">
        <h2
          id="closing-heading"
          className="text-cc-heading font-heading mx-auto max-w-2xl text-3xl leading-tight font-semibold tracking-tight sm:text-4xl"
        >
          Ready to put a real SLA behind your GraphQL stack?
        </h2>
        <p className="text-cc-ink-dim mx-auto mt-4 max-w-xl text-base">
          Start in our free Slack to feel the room, or contact us when you need
          the response window in writing.
        </p>
        <div className="mt-8 flex flex-col items-center justify-center gap-3 sm:flex-row">
          <SolidButton href="/services/support/contact?plan=Business">
            Contact support sales
          </SolidButton>
          <OutlineButton href="https://slack.chillicream.com/">
            Join the community Slack
          </OutlineButton>
        </div>
      </div>
    </section>
  );
}

// ---------------------------------------------------------------------------
// Page
// ---------------------------------------------------------------------------

export default function SupportPlanPromiseForwardPage() {
  return (
    <>
      <Hero />
      <ResponsePromises />
      <Plans />
      <CriticalitySigns />
      <Comparison />
      <EnterpriseBand />
      <Faq />
      <ClosingCta />
    </>
  );
}

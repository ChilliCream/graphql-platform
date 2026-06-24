import type { Metadata } from "next";
import type { ReactNode } from "react";

import { CheckIcon } from "@/src/components/CheckIcon";
import { OutlineButton, SolidButton } from "@/src/design-system/Button";

export const metadata: Metadata = {
  title: "GraphQL Support Plans, Written Like Docs | ChilliCream",
  description:
    "GraphQL support plans for Hot Chocolate, Fusion, and Nitro, written as a reference: response times, channels, incident limits, and SLAs per plan tier.",
  keywords: [
    "GraphQL support",
    "Hot Chocolate support",
    "Nitro support",
    "GraphQL SLA",
    "enterprise GraphQL support",
    "ChilliCream support plans",
  ],
  openGraph: {
    title: "GraphQL Support Plans, Written Like Docs | ChilliCream",
    description:
      "GraphQL support plans for Hot Chocolate, Fusion, and Nitro, written as a reference: response times, channels, incident limits, and SLAs per plan tier.",
  },
  robots: { index: false, follow: false },
};

// Accent for this variant: cyan #16b9e4 (from the brand spectrum). Applied
// only to: active TOC rail, plan price chips, and the Enterprise callout strip.
const ACCENT = "#16b9e4";

type PlanName = "Community" | "Startup" | "Business" | "Enterprise";

interface Plan {
  readonly name: PlanName;
  readonly price: string;
  readonly priceNote?: string;
  readonly tagline: string;
  readonly description: string;
  readonly perks: readonly string[];
  readonly cta: { readonly label: string; readonly href: string };
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

const RESPONSE_GROUP: ComparisonGroup = {
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
};

const CHANNELS_GROUP: ComparisonGroup = {
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
};

const STRATEGIC_GROUP: ComparisonGroup = {
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
};

const COMPARISON: readonly ComparisonGroup[] = [
  RESPONSE_GROUP,
  CHANNELS_GROUP,
  STRATEGIC_GROUP,
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

interface TocEntry {
  readonly num: string;
  readonly id: string;
  readonly label: string;
}

const TOC: readonly TocEntry[] = [
  { num: "01", id: "overview", label: "Overview" },
  { num: "02", id: "plans", label: "Plans" },
  { num: "03", id: "response", label: "Response & incidents" },
  { num: "04", id: "channels", label: "Channels" },
  { num: "05", id: "strategic", label: "Strategic add-ons" },
  { num: "06", id: "matrix", label: "Full comparison" },
  { num: "07", id: "faq", label: "FAQ" },
];

export default function SupportPreviewV4Page() {
  return (
    <div className="py-16 sm:py-20">
      <Hero />
      <InlineToc />
      <div className="mt-12 grid grid-cols-1 gap-12 lg:grid-cols-[240px_minmax(0,1fr)] lg:gap-16">
        <SidebarToc />
        <article className="max-w-[720px] min-w-0">
          <Section entry={TOC[0]}>
            <OverviewSection />
          </Section>
          <Section entry={TOC[1]}>
            <PlansSection />
          </Section>
          <Section entry={TOC[2]}>
            <ResponseSection />
          </Section>
          <Section entry={TOC[3]}>
            <ChannelsSection />
          </Section>
          <Section entry={TOC[4]}>
            <StrategicSection />
          </Section>
          <Section entry={TOC[5]}>
            <MatrixSection />
          </Section>
          <Section entry={TOC[6]}>
            <FaqContent />
          </Section>
          <ClosingSection />
        </article>
      </div>
    </div>
  );
}

function Hero() {
  return (
    <header className="border-cc-card-border border-b pb-10">
      <div className="text-cc-nav-label font-mono text-xs font-semibold tracking-widest uppercase">
        Support / Reference
      </div>
      <h1 className="font-heading text-cc-heading text-hero mt-5 max-w-4xl font-semibold tracking-tight">
        GraphQL support plans, written like docs.
      </h1>
      <p className="text-cc-prose text-lead mt-6 max-w-2xl">
        A reference for how ChilliCream supports Hot Chocolate, Fusion, and
        Nitro in production. Response times, channels, and incident limits per
        plan, laid out the way engineers read them.
      </p>
      <div className="mt-8 flex flex-wrap items-center gap-3">
        <SolidButton href="#plans">Read the plans</SolidButton>
        <OutlineButton href="/services/support/contact">
          Talk to us
        </OutlineButton>
      </div>
    </header>
  );
}

function InlineToc() {
  return (
    <nav
      aria-label="On this page"
      className="border-cc-card-border -mx-4 mt-10 overflow-x-auto border-y px-4 py-3 lg:hidden"
    >
      <div className="text-cc-nav-label mb-2 font-mono text-xs font-semibold tracking-widest uppercase">
        On this page
      </div>
      <ul className="flex items-center gap-2 whitespace-nowrap">
        {TOC.map((entry) => (
          <li key={entry.id}>
            <a
              href={`#${entry.id}`}
              className="border-cc-card-border hover:border-cc-card-border-hover hover:text-cc-heading text-cc-ink-dim inline-flex items-center gap-2 rounded-full border px-3 py-1.5 font-mono text-xs tracking-widest uppercase transition-colors"
            >
              <span style={{ color: ACCENT }}>{entry.num}</span>
              <span>{entry.label}</span>
            </a>
          </li>
        ))}
      </ul>
    </nav>
  );
}

function SidebarToc() {
  return (
    <aside
      aria-label="Section index"
      className="hidden lg:sticky lg:top-24 lg:block lg:h-fit lg:self-start"
    >
      <div className="text-cc-nav-label mb-4 font-mono text-xs font-semibold tracking-widest uppercase">
        Contents
      </div>
      <ol className="border-cc-card-border border-l">
        {TOC.map((entry, index) => {
          const isActive = index === 0;
          return (
            <li key={entry.id} className="relative">
              {isActive && (
                <span
                  aria-hidden
                  className="absolute top-0 bottom-0 -left-px w-px"
                  style={{ backgroundColor: ACCENT }}
                />
              )}
              <a
                href={`#${entry.id}`}
                className="hover:text-cc-heading group flex items-baseline gap-3 py-2 pl-4 font-mono text-xs tracking-widest uppercase transition-colors"
                style={{
                  color: isActive ? ACCENT : undefined,
                }}
              >
                <span
                  className="text-cc-ink-faint group-hover:text-cc-ink-dim"
                  style={{ color: isActive ? ACCENT : undefined }}
                >
                  {entry.num}
                </span>
                <span
                  className={
                    isActive
                      ? ""
                      : "text-cc-ink-dim group-hover:text-cc-heading"
                  }
                  style={{ color: isActive ? ACCENT : undefined }}
                >
                  {entry.label}
                </span>
              </a>
            </li>
          );
        })}
      </ol>
    </aside>
  );
}

interface SectionProps {
  readonly entry: TocEntry;
  readonly children: ReactNode;
}

function Section({ entry, children }: SectionProps) {
  return (
    <section
      id={entry.id}
      className="border-cc-card-border scroll-mt-24 border-t py-12 first:border-t-0 first:pt-0"
    >
      <SectionEyebrow entry={entry} />
      {children}
    </section>
  );
}

function SectionEyebrow({ entry }: { readonly entry: TocEntry }) {
  return (
    <div className="text-cc-ink-dim mb-3 font-mono text-xs font-semibold tracking-widest uppercase">
      <span style={{ color: ACCENT }}>{entry.num}</span>
      <span className="text-cc-ink-faint mx-2">/</span>
      <span>{entry.label}</span>
    </div>
  );
}

function H2({ children }: { readonly children: ReactNode }) {
  return (
    <h2 className="font-heading text-cc-heading text-h3 font-semibold tracking-tight">
      {children}
    </h2>
  );
}

function OverviewSection() {
  return (
    <>
      <H2>Overview</H2>
      <p className="text-cc-prose text-body mt-4 leading-relaxed">
        ChilliCream offers four support plans for teams running Hot Chocolate,
        Fusion, and Nitro. The Community plan is free and lives in public Slack.
        Startup, Business, and Enterprise are paid, staffed by ChilliCream
        engineers, and come with named channels and written response floors.
      </p>
      <dl className="divide-cc-card-border border-cc-card-border mt-8 divide-y border-y font-mono text-xs">
        <FrontmatterRow
          term="Plans"
          value="4 (Community, Startup, Business, Enterprise)"
        />
        <FrontmatterRow term="Channels" value="Slack, Email, Phone" />
        <FrontmatterRow
          term="Response floor"
          value="24h critical (Enterprise)"
        />
        <FrontmatterRow term="Billing" value="Monthly USD, excl. taxes" />
      </dl>
    </>
  );
}

function FrontmatterRow({
  term,
  value,
}: {
  readonly term: string;
  readonly value: string;
}) {
  return (
    <div className="grid grid-cols-[140px_minmax(0,1fr)] gap-4 py-3">
      <dt className="text-cc-nav-label tracking-widest uppercase">{term}</dt>
      <dd className="text-cc-prose normal-case">{value}</dd>
    </div>
  );
}

function PlansSection() {
  return (
    <>
      <H2>Plans</H2>
      <p className="text-cc-prose text-body mt-4 leading-relaxed">
        Four plans, priced flat per month, with everything you need to know
        listed below. The price chip is the canonical price. Perks are
        cumulative across paid tiers.
      </p>
      <dl className="border-cc-card-border divide-cc-card-border mt-8 divide-y border-y">
        {PLANS.map((plan) => (
          <PlanEntry key={plan.name} plan={plan} />
        ))}
      </dl>
      <p className="text-cc-ink-dim text-caption mt-4">
        Prices in USD. Excludes applicable taxes.
      </p>
    </>
  );
}

function PlanEntry({ plan }: { readonly plan: Plan }) {
  return (
    <div className="grid gap-3 py-6 sm:grid-cols-[minmax(0,1fr)_auto] sm:items-baseline sm:gap-6">
      <dt>
        <div className="flex items-baseline gap-3">
          <span className="font-heading text-cc-heading text-h6 font-semibold tracking-tight">
            {plan.name}
          </span>
          <span className="text-cc-ink-dim text-caption">{plan.tagline}</span>
        </div>
      </dt>
      <dd className="justify-self-start sm:justify-self-end">
        <PriceChip price={plan.price} note={plan.priceNote} />
      </dd>
      <dd className="sm:col-span-2">
        <p className="text-cc-prose text-body leading-relaxed">
          {plan.description}
        </p>
        <ul className="mt-4 space-y-2">
          {plan.perks.map((perk) => (
            <li key={perk} className="text-body flex items-start gap-3">
              <span
                className="mt-[3px] inline-flex shrink-0"
                style={{ color: ACCENT }}
                aria-hidden
              >
                <CheckIcon size={16} />
              </span>
              <span className="text-cc-prose">{perk}</span>
            </li>
          ))}
        </ul>
        <p className="mt-4">
          <a
            href={plan.cta.href}
            className="hover:text-cc-heading text-cc-ink-dim inline-flex items-baseline gap-2 font-mono text-xs tracking-widest uppercase transition-colors"
          >
            <span style={{ color: ACCENT }}>See also</span>
            <span aria-hidden style={{ color: ACCENT }}>
              &rarr;
            </span>
            <span>{plan.cta.label}</span>
          </a>
        </p>
      </dd>
    </div>
  );
}

function PriceChip({
  price,
  note,
}: {
  readonly price: string;
  readonly note?: string;
}) {
  return (
    <span
      className="inline-flex items-baseline gap-2 rounded-md border px-2.5 py-1 font-mono text-xs font-semibold tracking-wider uppercase"
      style={{ borderColor: `${ACCENT}55`, color: ACCENT }}
    >
      <span>{price}</span>
      {note && (
        <span className="text-cc-ink-dim font-normal tracking-normal normal-case">
          {note}
        </span>
      )}
    </span>
  );
}

function ResponseSection() {
  return (
    <>
      <H2>Response & incidents</H2>
      <p className="text-cc-prose text-body mt-4 leading-relaxed">
        Response floors describe the worst case. We routinely answer faster, but
        we will not commit to less than what is written here. Critical means
        production is impacted: down, data loss, or hard outage. Non-critical
        means it blocks work but not production.
      </p>
      <ReferenceTable group={RESPONSE_GROUP} />
    </>
  );
}

function ChannelsSection() {
  return (
    <>
      <H2>Channels</H2>
      <p className="text-cc-prose text-body mt-4 leading-relaxed">
        Every paid plan gets a private Slack channel staffed by ChilliCream
        engineers. Business and Enterprise add an issue tracking board and
        email; Enterprise adds phone support for the calls that should not wait
        for a ticket.
      </p>
      <ReferenceTable group={CHANNELS_GROUP} />
    </>
  );
}

function StrategicSection() {
  return (
    <>
      <H2>Strategic add-ons</H2>
      <p className="text-cc-prose text-body mt-4 leading-relaxed">
        Two perks are Enterprise only: a dedicated account manager who knows
        your topology and recurring status reviews that cover roadmap, upgrades,
        and posture.
      </p>
      <div className="mt-6 space-y-3">
        <EnterpriseCallout title="Dedicated account manager">
          One named contact who owns the relationship end to end and knows the
          stack you run.
        </EnterpriseCallout>
        <EnterpriseCallout title="Status reviews">
          Recurring check-ins on roadmap, upgrades, and posture.
        </EnterpriseCallout>
      </div>
    </>
  );
}

function EnterpriseCallout({
  title,
  children,
}: {
  readonly title: string;
  readonly children: ReactNode;
}) {
  return (
    <div
      className="border-cc-card-border bg-cc-card-bg border border-l-2 py-4 pr-5 pl-5"
      style={{ borderLeftColor: ACCENT }}
    >
      <div className="mb-1 font-mono text-xs font-semibold tracking-widest uppercase">
        <span style={{ color: ACCENT }}>Enterprise only</span>
      </div>
      <div className="font-heading text-cc-heading text-h6 font-semibold tracking-tight">
        {title}
      </div>
      <p className="text-cc-prose text-body mt-1 leading-relaxed">{children}</p>
    </div>
  );
}

function ReferenceTable({ group }: { readonly group: ComparisonGroup }) {
  return (
    <div className="mt-6 overflow-x-auto">
      <table className="text-body w-full min-w-[640px] border-collapse">
        <thead>
          <tr className="border-cc-card-border border-b">
            <th
              scope="col"
              className="text-cc-nav-label py-3 pr-4 text-left font-mono text-xs font-semibold tracking-widest uppercase"
            >
              {group.title}
            </th>
            {PLAN_NAMES.map((name) => (
              <th
                key={name}
                scope="col"
                className="text-cc-nav-label px-3 py-3 text-left font-mono text-xs font-semibold tracking-widest uppercase"
              >
                {name}
              </th>
            ))}
          </tr>
        </thead>
        <tbody>
          {group.rows.map((row) => (
            <tr key={row.title} className="border-cc-card-border border-b">
              <th
                scope="row"
                className="py-4 pr-4 text-left align-top font-normal"
              >
                <div className="text-cc-heading font-medium">{row.title}</div>
                {row.hint && (
                  <div className="text-cc-ink-dim text-caption mt-1 leading-relaxed">
                    {row.hint}
                  </div>
                )}
              </th>
              {row.values.map((value, index) => (
                <td
                  key={PLAN_NAMES[index]}
                  className="px-3 py-4 text-left align-top"
                >
                  <CellValueView value={value} />
                </td>
              ))}
            </tr>
          ))}
        </tbody>
      </table>
    </div>
  );
}

function CellValueView({ value }: { readonly value: CellValue }) {
  if (value === true) {
    return (
      <span
        className="inline-flex items-center"
        style={{ color: ACCENT }}
        aria-label="Included"
      >
        <CheckIcon size={16} />
      </span>
    );
  }
  if (value === false) {
    return (
      <span
        className="text-cc-ink-faint inline-flex items-center"
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
  return <span className="text-cc-prose text-caption">{value}</span>;
}

function MatrixSection() {
  return (
    <>
      <H2>Full comparison</H2>
      <p className="text-cc-prose text-body mt-4 leading-relaxed">
        The canonical table. Every row in the sections above appears here, in
        one place, the way an API reference lists every method.
      </p>
      <div className="border-cc-card-border mt-6 max-h-[640px] overflow-auto border-y">
        <table className="text-body w-full min-w-[720px] border-collapse">
          <thead className="bg-cc-bg sticky top-0">
            <tr className="border-cc-card-border border-b">
              <th
                scope="col"
                className="text-cc-nav-label bg-cc-bg py-3 pr-4 pl-1 text-left font-mono text-xs font-semibold tracking-widest uppercase"
              >
                Feature
              </th>
              {PLAN_NAMES.map((name) => (
                <th
                  key={name}
                  scope="col"
                  className="text-cc-nav-label bg-cc-bg px-3 py-3 text-left font-mono text-xs font-semibold tracking-widest uppercase"
                >
                  {name}
                </th>
              ))}
            </tr>
          </thead>
          <tbody>
            {COMPARISON.map((group) => (
              <MatrixGroup key={group.title} group={group} />
            ))}
          </tbody>
        </table>
      </div>
      <p className="text-cc-ink-dim text-caption mt-3">
        Scrolls horizontally on narrow screens. Header row stays pinned inside
        the table region.
      </p>
    </>
  );
}

function MatrixGroup({ group }: { readonly group: ComparisonGroup }) {
  return (
    <>
      <tr>
        <th
          scope="rowgroup"
          colSpan={5}
          className="text-cc-nav-label border-cc-card-border border-y pt-6 pr-4 pb-2 pl-1 text-left font-mono text-xs font-semibold tracking-widest uppercase"
        >
          {group.title}
        </th>
      </tr>
      {group.rows.map((row) => (
        <tr key={row.title} className="border-cc-card-border border-b">
          <th
            scope="row"
            className="py-3 pr-4 pl-1 text-left align-top font-normal"
          >
            <div className="text-cc-heading font-medium">{row.title}</div>
            {row.hint && (
              <div className="text-cc-ink-dim text-caption mt-1 leading-relaxed">
                {row.hint}
              </div>
            )}
          </th>
          {row.values.map((value, index) => (
            <td
              key={PLAN_NAMES[index]}
              className="px-3 py-3 text-left align-top"
            >
              <CellValueView value={value} />
            </td>
          ))}
        </tr>
      ))}
    </>
  );
}

function FaqContent() {
  return (
    <>
      <H2>FAQ</H2>
      <p className="text-cc-prose text-body mt-4 leading-relaxed">
        The questions buyers ask before they sign, answered straight.
      </p>
      <div className="border-cc-card-border divide-cc-card-border mt-6 divide-y border-y">
        {FAQ.map((item) => (
          <details key={item.q} className="group py-5" name="support-faq-v4">
            <summary className="flex cursor-pointer list-none items-start justify-between gap-6">
              <span className="text-cc-heading text-h6 font-medium">
                {item.q}
              </span>
              <span
                className="text-cc-ink-dim mt-1 inline-flex shrink-0 transition-transform group-open:rotate-45"
                aria-hidden
              >
                <PlusGlyph />
              </span>
            </summary>
            <p className="text-cc-prose text-body mt-3 pr-10 leading-relaxed">
              {item.a}
            </p>
          </details>
        ))}
      </div>
    </>
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

function ClosingSection() {
  return (
    <section className="border-cc-card-border scroll-mt-24 border-t py-12">
      <H2>Next steps</H2>
      <p className="text-cc-prose text-body mt-4 max-w-2xl leading-relaxed">
        Pick a plan, or talk to us about a custom Enterprise SLA. The community
        Slack is open to everyone in the meantime.
      </p>
      <div className="mt-6 flex flex-wrap items-center gap-3">
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

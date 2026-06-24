import type { Metadata } from "next";
import type { ReactNode } from "react";

import { CheckIcon } from "@/src/components/CheckIcon";

const DESCRIPTION =
  "GraphQL support plans for Hot Chocolate, Fusion, and Nitro: Community Slack, Startup at $450/mo, Business at $1,300/mo, Enterprise with 24h SLA and named AM.";

export const metadata: Metadata = {
  title: "GraphQL Support Plans | ChilliCream",
  description: DESCRIPTION,
  keywords: [
    "GraphQL support",
    "Hot Chocolate support",
    "Nitro support",
    "GraphQL SLA",
    "enterprise GraphQL support",
    "ChilliCream support plans",
  ],
  openGraph: {
    title: "GraphQL Support Plans | ChilliCream",
    description: DESCRIPTION,
  },
  robots: { index: false, follow: false },
};

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
    tagline: "for hackers and side projects",
    description: "For personal or non-commercial projects, to start hacking.",
    perks: ["Public Slack Channel"],
    cta: { label: "join slack", href: "https://slack.chillicream.com/" },
  },
  {
    name: "Startup",
    price: "$450",
    priceNote: "/mo",
    tagline: "small teams, steady cadence",
    description:
      "For small teams with moderate bandwidth and projects of low to medium complexity.",
    perks: ["Private Slack Channel", "2 critical incidents"],
    cta: {
      label: "contact",
      href: "/services/support/contact?plan=Startup",
    },
  },
  {
    name: "Business",
    price: "$1,300",
    priceNote: "/mo",
    tagline: "most popular",
    description: "For larger teams with business-critical projects.",
    perks: [
      "Private Slack Channel",
      "5 critical incidents",
      "2 non-critical incidents",
      "Email support",
    ],
    cta: {
      label: "contact",
      href: "/services/support/contact?plan=Business",
    },
  },
  {
    name: "Enterprise",
    price: "Custom",
    tagline: "whole-org coverage with SLAs",
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
      label: "contact",
      href: "/services/support/contact?plan=Enterprise",
    },
  },
];

type CellValue = boolean | string;

interface ComparisonRow {
  readonly title: string;
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
    title: "response & incidents",
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
    title: "channels",
    rows: [
      { title: "Public Slack Channel", values: [true, true, true, true] },
      { title: "Private Slack Channel", values: [false, true, true, true] },
      {
        title: "Private Issue Tracking Board",
        values: [false, false, true, true],
      },
      { title: "Email Support", values: [false, false, true, true] },
      { title: "Phone Support", values: [false, false, false, true] },
    ],
  },
  {
    title: "strategic",
    rows: [
      {
        title: "Dedicated Account Manager",
        values: [false, false, false, true],
      },
      { title: "Status Reviews", values: [false, false, false, true] },
    ],
  },
];

interface SlaRow {
  readonly plan: PlanName;
  readonly critical: string;
  readonly nonCritical: string;
}

const SLA_ROWS: readonly SlaRow[] = [
  { plan: "Community", critical: "best-effort", nonCritical: "best-effort" },
  { plan: "Startup", critical: "next business day", nonCritical: "n/a" },
  {
    plan: "Business",
    critical: "next business day",
    nonCritical: "3 business days",
  },
  {
    plan: "Enterprise",
    critical: "24 hours, any day",
    nonCritical: "next business day",
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

interface GlossaryTerm {
  readonly term: string;
  readonly definition: string;
}

const GLOSSARY: readonly GlossaryTerm[] = [
  {
    term: "critical",
    definition:
      "Production is impacted: the service is down, returning wrong data, or otherwise hard-blocked. A live user experience is degraded.",
  },
  {
    term: "non-critical",
    definition:
      "Bugs and questions that block work but not production. Local development issues, integration questions, and design reviews live here.",
  },
];

interface EnterpriseFact {
  readonly key: string;
  readonly value: string;
}

const ENTERPRISE_FACTS: readonly EnterpriseFact[] = [
  { key: "critical sla", value: "24 hours, any day" },
  { key: "account manager", value: "named, dedicated" },
  { key: "phone support", value: "included" },
  { key: "status reviews", value: "recurring" },
];

export default function SupportPreviewV5Page() {
  return (
    <div className="mx-auto w-full max-w-6xl px-4 py-12 sm:px-6 sm:py-16">
      <Masthead />
      <Block id="01" label="plans" title="graphql support plans">
        <PlanLedger />
      </Block>
      <Block id="02" label="sla" title="response window quick reference">
        <SlaReference />
      </Block>
      <Block id="03" label="matrix" title="feature comparison matrix">
        <ComparisonMatrix />
      </Block>
      <Block id="04" label="glossary" title="incident classification">
        <IncidentGlossary />
      </Block>
      <Block id="05" label="faq" title="frequently asked questions">
        <FaqList />
      </Block>
      <Block id="06" label="enterprise" title="enterprise addendum">
        <EnterpriseAddendum />
      </Block>
      <Block id="07" label="contact" title="contact" hideHeader>
        <ContactFooter />
      </Block>
    </div>
  );
}

function Masthead() {
  return (
    <header className="mb-12 grid gap-6 sm:mb-16 sm:grid-cols-[1fr_auto] sm:items-end">
      <div>
        <div className="text-cc-nav-label text-caption font-mono font-semibold tracking-widest uppercase">
          {"// chillicream / services / support"}
        </div>
        <h1 className="font-heading text-cc-heading text-h2 mt-4 font-semibold tracking-tight">
          GraphQL support plans, documented.
        </h1>
        <p className="text-cc-prose text-body mt-4 max-w-2xl leading-relaxed">
          A reference for the four ChilliCream support plans for Hot Chocolate,
          Fusion, and Nitro. Response windows, channels, incident counts, and
          strategic perks, written down so you can plan a budget around them.
        </p>
      </div>
      <dl className="text-cc-ink-dim text-caption grid grid-cols-2 gap-x-6 gap-y-1 font-mono tracking-wide sm:text-right">
        <dt className="text-cc-nav-label uppercase">last-updated:</dt>
        <dd className="text-cc-prose tabular-nums">2026-06-22</dd>
        <dt className="text-cc-nav-label uppercase">currency:</dt>
        <dd className="text-cc-prose">USD</dd>
        <dt className="text-cc-nav-label uppercase">version:</dt>
        <dd className="text-cc-prose tabular-nums">v5.registry</dd>
      </dl>
    </header>
  );
}

interface BlockProps {
  readonly id: string;
  readonly label: string;
  readonly title: string;
  readonly hideHeader?: boolean;
  readonly children: ReactNode;
}

function Block({ id, label, title, hideHeader, children }: BlockProps) {
  return (
    <section className="border-cc-card-border border-t py-10 sm:py-12">
      <div className="grid gap-6 sm:grid-cols-[7rem_1fr] sm:gap-8">
        <aside className="sm:pt-1">
          <div className="text-cc-nav-label text-caption font-mono font-semibold tracking-widest uppercase">
            {id} / {label}
          </div>
        </aside>
        <div>
          {!hideHeader && (
            <div className="mb-6">
              <div className="text-cc-ink-dim text-caption font-mono tracking-wide">
                {`// ${id} ${label}`}
              </div>
              <h2 className="font-heading text-cc-heading text-h5 mt-1 font-semibold tracking-tight">
                {title}
              </h2>
            </div>
          )}
          {children}
        </div>
      </div>
    </section>
  );
}

function PlanLedger() {
  return (
    <>
      <div className="border-cc-card-border grid border-t md:grid-cols-4">
        {PLANS.map((plan, idx) => (
          <PlanColumn
            key={plan.name}
            plan={plan}
            isLast={idx === PLANS.length - 1}
          />
        ))}
      </div>
      <p className="text-cc-ink-dim text-caption mt-4 font-mono tracking-wide">
        {"// prices in USD. excludes applicable taxes."}
      </p>
    </>
  );
}

function PlanColumn({
  plan,
  isLast,
}: {
  readonly plan: Plan;
  readonly isLast: boolean;
}) {
  const isBusiness = plan.name === "Business";
  const borderClass = isLast ? "" : "md:border-r border-cc-card-border";
  const businessHairline = isBusiness ? "md:border-x" : "";
  return (
    <article
      className={`flex flex-col gap-3 px-4 py-5 ${borderClass} ${businessHairline}`}
      style={isBusiness ? { borderColor: ACCENT } : undefined}
    >
      <div className="text-cc-nav-label text-caption font-mono font-semibold tracking-widest uppercase">
        {plan.name.toLowerCase()}
      </div>

      <div className="flex items-baseline gap-1">
        <span className="font-heading text-cc-heading text-h3 font-semibold tabular-nums">
          {plan.price}
        </span>
        {plan.priceNote && (
          <span className="text-cc-ink-dim text-caption font-mono">
            {plan.priceNote}
          </span>
        )}
      </div>

      <div className="text-cc-ink-dim text-caption font-mono tracking-wide">
        {plan.tagline}
      </div>

      <p className="text-cc-prose text-caption leading-relaxed">
        {plan.description}
      </p>

      <div className="mt-1">
        <div className="text-cc-nav-label text-caption font-mono tracking-widest uppercase">
          {"// includes"}
        </div>
        <ul className="mt-2 space-y-1">
          {plan.perks.map((perk) => (
            <li
              key={perk}
              className="text-cc-prose text-caption flex items-start gap-2"
            >
              <span className="font-mono" style={{ color: ACCENT }} aria-hidden>
                +
              </span>
              <span>{perk}</span>
            </li>
          ))}
        </ul>
      </div>

      <div className="mt-auto pt-3">
        <a
          href={plan.cta.href}
          className="hover:text-cc-heading text-caption inline-flex items-center gap-1 font-mono font-semibold tracking-widest uppercase transition-colors"
          style={{ color: ACCENT }}
        >
          {plan.cta.label}
          <span aria-hidden>{`->`}</span>
        </a>
      </div>
    </article>
  );
}

function SlaReference() {
  return (
    <div className="border-cc-card-border border-t">
      <div className="border-cc-card-border text-cc-nav-label text-caption grid grid-cols-3 gap-4 border-b px-1 py-2 font-mono font-semibold tracking-widest uppercase">
        <div>plan</div>
        <div>critical</div>
        <div>non-critical</div>
      </div>
      <dl>
        {SLA_ROWS.map((row, idx) => (
          <div
            key={row.plan}
            className={`grid grid-cols-3 gap-4 px-1 py-3 ${
              idx === SLA_ROWS.length - 1
                ? ""
                : "border-cc-card-border border-b"
            }`}
          >
            <dt className="text-cc-heading text-caption font-mono font-semibold tracking-wide">
              {row.plan.toLowerCase()}
            </dt>
            <dd className="text-cc-prose text-caption font-mono tabular-nums">
              {row.critical}
            </dd>
            <dd className="text-cc-prose text-caption font-mono tabular-nums">
              {row.nonCritical}
            </dd>
          </div>
        ))}
      </dl>
    </div>
  );
}

function ComparisonMatrix() {
  return (
    <div className="overflow-x-auto">
      <table className="text-caption w-full border-collapse">
        <thead>
          <tr className="border-cc-card-border border-y">
            <th
              scope="col"
              className="text-cc-nav-label text-caption w-[36%] px-3 py-2 text-left font-mono font-semibold tracking-widest uppercase"
            >
              feature
            </th>
            {PLAN_NAMES.map((name) => {
              const isBusiness = name === "Business";
              return (
                <th
                  key={name}
                  scope="col"
                  className={`text-caption px-3 py-2 text-left font-mono font-semibold tracking-widest uppercase ${
                    isBusiness ? "border-x" : "text-cc-heading"
                  }`}
                  style={
                    isBusiness
                      ? { borderColor: ACCENT, color: ACCENT }
                      : undefined
                  }
                >
                  {name.toLowerCase()}
                </th>
              );
            })}
          </tr>
        </thead>
        {COMPARISON.map((group) => (
          <ComparisonGroupBody key={group.title} group={group} />
        ))}
      </table>
    </div>
  );
}

function ComparisonGroupBody({ group }: { readonly group: ComparisonGroup }) {
  return (
    <tbody>
      <tr>
        <td
          role="presentation"
          colSpan={5}
          className="text-cc-nav-label border-cc-card-border text-caption border-b px-3 pt-5 pb-2 text-left font-mono font-semibold tracking-widest uppercase"
        >
          # {group.title}
        </td>
      </tr>
      {group.rows.map((row) => (
        <tr key={row.title} className="border-cc-card-border border-b">
          <th
            scope="row"
            className="text-cc-prose px-3 py-2 text-left align-middle font-sans font-normal"
            style={{ height: "36px" }}
          >
            {row.title}
          </th>
          {row.values.map((value, index) => {
            const planName = PLAN_NAMES[index];
            const isBusiness = planName === "Business";
            return (
              <td
                key={planName}
                className={`px-3 py-2 text-left align-middle ${
                  isBusiness ? "border-x" : ""
                }`}
                style={{
                  height: "36px",
                  ...(isBusiness ? { borderColor: ACCENT } : {}),
                }}
              >
                <ComparisonValue value={value} />
              </td>
            );
          })}
        </tr>
      ))}
    </tbody>
  );
}

function ComparisonValue({ value }: { readonly value: CellValue }) {
  if (value === true) {
    return (
      <span
        className="inline-flex items-center"
        style={{ color: ACCENT }}
        aria-label="Included"
      >
        <CheckIcon size={14} />
      </span>
    );
  }
  if (value === false) {
    return (
      <span
        className="text-cc-ink-faint font-mono tabular-nums"
        aria-label="Not included"
      >
        {`–`}
      </span>
    );
  }
  return (
    <span className="text-cc-prose text-caption font-mono tabular-nums">
      {value}
    </span>
  );
}

function IncidentGlossary() {
  return (
    <dl className="border-cc-card-border border-t">
      {GLOSSARY.map((item, idx) => (
        <div
          key={item.term}
          className={`grid gap-3 px-1 py-4 sm:grid-cols-[10rem_1fr] sm:gap-8 ${
            idx === GLOSSARY.length - 1 ? "" : "border-cc-card-border border-b"
          }`}
        >
          <dt
            className="text-caption font-mono font-semibold tracking-widest uppercase"
            style={{ color: ACCENT }}
          >
            {item.term}
          </dt>
          <dd className="text-cc-prose text-body leading-relaxed">
            {item.definition}
          </dd>
        </div>
      ))}
    </dl>
  );
}

function FaqList() {
  return (
    <div className="border-cc-card-border border-t">
      {FAQ.map((item, idx) => (
        <details
          key={item.q}
          name="support-faq-v5"
          className={`group px-1 py-4 ${
            idx === FAQ.length - 1 ? "" : "border-cc-card-border border-b"
          }`}
        >
          <summary className="flex cursor-pointer list-none items-start justify-between gap-6">
            <span className="flex items-start gap-2">
              <span
                className="text-caption font-mono font-semibold tracking-widest uppercase"
                style={{ color: ACCENT }}
                aria-hidden
              >
                Q.
              </span>
              <span className="text-cc-heading text-caption font-mono font-semibold tracking-widest uppercase">
                {item.q}
              </span>
            </span>
            <span
              className="text-cc-ink-dim text-caption font-mono tabular-nums"
              aria-hidden
            >
              <span className="group-open:hidden">[+]</span>
              <span className="hidden group-open:inline">[-]</span>
            </span>
          </summary>
          <div className="mt-3 flex items-start gap-2 pr-8">
            <span
              className="text-caption font-mono font-semibold tracking-widest uppercase"
              style={{ color: ACCENT }}
              aria-hidden
            >
              A.
            </span>
            <p className="text-cc-prose text-body leading-relaxed">{item.a}</p>
          </div>
        </details>
      ))}
    </div>
  );
}

function EnterpriseAddendum() {
  return (
    <>
      <p className="text-cc-prose text-body mb-5 max-w-2xl leading-relaxed">
        For organizations running ChilliCream across multiple teams and business
        units. The SLA is shaped around how you ship, and a dedicated account
        manager owns the relationship end to end.
      </p>
      <dl className="border-cc-card-border grid border-t sm:grid-cols-2">
        {ENTERPRISE_FACTS.map((fact, idx) => {
          const isLastRow = idx >= ENTERPRISE_FACTS.length - 2;
          const isRightCol = idx % 2 === 1;
          return (
            <div
              key={fact.key}
              className={`flex items-baseline justify-between gap-4 px-3 py-3 ${
                isLastRow ? "" : "border-cc-card-border border-b"
              } ${isRightCol ? "" : "sm:border-cc-card-border sm:border-r"}`}
            >
              <dt className="text-cc-nav-label text-caption font-mono font-semibold tracking-widest uppercase">
                {fact.key}
              </dt>
              <dd className="text-cc-heading text-caption font-mono tabular-nums">
                {fact.value}
              </dd>
            </div>
          );
        })}
      </dl>
      <div className="text-caption mt-5 flex flex-wrap items-center gap-x-5 gap-y-2 font-mono tracking-widest uppercase">
        <a
          href="/services/support/contact?plan=Enterprise"
          className="hover:text-cc-heading transition-colors"
          style={{ color: ACCENT }}
        >
          contact -&gt;
        </a>
        <span className="text-cc-ink-faint" aria-hidden>
          |
        </span>
        <a
          href="/services/advisory"
          className="hover:text-cc-heading transition-colors"
          style={{ color: ACCENT }}
        >
          pair with advisory -&gt;
        </a>
      </div>
    </>
  );
}

function ContactFooter() {
  return (
    <div className="pt-2">
      <div className="border-cc-card-border border-y px-1 py-4">
        <div className="text-cc-prose text-body font-mono tracking-wide">
          <span className="text-cc-ink-dim">$ </span>
          <span className="text-cc-heading">chillicream support </span>
          <span style={{ color: ACCENT }}>--contact</span>
        </div>
        <div className="text-caption mt-3 flex flex-wrap items-center gap-x-5 gap-y-2 font-mono tracking-widest uppercase">
          <a
            href="/services/support/contact"
            className="hover:text-cc-heading transition-colors"
            style={{ color: ACCENT }}
          >
            sales@chillicream
          </a>
          <span className="text-cc-ink-faint" aria-hidden>
            |
          </span>
          <a
            href="https://slack.chillicream.com/"
            className="hover:text-cc-heading transition-colors"
            style={{ color: ACCENT }}
          >
            join #community-slack
          </a>
        </div>
      </div>
      <div className="text-cc-ink-faint text-caption mt-4 font-mono tracking-widest uppercase">
        {"// eof"}
      </div>
    </div>
  );
}

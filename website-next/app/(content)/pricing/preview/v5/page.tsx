import type { Metadata } from "next";

import { CheckIcon } from "@/src/components/CheckIcon";
import { OutlineButton, SolidButton } from "@/src/design-system/Button";

export const metadata: Metadata = {
  title: "Pricing for Nitro by ChilliCream",
  description:
    "Nitro GraphQL pricing for the ChilliCream platform: start free on shared cloud, scale to a dedicated instance at $400 with SLA and SSO, or self-host on your own infra.",
  keywords: [
    "Nitro GraphQL pricing",
    "Nitro pricing",
    "ChilliCream pricing",
    "GraphQL platform pricing",
    "GraphQL plans",
    "Hot Chocolate",
    "schema registry pricing",
  ],
  openGraph: {
    title: "Pricing for Nitro by ChilliCream",
    description:
      "Nitro pricing: a free shared tier, a dedicated cloud at $400/mo with SLA and SSO, or a self-hosted plan on your own infrastructure.",
  },
  robots: { index: false, follow: false },
};

interface Plan {
  readonly id: "shared" | "dedicated" | "self";
  readonly name: string;
  readonly tagline: string;
  readonly price: string;
  readonly priceNote: string;
  readonly description: string;
  readonly features: readonly string[];
  readonly cta: string;
  readonly ctaHref: string;
  readonly editorsPick?: boolean;
}

const PLANS: readonly Plan[] = [
  {
    id: "shared",
    name: "Shared Instance",
    tagline: "Shared resources, fully managed.",
    price: "Free",
    priceNote: "pay-as-you-go",
    description:
      "A multi-tenant cloud region for teams that want to ship without standing anything up. One schema, three environments, and 5M operations a month at no cost. After that you pay only for what you use.",
    features: [
      "Multi-tenant cloud region",
      "1 Schema, 3 Environments",
      "Up to 5M ops per month included",
      "Community Slack support",
      "Pay only for what you use after",
    ],
    cta: "Start for Free",
    ctaHref: "/get-started",
  },
  {
    id: "dedicated",
    name: "Dedicated Instance",
    tagline: "Dedicated resources, fully managed.",
    price: "$400",
    priceNote: "per month",
    description:
      "A single-tenant cloud region, your choice of cloud, with private networking. SLA, SSO, audit log, and role-based access on day one. The plan most platform teams settle on when Nitro is in production.",
    features: [
      "Single-tenant cloud region",
      "Unlimited schemas",
      "BYOC region, private networking",
      "99.95% SLA, email and private chat",
      "SSO, audit log, role-based access",
    ],
    cta: "Start for Free",
    ctaHref: "/get-started",
    editorsPick: true,
  },
  {
    id: "self",
    name: "Self-Hosted",
    tagline: "Self managed, on your infrastructure.",
    price: "Custom",
    priceNote: "talk to us",
    description:
      "Run the full Nitro control plane on your own infrastructure, including air-gapped environments. Priority engineering support, a long-term release channel, and custom onboarding for regulated rollouts.",
    features: [
      "Run on your own infrastructure",
      "Air-gapped and on-prem supported",
      "Priority engineering support",
      "Long-term release channel",
      "Custom training and onboarding",
    ],
    cta: "Talk to Us",
    ctaHref: "/services/support/contact",
  },
];

type CellValue = boolean | string;

interface ComparisonRow {
  readonly label: string;
  readonly shared: CellValue;
  readonly dedicated: CellValue;
  readonly self: CellValue;
}

interface ComparisonGroup {
  readonly title: string;
  readonly rows: readonly ComparisonRow[];
}

const COMPARISON: readonly ComparisonGroup[] = [
  {
    title: "Hosting & isolation",
    rows: [
      {
        label: "Deployment model",
        shared: "Multi-tenant cloud",
        dedicated: "Single-tenant cloud or BYOC",
        self: "Your infrastructure, air-gap supported",
      },
      {
        label: "Included APIs / schemas",
        shared: "1 schema",
        dedicated: "Unlimited",
        self: "Unlimited",
      },
      {
        label: "Environments per API",
        shared: "3 (dev / QA / prod)",
        dedicated: "Unlimited, branchable stages",
        self: "Unlimited, branchable stages",
      },
      {
        label: "Included operations / month",
        shared: "5M, pay-as-you-go after",
        dedicated: "Custom volume",
        self: "Unmetered on your infra",
      },
      {
        label: "Log & trace retention",
        shared: "1 day",
        dedicated: "Configurable",
        self: "Your retention policy",
      },
      {
        label: "Fusion gateway runtime",
        shared: "Your ASP.NET Core app",
        dedicated: "Your ASP.NET Core app",
        self: "Your ASP.NET Core app, fully offline",
      },
    ],
  },
  {
    title: "Schema lifecycle",
    rows: [
      {
        label: "Schema registry with history & rollback",
        shared: true,
        dedicated: true,
        self: true,
      },
      {
        label:
          "Client registry, know which published clients are affected by a change",
        shared: true,
        dedicated: true,
        self: true,
      },
      {
        label: "Breaking-change classification (safe / dangerous / breaking)",
        shared: true,
        dedicated: true,
        self: true,
      },
      {
        label: "CI schema & client checks (validate / upload / publish)",
        shared: true,
        dedicated: true,
        self: true,
      },
      {
        label: "Stage promotion with approval gates",
        shared: true,
        dedicated: true,
        self: true,
      },
      {
        label:
          "Fusion composition lifecycle (begin / validate / commit / rollback)",
        shared: true,
        dedicated: true,
        self: true,
      },
      {
        label: ".NET Aspire integration (compose live subgraphs at build)",
        shared: true,
        dedicated: true,
        self: true,
      },
    ],
  },
  {
    title: "Observability",
    rows: [
      {
        label:
          "OpenTelemetry-native traces, metrics, logs (requires Nitro configuration in the server)",
        shared: true,
        dedicated: true,
        self: true,
      },
      {
        label:
          "Operation insights (p95/p99, throughput, error rate, impact score)",
        shared: true,
        dedicated: true,
        self: true,
      },
      {
        label: "Per-client tracking (GraphQL-Client-Id / Version)",
        shared: true,
        dedicated: true,
        self: true,
      },
      {
        label: "Resolver-level insights & sample traces",
        shared: true,
        dedicated: true,
        self: true,
      },
      {
        label: "Distributed tracing across Fusion subgraphs",
        shared: true,
        dedicated: true,
        self: true,
      },
      {
        label: "Service monitoring for any .NET service (REST, gRPC, jobs)",
        shared: true,
        dedicated: true,
        self: true,
      },
      {
        label: "Operation reporting (executed + available, persisted + ad-hoc)",
        shared: true,
        dedicated: true,
        self: true,
      },
    ],
  },
  {
    title: "Operations & delivery",
    rows: [
      {
        label: "Persisted / trusted operations enforcement",
        shared: true,
        dedicated: true,
        self: true,
      },
      {
        label: "Query cost analysis (@cost / @listSize, IBM spec)",
        shared: true,
        dedicated: true,
        self: true,
      },
      {
        label: "Request limits (depth, breadth, recursion, timeouts)",
        shared: true,
        dedicated: true,
        self: true,
      },
      {
        label: "Deployment audit log (every publish)",
        shared: true,
        dedicated: true,
        self: true,
      },
      {
        label: "Rollback by republishing an earlier tag",
        shared: true,
        dedicated: true,
        self: true,
      },
      {
        label:
          "Persisted-op distribution cache (filesystem / Azure Blob / custom)",
        shared: true,
        dedicated: true,
        self: true,
      },
    ],
  },
  {
    title: "Security & access",
    rows: [
      {
        label:
          "Roles (Owner / Admin / Collaborator) + stage-scoped publish permissions",
        shared: true,
        dedicated: true,
        self: true,
      },
      {
        label: "SSO (SAML / OIDC)",
        shared: false,
        dedicated: true,
        self: "Via your IdP",
      },
      {
        label: "Audit log for admin actions",
        shared: false,
        dedicated: true,
        self: "Your retention policy",
      },
      {
        label: "API keys (CI/CD) and PATs (user-bound)",
        shared: true,
        dedicated: true,
        self: true,
      },
      {
        label: "OAuth redirect-URL allowlist (anti token-leak)",
        shared: true,
        dedicated: true,
        self: true,
      },
      {
        label: "ASP.NET Core auth (JWT, cookie, OIDC, mTLS) at the gateway",
        shared: true,
        dedicated: true,
        self: true,
      },
      {
        label: "Authorization (@authorize, roles, policies, OPA)",
        shared: true,
        dedicated: true,
        self: true,
      },
    ],
  },
  {
    title: "Developer experience",
    rows: [
      {
        label: "Built-in GraphQL IDE",
        shared: "Served from your endpoint",
        dedicated: "Served from your endpoint",
        self: "Served from your endpoint",
      },
      {
        label: "MCP server endpoint over Streamable HTTP",
        shared: true,
        dedicated: true,
        self: true,
      },
      {
        label: "MCP feature collections (.graphql + .json + .html)",
        shared: true,
        dedicated: true,
        self: true,
      },
      {
        label: "MCP per-tool telemetry (latency, ops/min, error rate, impact)",
        shared: true,
        dedicated: true,
        self: true,
      },
      {
        label:
          "OpenAPI adapter (@http exposes GraphQL ops as REST + OpenAPI doc)",
        shared: true,
        dedicated: true,
        self: true,
      },
      {
        label:
          "CLI distribution (.NET tool, npm, Homebrew, self-contained binaries)",
        shared: true,
        dedicated: true,
        self: true,
      },
      {
        label: "Mock servers via CLI",
        shared: true,
        dedicated: true,
        self: true,
      },
    ],
  },
  {
    title: "Support & SLAs",
    rows: [
      {
        label: "Support channel",
        shared: "Community Slack",
        dedicated: "Email + private chat",
        self: "Priority engineering",
      },
      {
        label: "Uptime SLA",
        shared: "Best-effort",
        dedicated: "99.95%",
        self: "You operate it",
      },
      {
        label: "Release channel",
        shared: "Continuous",
        dedicated: "Continuous",
        self: "Long-term release channel",
      },
      {
        label: "Onboarding & training",
        shared: "Docs & community",
        dedicated: "Guided onboarding",
        self: "Custom training",
      },
    ],
  },
];

interface FaqItem {
  readonly question: string;
  readonly answer: string;
}

const FAQ: readonly FaqItem[] = [
  {
    question: "Is the Shared Instance really free?",
    answer:
      "Yes. The Shared Instance includes one schema, three environments, and up to 5M operations per month at no cost. Beyond that, you pay only for what you use, billed by metered operations.",
  },
  {
    question: "What does the 99.95% SLA on the Dedicated Instance cover?",
    answer:
      "The 99.95% uptime SLA covers the Nitro control plane on your dedicated instance: schema and client registry, CI checks, the GraphQL IDE that serves from your endpoint, and telemetry ingestion once Nitro is configured. Your own gateway and subgraphs are not part of the SLA.",
  },
  {
    question: "Do you support SSO and audit logs?",
    answer:
      "SSO via OIDC and SAML, role-based access control, and audit log are included on the Dedicated Instance and Self-Hosted plans. The Shared Instance ships basic access control only.",
  },
  {
    question: "Can I bring my own cloud region?",
    answer:
      "Yes. Dedicated Instance customers choose the cloud region the instance runs in (BYOC) and can connect over private networking. Self-Hosted runs wherever you run it, including air-gapped environments.",
  },
  {
    question: "How does a schema change affect my clients?",
    answer:
      "Nitro CI checks compare a new schema against the client registry and report which published clients are affected by a breaking change before you deploy. You decide whether to ship, deprecate, or hold.",
  },
  {
    question: "Can I move between plans later?",
    answer:
      "Yes. You can upgrade from Shared to Dedicated at any time and your schema, environments, and telemetry move with you. Talk to us if you need to migrate to Self-Hosted.",
  },
];

const ENTERPRISE_NOTES: readonly string[] = [
  "Dedicated solution architect",
  "Annual contracts and POs",
  "Security and DPA review",
  "Migration playbooks",
];

export default function PricingPreviewV5Page() {
  return (
    <article className="mx-auto w-full max-w-[68ch] px-4 sm:px-6">
      <Masthead />
      <SectionRule marker="01 / Plans" />
      <PlansDispatch />
      <SectionRule marker="02 / Comparison" />
      <ComparisonDispatch />
      <SectionRule marker="03 / Questions" />
      <FaqDispatch />
      <SectionRule marker="04 / Enterprise" />
      <EnterpriseDispatch />
      <Colophon />
    </article>
  );
}

function Masthead() {
  return (
    <header className="pt-16 pb-12 sm:pt-24 sm:pb-16">
      <p className="text-cc-nav-label font-mono text-xs tracking-[0.18em] uppercase">
        Nitro / Pricing / Dispatch No. 01
      </p>
      <h1 className="font-heading text-cc-heading text-hero mt-8 font-semibold text-balance">
        Pricing that scales with your GraphQL platform.
      </h1>
      <p className="text-cc-ink text-lead dropcap mt-10 text-pretty">
        Start free on the shared cloud. Move to a dedicated instance when you
        need an SLA, SSO, and your own region. Self-host on your own
        infrastructure when the workload, or the policy, demands it. The Nitro
        platform stays the same across all three; what shifts is where it runs
        and what we owe you.
      </p>
      <div className="mt-10 flex flex-wrap items-center gap-3">
        <SolidButton href="/get-started">Start for Free</SolidButton>
        <OutlineButton href="/services/support/contact">
          Talk to Sales
        </OutlineButton>
      </div>
      <style>{`
        .dropcap::first-letter {
          float: left;
          font-family: var(--font-heading, var(--font-sans));
          font-weight: 600;
          color: var(--color-cc-accent, #5eead4);
          font-size: 4.25rem;
          line-height: 0.85;
          padding: 0.35rem 0.65rem 0 0;
        }
      `}</style>
    </header>
  );
}

interface SectionRuleProps {
  readonly marker: string;
}

function SectionRule({ marker }: SectionRuleProps) {
  return (
    <div
      aria-hidden="true"
      className="border-cc-card-border flex items-center gap-4 border-t pt-6"
    >
      <span className="text-cc-nav-label font-mono text-xs tracking-[0.18em] uppercase">
        {marker}
      </span>
      <span className="border-cc-card-border hidden flex-1 border-t sm:block" />
    </div>
  );
}

function PlansDispatch() {
  return (
    <section aria-labelledby="plans-heading" className="py-24 sm:py-32">
      <h2 id="plans-heading" className="sr-only">
        Plans
      </h2>
      <div className="flex flex-col gap-20 sm:gap-24">
        {PLANS.map((plan, index) => (
          <PlanEntry key={plan.id} plan={plan} index={index} />
        ))}
      </div>
    </section>
  );
}

interface PlanEntryProps {
  readonly plan: Plan;
  readonly index: number;
}

function PlanEntry({ plan, index }: PlanEntryProps) {
  const CallToAction = plan.editorsPick ? SolidButton : OutlineButton;
  return (
    <article
      aria-labelledby={`plan-${plan.id}`}
      className={
        index === 0 ? "" : "border-cc-card-border border-t pt-20 sm:pt-24"
      }
    >
      <div className="flex flex-col gap-6 sm:flex-row sm:items-baseline sm:justify-between sm:gap-10">
        <div>
          {plan.editorsPick ? (
            <p className="text-cc-accent font-mono text-[0.65rem] tracking-[0.22em] uppercase">
              Editor&apos;s pick
            </p>
          ) : null}
          <h3
            id={`plan-${plan.id}`}
            className="font-heading text-cc-heading text-h3 mt-2 font-semibold"
          >
            {plan.name}
          </h3>
          <p className="text-cc-nav-label mt-2 font-mono text-xs tracking-[0.18em] uppercase">
            {plan.tagline}
          </p>
        </div>
        <div className="text-left sm:text-right">
          <p className="font-heading text-cc-heading text-h2 leading-none font-semibold">
            {plan.price}
          </p>
          <p className="text-cc-nav-label mt-2 font-mono text-xs tracking-[0.18em] uppercase">
            {plan.priceNote}
          </p>
        </div>
      </div>

      <p className="text-cc-ink text-body mt-8 text-pretty">
        {plan.description}
      </p>

      <ul className="mt-10 grid gap-x-10 sm:grid-cols-2">
        {plan.features.map((feature) => (
          <li
            key={feature}
            className="border-cc-card-border flex items-start gap-3 border-t py-4"
          >
            <span className="text-cc-accent mt-[6px] flex-none">
              <CheckIcon size={14} />
            </span>
            <span className="text-cc-ink text-body">{feature}</span>
          </li>
        ))}
      </ul>

      <div className="mt-10 flex flex-wrap items-center gap-3">
        <CallToAction href={plan.ctaHref}>{plan.cta}</CallToAction>
      </div>
    </article>
  );
}

function ComparisonDispatch() {
  return (
    <section aria-labelledby="compare-heading" className="py-24 sm:py-32">
      <header>
        <h2
          id="compare-heading"
          className="font-heading text-cc-heading text-h3 font-semibold"
        >
          Every capability, side by side.
        </h2>
        <p className="text-cc-ink text-body mt-6 text-pretty">
          The same Nitro platform across all three plans. What changes is where
          it runs, who you share it with, and what you get from us. The matrix
          below is set as a printed feature table, hairline rules only, no card
          chrome.
        </p>
      </header>

      <div className="-mx-4 mt-12 overflow-x-auto sm:-mx-6 lg:-mx-[calc((100vw-68ch)/2)]">
        <div className="min-w-[44rem] px-4 sm:px-6 lg:px-[calc((100vw-68ch)/2)]">
          <table className="w-full border-separate border-spacing-0 text-left">
            <thead>
              <tr>
                <th scope="col" className="w-2/5 pb-5 pl-2 align-bottom">
                  <span className="text-cc-nav-label font-mono text-[0.65rem] tracking-[0.18em] uppercase">
                    Capability
                  </span>
                </th>
                {PLANS.map((plan) => (
                  <th
                    key={plan.id}
                    scope="col"
                    className={`px-4 pb-5 text-center align-bottom ${
                      plan.editorsPick ? "bg-cc-accent/5" : ""
                    }`}
                  >
                    <div
                      className={`font-heading text-cc-heading text-base font-semibold ${
                        plan.editorsPick ? "text-cc-accent" : ""
                      }`}
                    >
                      {plan.name}
                    </div>
                    <div className="text-cc-nav-label mt-2 font-mono text-[0.65rem] tracking-[0.18em] uppercase">
                      {plan.price} · {plan.priceNote}
                    </div>
                  </th>
                ))}
              </tr>
            </thead>
            <tbody>
              {COMPARISON.map((group) => (
                <ComparisonGroupRows key={group.title} group={group} />
              ))}
            </tbody>
          </table>
        </div>
      </div>
    </section>
  );
}

interface ComparisonGroupRowsProps {
  readonly group: ComparisonGroup;
}

function ComparisonGroupRows({ group }: ComparisonGroupRowsProps) {
  return (
    <>
      <tr>
        <th
          scope="colgroup"
          colSpan={4}
          className="border-cc-card-border text-cc-nav-label border-t pt-8 pb-3 pl-2 text-left font-mono text-xs tracking-[0.18em] uppercase"
        >
          {group.title}
        </th>
      </tr>
      {group.rows.map((row) => (
        <tr key={row.label}>
          <th
            scope="row"
            className="border-cc-card-border text-cc-ink text-body border-t py-4 pr-6 pl-2 text-left align-top font-normal"
          >
            {row.label}
          </th>
          <ComparisonCell value={row.shared} />
          <ComparisonCell value={row.dedicated} highlight />
          <ComparisonCell value={row.self} />
        </tr>
      ))}
    </>
  );
}

interface ComparisonCellProps {
  readonly value: CellValue;
  readonly highlight?: boolean;
}

function ComparisonCell({ value, highlight = false }: ComparisonCellProps) {
  return (
    <td
      className={`border-cc-card-border text-body border-t px-4 py-4 text-center align-top ${
        highlight ? "bg-cc-accent/5" : ""
      }`}
    >
      {typeof value === "boolean" ? (
        value ? (
          <span className="text-cc-accent inline-flex" aria-label="Included">
            <CheckIcon size={14} />
          </span>
        ) : (
          <span
            className="text-cc-ink-faint inline-block"
            aria-label="Not included"
          >
            &ndash;
          </span>
        )
      ) : (
        <span className="text-cc-ink">{value}</span>
      )}
    </td>
  );
}

function FaqDispatch() {
  return (
    <section aria-labelledby="faq-heading" className="py-24 sm:py-32">
      <header>
        <h2
          id="faq-heading"
          className="font-heading text-cc-heading text-h3 font-semibold"
        >
          Honest answers about pricing.
        </h2>
        <p className="text-cc-ink text-body mt-6 text-pretty">
          The questions that come up most often, answered the way we would in a
          call with your platform team.
        </p>
      </header>

      <dl className="mt-12">
        {FAQ.map((item, index) => (
          <FaqEntry key={item.question} item={item} first={index === 0} />
        ))}
      </dl>
    </section>
  );
}

interface FaqEntryProps {
  readonly item: FaqItem;
  readonly first: boolean;
}

function FaqEntry({ item, first }: FaqEntryProps) {
  return (
    <div
      className={`border-cc-card-border py-8 ${
        first ? "border-t" : ""
      } border-b`}
    >
      <dt className="font-heading text-cc-heading text-h5 font-semibold">
        {item.question}
      </dt>
      <dd className="text-cc-ink text-lead mt-4 text-pretty">{item.answer}</dd>
    </div>
  );
}

function EnterpriseDispatch() {
  return (
    <section aria-labelledby="enterprise-heading" className="py-24 sm:py-32">
      <h2
        id="enterprise-heading"
        className="font-heading text-cc-heading text-h3 font-semibold"
      >
        A note for platform and security teams.
      </h2>
      <p className="text-cc-ink text-lead mt-6 text-pretty">
        We work directly with platform and security teams on bespoke commercial
        terms, on-prem and air-gapped rollouts, and migrations from existing
        GraphQL gateways. Engineers, not gatekeepers, run the call, and the
        first conversation is about your constraints, not our slide deck.
      </p>

      <ul className="mt-10 grid gap-x-10 sm:grid-cols-2">
        {ENTERPRISE_NOTES.map((note) => (
          <li
            key={note}
            className="border-cc-card-border flex items-start gap-3 border-t py-4"
          >
            <span className="text-cc-accent mt-[6px] flex-none">
              <CheckIcon size={14} />
            </span>
            <span className="text-cc-ink text-body">{note}</span>
          </li>
        ))}
      </ul>

      <div className="mt-10 flex flex-wrap items-center gap-3">
        <SolidButton href="/services/support/contact">
          Contact Sales
        </SolidButton>
        <OutlineButton href="/platform">Explore the platform</OutlineButton>
      </div>
    </section>
  );
}

function Colophon() {
  return (
    <footer className="border-cc-card-border mt-12 border-t py-24 text-center sm:py-32">
      <p className="text-cc-nav-label font-mono text-xs tracking-[0.18em] uppercase">
        A ChilliCream dispatch
      </p>
      <p className="font-heading text-cc-heading text-h2 mt-10 font-semibold text-balance">
        Ship your GraphQL platform with Nitro.
      </p>
      <p className="text-cc-ink text-lead mx-auto mt-8 max-w-[60ch] text-pretty">
        Start on the free Shared Instance in minutes. Upgrade when you need a
        dedicated region, an SLA, or SSO. The docs walk you through every step.
      </p>
      <div className="mt-10 flex flex-wrap items-center justify-center gap-3">
        <SolidButton href="/get-started">Start for Free</SolidButton>
        <OutlineButton href="/docs">Read the docs</OutlineButton>
      </div>
    </footer>
  );
}

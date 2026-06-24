import type { Metadata } from "next";
import type { ReactNode } from "react";

import { OutlineButton, SolidButton } from "@/src/design-system/Button";

export const metadata: Metadata = {
  title: "Pricing for Nitro by ChilliCream",
  description:
    "Nitro GraphQL pricing as a config file: start free on shared cloud, run a dedicated instance at $400/mo with SLA and SSO, or self-host your own infra.",
  keywords: [
    "Nitro GraphQL pricing",
    "ChilliCream pricing",
    "GraphQL platform pricing",
    "GraphQL plans",
    "Hot Chocolate",
    "GraphQL observability pricing",
    "schema registry pricing",
  ],
  openGraph: {
    title: "Pricing for Nitro by ChilliCream",
    description:
      "Nitro GraphQL pricing: shared cloud free tier, dedicated cloud at $400/mo with SLA and SSO, or self-hosted on your own infra.",
  },
  robots: { index: false, follow: false },
};

interface Plan {
  readonly id: "shared" | "dedicated" | "self";
  readonly name: string;
  readonly tagline: string;
  readonly price: string;
  readonly priceNote: string;
  readonly filename: string;
  readonly features: readonly string[];
  readonly cta: string;
  readonly ctaHref: string;
  readonly popular?: boolean;
}

const PLANS: readonly Plan[] = [
  {
    id: "shared",
    name: "Shared Instance",
    tagline: "Shared resources, fully managed.",
    price: "Free",
    priceNote: "pay-as-you-go",
    filename: "shared.toml",
    features: [
      "Multi-tenant cloud region",
      "1 Schema, 3 Environments",
      "Up to 5M ops / month included",
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
    filename: "dedicated.toml",
    features: [
      "Single-tenant cloud region",
      "Unlimited schemas",
      "BYOC region, private networking",
      "99.95% SLA, email + private chat",
      "SSO, audit log, role-based access",
    ],
    cta: "Start for Free",
    ctaHref: "/get-started",
    popular: true,
  },
  {
    id: "self",
    name: "Self-Hosted",
    tagline: "Self managed, on your infrastructure.",
    price: "Custom",
    priceNote: "talk to us",
    filename: "self-hosted.toml",
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
        dedicated: "Single-tenant or BYOC",
        self: "Your infra, air-gap ok",
      },
      {
        label: "Included APIs / schemas",
        shared: "1 schema",
        dedicated: "Unlimited",
        self: "Unlimited",
      },
      {
        label: "Environments per API",
        shared: "3 (dev/QA/prod)",
        dedicated: "Unlimited",
        self: "Unlimited",
      },
      {
        label: "Included ops / month",
        shared: "5M, then PAYG",
        dedicated: "Custom volume",
        self: "Unmetered",
      },
      {
        label: "Log & trace retention",
        shared: "1 day",
        dedicated: "Configurable",
        self: "Your policy",
      },
      {
        label: "Fusion gateway runtime",
        shared: "Your ASP.NET Core",
        dedicated: "Your ASP.NET Core",
        self: "ASP.NET, offline",
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
        label: "Client registry, published clients affected",
        shared: true,
        dedicated: true,
        self: true,
      },
      {
        label: "Breaking-change classification",
        shared: true,
        dedicated: true,
        self: true,
      },
      {
        label: "CI schema & client checks",
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
        label: "Fusion composition lifecycle",
        shared: true,
        dedicated: true,
        self: true,
      },
      {
        label: ".NET Aspire integration",
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
        label: "OpenTelemetry traces, metrics, logs",
        shared: true,
        dedicated: true,
        self: true,
      },
      {
        label: "Operation insights (p95/p99, throughput)",
        shared: true,
        dedicated: true,
        self: true,
      },
      {
        label: "Per-client tracking",
        shared: true,
        dedicated: true,
        self: true,
      },
      {
        label: "Resolver-level insights",
        shared: true,
        dedicated: true,
        self: true,
      },
      {
        label: "Distributed tracing across subgraphs",
        shared: true,
        dedicated: true,
        self: true,
      },
      {
        label: "Service monitoring for any .NET service",
        shared: true,
        dedicated: true,
        self: true,
      },
      {
        label: "Operation reporting (executed + available)",
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
        label: "Persisted / trusted operations",
        shared: true,
        dedicated: true,
        self: true,
      },
      {
        label: "Query cost analysis (@cost / @listSize)",
        shared: true,
        dedicated: true,
        self: true,
      },
      {
        label: "Request limits (depth, breadth, timeouts)",
        shared: true,
        dedicated: true,
        self: true,
      },
      {
        label: "Deployment audit log",
        shared: true,
        dedicated: true,
        self: true,
      },
      {
        label: "Rollback by republishing a tag",
        shared: true,
        dedicated: true,
        self: true,
      },
      {
        label: "Persisted-op distribution cache",
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
        label: "Roles + stage-scoped publish perms",
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
        self: "Your policy",
      },
      {
        label: "API keys (CI/CD) and PATs",
        shared: true,
        dedicated: true,
        self: true,
      },
      {
        label: "OAuth redirect-URL allowlist",
        shared: true,
        dedicated: true,
        self: true,
      },
      {
        label: "ASP.NET Core auth at the gateway",
        shared: true,
        dedicated: true,
        self: true,
      },
      {
        label: "Authorization (@authorize, roles, OPA)",
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
        shared: "From your endpoint",
        dedicated: "From your endpoint",
        self: "From your endpoint",
      },
      {
        label: "MCP server over Streamable HTTP",
        shared: true,
        dedicated: true,
        self: true,
      },
      {
        label: "MCP feature collections",
        shared: true,
        dedicated: true,
        self: true,
      },
      {
        label: "MCP per-tool telemetry",
        shared: true,
        dedicated: true,
        self: true,
      },
      {
        label: "OpenAPI adapter (@http)",
        shared: true,
        dedicated: true,
        self: true,
      },
      {
        label: "CLI distribution (.NET, npm, brew)",
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
        dedicated: "Email + chat",
        self: "Priority eng.",
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
        self: "Long-term",
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

export default function PricingPreviewV4Page() {
  return (
    <div className="font-sans">
      <Hero />
      <PlanTriad />
      <WhyTeams />
      <CompareMatrix />
      <EnterpriseBand />
      <FaqPanel />
      <ClosingCta />
    </div>
  );
}

// --- Terminal chrome --------------------------------------------------------

interface TerminalPanelProps {
  readonly filename: string;
  readonly children: ReactNode;
  readonly className?: string;
  readonly bodyClassName?: string;
  readonly accent?: boolean;
  readonly tabExtra?: ReactNode;
  readonly tabComment?: string;
}

function TerminalPanel({
  filename,
  children,
  className = "",
  bodyClassName = "",
  accent = false,
  tabExtra,
  tabComment,
}: TerminalPanelProps) {
  const wrapperClass = accent
    ? `rounded-2xl p-[1.5px] ${className}`
    : `border-cc-card-border overflow-hidden rounded-2xl border ${className}`;
  const wrapperStyle = accent
    ? { background: "var(--color-cc-accent)" }
    : undefined;
  const inner = (
    <div className="bg-cc-code-bg overflow-hidden rounded-[calc(1rem-1px)]">
      <div className="bg-cc-code-header border-cc-card-border flex items-center gap-3 border-b px-4 py-2.5">
        <span className="flex gap-1.5" aria-hidden="true">
          <span className="block h-2.5 w-2.5 rounded-full bg-[#f0786a]" />
          <span className="block h-2.5 w-2.5 rounded-full bg-[#e4c46b]" />
          <span className="block h-2.5 w-2.5 rounded-full bg-[#5eead4]" />
        </span>
        <span className="text-cc-ink font-mono text-xs">{filename}</span>
        {tabComment ? (
          <span className="text-cc-ink-dim font-mono text-xs">
            {tabComment}
          </span>
        ) : null}
        {tabExtra ? (
          <span className="ml-auto flex items-center gap-2">{tabExtra}</span>
        ) : null}
      </div>
      <div className={`p-5 sm:p-6 ${bodyClassName}`}>{children}</div>
    </div>
  );
  return (
    <div className={wrapperClass} style={wrapperStyle}>
      {inner}
    </div>
  );
}

function Prompt() {
  return <span className="text-cc-accent select-none">$</span>;
}

function Comment({ children }: { readonly children: ReactNode }) {
  return <span className="text-cc-ink-dim">{children}</span>;
}

// --- Hero -------------------------------------------------------------------

function Hero() {
  return (
    <section className="mx-auto max-w-5xl pt-10 pb-12 sm:pt-16 sm:pb-16">
      <p className="text-cc-nav-label text-caption font-mono tracking-[0.18em] uppercase">
        # nitro pricing
      </p>
      <h1 className="font-heading text-cc-heading text-h2 sm:text-hero mt-5 leading-[1.05] font-semibold">
        Pricing, as a config file.
      </h1>
      <p className="text-cc-ink text-lead mt-6 max-w-2xl text-pretty">
        Start free on shared cloud. Move to a dedicated instance when you need
        SLA, SSO, and your own region. Self-host on your own infrastructure when
        the workload, or the policy, demands it.
      </p>
      <div className="mt-8 flex flex-wrap items-center gap-3">
        <SolidButton href="/get-started">Start for Free</SolidButton>
        <OutlineButton href="/services/support/contact">
          Talk to Sales
        </OutlineButton>
      </div>

      <div className="mt-12">
        <TerminalPanel filename="pricing.sh">
          <pre className="text-cc-ink overflow-x-auto font-mono text-sm leading-relaxed">
            <code>
              <span>
                <Prompt /> <span className="text-cc-ink">nitro plans list</span>
              </span>
              {"\n\n"}
              <Comment># 3 plans available</Comment>
              {"\n"}
              <span className="text-cc-ink">NAME</span>
              {"                 "}
              <span className="text-cc-ink">PRICE</span>
              {"        "}
              <span className="text-cc-ink">TAGLINE</span>
              {"\n"}
              <span style={{ color: "#16b9e4" }}>shared</span>
              {"               "}
              <span className="text-cc-accent">Free</span>
              {"         "}
              <Comment>Shared resources, fully managed.</Comment>
              {"\n"}
              <span style={{ color: "#7c92c6" }}>dedicated</span>
              {"            "}
              <span className="text-cc-accent">$400</span>
              {"/mo     "}
              <Comment>Dedicated resources, fully managed.</Comment>
              {"\n"}
              <span style={{ color: "#f0786a" }}>self-hosted</span>
              {"          "}
              <span className="text-cc-accent">Custom</span>
              {"       "}
              <Comment>Self managed, on your infrastructure.</Comment>
              {"\n\n"}
              <Comment>
                # use `nitro plans show &lt;name&gt;` for the full config.
              </Comment>
            </code>
          </pre>
        </TerminalPanel>
      </div>
    </section>
  );
}

// --- Plan triad -------------------------------------------------------------

function PlanTriad() {
  return (
    <section
      aria-labelledby="plans-heading"
      className="mx-auto max-w-5xl pb-16 sm:pb-24"
    >
      <div className="mb-10 max-w-2xl">
        <p className="text-cc-nav-label text-caption font-mono tracking-[0.18em] uppercase">
          # plans
        </p>
        <h2
          id="plans-heading"
          className="font-heading text-cc-heading text-h3 mt-3 font-semibold"
        >
          Three configs. Pick the one that fits.
        </h2>
        <p className="text-cc-ink mt-4 text-base">
          Each plan ships as a small config you can read in under a minute. The
          values below match what we provision when you sign up.
        </p>
      </div>

      <div className="grid gap-6 lg:grid-cols-3 lg:items-stretch">
        {PLANS.map((plan) => (
          <PlanConfigPanel key={plan.id} plan={plan} />
        ))}
      </div>
    </section>
  );
}

function PlanConfigPanel({ plan }: { readonly plan: Plan }) {
  const slug = plan.id;
  return (
    <div className="flex flex-col">
      <TerminalPanel
        filename={plan.filename}
        accent={plan.popular}
        tabComment={plan.popular ? "# most-popular" : undefined}
      >
        <pre className="text-cc-ink font-mono text-sm leading-relaxed">
          <code>
            <span className="text-cc-ink-dim">[plan]</span>
            {"\n"}
            <span style={{ color: "#16b9e4" }}>name</span>
            {"     = "}
            <span className="text-cc-accent">{`"${plan.name}"`}</span>
            {"\n"}
            <span style={{ color: "#16b9e4" }}>id</span>
            {"       = "}
            <span className="text-cc-accent">{`"${slug}"`}</span>
            {"\n"}
            <span style={{ color: "#16b9e4" }}>tagline</span>
            {"  = "}
            <span className="text-cc-accent">{`"${plan.tagline}"`}</span>
            {"\n\n"}
            <span className="text-cc-ink-dim">[price]</span>
            {"\n"}
            <span style={{ color: "#7c92c6" }}>amount</span>
            {"   = "}
          </code>
          <span className="font-heading text-cc-heading text-h4 align-baseline font-semibold">
            {plan.price}
          </span>
          <code>
            {"\n"}
            <span style={{ color: "#7c92c6" }}>unit</span>
            {"     = "}
            <span className="text-cc-accent">{`"${plan.priceNote}"`}</span>
            {"\n\n"}
            <span className="text-cc-ink-dim">[features]</span>
            {"\n"}
            {plan.features.map((feature, i) => (
              <span key={feature}>
                <span style={{ color: "#f0786a" }}>{`f${i + 1}`}</span>
                {"       = "}
                <span className="text-cc-accent">{`"${feature}"`}</span>
                {"\n"}
              </span>
            ))}
          </code>
        </pre>
      </TerminalPanel>
      {plan.popular ? (
        <SolidButton href={plan.ctaHref} className="mt-5 w-full">
          {plan.cta}
        </SolidButton>
      ) : (
        <OutlineButton href={plan.ctaHref} className="mt-5 w-full">
          {plan.cta}
        </OutlineButton>
      )}
    </div>
  );
}

// --- Why teams pick Nitro ---------------------------------------------------

interface Blurb {
  readonly tag: string;
  readonly title: string;
  readonly body: string;
}

const BLURBS: readonly Blurb[] = [
  {
    tag: "// hosting",
    title: "Same Nitro, different runway.",
    body: "Shared, dedicated, or your own infra. The control plane, registry, and telemetry are the same. What changes is where it runs and who you share it with.",
  },
  {
    tag: "// lifecycle",
    title: "Schema changes you can read.",
    body: "CI checks classify every change as safe, dangerous, or breaking, and report the published clients affected before you deploy. Rollback is republishing an earlier tag.",
  },
  {
    tag: "// observability",
    title: "OpenTelemetry, native.",
    body: "Traces, metrics, and logs flow through standard OTLP once Nitro is configured. Distributed tracing across Fusion subgraphs and any .NET service comes with it.",
  },
];

function WhyTeams() {
  return (
    <section
      aria-labelledby="why-heading"
      className="mx-auto max-w-5xl pb-16 sm:pb-24"
    >
      <div className="mb-10 max-w-2xl">
        <p className="text-cc-nav-label text-caption font-mono tracking-[0.18em] uppercase">
          # why teams pick nitro
        </p>
        <h2
          id="why-heading"
          className="font-heading text-cc-heading text-h3 mt-3 font-semibold"
        >
          What the configs share.
        </h2>
      </div>

      <div className="grid gap-10 md:grid-cols-3">
        {BLURBS.map((blurb) => (
          <div key={blurb.title}>
            <p className="text-cc-nav-label text-caption font-mono">
              {blurb.tag}
            </p>
            <h3 className="font-heading text-cc-heading text-h5 mt-3 font-semibold">
              {blurb.title}
            </h3>
            <p className="text-cc-ink mt-3 text-base">{blurb.body}</p>
          </div>
        ))}
      </div>
    </section>
  );
}

// --- Compare plans (ASCII diff) ---------------------------------------------

function CompareMatrix() {
  return (
    <section
      aria-labelledby="compare-heading"
      className="mx-auto max-w-5xl pb-16 sm:pb-24"
    >
      <div className="mb-10 max-w-2xl">
        <p className="text-cc-nav-label text-caption font-mono tracking-[0.18em] uppercase">
          # compare plans
        </p>
        <h2
          id="compare-heading"
          className="font-heading text-cc-heading text-h3 mt-3 font-semibold"
        >
          Every capability, side by side.
        </h2>
        <p className="text-cc-ink mt-4 text-base">
          A single diff across the three configs. Same Nitro platform, different
          deployment and support shape.
        </p>
      </div>

      <TerminalPanel filename="plans-diff.txt">
        <div className="overflow-x-auto">
          <pre className="text-cc-ink font-mono text-[0.78rem] leading-relaxed sm:text-xs">
            <code>
              <span className="text-cc-ink-dim">
                {"# nitro plans diff --all"}
                {"\n"}
              </span>
              <DiffHeaderRow />
              <DiffRule />
              {COMPARISON.map((group, gi) => (
                <DiffGroup key={group.title} group={group} first={gi === 0} />
              ))}
            </code>
          </pre>
        </div>
      </TerminalPanel>
    </section>
  );
}

const COL_LABEL = 42;
const COL_PLAN = 22;

function padCell(value: string, width: number) {
  if (value.length >= width) return value.slice(0, width - 1) + " ";
  return value + " ".repeat(width - value.length);
}

function DiffHeaderRow() {
  return (
    <span className="text-cc-heading">
      {padCell("capability", COL_LABEL)}
      {padCell("shared", COL_PLAN)}
      {padCell("dedicated", COL_PLAN)}
      {padCell("self-hosted", COL_PLAN)}
      {"\n"}
    </span>
  );
}

function DiffRule() {
  return (
    <span className="text-cc-ink-faint">
      {"-".repeat(COL_LABEL + COL_PLAN * 3)}
      {"\n"}
    </span>
  );
}

function DiffGroup({
  group,
  first,
}: {
  readonly group: ComparisonGroup;
  readonly first: boolean;
}) {
  return (
    <>
      {!first ? "\n" : null}
      <span className="text-cc-nav-label">
        {`## ${group.title}`}
        {"\n"}
      </span>
      {group.rows.map((row) => (
        <DiffRow key={row.label} row={row} />
      ))}
    </>
  );
}

function DiffRow({ row }: { readonly row: ComparisonRow }) {
  return (
    <span>
      <span className="text-cc-ink">{padCell(row.label, COL_LABEL)}</span>
      <DiffCell value={row.shared} />
      <DiffCell value={row.dedicated} />
      <DiffCell value={row.self} />
      {"\n"}
    </span>
  );
}

function DiffCell({ value }: { readonly value: CellValue }) {
  if (typeof value === "boolean") {
    if (value) {
      return <span className="text-cc-accent">{padCell("[x]", COL_PLAN)}</span>;
    }
    return (
      <span className="text-cc-ink-faint">{padCell("[ ]", COL_PLAN)}</span>
    );
  }
  return <span className="text-cc-ink-dim">{padCell(value, COL_PLAN)}</span>;
}

// --- Enterprise band --------------------------------------------------------

function EnterpriseBand() {
  return (
    <section
      aria-labelledby="enterprise-heading"
      className="mx-auto max-w-5xl pb-16 sm:pb-24"
    >
      <TerminalPanel
        filename="procurement.md"
        tabExtra={
          <>
            <SolidButton
              href="/services/support/contact"
              className="!px-4 !py-1.5 text-xs"
            >
              Contact Sales
            </SolidButton>
            <OutlineButton href="/platform" className="!px-4 !py-1.5 text-xs">
              Explore the platform
            </OutlineButton>
          </>
        }
      >
        <div className="text-cc-ink font-mono text-sm leading-relaxed">
          <p className="text-cc-nav-label">{"### Enterprise"}</p>
          <h2
            id="enterprise-heading"
            className="font-heading text-cc-heading text-h4 mt-3 font-semibold"
          >
            Custom volume, procurement, or air-gapped?
          </h2>
          <p className="text-cc-ink mt-4 text-base">
            We work directly with platform and security teams on bespoke
            commercial terms, on-prem rollouts, and migrations from existing
            GraphQL gateways. Engineers, not gatekeepers, run the call.
          </p>
          <ul className="mt-6 grid gap-3 sm:grid-cols-2">
            <ChecklistItem>Dedicated solution architect</ChecklistItem>
            <ChecklistItem>Annual contracts and POs</ChecklistItem>
            <ChecklistItem>Security and DPA review</ChecklistItem>
            <ChecklistItem>Migration playbooks</ChecklistItem>
          </ul>
        </div>
      </TerminalPanel>
    </section>
  );
}

function ChecklistItem({ children }: { readonly children: ReactNode }) {
  return (
    <li className="text-cc-ink flex items-start gap-3 text-sm">
      <span className="text-cc-accent mt-[5px] flex-none font-mono">-</span>
      <span className="font-sans">{children}</span>
    </li>
  );
}

// --- FAQ --------------------------------------------------------------------

function FaqPanel() {
  return (
    <section
      aria-labelledby="faq-heading"
      className="mx-auto max-w-5xl pb-16 sm:pb-24"
    >
      <div className="mb-10 max-w-2xl">
        <p className="text-cc-nav-label text-caption font-mono tracking-[0.18em] uppercase">
          # faq
        </p>
        <h2
          id="faq-heading"
          className="font-heading text-cc-heading text-h3 mt-3 font-semibold"
        >
          Honest answers about pricing.
        </h2>
      </div>

      <TerminalPanel filename="faq.md">
        <dl>
          {FAQ.map((item, i) => (
            <div key={item.question}>
              {i > 0 ? (
                <div
                  aria-hidden="true"
                  className="border-cc-ink-faint my-6 border-t border-dashed"
                />
              ) : null}
              <dt className="text-cc-heading text-h6 font-mono">
                <span className="text-cc-accent">##</span> {item.question}
              </dt>
              <dd className="text-cc-ink mt-3 font-sans text-base leading-relaxed">
                {item.answer}
              </dd>
            </div>
          ))}
        </dl>
      </TerminalPanel>
    </section>
  );
}

// --- Closing CTA ------------------------------------------------------------

function ClosingCta() {
  return (
    <section className="mx-auto max-w-5xl pb-16 sm:pb-24">
      <div className="mb-8 max-w-2xl">
        <p className="text-cc-nav-label text-caption font-mono tracking-[0.18em] uppercase">
          # quickstart
        </p>
        <h2 className="font-heading text-cc-heading text-h3 mt-3 font-semibold">
          Ship your GraphQL platform with Nitro.
        </h2>
        <p className="text-cc-ink mt-4 text-base">
          Start on the free Shared Instance in minutes. Upgrade when you need a
          dedicated region, SLA, or SSO. The docs walk you through every step.
        </p>
      </div>

      <TerminalPanel filename="quickstart.sh">
        <pre className="text-cc-ink font-mono text-sm leading-relaxed">
          <code>
            <Comment># pick a plan, then run:</Comment>
            {"\n"}
            <Prompt />{" "}
            <span className="text-cc-ink">nitro start --plan shared</span>
            <span
              aria-hidden="true"
              className="bg-cc-accent ml-1 inline-block h-[1.1em] w-[0.55em] translate-y-[2px] animate-pulse"
            />
          </code>
        </pre>
      </TerminalPanel>

      <div className="mt-8 flex flex-wrap items-center gap-3">
        <SolidButton href="/get-started">Start for Free</SolidButton>
        <OutlineButton href="/docs">Read the docs</OutlineButton>
      </div>
    </section>
  );
}

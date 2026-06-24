import type { Metadata } from "next";
import type { ComponentType, ReactNode } from "react";

import { CheckIcon } from "@/src/components/CheckIcon";
import { OutlineButton, SolidButton } from "@/src/design-system/Button";
import { CoffeeTray } from "@/src/icons/CoffeeTray";
import { DripBrewer } from "@/src/icons/DripBrewer";
import { Espresso } from "@/src/icons/Espresso";

export const metadata: Metadata = {
  title: "Nitro GraphQL pricing, served three ways",
  description:
    "Nitro GraphQL pricing, same beans, three pours. Start free on Shared, scale on a $400 per month Dedicated Instance with SLA and SSO, or self-host on your infra.",
  robots: { index: false, follow: false },
};

interface Plan {
  readonly id: "shared" | "dedicated" | "self";
  readonly name: string;
  readonly brewLabel: string;
  readonly tagline: string;
  readonly price: string;
  readonly priceNote: string;
  readonly icon: ComponentType<{ readonly className?: string }>;
  readonly features: readonly string[];
  readonly cta: string;
  readonly ctaHref: string;
  readonly popular?: boolean;
}

const PLANS: readonly Plan[] = [
  {
    id: "shared",
    name: "Shared Instance",
    brewLabel: "House Pour",
    tagline: "Shared resources, fully managed.",
    price: "Free",
    priceNote: "pay-as-you-go",
    icon: DripBrewer,
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
    brewLabel: "Single-Origin Reserve",
    tagline: "Dedicated resources, fully managed.",
    price: "$400",
    priceNote: "per month",
    icon: Espresso,
    features: [
      "Single-tenant cloud region",
      "Unlimited schemas",
      "BYOC region, private networking",
      "99.95% SLA, email and private chat",
      "SSO, audit log, role-based access",
    ],
    cta: "Start for Free",
    ctaHref: "/get-started",
    popular: true,
  },
  {
    id: "self",
    name: "Self-Hosted",
    brewLabel: "Whole-Bean",
    tagline: "Self managed, on your infrastructure.",
    price: "Custom",
    priceNote: "talk to us",
    icon: CoffeeTray,
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

interface BarTile {
  readonly eyebrow: string;
  readonly title: string;
  readonly body: string;
}

const BAR_TILES: readonly BarTile[] = [
  {
    eyebrow: "Same beans",
    title: "One Nitro platform",
    body: "Schema registry, client registry, CI checks, MCP, and OpenTelemetry-native insights ship in every plan.",
  },
  {
    eyebrow: "Different cup",
    title: "Where it runs",
    body: "Shared multi-tenant cloud, single-tenant cloud or BYOC, or your own infrastructure including air-gapped.",
  },
  {
    eyebrow: "Different service",
    title: "SLA and support",
    body: "Community Slack on Shared, 99.95% SLA with email and private chat on Dedicated, priority engineering on Self-Hosted.",
  },
  {
    eyebrow: "You set the menu",
    title: "Governance and access",
    body: "Roles, stage-scoped publish permissions, API keys, OAuth allowlist. SSO and audit log on Dedicated and Self-Hosted.",
  },
];

export default function PricingPreviewV6Page() {
  return (
    <>
      <Hero />
      <PlanGrid />
      <BehindTheBar />
      <ComparisonMatrix />
      <Faq />
      <EnterpriseBand />
      <ClosingCta />
    </>
  );
}

function Hero() {
  return (
    <section className="pt-10 pb-12 text-center sm:pt-16 sm:pb-16">
      <p className="text-cc-nav-label font-mono text-xs tracking-[0.18em] uppercase">
        Nitro pricing
      </p>
      <h1 className="font-heading text-cc-heading sm:text-h2 mt-5 text-4xl font-semibold">
        Same beans. Pick your pour.
      </h1>
      <p className="text-cc-ink mx-auto mt-6 max-w-2xl text-base text-pretty sm:text-lg">
        One Nitro platform, three ways to serve it. Start free on shared cloud,
        move to a dedicated instance with SLA and SSO, or self-host on your own
        infrastructure when the workload, or the policy, demands it.
      </p>
      <div className="mt-9 flex flex-wrap items-center justify-center gap-3">
        <SolidButton href="/get-started">Start for Free</SolidButton>
        <OutlineButton href="/services/support/contact">
          Talk to Sales
        </OutlineButton>
      </div>
    </section>
  );
}

function PlanGrid() {
  return (
    <section aria-labelledby="plans-heading" className="pb-16 sm:pb-24">
      <h2 id="plans-heading" className="sr-only">
        Plans
      </h2>
      <MenuChalkboard />
      <div className="mt-6 grid gap-6 lg:grid-cols-3 lg:items-stretch">
        {PLANS.map((plan) => (
          <PlanCard key={plan.id} plan={plan} />
        ))}
      </div>
    </section>
  );
}

function MenuChalkboard() {
  return (
    <div className="border-cc-card-border bg-cc-surface flex flex-wrap items-center justify-between gap-3 rounded-2xl border px-5 py-3 sm:px-6">
      <div className="flex items-center gap-3">
        <span
          aria-hidden="true"
          className="bg-cc-accent inline-block h-1.5 w-1.5 rounded-full"
        />
        <span className="text-cc-nav-label font-mono text-[0.65rem] tracking-[0.22em] uppercase">
          On the menu
        </span>
      </div>
      <div className="text-cc-ink-dim flex flex-wrap items-center gap-x-5 gap-y-1 font-mono text-[0.7rem] tracking-[0.14em] uppercase">
        <span>House Pour</span>
        <span aria-hidden="true" className="text-cc-ink-faint">
          /
        </span>
        <span>Single-Origin Reserve</span>
        <span aria-hidden="true" className="text-cc-ink-faint">
          /
        </span>
        <span>Whole-Bean</span>
      </div>
      <div className="hidden font-mono text-[0.65rem] tracking-[0.22em] uppercase sm:block">
        <span className="text-cc-nav-label">Today, </span>
        <span className="text-cc-ink-dim">freshly brewed</span>
      </div>
    </div>
  );
}

function PlanCard({ plan }: { readonly plan: Plan }) {
  if (plan.popular) {
    return (
      <div
        className="relative rounded-3xl p-[1.5px] lg:-my-2"
        style={{
          background:
            "linear-gradient(140deg, #16b9e4 0%, #7c92c6 50%, #f0786a 100%)",
        }}
      >
        <TodaysPourPill />
        <div className="bg-cc-surface flex h-full flex-col rounded-[calc(1.5rem-1.5px)] p-7 sm:p-8">
          <PlanCardBody plan={plan} />
        </div>
      </div>
    );
  }

  return (
    <div className="bg-cc-card-bg border-cc-card-border hover:border-cc-card-border-hover flex h-full flex-col rounded-3xl border p-7 transition-colors sm:p-8">
      <PlanCardBody plan={plan} />
    </div>
  );
}

function PlanCardBody({ plan }: { readonly plan: Plan }) {
  const CallToAction = plan.popular ? SolidButton : OutlineButton;
  const Icon = plan.icon;
  return (
    <>
      <div className="flex items-start justify-between gap-4">
        <div>
          <p className="text-cc-nav-label font-mono text-[0.65rem] tracking-[0.2em] uppercase">
            {plan.brewLabel}
          </p>
          <h3 className="font-heading text-cc-heading text-h5 mt-2 font-semibold">
            {plan.name}
          </h3>
        </div>
        <Icon className="text-cc-ink-dim h-10 w-10 flex-none" />
      </div>
      <p className="text-cc-ink-dim mt-3 text-sm">{plan.tagline}</p>
      <div className="mt-6 flex items-baseline gap-2">
        <span className="font-heading text-cc-heading text-h3 font-semibold">
          {plan.price}
        </span>
        <span className="text-cc-nav-label font-mono text-xs">
          {plan.priceNote}
        </span>
      </div>
      <SteamLine />
      <div
        aria-hidden="true"
        className="border-cc-ink-faint my-6 border-t border-dashed"
      />
      <ul className="flex flex-1 flex-col gap-3">
        {plan.features.map((feature) => (
          <li key={feature} className="flex items-start gap-3">
            <span className="text-cc-accent mt-[5px] flex-none">
              <CheckIcon />
            </span>
            <span className="text-cc-ink text-sm">{feature}</span>
          </li>
        ))}
      </ul>
      <CallToAction href={plan.ctaHref} className="mt-8 w-full">
        {plan.cta}
      </CallToAction>
    </>
  );
}

function SteamLine() {
  return (
    <svg
      aria-hidden="true"
      viewBox="0 0 64 14"
      fill="none"
      className="text-cc-accent/55 mt-3 h-3 w-16"
    >
      <path
        d="M2 11 C 8 3, 14 3, 20 11 S 32 19, 38 11 S 50 3, 62 11"
        stroke="currentColor"
        strokeWidth="1.4"
        strokeLinecap="round"
        fill="none"
      />
    </svg>
  );
}

function TodaysPourPill() {
  return (
    <span className="bg-cc-surface text-cc-accent border-cc-accent absolute top-0 left-1/2 z-10 -translate-x-1/2 -translate-y-1/2 rounded-full border px-4 py-1 font-mono text-[0.65rem] tracking-[0.18em] whitespace-nowrap uppercase">
      Today&apos;s pour
    </span>
  );
}

function BehindTheBar() {
  return (
    <section
      aria-labelledby="behind-the-bar-heading"
      className="border-cc-card-border bg-cc-card-bg/40 mt-2 rounded-3xl border p-6 sm:p-10"
    >
      <div className="flex flex-col gap-3 text-center sm:text-left">
        <p className="text-cc-nav-label font-mono text-xs tracking-[0.18em] uppercase">
          Behind the bar
        </p>
        <h2
          id="behind-the-bar-heading"
          className="font-heading text-cc-heading text-h4 sm:text-h3 font-semibold"
        >
          One platform. Where, and how, you take it differs.
        </h2>
        <p className="text-cc-ink max-w-3xl text-base">
          The plan you pick changes the cup, not the beans. Nitro&apos;s schema
          registry, CI checks, MCP server, and OpenTelemetry-native insights
          ship in all three.
        </p>
      </div>
      <div
        aria-hidden="true"
        className="bg-cc-accent/60 mt-8 h-px w-12 rounded-full"
      />
      <ul className="mt-8 grid gap-6 sm:grid-cols-2 lg:grid-cols-4">
        {BAR_TILES.map((tile) => (
          <li
            key={tile.eyebrow}
            className="border-cc-card-border bg-cc-surface/60 rounded-2xl border p-5"
          >
            <p className="text-cc-nav-label font-mono text-[0.65rem] tracking-[0.18em] uppercase">
              {tile.eyebrow}
            </p>
            <h3 className="font-heading text-cc-heading mt-3 text-base font-semibold">
              {tile.title}
            </h3>
            <p className="text-cc-ink-dim mt-2 text-sm leading-relaxed">
              {tile.body}
            </p>
          </li>
        ))}
      </ul>
    </section>
  );
}

function ComparisonMatrix() {
  return (
    <section
      aria-labelledby="compare-heading"
      className="border-cc-card-border bg-cc-card-bg/40 mt-16 rounded-3xl border p-6 sm:mt-24 sm:p-10"
    >
      <div className="text-center">
        <p className="text-cc-nav-label font-mono text-xs tracking-[0.18em] uppercase">
          Compare plans
        </p>
        <h2
          id="compare-heading"
          className="font-heading text-cc-heading text-h4 sm:text-h3 mt-3 font-semibold"
        >
          Every capability, side by side.
        </h2>
        <p className="text-cc-ink mx-auto mt-4 max-w-2xl text-base">
          The same Nitro platform across all three plans. What changes is where
          it runs, who you share it with, and what you get from us.
        </p>
      </div>

      <div className="mt-10 overflow-x-auto">
        <table className="w-full min-w-[42rem] border-separate border-spacing-0 text-left text-sm">
          <thead>
            <tr>
              <th scope="col" className="w-2/5 pb-4 pl-2">
                <span className="sr-only">Capability</span>
              </th>
              {PLANS.map((plan) => (
                <th
                  key={plan.id}
                  scope="col"
                  className="pb-4 text-center align-bottom"
                >
                  <div
                    className={`font-heading text-cc-heading text-base font-semibold ${
                      plan.popular ? "text-cc-accent" : ""
                    }`}
                  >
                    {plan.name}
                  </div>
                  <div className="text-cc-nav-label mt-1 font-mono text-[0.65rem] tracking-[0.15em] uppercase">
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
    </section>
  );
}

function ComparisonGroupRows({ group }: { readonly group: ComparisonGroup }) {
  return (
    <>
      <tr>
        <th
          scope="colgroup"
          colSpan={4}
          className="border-cc-ink-faint text-cc-nav-label border-t pt-6 pb-3 pl-2 text-left font-mono text-xs tracking-[0.15em] uppercase"
        >
          {group.title}
        </th>
      </tr>
      {group.rows.map((row) => (
        <tr key={row.label}>
          <th
            scope="row"
            className="text-cc-ink py-3 pl-2 text-left align-top text-sm font-normal"
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

function ComparisonCell({
  value,
  highlight = false,
}: {
  readonly value: CellValue;
  readonly highlight?: boolean;
}) {
  return (
    <td
      className={`py-3 text-center align-top text-sm ${
        highlight ? "bg-cc-accent/5" : ""
      }`}
    >
      {typeof value === "boolean" ? (
        value ? (
          <span className="text-cc-accent inline-flex" aria-label="Included">
            <CheckIcon size={16} />
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

function Faq() {
  return (
    <section aria-labelledby="faq-heading" className="mt-20 sm:mt-28">
      <div className="text-center">
        <p className="text-cc-nav-label font-mono text-xs tracking-[0.18em] uppercase">
          Frequently asked
        </p>
        <h2
          id="faq-heading"
          className="font-heading text-cc-heading text-h4 sm:text-h3 mt-3 font-semibold"
        >
          Honest answers about pricing.
        </h2>
      </div>

      <dl className="mt-10 grid gap-4 md:grid-cols-2">
        {FAQ.map((item) => (
          <FaqEntry key={item.question} item={item} />
        ))}
      </dl>
    </section>
  );
}

function FaqEntry({ item }: { readonly item: FaqItem }) {
  return (
    <div className="border-cc-card-border bg-cc-card-bg/60 rounded-2xl border p-6">
      <dt className="font-heading text-cc-heading text-base font-semibold">
        {item.question}
      </dt>
      <dd className="text-cc-ink mt-3 text-sm leading-relaxed">
        {item.answer}
      </dd>
    </div>
  );
}

function EnterpriseBand() {
  return (
    <Section className="mt-20 sm:mt-28">
      <div className="border-cc-card-border bg-cc-card-bg/70 grid gap-8 rounded-3xl border p-8 sm:p-12 md:grid-cols-[1.4fr_1fr] md:items-center">
        <div>
          <p className="text-cc-nav-label font-mono text-xs tracking-[0.18em] uppercase">
            House blend
          </p>
          <h2 className="font-heading text-cc-heading text-h4 mt-3 font-semibold">
            Custom volume, procurement, or air-gapped?
          </h2>
          <p className="text-cc-ink mt-4 text-base">
            We work directly with platform and security teams on bespoke
            commercial terms, on-prem rollouts, and migrations from existing
            GraphQL gateways. Engineers, not gatekeepers, run the call.
          </p>
          <ul className="text-cc-ink mt-6 grid gap-3 text-sm sm:grid-cols-2">
            <EnterpriseCheck>Dedicated solution architect</EnterpriseCheck>
            <EnterpriseCheck>Annual contracts and POs</EnterpriseCheck>
            <EnterpriseCheck>Security and DPA review</EnterpriseCheck>
            <EnterpriseCheck>Migration playbooks</EnterpriseCheck>
          </ul>
        </div>
        <div className="flex flex-col gap-3 md:items-end">
          <SolidButton href="/services/support/contact">
            Contact Sales
          </SolidButton>
          <OutlineButton href="/platform">Explore the platform</OutlineButton>
        </div>
      </div>
    </Section>
  );
}

function EnterpriseCheck({ children }: { readonly children: ReactNode }) {
  return (
    <li className="flex items-start gap-3">
      <span className="text-cc-accent mt-[5px] flex-none">
        <CheckIcon />
      </span>
      <span>{children}</span>
    </li>
  );
}

function ClosingCta() {
  return (
    <section className="mt-20 mb-8 text-center sm:mt-28">
      <h2 className="font-heading text-cc-heading text-h4 sm:text-h3 font-semibold">
        Pour your first cup.
      </h2>
      <p className="text-cc-ink mx-auto mt-5 max-w-2xl text-base">
        Start on the free Shared Instance in minutes. Upgrade when you need a
        dedicated region, SLA, or SSO. The docs walk you through every step.
      </p>
      <div className="mt-8 flex flex-wrap items-center justify-center gap-3">
        <SolidButton href="/get-started">Start for Free</SolidButton>
        <OutlineButton href="/docs">Read the docs</OutlineButton>
      </div>
    </section>
  );
}

function Section({
  className = "",
  children,
}: {
  readonly className?: string;
  readonly children: ReactNode;
}) {
  return <section className={className}>{children}</section>;
}

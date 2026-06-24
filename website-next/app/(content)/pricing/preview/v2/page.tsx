import type { Metadata } from "next";
import Link from "next/link";
import type { ReactNode } from "react";

import { CheckIcon } from "@/src/components/CheckIcon";
import { OutlineButton, SolidButton } from "@/src/design-system/Button";

export const metadata: Metadata = {
  title: "Nitro Pricing, Plans for every GraphQL platform scale",
  description:
    "Compare ChilliCream Nitro pricing and plans for the GraphQL platform: free shared cloud, $400 dedicated, or self-hosted. Pick the scale that fits your team.",
  keywords: [
    "ChilliCream pricing",
    "Nitro pricing",
    "GraphQL platform pricing",
    "GraphQL plans",
    "Hot Chocolate Nitro",
    "GraphQL observability pricing",
    "dedicated GraphQL instance",
    "self-hosted GraphQL",
  ],
  openGraph: {
    title: "Nitro Pricing, Plans for every GraphQL platform scale",
    description:
      "Compare Nitro plans for the ChilliCream GraphQL platform. Free shared cloud, $400 dedicated with SLA and SSO, or self-hosted on your own infra.",
  },
  robots: { index: false, follow: false },
};

interface Tier {
  readonly id: string;
  readonly label: string;
  readonly ops: string;
  readonly blurb: string;
  readonly plan: "shared" | "dedicated" | "self";
}

const TIERS: readonly Tier[] = [
  {
    id: "hobby",
    label: "Hobby",
    ops: "~1M ops / mo",
    blurb: "Side projects & spikes",
    plan: "shared",
  },
  {
    id: "team",
    label: "Team",
    ops: "~10M ops / mo",
    blurb: "Production for one squad",
    plan: "shared",
  },
  {
    id: "growth",
    label: "Growth",
    ops: "~100M ops / mo",
    blurb: "Multi-env, multi-region",
    plan: "dedicated",
  },
  {
    id: "enterprise",
    label: "Enterprise",
    ops: "100M+ ops / mo",
    blurb: "Air-gap, SSO, custom SLA",
    plan: "self",
  },
];

interface Plan {
  readonly id: "shared" | "dedicated" | "self";
  readonly name: string;
  readonly description: string;
  readonly price: string;
  readonly priceNote: string;
  readonly features: readonly string[];
  readonly cta: string;
  readonly ctaHref: string;
  readonly popular?: boolean;
  /** Mono caption shown right under the plan name. */
  readonly fitFor: string;
}

const PLANS: readonly Plan[] = [
  {
    id: "shared",
    name: "Shared Instance",
    description: "Shared resources, fully managed",
    fitFor: "FITS: HOBBY · TEAM",
    price: "Free",
    priceNote: "pay-as-you-go",
    features: [
      "Multi-tenant cloud region",
      "1 Schema · 3 Environments",
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
    description: "Dedicated resources, fully managed",
    fitFor: "FITS: GROWTH",
    price: "$400",
    priceNote: "per month",
    features: [
      "Single-tenant cloud region",
      "Unlimited schemas",
      "BYOC region · private networking",
      "99.95% SLA · email + private chat",
      "SSO, audit log, role-based access",
    ],
    cta: "Start for Free",
    ctaHref: "/get-started",
    popular: true,
  },
  {
    id: "self",
    name: "Self-Hosted",
    description: "Self managed",
    fitFor: "FITS: ENTERPRISE",
    price: "Custom",
    priceNote: "talk to us",
    features: [
      "Run on your own infrastructure",
      "Air-gapped & on-prem supported",
      "Priority engineering support",
      "Long-term release channel",
      "Custom training & onboarding",
    ],
    cta: "Talk to Us",
    ctaHref: "/services/support/contact",
  },
];

interface CompareRow {
  readonly feature: string;
  readonly shared: string;
  readonly dedicated: string;
  readonly self: string;
  readonly note?: string;
}

interface CompareGroup {
  readonly title: string;
  readonly rows: readonly CompareRow[];
}

const COMPARE_GROUPS: readonly CompareGroup[] = [
  {
    title: "Hosting & isolation",
    rows: [
      {
        feature: "Deployment model",
        shared: "Multi-tenant cloud",
        dedicated: "Single-tenant cloud or BYOC",
        self: "Your infrastructure, air-gap supported",
      },
      {
        feature: "Included APIs / schemas",
        shared: "1 schema",
        dedicated: "Unlimited",
        self: "Unlimited",
      },
      {
        feature: "Environments per API",
        shared: "3 (dev / QA / prod)",
        dedicated: "Unlimited, branchable stages",
        self: "Unlimited, branchable stages",
      },
      {
        feature: "Included operations / month",
        shared: "5M, pay-as-you-go after",
        dedicated: "Custom volume",
        self: "Unmetered on your infra",
      },
      {
        feature: "Log & trace retention",
        shared: "1 day",
        dedicated: "Configurable",
        self: "Your retention policy",
      },
      {
        feature: "Fusion gateway runtime",
        shared: "Your ASP.NET Core app",
        dedicated: "Your ASP.NET Core app",
        self: "Your ASP.NET Core app, fully offline",
        note: "Gateway is always self-run, never a hosted hop",
      },
    ],
  },
  {
    title: "Schema lifecycle",
    rows: [
      {
        feature: "Schema registry with history & rollback",
        shared: "Included",
        dedicated: "Included",
        self: "Included",
      },
      {
        feature: "Client registry (persisted operations)",
        shared: "Included",
        dedicated: "Included",
        self: "Included",
        note: "Tracks which published clients are affected by a change",
      },
      {
        feature: "Breaking-change classification (safe / dangerous / breaking)",
        shared: "Included",
        dedicated: "Included",
        self: "Included",
      },
      {
        feature: "CI schema & client checks (validate / upload / publish)",
        shared: "Included",
        dedicated: "Included",
        self: "Included",
      },
      {
        feature: "Stage promotion with approval gates",
        shared: "Included",
        dedicated: "Included",
        self: "Included",
        note: "Opt-in via --wait-for-approval; 10-minute auto-timeout",
      },
      {
        feature:
          "Fusion composition lifecycle (begin / validate / commit / rollback)",
        shared: "Included",
        dedicated: "Included",
        self: "Included",
      },
      {
        feature: ".NET Aspire integration (compose live subgraphs at build)",
        shared: "Included",
        dedicated: "Included",
        self: "Included",
      },
    ],
  },
  {
    title: "Observability",
    rows: [
      {
        feature: "OpenTelemetry-native traces, metrics, logs",
        shared: "Included",
        dedicated: "Included",
        self: "Included",
        note: "Requires Nitro configuration in the server",
      },
      {
        feature:
          "Operation insights (p95/p99, throughput, error rate, impact score)",
        shared: "Included",
        dedicated: "Included",
        self: "Included",
      },
      {
        feature: "Per-client tracking (GraphQL-Client-Id / Version)",
        shared: "Included",
        dedicated: "Included",
        self: "Included",
      },
      {
        feature: "Resolver-level insights & sample traces",
        shared: "Included",
        dedicated: "Included",
        self: "Included",
      },
      {
        feature: "Distributed tracing across Fusion subgraphs",
        shared: "Included",
        dedicated: "Included",
        self: "Included",
      },
      {
        feature: "Service monitoring for any .NET service (REST, gRPC, jobs)",
        shared: "Included",
        dedicated: "Included",
        self: "Included",
      },
      {
        feature:
          "Operation reporting (executed + available, persisted + ad-hoc)",
        shared: "Included",
        dedicated: "Included",
        self: "Included",
      },
    ],
  },
  {
    title: "Operations & delivery",
    rows: [
      {
        feature: "Persisted / trusted operations enforcement",
        shared: "Included",
        dedicated: "Included",
        self: "Included",
      },
      {
        feature: "Query cost analysis (@cost / @listSize, IBM spec)",
        shared: "Included",
        dedicated: "Included",
        self: "Included",
      },
      {
        feature: "Request limits (depth, breadth, recursion, timeouts)",
        shared: "Included",
        dedicated: "Included",
        self: "Included",
      },
      {
        feature: "Deployment audit log (every publish)",
        shared: "Included",
        dedicated: "Included",
        self: "Included",
      },
      {
        feature: "Rollback by republishing an earlier tag",
        shared: "Included",
        dedicated: "Included",
        self: "Included",
      },
      {
        feature:
          "Persisted-op distribution cache (filesystem / Azure Blob / custom)",
        shared: "Included",
        dedicated: "Included",
        self: "Included",
      },
    ],
  },
  {
    title: "Security & access",
    rows: [
      {
        feature:
          "Roles (Owner / Admin / Collaborator) + stage-scoped publish permissions",
        shared: "Included",
        dedicated: "Included",
        self: "Included",
      },
      {
        feature: "SSO (SAML / OIDC)",
        shared: "Not included",
        dedicated: "Included",
        self: "Via your IdP",
      },
      {
        feature: "Audit log for admin actions",
        shared: "Not included",
        dedicated: "Included",
        self: "Your retention policy",
      },
      {
        feature: "API keys (CI/CD) and PATs (user-bound)",
        shared: "Included",
        dedicated: "Included",
        self: "Included",
      },
      {
        feature: "OAuth redirect-URL allowlist (anti token-leak)",
        shared: "Included",
        dedicated: "Included",
        self: "Included",
      },
      {
        feature: "ASP.NET Core auth (JWT, cookie, OIDC, mTLS) at the gateway",
        shared: "Included",
        dedicated: "Included",
        self: "Included",
      },
      {
        feature: "Authorization (@authorize, roles, policies, OPA)",
        shared: "Included",
        dedicated: "Included",
        self: "Included",
      },
    ],
  },
  {
    title: "Developer experience",
    rows: [
      {
        feature: "Built-in GraphQL IDE",
        shared: "Served from your endpoint",
        dedicated: "Served from your endpoint",
        self: "Served from your endpoint",
      },
      {
        feature: "MCP server endpoint over Streamable HTTP",
        shared: "Included",
        dedicated: "Included",
        self: "Included",
      },
      {
        feature: "MCP feature collections (.graphql + .json + .html)",
        shared: "Included",
        dedicated: "Included",
        self: "Included",
      },
      {
        feature:
          "MCP per-tool telemetry (latency, ops/min, error rate, impact)",
        shared: "Included",
        dedicated: "Included",
        self: "Included",
      },
      {
        feature:
          "OpenAPI adapter (@http exposes GraphQL ops as REST + OpenAPI doc)",
        shared: "Included",
        dedicated: "Included",
        self: "Included",
      },
      {
        feature:
          "CLI distribution (.NET tool, npm, Homebrew, self-contained binaries)",
        shared: "Included",
        dedicated: "Included",
        self: "Included",
      },
      {
        feature: "Mock servers via CLI",
        shared: "Included",
        dedicated: "Included",
        self: "Included",
      },
    ],
  },
  {
    title: "Support & SLAs",
    rows: [
      {
        feature: "Support channel",
        shared: "Community Slack",
        dedicated: "Email + private chat",
        self: "Priority engineering",
      },
      {
        feature: "Uptime SLA",
        shared: "Best-effort",
        dedicated: "99.95%",
        self: "You operate it",
      },
      {
        feature: "Release channel",
        shared: "Continuous",
        dedicated: "Continuous",
        self: "Long-term release channel",
      },
      {
        feature: "Onboarding & training",
        shared: "Docs & community",
        dedicated: "Guided onboarding",
        self: "Custom training",
      },
    ],
  },
];

interface Faq {
  readonly q: string;
  readonly a: ReactNode;
}

const FAQS: readonly Faq[] = [
  {
    q: "How does pricing work on the Shared Instance?",
    a: (
      <>
        The Shared Instance is free up to 5M operations per month. Beyond that
        you pay only for what you use, billed on the same account, no
        commitment. There&rsquo;s no card required to start. Move to the
        Dedicated Instance when you outgrow the shared region or need an SLA.
      </>
    ),
  },
  {
    q: "What’s included in the Dedicated Instance SLA?",
    a: (
      <>
        Dedicated includes a 99.95% uptime SLA on the Nitro control plane,
        single-tenant compute, private networking, and email plus private chat
        support. You can also bring your own cloud account (BYOC) so the data
        plane runs in a region you control.
      </>
    ),
  },
  {
    q: "Do all plans get SSO and audit logs?",
    a: (
      <>
        SSO, role-based access, and audit log ship with the Dedicated Instance
        and the Self-Hosted plan. The Shared Instance is intended for smaller
        teams and individuals, so it uses standard account logins.
      </>
    ),
  },
  {
    q: "Can I self-host the entire platform?",
    a: (
      <>
        Yes. The Self-Hosted plan runs the full Nitro control plane on your own
        infrastructure, including air-gapped and on-prem deployments. It ships
        on a long-term release channel with priority engineering support and
        optional onboarding.
      </>
    ),
  },
  {
    q: "What happens when a schema change affects existing clients?",
    a: (
      <>
        Nitro&rsquo;s schema registry runs CI checks against your registered
        client operations. When a change is breaking, the check reports the
        published clients affected so you can coordinate the rollout before
        merging. See the{" "}
        <Link className="text-cc-accent hover:underline" href="/docs">
          docs
        </Link>{" "}
        for the full check pipeline.
      </>
    ),
  },
  {
    q: "Do you offer refunds or annual billing?",
    a: (
      <>
        The Dedicated Instance is month-to-month by default and can be cancelled
        at any time. Annual billing and multi-year commitments are available,
        get in touch via the{" "}
        <Link
          className="text-cc-accent hover:underline"
          href="/services/support/contact"
        >
          contact form
        </Link>
        .
      </>
    ),
  },
];

export default function PricingV2Page() {
  return (
    <>
      <Hero />
      <ScaleSelector />
      <PlanGrid />
      <CompareTable />
      <Faq />
      <EnterpriseBand />
      <ClosingCta />
    </>
  );
}

function Hero() {
  return (
    <section className="pt-6 pb-2 text-center sm:pt-10">
      <p className="text-cc-nav-label font-mono text-xs tracking-[0.2em] uppercase">
        Nitro · Pricing
      </p>
      <h1 className="font-heading text-cc-heading text-h2 sm:text-h1 mt-4 font-semibold text-balance">
        Pick your scale.
        <br className="hidden sm:block" /> Pay only when you grow.
      </h1>
      <p className="text-cc-ink text-lead mx-auto mt-6 max-w-2xl text-pretty">
        Nitro is the control plane for your GraphQL APIs and .NET backend:
        observability, schema and client registry, CI checks, deployments, and
        the GraphQL IDE. Start free on the shared cloud, move to a dedicated
        instance, or self-host on your own infrastructure.
      </p>
      <div className="mt-8 flex flex-wrap items-center justify-center gap-3">
        <SolidButton href="/get-started">Start for Free</SolidButton>
        <OutlineButton href="/services/support/contact">
          Talk to Sales
        </OutlineButton>
      </div>
    </section>
  );
}

function ScaleSelector() {
  return (
    <section
      aria-labelledby="scale-heading"
      className="mt-16 scroll-mt-24 sm:mt-20"
      id="scale"
    >
      <div className="text-center">
        <p className="text-cc-nav-label font-mono text-xs tracking-[0.2em] uppercase">
          Step 1 · Estimate your usage
        </p>
        <h2
          id="scale-heading"
          className="font-heading text-cc-heading text-h3 mt-3 font-semibold"
        >
          How many operations per month?
        </h2>
        <p className="text-cc-ink-dim mx-auto mt-3 max-w-2xl text-sm">
          One GraphQL request to your gateway counts as one operation. Pick the
          tier that&rsquo;s closest, and we&rsquo;ll point at the plan that
          fits.
        </p>
      </div>

      <div className="border-cc-card-border bg-cc-card-bg/40 mt-8 rounded-3xl border p-3 sm:p-4">
        <div className="grid grid-cols-2 gap-2 sm:grid-cols-4 sm:gap-3">
          {TIERS.map((tier, index) => (
            <ScaleTile key={tier.id} tier={tier} index={index} />
          ))}
        </div>
        <p className="text-cc-nav-label mt-4 px-2 font-mono text-[0.65rem] tracking-[0.15em] uppercase">
          ~Estimate only · billing is per actual operation
        </p>
      </div>
    </section>
  );
}

interface ScaleTileProps {
  readonly tier: Tier;
  readonly index: number;
}

function ScaleTile({ tier, index }: ScaleTileProps) {
  // Pure CSS highlight: clicking the tile sets the URL hash to #scale-<id>,
  // which makes :target match and lights up the tile + reveals the matching
  // recommendation underneath. No client JS needed.
  return (
    <a
      id={`scale-${tier.id}`}
      href={`#scale-${tier.id}`}
      className="group border-cc-card-border hover:border-cc-card-border-hover target:border-cc-accent target:bg-cc-accent/5 bg-cc-card-bg/60 relative flex flex-col rounded-2xl border px-4 py-5 text-left no-underline transition-colors"
    >
      <span className="text-cc-nav-label font-mono text-[0.65rem] tracking-[0.15em] uppercase">
        Tier {index + 1}
      </span>
      <span className="font-heading text-cc-heading text-h6 mt-2 font-semibold">
        {tier.label}
      </span>
      <span className="text-cc-accent mt-1 font-mono text-xs">{tier.ops}</span>
      <span className="text-cc-ink-dim mt-3 text-sm">{tier.blurb}</span>
      <span
        aria-hidden="true"
        className="text-cc-accent mt-4 font-mono text-[0.65rem] tracking-[0.15em] uppercase opacity-0 group-target:opacity-100"
      >
        ▸ Recommended below
      </span>
    </a>
  );
}

function PlanGrid() {
  return (
    <section
      aria-labelledby="plans-heading"
      className="mt-20 scroll-mt-24 sm:mt-24"
      id="plans"
    >
      <div className="text-center">
        <p className="text-cc-nav-label font-mono text-xs tracking-[0.2em] uppercase">
          Step 2 · Pick your plan
        </p>
        <h2
          id="plans-heading"
          className="font-heading text-cc-heading text-h3 mt-3 font-semibold"
        >
          Three ways to run Nitro.
        </h2>
      </div>

      <div className="mt-10 grid gap-6 lg:grid-cols-3">
        {PLANS.map((plan) => (
          <PlanCard key={plan.id} plan={plan} />
        ))}
      </div>
    </section>
  );
}

interface PlanCardProps {
  readonly plan: Plan;
}

function PlanCard({ plan }: PlanCardProps) {
  const CallToActionButton = plan.popular ? SolidButton : OutlineButton;
  return (
    <article
      className={`relative flex h-full flex-col rounded-3xl border p-6 sm:p-7 ${
        plan.popular
          ? "border-cc-accent bg-cc-card-bg"
          : "border-cc-card-border bg-cc-card-bg/60"
      } `}
    >
      {plan.popular && (
        <span className="bg-cc-accent text-cc-surface absolute -top-3 left-6 rounded-full px-3 py-1 font-mono text-[0.65rem] tracking-[0.15em] uppercase">
          Popular
        </span>
      )}

      <header>
        <p className="text-cc-nav-label font-mono text-[0.65rem] tracking-[0.15em] uppercase">
          {plan.fitFor}
        </p>
        <h3 className="font-heading text-cc-heading text-h5 mt-2 font-semibold">
          {plan.name}
        </h3>
        <p className="text-cc-nav-label mt-1 font-mono text-xs">
          {plan.description}
        </p>
      </header>

      <div className="mt-6 flex items-baseline gap-2">
        <span className="font-heading text-cc-heading text-h3 font-semibold">
          {plan.price}
        </span>
        <span className="text-cc-nav-label font-mono text-xs">
          {plan.priceNote}
        </span>
      </div>

      <div
        aria-hidden="true"
        className="border-cc-ink-faint my-6 border-t border-dashed"
      />

      <ul className="flex flex-1 flex-col gap-3">
        {plan.features.map((feature) => (
          <li key={feature} className="flex items-start gap-3">
            <span className="text-cc-accent mt-1 flex-none">
              <CheckIcon />
            </span>
            <span className="text-cc-ink text-sm">{feature}</span>
          </li>
        ))}
      </ul>

      <CallToActionButton href={plan.ctaHref} className="mt-8 w-full">
        {plan.cta}
      </CallToActionButton>
    </article>
  );
}

function CompareTable() {
  return (
    <section
      aria-labelledby="compare-heading"
      className="mt-24 scroll-mt-24 sm:mt-28"
      id="compare"
    >
      <div className="text-center">
        <p className="text-cc-nav-label font-mono text-xs tracking-[0.2em] uppercase">
          Step 3 · Compare the details
        </p>
        <h2
          id="compare-heading"
          className="font-heading text-cc-heading text-h3 mt-3 font-semibold"
        >
          Feature comparison
        </h2>
      </div>

      <div className="border-cc-card-border bg-cc-card-bg/40 mt-10 overflow-hidden rounded-3xl border">
        <div className="overflow-x-auto">
          <table className="w-full min-w-[640px] border-collapse text-left text-sm">
            <thead>
              <tr className="border-cc-card-border border-b">
                <th
                  scope="col"
                  className="text-cc-nav-label px-5 py-4 font-mono text-[0.65rem] tracking-[0.15em] uppercase"
                >
                  Capability
                </th>
                <th
                  scope="col"
                  className="text-cc-heading font-heading px-5 py-4 text-sm font-semibold"
                >
                  Shared
                </th>
                <th
                  scope="col"
                  className="text-cc-heading font-heading px-5 py-4 text-sm font-semibold"
                >
                  <span className="text-cc-accent">Dedicated</span>
                </th>
                <th
                  scope="col"
                  className="text-cc-heading font-heading px-5 py-4 text-sm font-semibold"
                >
                  Self-Hosted
                </th>
              </tr>
            </thead>
            {COMPARE_GROUPS.map((group, groupIndex) => (
              <tbody key={group.title}>
                <tr
                  className={`bg-cc-card-bg/60 ${
                    groupIndex === 0 ? "" : "border-cc-card-border border-t"
                  }`}
                >
                  <th
                    scope="colgroup"
                    colSpan={4}
                    className="text-cc-nav-label px-5 py-3 text-left font-mono text-[0.65rem] tracking-[0.15em] uppercase"
                  >
                    {group.title}
                  </th>
                </tr>
                {group.rows.map((row) => (
                  <tr
                    key={row.feature}
                    className="border-cc-ink-faint border-b last:border-0"
                  >
                    <th
                      scope="row"
                      className="text-cc-ink px-5 py-3 align-top text-sm font-medium"
                    >
                      <span>{row.feature}</span>
                      {row.note && (
                        <span className="text-cc-ink-dim mt-1 block text-xs font-normal">
                          {row.note}
                        </span>
                      )}
                    </th>
                    <CompareCell>{row.shared}</CompareCell>
                    <CompareCell>{row.dedicated}</CompareCell>
                    <CompareCell>{row.self}</CompareCell>
                  </tr>
                ))}
              </tbody>
            ))}
          </table>
        </div>
      </div>
    </section>
  );
}

function CompareCell({ children }: { readonly children: ReactNode }) {
  const isEmpty = children === "Not included";
  const isIncluded = children === "Included";
  return (
    <td
      className={`px-5 py-3 align-top font-mono text-xs ${
        isEmpty
          ? "text-cc-ink-dim"
          : isIncluded
            ? "text-cc-accent"
            : "text-cc-ink"
      }`}
    >
      {children}
    </td>
  );
}

function Faq() {
  return (
    <section
      aria-labelledby="faq-heading"
      className="mt-24 scroll-mt-24 sm:mt-28"
      id="faq"
    >
      <div className="text-center">
        <p className="text-cc-nav-label font-mono text-xs tracking-[0.2em] uppercase">
          FAQ
        </p>
        <h2
          id="faq-heading"
          className="font-heading text-cc-heading text-h3 mt-3 font-semibold"
        >
          Common questions
        </h2>
      </div>

      <div className="mt-10 grid gap-4 md:grid-cols-2">
        {FAQS.map((faq) => (
          <details
            key={faq.q}
            className="group border-cc-card-border hover:border-cc-card-border-hover bg-cc-card-bg/60 rounded-2xl border p-5 transition-colors"
          >
            <summary className="text-cc-heading font-heading flex cursor-pointer list-none items-start justify-between gap-4 text-base font-semibold">
              <span>{faq.q}</span>
              <span
                aria-hidden="true"
                className="text-cc-accent mt-1 flex-none font-mono text-sm transition-transform group-open:rotate-45"
              >
                +
              </span>
            </summary>
            <div className="text-cc-ink mt-3 text-sm">{faq.a}</div>
          </details>
        ))}
      </div>
    </section>
  );
}

function EnterpriseBand() {
  return (
    <section
      aria-labelledby="enterprise-heading"
      className="border-cc-card-border bg-cc-card-bg/60 mt-24 rounded-3xl border p-8 sm:mt-28 sm:p-12"
    >
      <div className="grid items-center gap-8 md:grid-cols-[1.4fr_1fr]">
        <div>
          <p className="text-cc-nav-label font-mono text-xs tracking-[0.2em] uppercase">
            Enterprise
          </p>
          <h2
            id="enterprise-heading"
            className="font-heading text-cc-heading text-h3 mt-3 font-semibold text-balance"
          >
            Regulated industry, air-gapped, or a custom SLA?
          </h2>
          <p className="text-cc-ink mt-4 max-w-xl text-base text-pretty">
            We work directly with platform teams on procurement, data residency
            review, custom SLAs, and dedicated onboarding. Bring us a
            constraint, we&rsquo;ll come back with an architecture.
          </p>
          <div className="mt-6 flex flex-wrap gap-3">
            <SolidButton href="/services/support/contact">
              Talk to Sales
            </SolidButton>
            <OutlineButton href="/platform">See the platform</OutlineButton>
          </div>
        </div>
        <ul className="grid gap-3">
          {[
            "Procurement, MSA, and security review",
            "BYOC or fully on-prem deployments",
            "Dedicated onboarding & runbooks",
            "Custom SLA and incident escalation",
          ].map((item) => (
            <li
              key={item}
              className="border-cc-card-border bg-cc-bg/40 flex items-start gap-3 rounded-xl border px-4 py-3"
            >
              <span className="text-cc-accent mt-1 flex-none">
                <CheckIcon />
              </span>
              <span className="text-cc-ink text-sm">{item}</span>
            </li>
          ))}
        </ul>
      </div>
    </section>
  );
}

function ClosingCta() {
  return (
    <section className="mt-20 mb-8 text-center sm:mt-24">
      <h2 className="font-heading text-cc-heading text-h3 font-semibold">
        Ready to ship faster?
      </h2>
      <p className="text-cc-ink mx-auto mt-4 max-w-2xl text-base">
        Spin up a free Shared Instance in minutes, or browse the docs to see how
        Nitro fits into your existing CI and gateway.
      </p>
      <div className="mt-7 flex flex-wrap items-center justify-center gap-3">
        <SolidButton href="/get-started">Start for Free</SolidButton>
        <OutlineButton href="/docs">Read the Docs</OutlineButton>
      </div>
    </section>
  );
}

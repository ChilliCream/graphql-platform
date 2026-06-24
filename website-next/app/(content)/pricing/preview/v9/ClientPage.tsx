"use client";

import { MotionConfig, motion, useReducedMotion } from "motion/react";
import type { ReactNode } from "react";

import { CheckIcon } from "@/src/components/CheckIcon";
import { OutlineButton, SolidButton } from "@/src/design-system/Button";

// Concept: Tier Cascade. As each section enters view, the three tiers cascade
// in left to right, then their inner content (feature checks, comparison rows,
// FAQ cards) staggers in once and stays. No scroll coupling: every reveal uses
// whileInView with viewport={{ once: true }}. Honors prefers-reduced-motion via
// useReducedMotion to collapse animations to opacity-only with zero delay.

interface Plan {
  readonly id: "shared" | "dedicated" | "self";
  readonly name: string;
  readonly tagline: string;
  readonly price: string;
  readonly priceNote: string;
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
    tagline: "Dedicated resources, fully managed.",
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
    tagline: "Self managed, on your infrastructure.",
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
          "Client registry · know which published clients are affected by a change",
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

const EASE_OUT_QUART: readonly [number, number, number, number] = [
  0.22, 1, 0.36, 1,
];

const VIEWPORT_ONCE = { once: true, margin: "-10% 0px" } as const;

// Plan card cascade timing: left to right at 0, 0.08, 0.16s.
const CASCADE_DELAYS: Readonly<Record<Plan["id"], number>> = {
  shared: 0,
  dedicated: 0.08,
  self: 0.16,
};

export function ClientPage() {
  return (
    <MotionConfig reducedMotion="user">
      <Hero />
      <PlanTierStrip />
      <ScaleStrip />
      <ComparisonMatrix />
      <Faq />
      <EnterpriseBand />
      <ClosingCta />
    </MotionConfig>
  );
}

function Hero() {
  return (
    <section className="relative pt-10 pb-14 sm:pt-16 sm:pb-20">
      {/* Faint cc-accent radial glow anchored top-left */}
      <div
        aria-hidden="true"
        className="pointer-events-none absolute inset-0 -z-10"
        style={{
          background:
            "radial-gradient(60% 50% at 0% 0%, rgba(94, 234, 212, 0.06), transparent 70%)",
        }}
      />
      <div className="grid gap-10 md:grid-cols-[1.4fr_1fr] md:items-start">
        <div>
          <motion.p
            initial={{ opacity: 0, y: 8 }}
            animate={{ opacity: 1, y: 0 }}
            transition={{ duration: 0.5, ease: EASE_OUT_QUART }}
            className="text-cc-nav-label font-mono text-xs tracking-[0.18em] uppercase"
          >
            Nitro GraphQL pricing
          </motion.p>
          <motion.h1
            initial={{ opacity: 0, y: 12 }}
            animate={{ opacity: 1, y: 0 }}
            transition={{ duration: 0.6, ease: EASE_OUT_QUART, delay: 0.06 }}
            className="font-heading text-cc-heading sm:text-h2 mt-5 text-4xl font-semibold"
          >
            Pricing that cascades with your GraphQL platform.
          </motion.h1>
          <motion.p
            initial={{ opacity: 0, y: 12 }}
            animate={{ opacity: 1, y: 0 }}
            transition={{ duration: 0.6, ease: EASE_OUT_QUART, delay: 0.14 }}
            className="text-cc-ink mt-6 max-w-2xl text-base text-pretty sm:text-lg"
          >
            Start free on shared cloud. Move to a dedicated instance when you
            need SLA, SSO, and your own region. Self-host on your own
            infrastructure when the workload, or the policy, demands it.
          </motion.p>
          <motion.div
            initial={{ opacity: 0, y: 8 }}
            animate={{ opacity: 1, y: 0 }}
            transition={{ duration: 0.5, ease: EASE_OUT_QUART, delay: 0.22 }}
            className="mt-9 flex flex-wrap items-center gap-3"
          >
            <SolidButton href="/get-started">Start for Free</SolidButton>
            <OutlineButton href="/services/support/contact">
              Talk to Sales
            </OutlineButton>
          </motion.div>
        </div>

        <HeroLegend />
      </div>
    </section>
  );
}

function HeroLegend() {
  const items: readonly {
    readonly mono: string;
    readonly label: string;
    readonly note: string;
  }[] = [
    {
      mono: "SR",
      label: "Schema registry",
      note: "History, rollback, CI checks",
    },
    {
      mono: "OT",
      label: "OpenTelemetry-native",
      note: "Traces, metrics, logs",
    },
    {
      mono: "FX",
      label: "Fusion + ASP.NET Core",
      note: "Gateway you run yourself",
    },
  ];

  return (
    <motion.aside
      initial={{ opacity: 0, y: 12 }}
      animate={{ opacity: 1, y: 0 }}
      transition={{ duration: 0.6, ease: EASE_OUT_QUART, delay: 0.18 }}
      className="border-cc-card-border bg-cc-card-bg/60 rounded-2xl border p-6"
      aria-label="Included on every tier"
    >
      <p className="text-cc-nav-label font-mono text-xs tracking-[0.18em] uppercase">
        Included on every tier
      </p>
      <ul className="mt-5 flex flex-col gap-4">
        {items.map((item) => (
          <li key={item.label} className="flex items-start gap-3">
            <span
              aria-hidden="true"
              className="border-cc-card-border text-cc-accent bg-cc-surface flex h-9 w-9 flex-none items-center justify-center rounded-full border font-mono text-[0.65rem] tracking-[0.12em] uppercase"
            >
              {item.mono}
            </span>
            <span className="flex flex-col">
              <span className="text-cc-heading font-heading text-sm font-semibold">
                {item.label}
              </span>
              <span className="text-cc-ink-dim text-xs">{item.note}</span>
            </span>
          </li>
        ))}
      </ul>
    </motion.aside>
  );
}

function PlanTierStrip() {
  return (
    <section
      aria-labelledby="plans-heading"
      className="relative pb-16 sm:pb-24"
    >
      <h2 id="plans-heading" className="sr-only">
        Plans
      </h2>
      {/* Connected baseline rail behind cards, suggesting a tier ladder. */}
      <div
        aria-hidden="true"
        className="bg-cc-card-border pointer-events-none absolute right-8 left-8 hidden h-px lg:block"
        style={{ top: "calc(50% + 1.5rem)" }}
      />
      <div className="relative grid gap-6 lg:grid-cols-3 lg:items-stretch">
        {PLANS.map((plan) => (
          <PlanCard key={plan.id} plan={plan} />
        ))}
      </div>
    </section>
  );
}

function PlanCard({ plan }: { readonly plan: Plan }) {
  const delay = CASCADE_DELAYS[plan.id];
  const cardInitial = { opacity: 0, y: 12 };
  const cardAnimate = { opacity: 1, y: 0 };
  const cardTransition = {
    duration: 0.55,
    ease: EASE_OUT_QUART,
    delay,
  } as const;

  if (plan.popular) {
    return (
      <motion.div
        initial={cardInitial}
        whileInView={cardAnimate}
        viewport={VIEWPORT_ONCE}
        transition={cardTransition}
        className="relative rounded-3xl p-[1.5px] lg:-my-2"
        style={{
          background:
            "linear-gradient(140deg, #16b9e4 0%, #7c92c6 50%, #f0786a 100%)",
        }}
      >
        <PopularPill />
        <div className="bg-cc-surface flex h-full flex-col rounded-[calc(1.5rem-1.5px)] p-7 sm:p-8">
          <PlanCardBody plan={plan} cascadeDelay={delay} />
        </div>
      </motion.div>
    );
  }

  return (
    <motion.div
      initial={cardInitial}
      whileInView={cardAnimate}
      viewport={VIEWPORT_ONCE}
      transition={cardTransition}
      className="bg-cc-card-bg border-cc-card-border hover:border-cc-card-border-hover flex h-full flex-col rounded-3xl border p-7 transition-colors sm:p-8"
    >
      <PlanCardBody plan={plan} cascadeDelay={delay} />
    </motion.div>
  );
}

function PlanCardBody({
  plan,
  cascadeDelay,
}: {
  readonly plan: Plan;
  readonly cascadeDelay: number;
}) {
  const CallToAction = plan.popular ? SolidButton : OutlineButton;
  // Inner stagger starts shortly after the card has landed.
  const innerDelay = cascadeDelay + 0.25;

  return (
    <>
      <h3 className="font-heading text-cc-heading text-h5 font-semibold">
        {plan.name}
      </h3>
      <p className="text-cc-ink-dim mt-2 text-sm">{plan.tagline}</p>
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
      <motion.ul
        initial="hidden"
        whileInView="show"
        viewport={VIEWPORT_ONCE}
        variants={{
          hidden: {},
          show: {
            transition: {
              staggerChildren: 0.04,
              delayChildren: innerDelay,
            },
          },
        }}
        className="flex flex-1 flex-col gap-3"
      >
        {plan.features.map((feature) => (
          <motion.li
            key={feature}
            variants={{
              hidden: { opacity: 0, y: 4 },
              show: {
                opacity: 1,
                y: 0,
                transition: { duration: 0.3, ease: EASE_OUT_QUART },
              },
            }}
            className="flex items-start gap-3"
          >
            <span className="text-cc-accent mt-[5px] flex-none">
              <CheckIcon />
            </span>
            <span className="text-cc-ink text-sm">{feature}</span>
          </motion.li>
        ))}
      </motion.ul>
      <CallToAction href={plan.ctaHref} className="mt-8 w-full">
        {plan.cta}
      </CallToAction>
    </>
  );
}

function PopularPill() {
  return (
    <span className="bg-cc-surface text-cc-accent border-cc-accent absolute top-0 left-1/2 z-10 -translate-x-1/2 -translate-y-1/2 rounded-full border px-4 py-1 font-mono text-[0.65rem] tracking-[0.18em] whitespace-nowrap uppercase">
      Most popular
    </span>
  );
}

function ScaleStrip() {
  const stats: readonly {
    readonly headline: string;
    readonly label: string;
    readonly note: string;
  }[] = [
    {
      headline: "5M ops",
      label: "Included on Shared",
      note: "Pay-as-you-go beyond.",
    },
    {
      headline: "99.95%",
      label: "Dedicated uptime SLA",
      note: "Email + private chat.",
    },
    {
      headline: "BYOC",
      label: "Choose your region",
      note: "Private networking.",
    },
  ];

  return (
    <section aria-label="What scales with you" className="pb-16 sm:pb-24">
      <motion.div
        initial="hidden"
        whileInView="show"
        viewport={VIEWPORT_ONCE}
        variants={{
          hidden: {},
          show: { transition: { staggerChildren: 0.08, delayChildren: 0.05 } },
        }}
        className="grid gap-4 sm:grid-cols-3"
      >
        {stats.map((stat) => (
          <motion.div
            key={stat.headline}
            variants={{
              hidden: { opacity: 0, y: 10 },
              show: {
                opacity: 1,
                y: 0,
                transition: { duration: 0.45, ease: EASE_OUT_QUART },
              },
            }}
            className="border-cc-card-border bg-cc-card-bg/60 rounded-2xl border p-5"
          >
            <div className="font-heading text-cc-heading text-h4 font-semibold">
              {stat.headline}
            </div>
            <div className="text-cc-nav-label mt-2 font-mono text-[0.65rem] tracking-[0.15em] uppercase">
              {stat.label}
            </div>
            <div className="text-cc-ink mt-3 text-sm">{stat.note}</div>
          </motion.div>
        ))}
      </motion.div>
    </section>
  );
}

function ComparisonMatrix() {
  return (
    <section
      aria-labelledby="compare-heading"
      className="border-cc-card-border bg-cc-card-bg/40 relative rounded-3xl border p-6 sm:p-10"
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

      <MatrixGrid />
    </section>
  );
}

function MatrixGrid() {
  return (
    <div className="mt-10">
      {/* Column header row */}
      <div className="grid grid-cols-[minmax(0,1.6fr)_minmax(0,1fr)_minmax(0,1fr)_minmax(0,1fr)] items-end gap-x-4 pb-4">
        <div className="sr-only">Capability</div>
        {PLANS.map((plan) => (
          <div key={plan.id} className="px-2 text-center">
            <div
              className={`font-heading text-cc-heading text-sm font-semibold sm:text-base ${
                plan.popular ? "text-cc-accent" : ""
              }`}
            >
              {plan.name}
            </div>
            <div className="text-cc-nav-label mt-1 font-mono text-[0.6rem] tracking-[0.15em] uppercase">
              {plan.price} · {plan.priceNote}
            </div>
          </div>
        ))}
      </div>

      <div className="flex flex-col">
        {COMPARISON.map((group) => (
          <MatrixGroup key={group.title} group={group} />
        ))}
      </div>
    </div>
  );
}

function MatrixGroup({ group }: { readonly group: ComparisonGroup }) {
  return (
    <motion.section
      initial="hidden"
      whileInView="show"
      viewport={VIEWPORT_ONCE}
      variants={{
        hidden: {},
        show: { transition: { staggerChildren: 0.03, delayChildren: 0.18 } },
      }}
      aria-label={group.title}
      className="border-cc-ink-faint border-t"
    >
      <motion.header
        variants={{
          hidden: { opacity: 0, x: -8 },
          show: {
            opacity: 1,
            x: 0,
            transition: { duration: 0.4, ease: EASE_OUT_QUART },
          },
        }}
        className="grid grid-cols-[minmax(0,1.6fr)_minmax(0,1fr)_minmax(0,1fr)_minmax(0,1fr)] items-center gap-x-4 pt-6 pb-3"
      >
        <span className="text-cc-nav-label inline-flex items-center pl-2 font-mono text-xs tracking-[0.15em] uppercase">
          <span
            aria-hidden="true"
            className="bg-cc-accent mr-3 inline-block h-1 w-3 rounded-full"
          />
          {group.title}
        </span>
        {/* Dedicated soft tint as a column hint */}
        <span aria-hidden="true" />
        <span aria-hidden="true" className="bg-cc-accent/5 h-full rounded-sm" />
        <span aria-hidden="true" />
      </motion.header>

      <div role="list" className="flex flex-col">
        {group.rows.map((row) => (
          <motion.div
            key={row.label}
            role="listitem"
            variants={{
              hidden: { opacity: 0, y: 4 },
              show: {
                opacity: 1,
                y: 0,
                transition: { duration: 0.32, ease: EASE_OUT_QUART },
              },
            }}
            className="border-cc-ink-faint/50 grid grid-cols-[minmax(0,1.6fr)_minmax(0,1fr)_minmax(0,1fr)_minmax(0,1fr)] items-start gap-x-4 border-t py-3"
          >
            <div className="text-cc-ink pl-2 text-sm">{row.label}</div>
            <MatrixCell value={row.shared} />
            <MatrixCell value={row.dedicated} highlight />
            <MatrixCell value={row.self} />
          </motion.div>
        ))}
      </div>
    </motion.section>
  );
}

function MatrixCell({
  value,
  highlight = false,
}: {
  readonly value: CellValue;
  readonly highlight?: boolean;
}) {
  return (
    <div
      className={`flex min-h-[1.5rem] items-start justify-center px-2 text-center text-sm ${
        highlight ? "bg-cc-accent/5 rounded-sm" : ""
      }`}
    >
      {typeof value === "boolean" ? (
        value ? (
          <span
            className="text-cc-accent inline-flex pt-[2px]"
            aria-label="Included"
          >
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
    </div>
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

      <motion.dl
        initial="hidden"
        whileInView="show"
        viewport={VIEWPORT_ONCE}
        variants={{
          hidden: {},
          // Pairwise stagger: two cards per row, each pair starts 0.08s later.
          show: { transition: { staggerChildren: 0.04, delayChildren: 0.05 } },
        }}
        className="mt-10 grid gap-4 md:grid-cols-2"
      >
        {FAQ.map((item) => (
          <FaqEntry key={item.question} item={item} />
        ))}
      </motion.dl>
    </section>
  );
}

function FaqEntry({ item }: { readonly item: FaqItem }) {
  const reduce = useReducedMotion();
  return (
    <motion.div
      variants={{
        hidden: { opacity: 0, y: reduce ? 0 : 8 },
        show: {
          opacity: 1,
          y: 0,
          transition: { duration: 0.4, ease: EASE_OUT_QUART },
        },
      }}
      className="border-cc-card-border bg-cc-card-bg/60 rounded-2xl border p-6"
    >
      <dt className="font-heading text-cc-heading text-base font-semibold">
        {item.question}
      </dt>
      <dd className="text-cc-ink mt-3 text-sm leading-relaxed">
        {item.answer}
      </dd>
    </motion.div>
  );
}

function EnterpriseBand() {
  return (
    <Section className="mt-20 sm:mt-28">
      <motion.div
        initial={{ opacity: 0, y: 12 }}
        whileInView={{ opacity: 1, y: 0 }}
        viewport={VIEWPORT_ONCE}
        transition={{ duration: 0.55, ease: EASE_OUT_QUART }}
        className="border-cc-card-border bg-cc-card-bg/70 grid gap-8 rounded-3xl border p-8 sm:p-12 md:grid-cols-[1.4fr_1fr] md:items-center"
      >
        <div>
          <p className="text-cc-nav-label font-mono text-xs tracking-[0.18em] uppercase">
            Enterprise
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
            <EnterpriseCheck>Annual contracts & POs</EnterpriseCheck>
            <EnterpriseCheck>Security & DPA review</EnterpriseCheck>
            <EnterpriseCheck>Migration playbooks</EnterpriseCheck>
          </ul>
        </div>
        <div className="flex flex-col gap-3 md:items-end">
          <SolidButton href="/services/support/contact">
            Contact Sales
          </SolidButton>
          <OutlineButton href="/platform">Explore the platform</OutlineButton>
        </div>
      </motion.div>
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
      <motion.h2
        initial={{ opacity: 0, y: 10 }}
        whileInView={{ opacity: 1, y: 0 }}
        viewport={VIEWPORT_ONCE}
        transition={{ duration: 0.5, ease: EASE_OUT_QUART }}
        className="font-heading text-cc-heading text-h4 sm:text-h3 font-semibold"
      >
        Ship your GraphQL platform with Nitro.
      </motion.h2>
      <motion.p
        initial={{ opacity: 0, y: 10 }}
        whileInView={{ opacity: 1, y: 0 }}
        viewport={VIEWPORT_ONCE}
        transition={{ duration: 0.5, ease: EASE_OUT_QUART, delay: 0.08 }}
        className="text-cc-ink mx-auto mt-5 max-w-2xl text-base"
      >
        Start on the free Shared Instance in minutes. Upgrade when you need a
        dedicated region, SLA, or SSO. The docs walk you through every step.
      </motion.p>
      <motion.div
        initial={{ opacity: 0, y: 8 }}
        whileInView={{ opacity: 1, y: 0 }}
        viewport={VIEWPORT_ONCE}
        transition={{ duration: 0.5, ease: EASE_OUT_QUART, delay: 0.16 }}
        className="mt-8 flex flex-wrap items-center justify-center gap-3"
      >
        <SolidButton href="/get-started">Start for Free</SolidButton>
        <OutlineButton href="/docs">Read the docs</OutlineButton>
      </motion.div>
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

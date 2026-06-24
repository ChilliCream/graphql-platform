"use client";

import {
  MotionConfig,
  animate,
  motion,
  useInView,
  useMotionValue,
  useReducedMotion,
  useTransform,
} from "motion/react";
import type { ReactNode } from "react";
import { useEffect, useRef } from "react";

import { CheckIcon } from "@/src/components/CheckIcon";
import { OutlineButton, SolidButton } from "@/src/design-system/Button";

// Note: This page is a "use client" file because the centerpiece animation
// relies on motion hooks (useInView, useMotionValue, useTransform, animate,
// useReducedMotion). Next.js does not allow `export const metadata` from a
// client component, so robots/no-index for this preview path is enforced via
// the parent route configuration. The page is at /pricing/preview/v7/ which
// is an internal preview path and not surfaced in production navigation.
// Primary keyword: Nitro GraphQL pricing.

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
  readonly priceNumeric?: number;
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
    priceNumeric: 400,
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

export default function PricingPreviewV7Page() {
  return (
    <MotionConfig reducedMotion="user">
      <Hero />
      <PlanGrid />
      <UpgradePathStrip />
      <ComparisonMatrix />
      <Faq />
      <EnterpriseBand />
      <ClosingCta />
    </MotionConfig>
  );
}

function Hero() {
  return (
    <section className="pt-10 pb-14 text-center sm:pt-16 sm:pb-20">
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
        transition={{ duration: 0.6, ease: EASE_OUT_QUART, delay: 0.08 }}
        className="font-heading text-cc-heading sm:text-h2 mt-5 text-4xl font-semibold"
      >
        Pricing that scales with your GraphQL platform.
      </motion.h1>
      <motion.p
        initial={{ opacity: 0, y: 12 }}
        animate={{ opacity: 1, y: 0 }}
        transition={{ duration: 0.6, ease: EASE_OUT_QUART, delay: 0.16 }}
        className="text-cc-ink mx-auto mt-6 max-w-2xl text-base text-pretty sm:text-lg"
      >
        Start free on shared cloud. Move to a dedicated instance when you need
        SLA, SSO, and your own region. Self-host on your own infrastructure when
        the workload, or the policy, demands it.
      </motion.p>
      <motion.div
        initial={{ opacity: 0, y: 8 }}
        animate={{ opacity: 1, y: 0 }}
        transition={{ duration: 0.5, ease: EASE_OUT_QUART, delay: 0.24 }}
        className="mt-9 flex flex-wrap items-center justify-center gap-3"
      >
        <SolidButton href="/get-started">Start for Free</SolidButton>
        <OutlineButton href="/services/support/contact">
          Talk to Sales
        </OutlineButton>
      </motion.div>
    </section>
  );
}

function PlanGrid() {
  const containerRef = useRef<HTMLDivElement>(null);
  const inView = useInView(containerRef, { once: true, amount: 0.35 });

  return (
    <section aria-labelledby="plans-heading" className="pb-16 sm:pb-24">
      <h2 id="plans-heading" className="sr-only">
        Plans
      </h2>
      <motion.div
        ref={containerRef}
        variants={{
          hidden: {},
          show: { transition: { staggerChildren: 0.12, delayChildren: 0.05 } },
        }}
        initial="hidden"
        animate={inView ? "show" : "hidden"}
        className="grid gap-6 lg:grid-cols-3 lg:items-stretch"
      >
        {PLANS.map((plan) => (
          <PlanCard key={plan.id} plan={plan} parentInView={inView} />
        ))}
      </motion.div>
    </section>
  );
}

function PlanCard({
  plan,
  parentInView,
}: {
  readonly plan: Plan;
  readonly parentInView: boolean;
}) {
  const cardVariants = {
    hidden: { opacity: 0, y: 24 },
    show: {
      opacity: 1,
      y: 0,
      transition: { duration: 0.5, ease: EASE_OUT_QUART },
    },
  } as const;

  if (plan.popular) {
    return (
      <motion.div
        variants={cardVariants}
        className="relative rounded-3xl p-[1.5px] lg:-my-2"
        style={{
          background:
            "linear-gradient(140deg, #16b9e4 0%, #7c92c6 50%, #f0786a 100%)",
        }}
      >
        <PopularPill />
        <DedicatedGradientFrame active={parentInView} />
        <div className="bg-cc-surface relative flex h-full flex-col rounded-[calc(1.5rem-1.5px)] p-7 sm:p-8">
          <PlanCardBody plan={plan} active={parentInView} />
        </div>
      </motion.div>
    );
  }

  return (
    <motion.div
      variants={cardVariants}
      className="bg-cc-card-bg border-cc-card-border hover:border-cc-card-border-hover flex h-full flex-col rounded-3xl border p-7 transition-colors sm:p-8"
    >
      <PlanCardBody plan={plan} active={parentInView} />
    </motion.div>
  );
}

function DedicatedGradientFrame({ active }: { readonly active: boolean }) {
  const reduce = useReducedMotion();
  return (
    <svg
      aria-hidden="true"
      className="pointer-events-none absolute inset-0 h-full w-full"
      preserveAspectRatio="none"
      viewBox="0 0 100 100"
    >
      <defs>
        <linearGradient id="dedicatedGradient-v7" x1="0" y1="0" x2="1" y2="1">
          <stop offset="0%" stopColor="#16b9e4" />
          <stop offset="50%" stopColor="#7c92c6" />
          <stop offset="100%" stopColor="#f0786a" />
        </linearGradient>
      </defs>
      <motion.rect
        x="1"
        y="1"
        width="98"
        height="98"
        rx="5"
        ry="5"
        fill="none"
        stroke="url(#dedicatedGradient-v7)"
        strokeWidth="0.6"
        vectorEffect="non-scaling-stroke"
        initial={reduce ? { pathLength: 1 } : { pathLength: 0 }}
        animate={active ? { pathLength: 1 } : { pathLength: 0 }}
        transition={{ duration: 1.2, ease: "easeInOut", delay: 0.4 }}
      />
    </svg>
  );
}

function PlanCardBody({
  plan,
  active,
}: {
  readonly plan: Plan;
  readonly active: boolean;
}) {
  const CallToAction = plan.popular ? SolidButton : OutlineButton;
  return (
    <>
      <h3 className="font-heading text-cc-heading text-h5 font-semibold">
        {plan.name}
      </h3>
      <p className="text-cc-ink-dim mt-2 text-sm">{plan.tagline}</p>
      <div className="mt-6 flex items-baseline gap-2">
        <span className="font-heading text-cc-heading text-h3 font-semibold">
          {plan.priceNumeric !== undefined ? (
            <AnimatedPrice target={plan.priceNumeric} active={active} />
          ) : (
            plan.price
          )}
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
        variants={{
          hidden: {},
          show: { transition: { staggerChildren: 0.06, delayChildren: 0.3 } },
        }}
        className="flex flex-1 flex-col gap-3"
      >
        {plan.features.map((feature) => (
          <motion.li
            key={feature}
            variants={{
              hidden: { opacity: 0, x: -6 },
              show: {
                opacity: 1,
                x: 0,
                transition: { duration: 0.35, ease: EASE_OUT_QUART },
              },
            }}
            className="flex items-start gap-3"
          >
            <motion.span
              variants={{
                hidden: { scale: 0 },
                show: {
                  scale: 1,
                  transition: { duration: 0.3, ease: EASE_OUT_QUART },
                },
              }}
              className="text-cc-accent mt-[5px] flex-none"
            >
              <CheckIcon />
            </motion.span>
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

function AnimatedPrice({
  target,
  active,
}: {
  readonly target: number;
  readonly active: boolean;
}) {
  const reduce = useReducedMotion();
  const count = useMotionValue(reduce ? target : 0);
  const display = useTransform(count, (v) => `$${Math.round(v)}`);

  useEffect(() => {
    if (!active) {
      return;
    }
    if (reduce) {
      count.set(target);
      return;
    }
    const controls = animate(count, target, {
      duration: 1.1,
      ease: "easeOut",
      delay: 0.4,
    });
    return () => controls.stop();
  }, [active, count, reduce, target]);

  return <motion.span aria-label={`$${target}`}>{display}</motion.span>;
}

function PopularPill() {
  return (
    <span className="bg-cc-surface text-cc-accent border-cc-accent absolute top-0 left-1/2 z-10 -translate-x-1/2 -translate-y-1/2 rounded-full border px-4 py-1 font-mono text-[0.65rem] tracking-[0.18em] whitespace-nowrap uppercase">
      Most popular
    </span>
  );
}

function UpgradePathStrip() {
  const ref = useRef<HTMLDivElement>(null);
  const inView = useInView(ref, { once: true, amount: 0.5 });
  const reduce = useReducedMotion();

  const stops: readonly {
    readonly label: string;
    readonly position: string;
  }[] = [
    { label: "Shared", position: "0%" },
    { label: "Dedicated", position: "50%" },
    { label: "Self-Hosted", position: "100%" },
  ];

  return (
    <section
      aria-label="Upgrade path"
      className="mb-16 hidden sm:mb-24 lg:block"
    >
      <div ref={ref} className="relative mx-auto max-w-4xl px-4 py-2">
        <p className="text-cc-nav-label mb-4 text-center font-mono text-xs tracking-[0.18em] uppercase">
          Upgrade path
        </p>
        <div className="relative h-10">
          <motion.div
            initial={reduce ? { scaleX: 1 } : { scaleX: 0 }}
            animate={inView ? { scaleX: 1 } : { scaleX: 0 }}
            transition={{ duration: 1, ease: EASE_OUT_QUART }}
            style={{ transformOrigin: "left" }}
            className="bg-cc-ink-faint absolute top-1/2 right-0 left-0 h-px -translate-y-1/2"
          />
          <motion.div
            initial={reduce ? { left: "100%" } : { left: "0%" }}
            animate={inView ? { left: "100%" } : { left: "0%" }}
            transition={{ duration: 1.4, ease: EASE_OUT_QUART, delay: 0.1 }}
            className="bg-cc-accent absolute top-1/2 h-2 w-2 -translate-x-1/2 -translate-y-1/2 rounded-full shadow-[0_0_12px_rgba(94,234,212,0.7)]"
          />
          {stops.map((stop, i) => (
            <motion.div
              key={stop.label}
              initial={reduce ? { opacity: 1 } : { opacity: 0 }}
              animate={inView ? { opacity: 1 } : { opacity: 0 }}
              transition={{
                duration: 0.4,
                ease: EASE_OUT_QUART,
                delay: 0.4 + i * 0.15,
              }}
              className="absolute top-1/2 flex -translate-y-1/2 flex-col items-center"
              style={{
                left: stop.position,
                transform: `translate(-50%, -50%)`,
              }}
            >
              <span className="bg-cc-bg border-cc-card-border h-3 w-3 rounded-full border" />
              <span className="text-cc-ink-dim font-heading mt-3 text-xs whitespace-nowrap">
                {stop.label}
              </span>
            </motion.div>
          ))}
        </div>
      </div>
    </section>
  );
}

function ComparisonMatrix() {
  return (
    <section
      aria-labelledby="compare-heading"
      className="border-cc-card-border bg-cc-card-bg/40 rounded-3xl border p-6 sm:p-10"
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
            {COMPARISON.map((group, groupIndex) => (
              <ComparisonGroupRows
                key={group.title}
                group={group}
                groupIndex={groupIndex}
              />
            ))}
          </tbody>
        </table>
      </div>
    </section>
  );
}

function ComparisonGroupRows({
  group,
  groupIndex,
}: {
  readonly group: ComparisonGroup;
  readonly groupIndex: number;
}) {
  return (
    <>
      <motion.tr
        initial={{ opacity: 0, y: 8 }}
        whileInView={{ opacity: 1, y: 0 }}
        viewport={{ once: true, amount: 0.3 }}
        transition={{ duration: 0.45, ease: EASE_OUT_QUART }}
      >
        <th
          scope="colgroup"
          colSpan={4}
          className="border-cc-ink-faint text-cc-nav-label border-t pt-6 pb-3 pl-2 text-left font-mono text-xs tracking-[0.15em] uppercase"
        >
          {group.title}
        </th>
      </motion.tr>
      {group.rows.map((row, rowIndex) => (
        <motion.tr
          key={row.label}
          initial={{ opacity: 0, y: 6 }}
          whileInView={{ opacity: 1, y: 0 }}
          viewport={{ once: true, amount: 0.2 }}
          transition={{
            duration: 0.35,
            ease: EASE_OUT_QUART,
            delay: Math.min(rowIndex * 0.04, 0.24),
          }}
        >
          <th
            scope="row"
            className="text-cc-ink py-3 pl-2 text-left align-top text-sm font-normal"
          >
            {row.label}
          </th>
          <ComparisonCell value={row.shared} />
          <ComparisonCell
            value={row.dedicated}
            highlight
            pulse={rowIndex === 0 && groupIndex === 0}
          />
          <ComparisonCell value={row.self} />
        </motion.tr>
      ))}
    </>
  );
}

function ComparisonCell({
  value,
  highlight = false,
  pulse = false,
}: {
  readonly value: CellValue;
  readonly highlight?: boolean;
  readonly pulse?: boolean;
}) {
  const inner =
    typeof value === "boolean" ? (
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
    );

  if (highlight && pulse) {
    return (
      <motion.td
        className="bg-cc-accent/5 py-3 text-center align-top text-sm"
        initial={{ backgroundColor: "rgba(94, 234, 212, 0.05)" }}
        whileInView={{
          backgroundColor: [
            "rgba(94, 234, 212, 0.05)",
            "rgba(94, 234, 212, 0.18)",
            "rgba(94, 234, 212, 0.05)",
          ],
        }}
        viewport={{ once: true, amount: 0.6 }}
        transition={{ duration: 1.2, ease: "easeInOut" }}
      >
        {inner}
      </motion.td>
    );
  }

  return (
    <td
      className={`py-3 text-center align-top text-sm ${
        highlight ? "bg-cc-accent/5" : ""
      }`}
    >
      {inner}
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
        {FAQ.map((item, i) => (
          <FaqEntry key={item.question} item={item} index={i} />
        ))}
      </dl>
    </section>
  );
}

function FaqEntry({
  item,
  index,
}: {
  readonly item: FaqItem;
  readonly index: number;
}) {
  return (
    <motion.div
      initial={{ opacity: 0, y: 8 }}
      whileInView={{ opacity: 1, y: 0 }}
      viewport={{ once: true, amount: 0.35 }}
      transition={{
        duration: 0.45,
        ease: EASE_OUT_QUART,
        delay: Math.min(index * 0.05, 0.2),
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
        viewport={{ once: true, amount: 0.3 }}
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
        viewport={{ once: true, amount: 0.4 }}
        transition={{ duration: 0.5, ease: EASE_OUT_QUART }}
        className="font-heading text-cc-heading text-h4 sm:text-h3 font-semibold"
      >
        Ship your GraphQL platform with Nitro.
      </motion.h2>
      <motion.p
        initial={{ opacity: 0, y: 10 }}
        whileInView={{ opacity: 1, y: 0 }}
        viewport={{ once: true, amount: 0.4 }}
        transition={{ duration: 0.5, ease: EASE_OUT_QUART, delay: 0.08 }}
        className="text-cc-ink mx-auto mt-5 max-w-2xl text-base"
      >
        Start on the free Shared Instance in minutes. Upgrade when you need a
        dedicated region, SLA, or SSO. The docs walk you through every step.
      </motion.p>
      <motion.div
        initial={{ opacity: 0, scale: 0.96 }}
        whileInView={{ opacity: 1, scale: 1 }}
        viewport={{ once: true, amount: 0.4 }}
        transition={{ duration: 0.5, ease: EASE_OUT_QUART, delay: 0.18 }}
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

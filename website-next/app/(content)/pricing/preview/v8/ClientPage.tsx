"use client";

import { useInView, useReducedMotion } from "motion/react";
import type { CSSProperties } from "react";
import { useEffect, useRef, useState } from "react";

import { CheckIcon } from "@/src/components/CheckIcon";
import { OutlineButton, SolidButton } from "@/src/design-system/Button";

// Concept: "Counters at the Switch". Pricing presented as a control-room
// readout where each metric (5M ops, 99.95% SLA, etc.) counts up once when
// scrolled into view via useInView({ once: true }). No scroll-coupled motion.

interface Plan {
  readonly id: "shared" | "dedicated" | "self";
  readonly name: string;
  readonly tagline: string;
  readonly price: string;
  readonly priceNote: string;
  readonly priceTarget: number | null;
  readonly priceFormatted: (n: number) => string;
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
    priceTarget: 0,
    priceFormatted: (n) => (n === 0 ? "$0" : `$${n}`),
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
    priceTarget: 400,
    priceFormatted: (n) => `$${n.toLocaleString("en-US")}`,
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
    priceTarget: null,
    priceFormatted: () => "Custom",
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

// Numeric-first comparison cells. A `value` of `true` means "included with no
// quantity" and renders a CheckIcon; a string means an explicit quantity or
// qualifier. Rows are scoped to four essential groups per the spec.
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
        label: "Included schemas",
        shared: "1",
        dedicated: "Unlimited",
        self: "Unlimited",
      },
      {
        label: "Environments per API",
        shared: "3",
        dedicated: "Unlimited",
        self: "Unlimited",
      },
      {
        label: "Included operations / month",
        shared: "5M",
        dedicated: "Custom volume",
        self: "Unmetered on your infra",
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
        label: "Client registry · published clients affected",
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
    ],
  },
  {
    title: "Security & access",
    rows: [
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
        label: "Uptime SLA",
        shared: "Best-effort",
        dedicated: "99.95%",
        self: "You operate it",
      },
      {
        label: "Log & trace retention",
        shared: "1 day",
        dedicated: "Configurable",
        self: "Your retention policy",
      },
    ],
  },
  {
    title: "Support",
    rows: [
      {
        label: "Support channel",
        shared: "Community Slack",
        dedicated: "Email + private chat",
        self: "Priority engineering",
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

// The faint dotted grid behind the hero and the numbers band. Pure inline
// background so we do not touch global page styles; cc-bg shows everywhere
// else through the (content) layout.
const DOTTED_GRID_STYLE: CSSProperties = {
  backgroundImage:
    "radial-gradient(circle, rgba(255,255,255,0.05) 1px, transparent 1px)",
  backgroundSize: "32px 32px",
  backgroundPosition: "0 0",
};

export function ClientPage() {
  return (
    <>
      <Hero />
      <PlanTriptych />
      <NumbersBand />
      <CompressedComparison />
      <FinePrint />
      <Faq />
      <ClosingCta />
    </>
  );
}

function Hero() {
  return (
    <section
      aria-labelledby="hero-heading"
      className="relative overflow-hidden pt-10 pb-14 text-center sm:pt-16 sm:pb-20"
      style={DOTTED_GRID_STYLE}
    >
      <p className="text-cc-nav-label font-mono text-xs tracking-[0.18em] uppercase">
        Nitro GraphQL pricing
      </p>
      <h1
        id="hero-heading"
        className="font-heading text-cc-heading sm:text-h2 mt-5 text-4xl font-semibold"
      >
        Pricing is the dial. Watch the numbers move.
      </h1>
      <p className="text-cc-ink mx-auto mt-6 max-w-2xl text-base text-pretty sm:text-lg">
        Start free on shared cloud. Move to a dedicated instance when you need
        SLA, SSO, and your own region. Self-host on your own infrastructure when
        the workload, or the policy, demands it.
      </p>
      <div className="mt-9 flex flex-wrap items-center justify-center gap-3">
        <SolidButton href="/get-started">Start for Free</SolidButton>
        <OutlineButton href="/services/support/contact">
          Talk to Sales
        </OutlineButton>
      </div>

      <div className="mt-14">
        <HeroMetricStrip />
      </div>
    </section>
  );
}

interface MetricSpec {
  readonly eyebrow: string;
  readonly target: number;
  readonly suffix?: string;
  readonly prefix?: string;
  readonly decimals?: number;
  readonly caption: string;
  readonly format?: "compact" | "plain";
}

const HERO_METRICS: readonly MetricSpec[] = [
  {
    eyebrow: "Ops / month",
    target: 5_000_000,
    suffix: "",
    caption: "Included on Shared, no card required.",
    format: "compact",
  },
  {
    eyebrow: "Uptime SLA",
    target: 99.95,
    suffix: "%",
    decimals: 2,
    caption: "Dedicated control plane.",
  },
  {
    eyebrow: "Environments",
    target: 3,
    caption: "Dev, QA, prod on Shared. Unlimited on the others.",
  },
  {
    eyebrow: "Setup cost",
    target: 0,
    prefix: "$",
    caption: "Bring a schema, get a URL.",
  },
];

function HeroMetricStrip() {
  const ref = useRef<HTMLDivElement | null>(null);
  const inView = useInView(ref, { once: true, amount: 0.3 });

  return (
    <div
      ref={ref}
      className="border-cc-card-border bg-cc-card-bg/50 mx-auto grid max-w-4xl gap-px overflow-hidden rounded-2xl border sm:grid-cols-2 lg:grid-cols-4"
    >
      {HERO_METRICS.map((metric) => (
        <MetricCell key={metric.eyebrow} metric={metric} active={inView} />
      ))}
    </div>
  );
}

function MetricCell({
  metric,
  active,
}: {
  readonly metric: MetricSpec;
  readonly active: boolean;
}) {
  return (
    <div className="bg-cc-surface flex flex-col items-center px-5 py-7 text-center">
      <p className="text-cc-nav-label font-mono text-[0.65rem] tracking-[0.18em] uppercase">
        {metric.eyebrow}
      </p>
      <p className="text-cc-accent font-heading sm:text-h2 mt-3 text-3xl font-semibold tabular-nums">
        <CountUp
          active={active}
          target={metric.target}
          decimals={metric.decimals}
          prefix={metric.prefix}
          suffix={metric.suffix}
          format={metric.format}
        />
      </p>
      <p className="text-cc-ink-dim mt-3 max-w-[16ch] text-xs leading-snug">
        {metric.caption}
      </p>
    </div>
  );
}

interface CountUpProps {
  readonly active: boolean;
  readonly target: number;
  readonly decimals?: number;
  readonly prefix?: string;
  readonly suffix?: string;
  readonly format?: "compact" | "plain";
  readonly durationMs?: number;
}

// Time-driven count-up. Starts once when `active` flips true (caller wires
// useInView({ once: true })). Respects useReducedMotion by jumping to the
// final value. Uses requestAnimationFrame and an ease-out curve.
function CountUp({
  active,
  target,
  decimals = 0,
  prefix = "",
  suffix = "",
  format = "plain",
  durationMs = 1200,
}: CountUpProps) {
  const reduced = useReducedMotion();
  // If motion is reduced, render the final value immediately; otherwise the
  // count-up effect drives `value` from 0 -> target via requestAnimationFrame.
  const [value, setValue] = useState(reduced ? target : 0);

  useEffect(() => {
    if (!active || reduced) return;
    let raf = 0;
    const start = performance.now();
    const tick = (now: number) => {
      const elapsed = now - start;
      const t = Math.min(1, elapsed / durationMs);
      // ease-out cubic
      const eased = 1 - Math.pow(1 - t, 3);
      setValue(target * eased);
      if (t < 1) {
        raf = requestAnimationFrame(tick);
      } else {
        setValue(target);
      }
    };
    raf = requestAnimationFrame(tick);
    return () => cancelAnimationFrame(raf);
  }, [active, target, durationMs, reduced]);

  return (
    <span>
      {prefix}
      {formatNumber(value, decimals, format)}
      {suffix}
    </span>
  );
}

function formatNumber(value: number, decimals: number, format?: string) {
  if (format === "compact" && value >= 1_000_000) {
    return `${(value / 1_000_000).toFixed(value === Math.floor(value) ? 0 : 1)}M`;
  }
  if (format === "compact" && value >= 1_000) {
    return `${(value / 1_000).toFixed(0)}K`;
  }
  return value.toLocaleString("en-US", {
    minimumFractionDigits: decimals,
    maximumFractionDigits: decimals,
  });
}

function PlanTriptych() {
  return (
    <section aria-labelledby="plans-heading" className="pb-16 sm:pb-24">
      <div className="text-center">
        <p className="text-cc-nav-label font-mono text-xs tracking-[0.18em] uppercase">
          Pick a switch
        </p>
        <h2
          id="plans-heading"
          className="font-heading text-cc-heading text-h4 sm:text-h3 mt-3 font-semibold"
        >
          Three positions. One Nitro.
        </h2>
        <div className="mt-7 inline-flex">
          <SegmentedSwitch />
        </div>
      </div>

      <div className="mt-10 grid gap-6 lg:grid-cols-3 lg:items-stretch">
        {PLANS.map((plan) => (
          <PlanCard key={plan.id} plan={plan} />
        ))}
      </div>
    </section>
  );
}

// Decorative segmented control showing the three plan positions. Purely
// visual; the cards below are the real interaction surface.
function SegmentedSwitch() {
  return (
    <div className="border-cc-card-border bg-cc-card-bg/60 inline-flex rounded-full border p-1 font-mono text-[0.7rem] tracking-[0.16em] uppercase">
      <span className="text-cc-ink-dim rounded-full px-3 py-1.5">Shared</span>
      <span className="bg-cc-accent/15 text-cc-accent border-cc-accent/40 rounded-full border px-3 py-1.5">
        Dedicated
      </span>
      <span className="text-cc-ink-dim rounded-full px-3 py-1.5">Self</span>
    </div>
  );
}

function PlanCard({ plan }: { readonly plan: Plan }) {
  const ref = useRef<HTMLDivElement | null>(null);
  const inView = useInView(ref, { once: true, amount: 0.3 });

  if (plan.popular) {
    return (
      <div
        ref={ref}
        className="relative rounded-3xl p-[1.5px] lg:-my-2"
        style={{
          background:
            "linear-gradient(140deg, #16b9e4 0%, #7c92c6 50%, #f0786a 100%)",
        }}
      >
        <PopularPill />
        <div className="bg-cc-surface flex h-full flex-col rounded-[calc(1.5rem-1.5px)] p-7 sm:p-8">
          <PlanCardBody plan={plan} active={inView} />
        </div>
      </div>
    );
  }

  return (
    <div
      ref={ref}
      className="bg-cc-card-bg border-cc-card-border hover:border-cc-card-border-hover flex h-full flex-col rounded-3xl border p-7 transition-colors sm:p-8"
    >
      <PlanCardBody plan={plan} active={inView} />
    </div>
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
        <span className="font-heading text-cc-heading text-h2 font-semibold tabular-nums">
          <PlanPrice plan={plan} active={active} />
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

function PlanPrice({
  plan,
  active,
}: {
  readonly plan: Plan;
  readonly active: boolean;
}) {
  if (plan.priceTarget === null) {
    // Custom is shown as a static glyph (per spec).
    return <span aria-label="Custom price">{plan.price}</span>;
  }
  if (plan.id === "shared") {
    // Shared price is "Free" in copy; render the word, not a ticker.
    return <span>{plan.price}</span>;
  }
  return (
    <CountUp
      active={active}
      target={plan.priceTarget}
      prefix="$"
      durationMs={1100}
    />
  );
}

function PopularPill() {
  return (
    <span className="bg-cc-surface text-cc-accent border-cc-accent absolute top-0 left-1/2 z-10 -translate-x-1/2 -translate-y-1/2 rounded-full border px-4 py-1 font-mono text-[0.65rem] tracking-[0.18em] whitespace-nowrap uppercase">
      Most popular
    </span>
  );
}

const NUMBERS_BAND: readonly MetricSpec[] = [
  {
    eyebrow: "Operations / month",
    target: 5_000_000,
    caption: "Included on Shared.",
    format: "compact",
  },
  {
    eyebrow: "Schemas",
    target: 1,
    caption: "On Shared. Unlimited on Dedicated and Self.",
  },
  {
    eyebrow: "Environments",
    target: 3,
    caption: "Dev, QA, prod on Shared.",
  },
  {
    eyebrow: "BYOC region",
    target: 1,
    caption: "Pick yours on Dedicated.",
  },
  {
    eyebrow: "Retention (days)",
    target: 1,
    caption: "On Shared. Configurable on Dedicated.",
  },
  {
    eyebrow: "Uptime SLA",
    target: 99.95,
    suffix: "%",
    decimals: 2,
    caption: "On the Dedicated control plane.",
  },
];

function NumbersBand() {
  const ref = useRef<HTMLDivElement | null>(null);
  const inView = useInView(ref, { once: true, amount: 0.2 });

  return (
    <section
      aria-labelledby="numbers-heading"
      className="border-cc-card-border bg-cc-card-bg/40 relative overflow-hidden rounded-3xl border p-6 sm:p-10"
      style={DOTTED_GRID_STYLE}
    >
      <div className="text-center">
        <p className="text-cc-nav-label font-mono text-xs tracking-[0.18em] uppercase">
          By the numbers
        </p>
        <h2
          id="numbers-heading"
          className="font-heading text-cc-heading text-h4 sm:text-h3 mt-3 font-semibold"
        >
          What each tier actually buys you.
        </h2>
        <p className="text-cc-ink mx-auto mt-4 max-w-2xl text-base">
          The same Nitro platform on every plan. The dial changes the
          quantities, not the capability set.
        </p>
      </div>

      <div
        ref={ref}
        className="border-cc-card-border bg-cc-card-border mt-10 grid gap-px overflow-hidden rounded-2xl border sm:grid-cols-2 lg:grid-cols-3"
      >
        {NUMBERS_BAND.map((metric) => (
          <MetricCell key={metric.eyebrow} metric={metric} active={inView} />
        ))}
      </div>
    </section>
  );
}

function CompressedComparison() {
  return (
    <section
      aria-labelledby="compare-heading"
      className="border-cc-card-border bg-cc-card-bg/40 mt-20 rounded-3xl border p-6 sm:mt-28 sm:p-10"
    >
      <div className="text-center">
        <p className="text-cc-nav-label font-mono text-xs tracking-[0.18em] uppercase">
          Compare plans
        </p>
        <h2
          id="compare-heading"
          className="font-heading text-cc-heading text-h4 sm:text-h3 mt-3 font-semibold"
        >
          Quantities first. Checks where no number applies.
        </h2>
        <p className="text-cc-ink mx-auto mt-4 max-w-2xl text-base">
          Four essential groups. Every cell that can be a number, is.
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
      ) : isNumericLike(value) ? (
        <span className="text-cc-heading font-mono tabular-nums">{value}</span>
      ) : (
        <span className="text-cc-ink">{value}</span>
      )}
    </td>
  );
}

function isNumericLike(value: string) {
  // True for strings that start with a digit or currency, so quantities render
  // in the mono control-room voice while qualifiers stay in the body voice.
  return /^[\d$]/.test(value.trim()) || /%$/.test(value.trim());
}

function FinePrint() {
  return (
    <section
      aria-labelledby="finepoint-heading"
      className="border-cc-card-border bg-cc-card-bg/30 mt-16 rounded-3xl border p-6 sm:p-8"
    >
      <p className="text-cc-nav-label font-mono text-xs tracking-[0.18em] uppercase">
        Honest fine print
      </p>
      <h2
        id="finepoint-heading"
        className="font-heading text-cc-heading text-h5 mt-3 font-semibold"
      >
        What the numbers do not say.
      </h2>
      <p className="text-cc-ink-dim mt-4 font-mono text-xs leading-relaxed sm:text-sm">
        Telemetry requires Nitro configuration in your server before any ops
        appear on the dashboard. The built-in GraphQL IDE is served from your
        endpoint on every plan, not from us. The Fusion gateway is always your
        ASP.NET Core app, on Shared, Dedicated, or Self-Hosted. SLA covers the
        Nitro control plane, not your subgraphs.
      </p>
    </section>
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
    <div className="border-cc-card-border bg-cc-card-bg/60 hover:border-cc-card-border-hover rounded-2xl border p-6 transition-colors">
      <dt className="font-heading text-cc-heading text-base font-semibold">
        {item.question}
      </dt>
      <dd className="text-cc-ink mt-3 text-sm leading-relaxed">
        {item.answer}
      </dd>
    </div>
  );
}

function ClosingCta() {
  const ref = useRef<HTMLDivElement | null>(null);
  const inView = useInView(ref, { once: true, amount: 0.5 });

  return (
    <section
      ref={ref}
      className="mt-20 mb-8 text-center sm:mt-28"
      aria-labelledby="closing-heading"
    >
      <p className="text-cc-nav-label font-mono text-xs tracking-[0.18em] uppercase">
        Last number
      </p>
      <h2
        id="closing-heading"
        className="font-heading text-cc-heading text-h4 sm:text-h3 mt-3 font-semibold"
      >
        Ship your GraphQL platform with Nitro.
      </h2>
      <p className="text-cc-ink mx-auto mt-5 max-w-2xl text-base">
        Start on the free Shared Instance in minutes. Upgrade when you need a
        dedicated region, SLA, or SSO. The docs walk you through every step.
      </p>
      <div className="mt-8 flex flex-wrap items-center justify-center gap-3">
        <SolidButton href="/get-started">Start for Free</SolidButton>
        <OutlineButton href="/docs">Read the docs</OutlineButton>
      </div>

      <div className="border-cc-card-border bg-cc-card-bg/50 mx-auto mt-12 inline-flex flex-col items-center rounded-2xl border px-8 py-6">
        <p className="text-cc-nav-label font-mono text-[0.65rem] tracking-[0.18em] uppercase">
          Deploy targets supported
        </p>
        <p className="text-cc-accent font-heading text-h3 mt-2 font-semibold tabular-nums">
          <CountUp active={inView} target={3} />
        </p>
        <p className="text-cc-ink-dim mt-1 font-mono text-xs">
          Shared cloud, Dedicated cloud, Self-Hosted.
        </p>
      </div>
    </section>
  );
}

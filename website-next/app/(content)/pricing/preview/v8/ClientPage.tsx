"use client";

import { useInView, useReducedMotion } from "motion/react";
import type { CSSProperties } from "react";
import { useEffect, useRef, useState } from "react";

import { CheckIcon } from "@/src/components/CheckIcon";
import type {
  Cell,
  ComparisonGroup,
  PricingFaq,
  Tier,
  TierId,
} from "@/src/components/pricing/pricingData";
import {
  COMPARISON,
  FAQ,
  TIERS,
  UNLOCKS,
  UNLOCKS_NOTE,
} from "@/src/components/pricing/pricingData";
import { OutlineButton, SolidButton } from "@/src/design-system/Button";

// Concept: "Counters at the Switch". Pricing presented as a control-room
// readout where each metric (1M free ops, $20 Pay as you go, 60-day retention,
// etc.) counts up once when scrolled into view via useInView({ once: true }).
// No scroll-coupled motion. All tier, comparison, unlock, and FAQ data comes
// from the shared pricing module so the numbers stay in one place.

const CLOUD_TIERS = TIERS.filter((tier) => tier.id !== "self");
const SELF_TIER = TIERS.find((tier) => tier.id === "self");

// How each tier's headline price renders in the count-up ticker. `target` null
// means the price is a static word (Custom). `lead` prints a small qualifier in
// front of the big number (e.g. "from" for Dedicated).
const PRICE_TICKER: Record<
  TierId,
  {
    readonly target: number | null;
    readonly prefix: string;
    readonly lead?: string;
  }
> = {
  free: { target: 0, prefix: "$" },
  payg: { target: 20, prefix: "$" },
  dedicated: { target: 400, prefix: "$", lead: "from" },
  self: { target: null, prefix: "" },
};

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
      <UnlockAsYouGrow />
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
        Start free on shared cloud. Move to Pay as you go when you outgrow the
        free limits, a Dedicated instance when you need your own region, SSO,
        and volume-based pricing, or self-host on your own infrastructure.
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
    eyebrow: "Free ops / month",
    target: 1_000_000,
    caption: "Free forever, no card required.",
    format: "compact",
  },
  {
    eyebrow: "Pay as you go",
    target: 20,
    prefix: "$",
    caption: "Per month, 5M operations included.",
  },
  {
    eyebrow: "Retention (days)",
    target: 60,
    caption: "On Pay as you go. 3 days on Free.",
  },
  {
    eyebrow: "To start",
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
          Four positions. One Nitro.
        </h2>
        <div className="mt-7 inline-flex">
          <SegmentedSwitch />
        </div>
      </div>

      <div className="mt-10 grid gap-6 lg:grid-cols-3 lg:items-stretch">
        {CLOUD_TIERS.map((tier) => (
          <PlanCard key={tier.id} tier={tier} />
        ))}
      </div>

      {SELF_TIER && <SelfHostedStrip tier={SELF_TIER} />}
    </section>
  );
}

// Decorative segmented control showing the four plan positions. Purely visual;
// the cards below are the real interaction surface.
function SegmentedSwitch() {
  return (
    <div className="border-cc-card-border bg-cc-card-bg/60 inline-flex flex-wrap justify-center rounded-full border p-1 font-mono text-[0.7rem] tracking-[0.16em] uppercase">
      <span className="text-cc-ink-dim rounded-full px-3 py-1.5">Free</span>
      <span className="bg-cc-accent/15 text-cc-accent border-cc-accent/40 rounded-full border px-3 py-1.5">
        Pay as you go
      </span>
      <span className="text-cc-ink-dim rounded-full px-3 py-1.5">
        Dedicated
      </span>
      <span className="text-cc-ink-dim rounded-full px-3 py-1.5">Self</span>
    </div>
  );
}

function PlanCard({ tier }: { readonly tier: Tier }) {
  const ref = useRef<HTMLDivElement | null>(null);
  const inView = useInView(ref, { once: true, amount: 0.3 });

  if (tier.popular) {
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
          <PlanCardBody tier={tier} active={inView} />
        </div>
      </div>
    );
  }

  return (
    <div
      ref={ref}
      className="bg-cc-card-bg border-cc-card-border hover:border-cc-card-border-hover flex h-full flex-col rounded-3xl border p-7 transition-colors sm:p-8"
    >
      <PlanCardBody tier={tier} active={inView} />
    </div>
  );
}

function PlanCardBody({
  tier,
  active,
}: {
  readonly tier: Tier;
  readonly active: boolean;
}) {
  const CallToAction = tier.popular ? SolidButton : OutlineButton;
  const ticker = PRICE_TICKER[tier.id];
  return (
    <>
      <h3 className="font-heading text-cc-heading text-h5 font-semibold">
        {tier.name}
      </h3>
      <p className="text-cc-ink-dim mt-2 text-sm">{tier.tagline}</p>
      <div className="mt-6 flex items-baseline gap-2">
        {ticker.lead && (
          <span className="text-cc-ink-dim text-sm">{ticker.lead}</span>
        )}
        <span className="font-heading text-cc-heading text-h2 font-semibold tabular-nums">
          <PlanPrice tier={tier} active={active} />
        </span>
        <span className="text-cc-nav-label font-mono text-xs">
          {tier.priceNote}
        </span>
      </div>
      <div
        aria-hidden="true"
        className="border-cc-ink-faint my-6 border-t border-dashed"
      />
      <ul className="flex flex-1 flex-col gap-3">
        {tier.features.map((feature) => (
          <li key={feature} className="flex items-start gap-3">
            <span className="text-cc-accent mt-[5px] flex-none">
              <CheckIcon />
            </span>
            <span className="text-cc-ink text-sm">{feature}</span>
          </li>
        ))}
      </ul>
      <CallToAction href={tier.ctaHref} className="mt-8 w-full">
        {tier.cta}
      </CallToAction>
    </>
  );
}

function PlanPrice({
  tier,
  active,
}: {
  readonly tier: Tier;
  readonly active: boolean;
}) {
  const ticker = PRICE_TICKER[tier.id];
  if (ticker.target === null) {
    // Custom is shown as a static glyph (per spec).
    return <span aria-label={`${tier.name} price`}>{tier.price}</span>;
  }
  return (
    <CountUp
      active={active}
      target={ticker.target}
      prefix={ticker.prefix}
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

// Self-Hosted is the fourth tier. It sits below the three cloud cards as a slim
// strip, the way this preview keeps everything on one switchboard.
function SelfHostedStrip({ tier }: { readonly tier: Tier }) {
  return (
    <div className="border-cc-card-border bg-cc-card-bg/60 mt-6 flex flex-col gap-5 rounded-3xl border p-6 sm:flex-row sm:items-center sm:justify-between sm:p-8">
      <div>
        <div className="flex items-center gap-3">
          <h3 className="font-heading text-cc-heading text-h6 font-semibold">
            {tier.name}
          </h3>
          <span className="text-cc-nav-label font-mono text-[0.65rem] tracking-[0.16em] uppercase">
            {tier.price} · {tier.priceNote}
          </span>
        </div>
        <p className="text-cc-ink mt-2 max-w-2xl text-sm text-pretty">
          {tier.tagline} Run on your own infrastructure, air-gapped or on-prem,
          with configurable retention, priority engineering support, and a
          long-term release channel.
        </p>
      </div>
      <OutlineButton href={tier.ctaHref} className="shrink-0 sm:w-auto">
        {tier.cta}
      </OutlineButton>
    </div>
  );
}

const NUMBERS_BAND: readonly MetricSpec[] = [
  {
    eyebrow: "Free ops / month",
    target: 1_000_000,
    caption: "On Free, no card required.",
    format: "compact",
  },
  {
    eyebrow: "Pay as you go ops",
    target: 5_000_000,
    caption: "Included, then $2 / million.",
    format: "compact",
  },
  {
    eyebrow: "Ingest (GB)",
    target: 2,
    caption: "On Free, and per 1M ops on Pay as you go.",
  },
  {
    eyebrow: "Retention (days)",
    target: 60,
    caption: "On Pay as you go. 3 days on Free.",
  },
  {
    eyebrow: "Dedicated from",
    target: 400,
    prefix: "$",
    caption: "Per month, priced by volume.",
  },
  {
    eyebrow: "Per extra million",
    target: 2,
    prefix: "$",
    caption: "Beyond 5M on Pay as you go.",
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
          The full platform across all four tiers. Every cell that can be a
          number, is.
        </p>
      </div>

      <div className="mt-10 overflow-x-auto">
        <table className="w-full min-w-[56rem] border-separate border-spacing-0 text-left text-sm">
          <thead>
            <tr>
              <th scope="col" className="w-1/3 pb-4 pl-2">
                <span className="sr-only">Capability</span>
              </th>
              {TIERS.map((tier) => (
                <th
                  key={tier.id}
                  scope="col"
                  className="pb-4 text-center align-bottom"
                >
                  <div
                    className={`font-heading text-cc-heading text-base font-semibold ${
                      tier.popular ? "text-cc-accent" : ""
                    }`}
                  >
                    {tier.name}
                  </div>
                  <div className="text-cc-nav-label mt-1 font-mono text-[0.65rem] tracking-[0.15em] uppercase">
                    {tier.price} · {tier.priceNote}
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
          colSpan={5}
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
          <ComparisonCell value={row.free} />
          <ComparisonCell value={row.payg} highlight />
          <ComparisonCell value={row.dedicated} />
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
  readonly value: Cell;
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
        Telemetry requires Nitro configuration in your server before any
        operations appear on the dashboard. The built-in GraphQL IDE is served
        from your endpoint on every plan, not from us. The Fusion gateway is
        always your ASP.NET Core app, on Free, Pay as you go, Dedicated, or
        Self-Hosted.
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

function FaqEntry({ item }: { readonly item: PricingFaq }) {
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

// The spend-based progression: commit to a minimum monthly spend to unlock
// more, up to your spend. A vertical list of unlock rows in this preview's
// control-room voice, each with a switch glyph on the left, the unlock in the
// middle, and the monthly spend ticking up on the right.
function UnlockAsYouGrow() {
  return (
    <section aria-labelledby="unlock-heading" className="mt-20 sm:mt-28">
      <div className="text-center">
        <p className="text-cc-nav-label font-mono text-xs tracking-[0.18em] uppercase">
          Spend tiers
        </p>
        <h2
          id="unlock-heading"
          className="font-heading text-cc-heading text-h4 sm:text-h3 mt-3 font-semibold"
        >
          Unlock more as you grow
        </h2>
        <p className="text-cc-ink mx-auto mt-4 max-w-2xl text-base">
          Commit to a minimum monthly spend to unlock more, up to your spend.
        </p>
      </div>

      <ul className="mx-auto mt-10 flex max-w-3xl flex-col gap-3">
        {UNLOCKS.map((unlock, index) => {
          const Glyph = UNLOCK_ICONS[index] ?? SupportGlyph;
          return (
            <li
              key={unlock.title}
              className="border-cc-card-border bg-cc-card-bg/60 hover:border-cc-card-border-hover flex items-center gap-4 rounded-2xl border p-5 transition-colors sm:gap-5 sm:p-6"
            >
              <span className="border-cc-card-border bg-cc-surface text-cc-accent flex size-11 shrink-0 items-center justify-center rounded-xl border">
                <Glyph className="size-5" />
              </span>
              <div className="min-w-0 flex-1">
                <h3 className="font-heading text-cc-heading text-base font-semibold">
                  {unlock.title}
                </h3>
                <p className="text-cc-ink-dim mt-1 text-sm text-pretty">
                  {unlock.description}
                </p>
              </div>
              <span className="text-cc-accent shrink-0 font-mono text-sm font-semibold tabular-nums sm:text-base">
                {unlock.spend}
              </span>
            </li>
          );
        })}
      </ul>
      <p className="text-cc-nav-label mt-5 text-center font-mono text-[0.7rem]">
        {UNLOCKS_NOTE}
      </p>
    </section>
  );
}

interface GlyphProps {
  readonly className?: string;
}

/** Lifebuoy glyph for the first spend tier. */
function SupportGlyph({ className }: GlyphProps) {
  return (
    <svg
      viewBox="0 0 24 24"
      fill="none"
      stroke="currentColor"
      strokeWidth={1.6}
      aria-hidden="true"
      className={className}
    >
      <circle cx="12" cy="12" r="9" />
      <circle cx="12" cy="12" r="3.4" />
      <path d="M5.2 5.2l4.4 4.4M18.8 5.2l-4.4 4.4M5.2 18.8l4.4-4.4M18.8 18.8l-4.4-4.4" />
    </svg>
  );
}

/** Shield-with-check glyph for the second spend tier. */
function ShieldGlyph({ className }: GlyphProps) {
  return (
    <svg
      viewBox="0 0 24 24"
      fill="none"
      stroke="currentColor"
      strokeWidth={1.6}
      strokeLinecap="round"
      strokeLinejoin="round"
      aria-hidden="true"
      className={className}
    >
      <path d="M12 3l7 3v5c0 4-3 6.6-7 8-4-1.4-7-4-7-8V6z" />
      <path d="M9 12l2 2 4-4" />
    </svg>
  );
}

/** Cloud glyph for the BYOC spend tier. */
function CloudGlyph({ className }: GlyphProps) {
  return (
    <svg
      viewBox="0 0 24 24"
      fill="none"
      stroke="currentColor"
      strokeWidth={1.6}
      strokeLinecap="round"
      strokeLinejoin="round"
      aria-hidden="true"
      className={className}
    >
      <path d="M7 18h10a4 4 0 0 0 .5-7.97A5.5 5.5 0 0 0 6.5 9 4 4 0 0 0 7 18z" />
    </svg>
  );
}

const UNLOCK_ICONS = [SupportGlyph, ShieldGlyph, CloudGlyph];

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
        Start free in minutes. Move to Pay as you go when you grow, a Dedicated
        instance when you need your own region and SSO. The docs walk you
        through every step.
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

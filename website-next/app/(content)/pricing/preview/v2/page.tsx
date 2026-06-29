import type { Metadata } from "next";

import { CheckIcon } from "@/src/components/CheckIcon";
import type {
  Cell,
  PricingFaq,
  Tier,
  TierId,
  Unlock,
} from "@/src/components/pricing/pricingData";
import {
  COMPARISON,
  FAQ,
  TIERS,
  UNLOCKS,
  UNLOCKS_NOTE,
} from "@/src/components/pricing/pricingData";
import { OutlineButton, SolidButton } from "@/src/design-system/Button";

export const metadata: Metadata = {
  title: "Nitro Pricing, Plans for every GraphQL platform scale",
  description:
    "Compare ChilliCream Nitro pricing for the GraphQL platform: start free on shared cloud, pay as you go at $20/mo, run a dedicated single-tenant instance from $400, or self-host on your own infrastructure.",
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
      "Compare Nitro plans for the ChilliCream GraphQL platform: free shared cloud, pay as you go at $20/mo, a dedicated single-tenant instance from $400, or self-hosted on your own infra.",
  },
  robots: { index: false, follow: false },
};

const CLOUD_TIERS = TIERS.filter((tier) => tier.id !== "self");
const SELF_HOSTED = TIERS.find((tier) => tier.id === "self");
const TIER_BY_ID = new Map<TierId, Tier>(TIERS.map((tier) => [tier.id, tier]));

/**
 * Usage buckets for the "estimate your scale" step. These are presentational
 * guidance only (no prices, no feature lists): each bucket points at a tier id
 * and the headline ops figure is a rough hint, not a billing threshold.
 */
interface ScaleBucket {
  readonly tierId: TierId;
  readonly ops: string;
  readonly blurb: string;
}

const SCALE: readonly ScaleBucket[] = [
  { tierId: "free", ops: "~1M ops / mo", blurb: "Side projects & spikes" },
  { tierId: "payg", ops: "~5M ops / mo", blurb: "Production for one team" },
  {
    tierId: "dedicated",
    ops: "50M+ ops / mo",
    blurb: "Multi-env, multi-region, BYOC",
  },
  {
    tierId: "self",
    ops: "Any volume",
    blurb: "On-prem, air-gapped, regulated",
  },
];

const BLURB_BY_TIER = new Map<TierId, string>(
  SCALE.map((bucket) => [bucket.tierId, bucket.blurb]),
);

export default function PricingV2Page() {
  return (
    <>
      <Hero />
      <ScaleSelector />
      <PlanGrid />
      <CompareTable />
      <UnlockBand />
      <Faq />
      <RegulatedBand />
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
        the GraphQL IDE. Start free on the shared cloud, switch to pay as you go
        as traffic grows, run a dedicated single-tenant instance, or self-host
        on your own infrastructure.
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
          bucket closest to your scale, and we&rsquo;ll point at the plan that
          fits.
        </p>
      </div>

      <div className="border-cc-card-border bg-cc-card-bg/40 mt-8 rounded-3xl border p-3 sm:p-4">
        <div className="grid grid-cols-2 gap-2 sm:grid-cols-4 sm:gap-3">
          {SCALE.map((bucket, index) => (
            <ScaleTile key={bucket.tierId} bucket={bucket} index={index} />
          ))}
        </div>
        <p className="text-cc-nav-label mt-4 px-2 font-mono text-[0.65rem] tracking-[0.15em] uppercase">
          Usage guide only · Pay as you go bills per actual operation, Free is
          capped at its included volume
        </p>
      </div>
    </section>
  );
}

interface ScaleTileProps {
  readonly bucket: ScaleBucket;
  readonly index: number;
}

function ScaleTile({ bucket, index }: ScaleTileProps) {
  // Pure CSS highlight: clicking the tile sets the URL hash to
  // #scale-<tier-id>, which makes :target match and lights up the tile +
  // reveals the matching recommendation. No client JS needed.
  const tier = TIER_BY_ID.get(bucket.tierId);
  return (
    <a
      id={`scale-${bucket.tierId}`}
      href={`#scale-${bucket.tierId}`}
      className="group border-cc-card-border hover:border-cc-card-border-hover target:border-cc-accent target:bg-cc-accent/5 bg-cc-card-bg/60 relative flex flex-col rounded-2xl border px-4 py-5 text-left no-underline transition-colors"
    >
      <span className="text-cc-nav-label font-mono text-[0.65rem] tracking-[0.15em] uppercase">
        Tier {index + 1}
      </span>
      <span className="font-heading text-cc-heading text-h6 mt-2 font-semibold">
        {tier?.name}
      </span>
      <span className="text-cc-accent mt-1 font-mono text-xs">
        {bucket.ops}
      </span>
      <span className="text-cc-ink-dim mt-3 text-sm">{bucket.blurb}</span>
      <span
        aria-hidden="true"
        className="text-cc-accent mt-4 font-mono text-[0.65rem] tracking-[0.15em] uppercase opacity-0 group-target:opacity-100"
      >
        ▸ See plan below
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
          Three cloud tiers, plus self-hosted.
        </h2>
        <p className="text-cc-ink-dim mx-auto mt-3 max-w-2xl text-sm">
          Free and Pay as you go run on the shared cloud. Dedicated is
          single-tenant. Self-host when you need to run it yourself.
        </p>
      </div>

      <div className="mt-10 grid gap-6 lg:grid-cols-3">
        {CLOUD_TIERS.map((tier) => (
          <PlanCard key={tier.id} tier={tier} />
        ))}
      </div>

      {SELF_HOSTED && <SelfHostedStrip tier={SELF_HOSTED} />}
    </section>
  );
}

interface PlanCardProps {
  readonly tier: Tier;
}

function PlanCard({ tier }: PlanCardProps) {
  const CallToActionButton = tier.popular ? SolidButton : OutlineButton;
  const audience = BLURB_BY_TIER.get(tier.id) ?? tier.tagline;
  return (
    <article
      className={`relative flex h-full flex-col rounded-3xl border p-6 sm:p-7 ${
        tier.popular
          ? "border-cc-accent bg-cc-card-bg"
          : "border-cc-card-border bg-cc-card-bg/60"
      } `}
    >
      {tier.popular && (
        <span className="bg-cc-accent text-cc-surface absolute -top-3 left-6 rounded-full px-3 py-1 font-mono text-[0.65rem] tracking-[0.15em] uppercase">
          Popular
        </span>
      )}

      <header>
        <p className="text-cc-nav-label font-mono text-[0.65rem] tracking-[0.15em] uppercase">
          {audience}
        </p>
        <h3 className="font-heading text-cc-heading text-h5 mt-2 font-semibold">
          {tier.name}
        </h3>
        <p className="text-cc-nav-label mt-1 font-mono text-xs">
          {tier.tagline}
        </p>
      </header>

      <div className="mt-6 flex items-baseline gap-2">
        <span className="font-heading text-cc-heading text-h3 font-semibold">
          {tier.price}
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
            <span className="text-cc-accent mt-1 flex-none">
              <CheckIcon />
            </span>
            <span className="text-cc-ink text-sm">{feature}</span>
          </li>
        ))}
      </ul>

      <CallToActionButton href={tier.ctaHref} className="mt-8 w-full">
        {tier.cta}
      </CallToActionButton>
    </article>
  );
}

function SelfHostedStrip({ tier }: PlanCardProps) {
  return (
    <div className="border-cc-card-border bg-cc-card-bg/60 mt-6 flex flex-col gap-5 rounded-3xl border p-6 sm:flex-row sm:items-center sm:justify-between sm:p-8">
      <div>
        <p className="text-cc-nav-label font-mono text-[0.65rem] tracking-[0.15em] uppercase">
          {BLURB_BY_TIER.get(tier.id) ?? tier.tagline}
        </p>
        <h3 className="font-heading text-cc-heading text-h5 mt-2 font-semibold">
          {tier.name}
        </h3>
        <p className="text-cc-ink mt-2 max-w-2xl text-sm text-pretty">
          {tier.tagline} Run on your own infrastructure, air-gapped or on-prem,
          with configurable retention and priority engineering support.
        </p>
      </div>
      <OutlineButton href={tier.ctaHref} className="shrink-0 sm:w-auto">
        {tier.cta}
      </OutlineButton>
    </div>
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
          <table className="w-full min-w-[820px] border-collapse text-left text-sm">
            <thead>
              <tr className="border-cc-card-border border-b">
                <th
                  scope="col"
                  className="text-cc-nav-label px-5 py-4 font-mono text-[0.65rem] tracking-[0.15em] uppercase"
                >
                  Capability
                </th>
                {TIERS.map((tier) => (
                  <th
                    key={tier.id}
                    scope="col"
                    className="text-cc-heading font-heading px-5 py-4 text-sm font-semibold"
                  >
                    <span className={tier.popular ? "text-cc-accent" : ""}>
                      {tier.name}
                    </span>
                  </th>
                ))}
              </tr>
            </thead>
            {COMPARISON.map((group, groupIndex) => (
              <tbody key={group.title}>
                <tr
                  className={`bg-cc-card-bg/60 ${
                    groupIndex === 0 ? "" : "border-cc-card-border border-t"
                  }`}
                >
                  <th
                    scope="colgroup"
                    colSpan={5}
                    className="text-cc-nav-label px-5 py-3 text-left font-mono text-[0.65rem] tracking-[0.15em] uppercase"
                  >
                    {group.title}
                  </th>
                </tr>
                {group.rows.map((row) => (
                  <tr
                    key={row.label}
                    className="border-cc-ink-faint border-b last:border-0"
                  >
                    <th
                      scope="row"
                      className="text-cc-ink px-5 py-3 align-top text-sm font-medium"
                    >
                      {row.label}
                    </th>
                    {TIERS.map((tier) => (
                      <CompareCell key={tier.id} value={row[tier.id]} />
                    ))}
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

interface CompareCellProps {
  readonly value: Cell;
}

function CompareCell({ value }: CompareCellProps) {
  if (value === true) {
    return (
      <td className="px-5 py-3 align-top">
        <span className="text-cc-accent inline-flex">
          <CheckIcon />
        </span>
        <span className="sr-only">Included</span>
      </td>
    );
  }
  if (value === false) {
    return (
      <td className="text-cc-ink-faint px-5 py-3 align-top">
        <span aria-hidden="true">-</span>
        <span className="sr-only">Not included</span>
      </td>
    );
  }
  return (
    <td className="text-cc-ink px-5 py-3 align-top font-mono text-xs">
      {value}
    </td>
  );
}

function UnlockBand() {
  return (
    <section
      aria-labelledby="unlock-heading"
      className="mt-24 scroll-mt-24 sm:mt-28"
      id="unlock"
    >
      <div className="mx-auto max-w-2xl text-center">
        <h2
          id="unlock-heading"
          className="font-heading text-cc-heading text-h3 font-semibold"
        >
          Unlock more as you grow
        </h2>
        <p className="text-cc-ink-dim mt-3 text-sm text-pretty">
          Commit to a minimum monthly spend to unlock more, up to your spend.
        </p>
      </div>

      <ul className="mx-auto mt-10 flex max-w-3xl flex-col gap-3">
        {UNLOCKS.map((unlock, index) => (
          <UnlockRow key={unlock.title} unlock={unlock} index={index} />
        ))}
      </ul>
      <p className="text-cc-nav-label mt-5 text-center font-mono text-[0.7rem]">
        {UNLOCKS_NOTE}
      </p>
    </section>
  );
}

interface UnlockRowProps {
  readonly unlock: Unlock;
  readonly index: number;
}

function UnlockRow({ unlock, index }: UnlockRowProps) {
  const Glyph = UNLOCK_ICONS[index] ?? SupportGlyph;
  return (
    <li className="border-cc-card-border bg-cc-card-bg/60 flex items-center gap-4 rounded-2xl border p-5 sm:gap-5 sm:p-6">
      <span className="border-cc-card-border bg-cc-bg/40 text-cc-accent flex size-11 shrink-0 items-center justify-center rounded-xl border">
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
      <span className="text-cc-accent shrink-0 font-mono text-sm font-semibold sm:text-base">
        {unlock.spend}
      </span>
    </li>
  );
}

interface GlyphProps {
  readonly className?: string;
}

/** Lifebuoy, for the Business Support unlock. */
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

/** Shield with a check, for the priority support unlock. */
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

/** Cloud, for the BYOC unlock. */
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
        {FAQ.map((faq) => (
          <FaqItem key={faq.question} faq={faq} />
        ))}
      </div>
    </section>
  );
}

interface FaqItemProps {
  readonly faq: PricingFaq;
}

function FaqItem({ faq }: FaqItemProps) {
  return (
    <details className="group border-cc-card-border hover:border-cc-card-border-hover bg-cc-card-bg/60 rounded-2xl border p-5 transition-colors">
      <summary className="text-cc-heading font-heading flex cursor-pointer list-none items-start justify-between gap-4 text-base font-semibold">
        <span>{faq.question}</span>
        <span
          aria-hidden="true"
          className="text-cc-accent mt-1 flex-none font-mono text-sm transition-transform group-open:rotate-45"
        >
          +
        </span>
      </summary>
      <div className="text-cc-ink mt-3 text-sm">{faq.answer}</div>
    </details>
  );
}

function RegulatedBand() {
  return (
    <section
      aria-labelledby="regulated-heading"
      className="border-cc-card-border bg-cc-card-bg/60 mt-24 rounded-3xl border p-8 sm:mt-28 sm:p-12"
    >
      <div className="grid items-center gap-8 md:grid-cols-[1.4fr_1fr]">
        <div>
          <p className="text-cc-nav-label font-mono text-xs tracking-[0.2em] uppercase">
            Regulated &amp; on-prem
          </p>
          <h2
            id="regulated-heading"
            className="font-heading text-cc-heading text-h3 mt-3 font-semibold text-balance"
          >
            Regulated industry or air-gapped?
          </h2>
          <p className="text-cc-ink mt-4 max-w-xl text-base text-pretty">
            We work directly with platform teams on procurement, data residency
            review, and dedicated onboarding. Bring us a constraint, we&rsquo;ll
            come back with an architecture.
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
        Spin up a free project in minutes, or browse the docs to see how Nitro
        fits into your existing CI and gateway.
      </p>
      <div className="mt-7 flex flex-wrap items-center justify-center gap-3">
        <SolidButton href="/get-started">Start for Free</SolidButton>
        <OutlineButton href="/docs">Read the Docs</OutlineButton>
      </div>
    </section>
  );
}

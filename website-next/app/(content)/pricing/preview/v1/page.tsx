import type { Metadata } from "next";
import type { ReactNode } from "react";

import { CheckIcon } from "@/src/components/CheckIcon";
import type {
  Cell,
  ComparisonGroup,
  PricingFaq,
  Tier,
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
  title: "Pricing for Nitro by ChilliCream",
  description:
    "Nitro pricing for the ChilliCream GraphQL platform: start free on shared cloud, scale on Pay as you go billed by usage, run a dedicated single-tenant instance with SSO and private networking, or self-host.",
  keywords: [
    "Nitro pricing",
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
      "Plans for the Nitro GraphQL platform: free shared cloud, Pay as you go at $20/mo billed by usage, a dedicated single-tenant instance from $400/mo, or self-hosted on your own infra.",
  },
  robots: { index: false, follow: false },
};

// The three cloud tiers are shown as plan cards; self-hosted sits below as a
// slim strip. The comparison table renders all four tiers as columns.
const CLOUD_TIERS = TIERS.filter((tier) => tier.id !== "self");
const SELF_HOSTED = TIERS.find((tier) => tier.id === "self");

export default function PricingPreviewV1Page() {
  return (
    <>
      <Hero />
      <PlanGrid />
      <ComparisonMatrix />
      <UnlockAsYouGrow />
      <Faq />
      <CustomBand />
      <ClosingCta />
    </>
  );
}

function Hero() {
  return (
    <section className="pt-10 pb-14 text-center sm:pt-16 sm:pb-20">
      <p className="text-cc-nav-label font-mono text-xs tracking-[0.18em] uppercase">
        Nitro pricing
      </p>
      <h1 className="font-heading text-cc-heading sm:text-h2 mt-5 text-4xl font-semibold">
        Pricing that scales with your GraphQL platform.
      </h1>
      <p className="text-cc-ink mx-auto mt-6 max-w-2xl text-base text-pretty sm:text-lg">
        Start free on shared cloud. Move to Pay as you go when you outgrow the
        free limits, billed only for the operations and ingest you use. Run a
        dedicated single-tenant instance, or self-host on your own
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
      <div className="grid gap-6 lg:grid-cols-3 lg:items-stretch">
        {CLOUD_TIERS.map((tier) => (
          <PlanCard key={tier.id} plan={tier} />
        ))}
      </div>
      {SELF_HOSTED && <SelfHostedStrip tier={SELF_HOSTED} />}
    </section>
  );
}

function PlanCard({ plan }: { readonly plan: Tier }) {
  if (plan.popular) {
    return (
      <div
        className="relative rounded-3xl p-[1.5px] lg:-my-2"
        style={{
          background:
            "linear-gradient(140deg, #16b9e4 0%, #7c92c6 50%, #f0786a 100%)",
        }}
      >
        <PopularPill />
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

function PlanCardBody({ plan }: { readonly plan: Tier }) {
  const CallToAction = plan.popular ? SolidButton : OutlineButton;
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

function SelfHostedStrip({ tier }: { readonly tier: Tier }) {
  return (
    <div className="border-cc-card-border bg-cc-card-bg/60 mt-6 flex flex-col gap-5 rounded-3xl border p-6 sm:flex-row sm:items-center sm:justify-between sm:p-8">
      <div>
        <h3 className="font-heading text-cc-heading text-h6 font-semibold">
          {tier.name}
        </h3>
        <p className="text-cc-ink mt-2 text-sm text-pretty">
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

function PopularPill() {
  return (
    <span className="bg-cc-surface text-cc-accent border-cc-accent absolute top-0 left-1/2 z-10 -translate-x-1/2 -translate-y-1/2 rounded-full border px-4 py-1 font-mono text-[0.65rem] tracking-[0.18em] whitespace-nowrap uppercase">
      Most popular
    </span>
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
          The same Nitro platform on every plan. What changes is where it runs,
          who you share it with, how it is billed, and the support you get.
        </p>
      </div>

      <div className="mt-10 overflow-x-auto">
        <table className="w-full min-w-[56rem] border-separate border-spacing-0 text-left text-sm">
          <thead>
            <tr>
              <th scope="col" className="w-2/5 pb-4 pl-2">
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
      ) : (
        <span className="text-cc-ink">{value}</span>
      )}
    </td>
  );
}

function UnlockAsYouGrow() {
  return (
    <section aria-labelledby="unlock-heading" className="mt-20 sm:mt-28">
      <div className="mx-auto max-w-2xl text-center">
        <h2
          id="unlock-heading"
          className="font-heading text-cc-heading text-h4 sm:text-h3 font-semibold"
        >
          Unlock more as you grow
        </h2>
        <p className="text-cc-ink mt-4 text-base text-pretty">
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

function UnlockRow({
  unlock,
  index,
}: {
  readonly unlock: Unlock;
  readonly index: number;
}) {
  const Glyph = UNLOCK_GLYPHS[index] ?? SupportGlyph;
  return (
    <li className="border-cc-card-border bg-cc-card-bg flex items-center gap-4 rounded-2xl border p-5 sm:gap-5 sm:p-6">
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

/** Shield with a check, for the priority-support unlock. */
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

const UNLOCK_GLYPHS = [SupportGlyph, ShieldGlyph, CloudGlyph];

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

function CustomBand() {
  return (
    <Section className="mt-20 sm:mt-28">
      <div className="border-cc-card-border bg-cc-card-bg/70 grid gap-8 rounded-3xl border p-8 sm:p-12 md:grid-cols-[1.4fr_1fr] md:items-center">
        <div>
          <p className="text-cc-nav-label font-mono text-xs tracking-[0.18em] uppercase">
            Custom plans
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
            <CustomCheck>Dedicated solution architect</CustomCheck>
            <CustomCheck>Annual contracts &amp; POs</CustomCheck>
            <CustomCheck>Security &amp; DPA review</CustomCheck>
            <CustomCheck>Migration playbooks</CustomCheck>
            <CustomCheck>Custom commercial terms</CustomCheck>
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

function CustomCheck({ children }: { readonly children: ReactNode }) {
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
        Ship your GraphQL platform with Nitro.
      </h2>
      <p className="text-cc-ink mx-auto mt-5 max-w-2xl text-base">
        Start free on shared cloud in minutes. Move to Pay as you go when you
        grow, or talk to us about a dedicated instance. The docs walk you
        through every step.
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

import type { Metadata } from "next";

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
    "Nitro GraphQL pricing for the ChilliCream platform: start free on shared cloud (1M operations, 2 GB ingest, 3-day retention), move to Pay as you go at $20/mo, or run a dedicated, volume-based instance from $400.",
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
      "Nitro pricing: a free shared tier, Pay as you go at $20/mo with usage-based billing, a dedicated volume-based cloud from $400, or self-hosted on your own infrastructure.",
  },
  robots: { index: false, follow: false },
};

const CLOUD_TIERS = TIERS.filter((tier) => tier.id !== "self");
const SELF_HOSTED = TIERS.find((tier) => tier.id === "self");

const PLATFORM_NOTES: readonly string[] = [
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
      <SectionRule marker="03 / Growth" />
      <GrowthDispatch />
      <SectionRule marker="04 / Questions" />
      <FaqDispatch />
      <SectionRule marker="05 / Platform" />
      <PlatformDispatch />
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
        Start free on the shared cloud, with 1M operations, 2 GB of ingest, and
        3-day retention. Move to Pay as you go at $20 a month when you outgrow
        the free limits, then pay only for what you run. Run a dedicated,
        single-tenant instance priced by volume from $400, or self-host on your
        own infrastructure when the policy demands it. The Nitro platform stays
        the same across all four; what shifts is where it runs and what we owe
        you.
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
        {CLOUD_TIERS.map((plan, index) => (
          <PlanEntry key={plan.id} plan={plan} index={index} />
        ))}
      </div>
      <SelfHostedStrip />
    </section>
  );
}

interface PlanEntryProps {
  readonly plan: Tier;
  readonly index: number;
}

function PlanEntry({ plan, index }: PlanEntryProps) {
  const CallToAction = plan.popular ? SolidButton : OutlineButton;
  return (
    <article
      aria-labelledby={`plan-${plan.id}`}
      className={
        index === 0 ? "" : "border-cc-card-border border-t pt-20 sm:pt-24"
      }
    >
      <div className="flex flex-col gap-6 sm:flex-row sm:items-baseline sm:justify-between sm:gap-10">
        <div>
          {plan.popular ? (
            <p className="text-cc-accent font-mono text-[0.65rem] tracking-[0.22em] uppercase">
              Most popular
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

function SelfHostedStrip() {
  if (!SELF_HOSTED) {
    return null;
  }
  const plan = SELF_HOSTED;
  return (
    <article
      aria-labelledby={`plan-${plan.id}`}
      className="border-cc-card-border mt-20 border-t pt-12 sm:mt-24"
    >
      <div className="flex flex-col gap-6 sm:flex-row sm:items-baseline sm:justify-between sm:gap-10">
        <div>
          <p className="text-cc-nav-label font-mono text-[0.65rem] tracking-[0.22em] uppercase">
            Also available
          </p>
          <h3
            id={`plan-${plan.id}`}
            className="font-heading text-cc-heading text-h4 mt-2 font-semibold"
          >
            {plan.name}
          </h3>
          <p className="text-cc-nav-label mt-2 font-mono text-xs tracking-[0.18em] uppercase">
            {plan.tagline}
          </p>
        </div>
        <div className="text-left sm:text-right">
          <p className="font-heading text-cc-heading text-h3 leading-none font-semibold">
            {plan.price}
          </p>
          <p className="text-cc-nav-label mt-2 font-mono text-xs tracking-[0.18em] uppercase">
            {plan.priceNote}
          </p>
        </div>
      </div>

      <p className="text-cc-ink text-body mt-6 text-pretty">
        Run the full Nitro control plane on your own infrastructure, including
        air-gapped and on-prem environments, with configurable retention,
        priority engineering support, and a long-term release channel.
      </p>

      <ul className="mt-8 flex flex-wrap gap-x-8 gap-y-3">
        {plan.features.map((feature) => (
          <li
            key={feature}
            className="text-cc-ink-dim flex items-center gap-2 text-sm"
          >
            <span className="text-cc-accent flex-none">
              <CheckIcon size={12} />
            </span>
            {feature}
          </li>
        ))}
      </ul>

      <div className="mt-8 flex flex-wrap items-center gap-3">
        <OutlineButton href={plan.ctaHref}>{plan.cta}</OutlineButton>
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
          The same Nitro platform across all four plans. What changes is where
          it runs, who you share it with, and what you get from us. The matrix
          below is set as a printed feature table, hairline rules only, no card
          chrome.
        </p>
      </header>

      <div className="-mx-4 mt-12 overflow-x-auto sm:-mx-6 lg:-mx-[calc((100vw-68ch)/2)]">
        <div className="min-w-[56rem] px-4 sm:px-6 lg:px-[calc((100vw-68ch)/2)]">
          <table className="w-full border-separate border-spacing-0 text-left">
            <thead>
              <tr>
                <th scope="col" className="w-2/5 pb-5 pl-2 align-bottom">
                  <span className="text-cc-nav-label font-mono text-[0.65rem] tracking-[0.18em] uppercase">
                    Capability
                  </span>
                </th>
                {TIERS.map((tier) => (
                  <th
                    key={tier.id}
                    scope="col"
                    className={`px-4 pb-5 text-center align-bottom ${
                      tier.popular ? "bg-cc-accent/5" : ""
                    }`}
                  >
                    <div
                      className={`font-heading text-cc-heading text-base font-semibold ${
                        tier.popular ? "text-cc-accent" : ""
                      }`}
                    >
                      {tier.name}
                    </div>
                    <div className="text-cc-nav-label mt-2 font-mono text-[0.65rem] tracking-[0.18em] uppercase">
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
          colSpan={5}
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
          <ComparisonCell value={row.free} />
          <ComparisonCell value={row.payg} highlight />
          <ComparisonCell value={row.dedicated} />
          <ComparisonCell value={row.self} />
        </tr>
      ))}
    </>
  );
}

interface ComparisonCellProps {
  readonly value: Cell;
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

function GrowthDispatch() {
  return (
    <section aria-labelledby="growth-heading" className="py-24 sm:py-32">
      <header className="mx-auto max-w-[52ch] text-center">
        <h2
          id="growth-heading"
          className="font-heading text-cc-heading text-h3 font-semibold"
        >
          Unlock more as you grow
        </h2>
        <p className="text-cc-ink text-body mt-6 text-pretty">
          Commit to a minimum monthly spend to unlock more support and
          deployment options, up to your spend.
        </p>
      </header>

      <ul className="mt-12">
        {UNLOCKS.map((unlock, index) => (
          <UnlockEntry key={unlock.title} unlock={unlock} index={index} />
        ))}
      </ul>

      <p className="text-cc-nav-label mt-6 text-center font-mono text-xs tracking-[0.18em] uppercase">
        {UNLOCKS_NOTE}
      </p>
    </section>
  );
}

interface UnlockEntryProps {
  readonly unlock: Unlock;
  readonly index: number;
}

function UnlockEntry({ unlock, index }: UnlockEntryProps) {
  const Glyph = UNLOCK_ICONS[index] ?? SupportGlyph;
  return (
    <li className="border-cc-card-border flex items-center gap-5 border-t py-7">
      <span className="text-cc-accent flex-none">
        <Glyph className="size-6" />
      </span>
      <div className="min-w-0 flex-1">
        <h3 className="font-heading text-cc-heading text-h5 font-semibold">
          {unlock.title}
        </h3>
        <p className="text-cc-ink-dim text-body mt-1 text-pretty">
          {unlock.description}
        </p>
      </div>
      <span className="text-cc-accent flex-none font-mono text-sm font-semibold tracking-[0.05em] tabular-nums">
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
  readonly item: PricingFaq;
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

function PlatformDispatch() {
  return (
    <section aria-labelledby="platform-heading" className="py-24 sm:py-32">
      <h2
        id="platform-heading"
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
        {PLATFORM_NOTES.map((note) => (
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
        Start free in minutes. Move to Pay as you go when you outgrow the free
        limits, or to a dedicated instance when you need your own region, SSO,
        and an audit log. The docs walk you through every step.
      </p>
      <div className="mt-10 flex flex-wrap items-center justify-center gap-3">
        <SolidButton href="/get-started">Start for Free</SolidButton>
        <OutlineButton href="/docs">Read the docs</OutlineButton>
      </div>
    </footer>
  );
}

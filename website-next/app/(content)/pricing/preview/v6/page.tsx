import type { Metadata } from "next";
import { Fragment } from "react";
import type { ComponentType, ReactNode } from "react";

import { CheckIcon } from "@/src/components/CheckIcon";
import type {
  Cell,
  ComparisonGroup,
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
import { CoffeeTray } from "@/src/icons/CoffeeTray";
import { DripBrewer } from "@/src/icons/DripBrewer";
import { Espresso } from "@/src/icons/Espresso";
import { FrenchPress } from "@/src/icons/FrenchPress";

export const metadata: Metadata = {
  title: "Nitro GraphQL pricing, served your way",
  description:
    "Nitro GraphQL pricing, same beans, your pour. Start free on shared cloud, scale on usage-based Pay as you go from $20 a month, or reserve a single-tenant Dedicated instance priced by volume from $400.",
  robots: { index: false, follow: false },
};

// Visual-only decoration per tier: the coffee "brew" label and icon. The
// pricing facts all come from the shared module; this map only styles them.
interface TierPresentation {
  readonly brewLabel: string;
  readonly icon: ComponentType<{ readonly className?: string }>;
}

const TIER_PRESENTATION: Record<TierId, TierPresentation> = {
  free: { brewLabel: "House Pour", icon: FrenchPress },
  payg: { brewLabel: "Daily Drip", icon: DripBrewer },
  dedicated: { brewLabel: "Single-Origin Reserve", icon: Espresso },
  self: { brewLabel: "Whole-Bean", icon: CoffeeTray },
};

const CLOUD_TIERS = TIERS.filter((tier) => tier.id !== "self");
const SELF_HOSTED = TIERS.find((tier) => tier.id === "self");

// Escalating coffee strength per unlock tier: a light brew for the first
// threshold, a stronger pour for each one above it.
const UNLOCK_ICONS: readonly ComponentType<{ readonly className?: string }>[] =
  [FrenchPress, DripBrewer, Espresso];

interface BarTile {
  readonly eyebrow: string;
  readonly title: string;
  readonly body: string;
}

const BAR_TILES: readonly BarTile[] = [
  {
    eyebrow: "Same beans",
    title: "One Nitro platform",
    body: "Schema registry, client registry, CI checks, MCP, and OpenTelemetry-native insights ship on every tier.",
  },
  {
    eyebrow: "Different cup",
    title: "Where it runs",
    body: "Shared multi-tenant cloud on Free and Pay as you go, single-tenant cloud or BYOC on Dedicated, or your own infrastructure including air-gapped.",
  },
  {
    eyebrow: "Different pour",
    title: "What you pay",
    body: "Free is capped at 1M operations and 2 GB a month. Pay as you go is usage based from $20. Dedicated is priced by volume from $400.",
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
      <UnlockAsYouGrow />
      <Faq />
      <CustomBand />
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
        One Nitro platform, poured to fit. Start free on shared cloud, move to
        usage-based Pay as you go when you outgrow it, reserve a single-tenant
        Dedicated instance priced by volume, or self-host on your own
        infrastructure when the policy demands it.
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
        {CLOUD_TIERS.map((tier) => (
          <PlanCard key={tier.id} tier={tier} />
        ))}
      </div>
      {SELF_HOSTED && <SelfHostedStrip tier={SELF_HOSTED} />}
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
        {CLOUD_TIERS.map((tier, index) => (
          <Fragment key={tier.id}>
            {index > 0 && (
              <span aria-hidden="true" className="text-cc-ink-faint">
                /
              </span>
            )}
            <span>{TIER_PRESENTATION[tier.id].brewLabel}</span>
          </Fragment>
        ))}
      </div>
      <div className="hidden font-mono text-[0.65rem] tracking-[0.22em] uppercase sm:block">
        <span className="text-cc-nav-label">Today, </span>
        <span className="text-cc-ink-dim">freshly brewed</span>
      </div>
    </div>
  );
}

function PlanCard({ tier }: { readonly tier: Tier }) {
  if (tier.popular) {
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
          <PlanCardBody tier={tier} />
        </div>
      </div>
    );
  }

  return (
    <div className="bg-cc-card-bg border-cc-card-border hover:border-cc-card-border-hover flex h-full flex-col rounded-3xl border p-7 transition-colors sm:p-8">
      <PlanCardBody tier={tier} />
    </div>
  );
}

function PlanCardBody({ tier }: { readonly tier: Tier }) {
  const CallToAction = tier.popular ? SolidButton : OutlineButton;
  const { brewLabel, icon: Icon } = TIER_PRESENTATION[tier.id];
  return (
    <>
      <div className="flex items-start justify-between gap-4">
        <div>
          <p className="text-cc-nav-label font-mono text-[0.65rem] tracking-[0.2em] uppercase">
            {brewLabel}
          </p>
          <h3 className="font-heading text-cc-heading text-h5 mt-2 font-semibold">
            {tier.name}
          </h3>
        </div>
        <Icon className="text-cc-ink-dim h-10 w-10 flex-none" />
      </div>
      <p className="text-cc-ink-dim mt-3 text-sm">{tier.tagline}</p>
      <div className="mt-6 flex items-baseline gap-2">
        <span className="font-heading text-cc-heading text-h3 font-semibold">
          {tier.price}
        </span>
        <span className="text-cc-nav-label font-mono text-xs">
          {tier.priceNote}
        </span>
      </div>
      <SteamLine />
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

function SelfHostedStrip({ tier }: { readonly tier: Tier }) {
  const { brewLabel, icon: Icon } = TIER_PRESENTATION[tier.id];
  return (
    <div className="border-cc-card-border bg-cc-card-bg/60 mt-6 flex flex-col gap-5 rounded-3xl border p-6 sm:flex-row sm:items-center sm:justify-between sm:p-8">
      <div className="flex items-start gap-4">
        <Icon className="text-cc-ink-dim hidden h-10 w-10 flex-none sm:block" />
        <div>
          <p className="text-cc-nav-label font-mono text-[0.65rem] tracking-[0.2em] uppercase">
            {brewLabel}
          </p>
          <h3 className="font-heading text-cc-heading text-h6 mt-2 font-semibold">
            {tier.name}
          </h3>
          <p className="text-cc-ink-dim mt-2 max-w-2xl text-sm text-pretty">
            {tier.tagline} Run on your own infrastructure, air-gapped or
            on-prem, with configurable retention, priority engineering support,
            and a long-term release channel.
          </p>
        </div>
      </div>
      <OutlineButton href={tier.ctaHref} className="shrink-0 sm:w-auto">
        {tier.cta}
      </OutlineButton>
    </div>
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
          The tier you pick changes the cup, not the beans. Nitro&apos;s schema
          registry, CI checks, MCP server, and OpenTelemetry-native insights
          ship on every one.
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
          Compare tiers
        </p>
        <h2
          id="compare-heading"
          className="font-heading text-cc-heading text-h4 sm:text-h3 mt-3 font-semibold"
        >
          Every capability, side by side.
        </h2>
        <p className="text-cc-ink mx-auto mt-4 max-w-2xl text-base">
          The same Nitro platform across all four tiers. What changes is where
          it runs, what you pay, and what you get from us.
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
        <p className="text-cc-nav-label font-mono text-xs tracking-[0.18em] uppercase">
          More per pour
        </p>
        <h2
          id="unlock-heading"
          className="font-heading text-cc-heading text-h4 sm:text-h3 mt-3 font-semibold"
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
  const Icon = UNLOCK_ICONS[index] ?? FrenchPress;
  return (
    <li className="border-cc-card-border bg-cc-surface/60 flex items-center gap-4 rounded-2xl border p-5 sm:gap-5 sm:p-6">
      <span className="border-cc-card-border bg-cc-card-bg text-cc-ink-dim flex size-11 shrink-0 items-center justify-center rounded-xl border">
        <Icon className="h-5 w-5" />
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
            <CustomCheck>Dedicated solution architect</CustomCheck>
            <CustomCheck>Annual contracts and POs</CustomCheck>
            <CustomCheck>Security and DPA review</CustomCheck>
            <CustomCheck>Migration playbooks</CustomCheck>
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
        Pour your first cup.
      </h2>
      <p className="text-cc-ink mx-auto mt-5 max-w-2xl text-base">
        Start free on shared cloud in minutes. Move to Pay as you go when you
        outgrow the free pour, or reserve a Dedicated instance when the
        workload, or the policy, demands it. The docs walk you through every
        step.
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

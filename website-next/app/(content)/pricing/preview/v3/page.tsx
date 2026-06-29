import type { Metadata } from "next";
import Link from "next/link";
import { Fragment } from "react";
import type { ComponentType, ReactNode } from "react";

import { CheckIcon } from "@/src/components/CheckIcon";
import type {
  Cell,
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
import { NitroMonitoring } from "@/src/nitro";

export const metadata: Metadata = {
  title: "Nitro Pricing: Plans for every team | ChilliCream",
  description:
    "Nitro pricing for the ChilliCream GraphQL platform. Start free on shared cloud (1M ops, 2 GB, 3-day retention), pay as you go at $20/mo, scale to a dedicated single-tenant instance, or self-host anywhere.",
  keywords: [
    "Nitro pricing",
    "ChilliCream plans",
    "GraphQL platform pricing",
    "Nitro plans",
    "GraphQL observability pricing",
    "GraphQL schema registry pricing",
    "self-hosted GraphQL",
    "BYOC GraphQL",
  ],
  robots: { index: false, follow: false },
  openGraph: {
    title: "Nitro Pricing: Plans for every team",
    description:
      "Start free. Pay as you grow. Run anywhere. Pricing for the ChilliCream Nitro GraphQL platform: observability, schema registry, CI checks, and the GraphQL IDE.",
  },
};

/* ---------------------------------------------------------------------- */
/* Narrative framing                                                       */
/*                                                                         */
/* All pricing facts (price, features, CTA) come from the shared pricing   */
/* module. This map only carries the per-chapter narrative voice that is   */
/* unique to this preview; it never restates a number.                     */
/* ---------------------------------------------------------------------- */

interface Chapter {
  readonly chapter: string;
  readonly subtitle: string;
  readonly story: string;
  readonly forWho: string;
  readonly unlocks: string;
}

const CHAPTERS: Partial<Record<TierId, Chapter>> = {
  free: {
    chapter: "Chapter 01",
    subtitle: "Shared cloud, fully managed",
    story:
      "Start the day you decide. No card, no procurement loop. Push a schema, wire up a client, watch the first traces land. The platform you'll grow into is already the platform you start on.",
    forWho: "Solo builders, early teams, weekend experiments.",
    unlocks:
      "A live schema registry, the GraphQL IDE served from your endpoint, and operation telemetry once Nitro is configured for your service. Capped, so the bill stays at zero.",
  },
  payg: {
    chapter: "Chapter 02",
    subtitle: "Shared cloud, usage based",
    story:
      "The free caps were a starting line, not a ceiling. Pay as you go keeps you on the same shared cloud, lifts the included volume, stretches retention, and bills only for what you actually run past the line. No replatforming, just more room.",
    forWho: "Growing teams shipping to production.",
    unlocks:
      "A larger included volume, 60-day retention, email support, and metered usage after the included operations and ingest, so a busy month costs what it should and a quiet one doesn't.",
  },
  dedicated: {
    chapter: "Chapter 03",
    subtitle: "Single-tenant, fully managed",
    story:
      "When your platform stops being a side project, the rules change. You need a region that's yours, controls auditors recognise, and pricing that follows the footprint instead of every operation. Same product, dedicated metal.",
    forWho: "Scale-ups, platform teams, regulated industries.",
    unlocks:
      "A single-tenant region (or BYOC), volume based pricing on compute, storage, and nodes, configurable retention, private networking, SSO, audit log, and role-based access.",
  },
};

const CLOUD_TIERS = TIERS.filter((tier) => tier.id !== "self");
const SELF_HOSTED = TIERS.find((tier) => tier.id === "self");

const TIER_COLUMNS: readonly { readonly id: TierId; readonly name: string }[] =
  TIERS.map((tier) => ({ id: tier.id, name: tier.name }));

export default function PricingV3Page() {
  return (
    <div className="-mx-5 sm:-mx-12">
      <div className="mx-auto max-w-5xl px-5 sm:px-12">
        <Hero />
      </div>

      <div className="mx-auto mt-20 max-w-5xl px-5 sm:mt-28 sm:px-12">
        <ProductBand />
      </div>

      <div className="mx-auto mt-24 max-w-5xl px-5 sm:mt-36 sm:px-12">
        <PlansSection />
      </div>

      <div className="mx-auto mt-24 max-w-5xl px-5 sm:mt-32 sm:px-12">
        <HonestyBlock />
      </div>

      <div className="mx-auto mt-24 max-w-5xl px-5 sm:mt-32 sm:px-12">
        <ComparisonTable />
      </div>

      <div className="mx-auto mt-24 max-w-5xl px-5 sm:mt-32 sm:px-12">
        <UnlockSection />
      </div>

      <div className="mx-auto mt-24 max-w-5xl px-5 sm:mt-32 sm:px-12">
        <FaqSection />
      </div>

      <div className="mx-auto mt-24 max-w-5xl px-5 sm:mt-32 sm:px-12">
        <ScaleBand />
      </div>

      <div className="mx-auto mt-20 mb-8 max-w-5xl px-5 sm:mt-28 sm:px-12">
        <ClosingCta />
      </div>
    </div>
  );
}

/* ---------------------------------------------------------------------- */
/* Sections                                                                */
/* ---------------------------------------------------------------------- */

function Hero() {
  return (
    <section className="relative pt-6 sm:pt-10">
      <Eyebrow>Pricing · Three acts, one platform</Eyebrow>
      <h1 className="font-heading text-cc-heading text-h2 sm:text-hero mt-6 font-semibold tracking-tight">
        Start free.
        <br />
        Pay as you grow.
        <br />
        <span
          className="bg-clip-text text-transparent"
          style={{
            backgroundImage:
              "linear-gradient(90deg, #16b9e4 0%, #7c92c6 55%, #f0786a 100%)",
          }}
        >
          Run anywhere.
        </span>
      </h1>
      <p className="lead text-cc-ink-dim mt-8 max-w-3xl text-pretty">
        Nitro is the control plane behind your GraphQL APIs: schema registry, CI
        checks, deployments, observability, and the GraphQL IDE. Run it free on
        shared cloud, move to pay as you go when you outgrow the caps, take a
        dedicated single-tenant instance when isolation asks for it, or
        self-host anywhere. Pick the one that fits where you are today, switch
        when you&rsquo;re ready.
      </p>

      <div className="mt-10 flex flex-wrap items-center gap-4">
        <SolidButton href="/get-started">Start for Free</SolidButton>
        <OutlineButton href="/services/support/contact">
          Talk to Sales
        </OutlineButton>
        <Link
          href="#plans"
          className="text-cc-nav-label hover:text-cc-ink ml-2 font-mono text-xs tracking-[0.18em] uppercase"
        >
          Jump to plans &rarr;
        </Link>
      </div>

      <div
        aria-hidden="true"
        className="pointer-events-none absolute -top-10 right-0 -z-10 h-72 w-72 rounded-full opacity-30 blur-3xl sm:h-96 sm:w-96"
        style={{
          background:
            "radial-gradient(circle, #16b9e4 0%, rgba(124, 146, 198, 0.4) 45%, transparent 70%)",
        }}
      />
    </section>
  );
}

function ProductBand() {
  return (
    <section aria-labelledby="product-band-heading">
      <div className="flex items-end justify-between gap-6">
        <div>
          <Eyebrow>Before the price</Eyebrow>
          <h2
            id="product-band-heading"
            className="font-heading text-cc-heading text-h3 mt-4 font-semibold"
          >
            What every plan gets you
          </h2>
        </div>
        <p className="text-cc-ink-dim hidden max-w-sm text-sm sm:block">
          Same control plane, same dashboards. Schemas and environments are
          included everywhere. The plan changes where it runs and how it scales,
          not what you see when you open it.
        </p>
      </div>

      <div className="border-cc-card-border bg-cc-card-bg mx-auto mt-8 max-w-5xl overflow-hidden rounded-xl border">
        <NitroMonitoring />
      </div>

      <p className="text-cc-ink-dim mt-4 text-center font-mono text-xs tracking-wide sm:text-left">
        Live monitoring overview · the same dashboard ships in every plan
      </p>
    </section>
  );
}

function PlansSection() {
  return (
    <section id="plans" aria-labelledby="plans-heading">
      <Eyebrow>The plans</Eyebrow>
      <h2
        id="plans-heading"
        className="font-heading text-cc-heading text-h3 sm:text-h2 mt-4 font-semibold"
      >
        Three chapters. Same story.
      </h2>
      <p className="text-cc-ink-dim mt-5 max-w-2xl text-pretty">
        Three ways to run Nitro on our cloud, every one shipping the full
        control plane. The difference is where it lives and what scales with
        you. Need to run it yourself? That&rsquo;s the coda below.
      </p>

      <div className="mt-16 flex flex-col gap-20 sm:gap-28">
        {CLOUD_TIERS.map((tier, index) => {
          const chapter = CHAPTERS[tier.id];
          if (!chapter) {
            return null;
          }
          return (
            <PlanStory
              key={tier.id}
              tier={tier}
              chapter={chapter}
              index={index}
            />
          );
        })}
      </div>

      {SELF_HOSTED && <SelfHostedStrip tier={SELF_HOSTED} />}
    </section>
  );
}

interface PlanStoryProps {
  readonly tier: Tier;
  readonly chapter: Chapter;
  readonly index: number;
}

function PlanStory({ tier, chapter, index }: PlanStoryProps) {
  const reversed = index % 2 === 1;
  return (
    <article
      className={`grid items-center gap-10 lg:grid-cols-12 lg:gap-12 ${
        reversed ? "lg:[&>*:first-child]:order-2" : ""
      }`}
    >
      <div className="lg:col-span-5">
        <p className="text-cc-accent font-mono text-xs tracking-[0.22em] uppercase">
          {chapter.chapter}
        </p>
        <h3 className="font-heading text-cc-heading text-h3 mt-4 font-semibold">
          {tier.name}
        </h3>
        <p className="text-cc-nav-label mt-2 font-mono text-xs">
          {chapter.subtitle}
        </p>
        <p className="text-cc-ink mt-6 text-pretty">{chapter.story}</p>

        <dl className="mt-8 grid gap-4">
          <div>
            <dt className="text-cc-nav-label font-mono text-[0.65rem] tracking-[0.18em] uppercase">
              For
            </dt>
            <dd className="text-cc-ink mt-1 text-sm">{chapter.forWho}</dd>
          </div>
          <div>
            <dt className="text-cc-nav-label font-mono text-[0.65rem] tracking-[0.18em] uppercase">
              Unlocks
            </dt>
            <dd className="text-cc-ink mt-1 text-sm">{chapter.unlocks}</dd>
          </div>
        </dl>
      </div>

      <div className="lg:col-span-7">
        <PlanCard tier={tier} />
      </div>
    </article>
  );
}

function PlanCard({ tier }: { readonly tier: Tier }) {
  const Cta = tier.popular ? SolidButton : OutlineButton;
  return (
    <div
      className={`relative overflow-hidden rounded-3xl border p-7 sm:p-9 ${
        tier.popular
          ? "border-cc-accent bg-cc-card-bg"
          : "border-cc-card-border bg-cc-card-bg/60"
      }`}
    >
      {tier.popular && (
        <div className="absolute top-5 right-5">
          <span className="border-cc-accent text-cc-accent rounded-full border px-3 py-1 font-mono text-[0.6rem] tracking-[0.18em] uppercase">
            Most Popular
          </span>
        </div>
      )}

      {tier.popular && (
        <div
          aria-hidden="true"
          className="pointer-events-none absolute -top-24 -right-24 h-64 w-64 rounded-full opacity-25 blur-3xl"
          style={{
            background: "radial-gradient(circle, #5eead4 0%, transparent 70%)",
          }}
        />
      )}

      <div className="relative">
        <div className="flex items-baseline gap-3">
          <span className="font-heading text-cc-heading text-hero font-semibold">
            {tier.price}
          </span>
          <span className="text-cc-nav-label font-mono text-xs">
            {tier.priceNote}
          </span>
        </div>

        <div className="border-cc-ink-faint my-7 border-t border-dashed" />

        <ul className="grid gap-3 sm:grid-cols-2">
          {tier.features.map((feature) => (
            <li key={feature} className="flex items-start gap-3">
              <span className="text-cc-accent mt-1 flex-none">
                <CheckIcon />
              </span>
              <span className="text-cc-ink text-sm">{feature}</span>
            </li>
          ))}
        </ul>

        <div className="mt-8 flex flex-wrap items-center gap-4">
          <Cta href={tier.ctaHref} className="min-w-[10rem]">
            {tier.cta}
          </Cta>
          <Link
            href="/docs"
            className="text-cc-nav-label hover:text-cc-ink font-mono text-xs tracking-[0.18em] uppercase"
          >
            Read the docs &rarr;
          </Link>
        </div>
      </div>
    </div>
  );
}

function SelfHostedStrip({ tier }: { readonly tier: Tier }) {
  return (
    <div className="border-cc-card-border bg-cc-card-bg/60 mt-16 sm:mt-20">
      <div className="border-cc-card-border bg-cc-card-bg/60 relative overflow-hidden rounded-3xl border p-7 sm:p-9">
        <div
          aria-hidden="true"
          className="pointer-events-none absolute -bottom-24 -left-24 h-64 w-64 rounded-full opacity-20 blur-3xl"
          style={{
            background: "radial-gradient(circle, #7c92c6 0%, transparent 70%)",
          }}
        />
        <div className="relative grid items-center gap-6 lg:grid-cols-[1fr_auto]">
          <div>
            <p className="text-cc-accent font-mono text-xs tracking-[0.22em] uppercase">
              Coda · Run it yourself
            </p>
            <h3 className="font-heading text-cc-heading text-h4 mt-3 font-semibold">
              {tier.name}
            </h3>
            <p className="text-cc-ink mt-3 max-w-2xl text-pretty">
              Some environments don&rsquo;t get a public endpoint. Air-gapped
              networks, sovereign clouds, the floor of a factory. Run the entire
              control plane on infrastructure you own, with configurable
              retention, priority engineering support, and a long-term release
              channel.
            </p>
            <ul className="mt-6 flex flex-wrap gap-x-6 gap-y-2">
              {tier.features.map((feature) => (
                <li
                  key={feature}
                  className="text-cc-ink flex items-center gap-2 text-sm"
                >
                  <span className="text-cc-accent flex-none">
                    <CheckIcon />
                  </span>
                  {feature}
                </li>
              ))}
            </ul>
          </div>
          <div className="flex flex-col gap-3 lg:items-end">
            <span className="font-heading text-cc-heading text-h4 font-semibold">
              {tier.price}
            </span>
            <OutlineButton href={tier.ctaHref} className="min-w-[10rem]">
              {tier.cta}
            </OutlineButton>
          </div>
        </div>
      </div>
    </div>
  );
}

function HonestyBlock() {
  return (
    <section aria-labelledby="honesty-heading">
      <Eyebrow>How we price</Eyebrow>
      <h2
        id="honesty-heading"
        className="font-heading text-cc-heading text-h3 sm:text-h2 mt-4 font-semibold"
      >
        The honest version.
      </h2>
      <p className="text-cc-ink-dim mt-5 max-w-2xl text-pretty">
        Pricing pages are usually written to make the cheap plan unusable and
        the expensive plan inevitable. We try not to do that. Here is
        what&rsquo;s actually true.
      </p>

      <div className="mt-10 grid gap-6 sm:grid-cols-2 lg:grid-cols-3">
        <HonestyCard
          title="No per-seat tax"
          body="Plans price the platform, not the people behind it. Add the whole engineering org to your workspace; the price stays the same."
        />
        <HonestyCard
          title="Pay only past the line"
          body="Pay as you go is $20 a month and includes 5M operations and 2 GB of ingest per million. Past that you pay $2 per additional million operations and $1.15 per additional gigabyte, billed against the same dashboard you'd use anyway."
        />
        <HonestyCard
          title="BYOC, explained plainly"
          body="Bring Your Own Cloud puts a dedicated Nitro region inside your AWS, Azure, or GCP account, with private networking back to your services. Your data, your perimeter, our control plane."
        />
        <HonestyCard
          title="The free plan is not a trial"
          body="Free isn't a 14-day countdown. It includes 1M operations, 2 GB of ingest, and 3-day retention every month, capped so it stays free. The day you outgrow it, your schemas, environments, and history move with you."
        />
        <HonestyCard
          title="Self-hosting is a first-class option"
          body="Self-Hosted runs the same product as Dedicated, on your infrastructure, on a long-term release channel, for environments where public endpoints aren't an option."
        />
      </div>
    </section>
  );
}

function HonestyCard({
  title,
  body,
}: {
  readonly title: string;
  readonly body: string;
}) {
  return (
    <div className="border-cc-card-border bg-cc-card-bg/60 hover:border-cc-card-border-hover h-full rounded-2xl border p-6 transition-colors">
      <h3 className="font-heading text-cc-heading text-h6 font-semibold">
        {title}
      </h3>
      <p className="text-cc-ink mt-3 text-sm text-pretty">{body}</p>
    </div>
  );
}

function ComparisonTable() {
  return (
    <section aria-labelledby="compare-heading">
      <Eyebrow>Compare</Eyebrow>
      <h2
        id="compare-heading"
        className="font-heading text-cc-heading text-h3 sm:text-h2 mt-4 font-semibold"
      >
        The capabilities, side by side.
      </h2>
      <p className="text-cc-ink-dim mt-5 max-w-2xl text-pretty">
        Same control plane in every column. The columns differ in where it runs,
        how it scales, and what wraps around it.
      </p>

      <div className="border-cc-card-border bg-cc-card-bg/60 mt-10 overflow-hidden rounded-2xl border">
        <div className="overflow-x-auto">
          <table className="w-full min-w-[860px] border-collapse text-left">
            <thead>
              <tr className="border-cc-card-border border-b">
                <th
                  scope="col"
                  className="text-cc-nav-label px-5 py-4 font-mono text-[0.65rem] tracking-[0.18em] uppercase"
                >
                  Capability
                </th>
                {TIER_COLUMNS.map((column) => (
                  <th
                    key={column.id}
                    scope="col"
                    className={`px-5 py-4 font-mono text-[0.65rem] tracking-[0.18em] uppercase ${
                      column.id === "payg"
                        ? "text-cc-accent"
                        : "text-cc-nav-label"
                    }`}
                  >
                    {column.name}
                  </th>
                ))}
              </tr>
            </thead>
            <tbody>
              {COMPARISON.map((group, gi) => {
                const isLastGroup = gi === COMPARISON.length - 1;
                return (
                  <Fragment key={group.title}>
                    <tr className="border-cc-card-border bg-cc-card-bg/40 border-b">
                      <th
                        scope="colgroup"
                        colSpan={TIER_COLUMNS.length + 1}
                        className="text-cc-nav-label px-5 py-3 font-mono text-[0.65rem] tracking-[0.18em] uppercase"
                      >
                        {group.title}
                      </th>
                    </tr>
                    {group.rows.map((row, ri) => {
                      const isLastRow =
                        isLastGroup && ri === group.rows.length - 1;
                      return (
                        <tr
                          key={row.label}
                          className={
                            isLastRow ? "" : "border-cc-card-border border-b"
                          }
                        >
                          <th
                            scope="row"
                            className="text-cc-heading px-5 py-4 text-sm font-medium"
                          >
                            {row.label}
                          </th>
                          <td className="px-5 py-4 text-sm">
                            <CellValue value={row.free} />
                          </td>
                          <td className="px-5 py-4 text-sm">
                            <CellValue value={row.payg} accent />
                          </td>
                          <td className="px-5 py-4 text-sm">
                            <CellValue value={row.dedicated} />
                          </td>
                          <td className="px-5 py-4 text-sm">
                            <CellValue value={row.self} />
                          </td>
                        </tr>
                      );
                    })}
                  </Fragment>
                );
              })}
            </tbody>
          </table>
        </div>
      </div>
    </section>
  );
}

function CellValue({
  value,
  accent = false,
}: {
  readonly value: Cell;
  readonly accent?: boolean;
}) {
  if (value === true) {
    return (
      <span className="text-cc-accent inline-flex items-center">
        <CheckIcon />
        <span className="sr-only">Included</span>
      </span>
    );
  }
  if (value === false) {
    return (
      <span className="inline-flex items-center">
        <span
          aria-hidden="true"
          className="bg-cc-ink-faint inline-block h-px w-3"
        />
        <span className="sr-only">Not included</span>
      </span>
    );
  }
  return (
    <span className={accent ? "text-cc-heading" : "text-cc-ink"}>{value}</span>
  );
}

function UnlockSection() {
  return (
    <section aria-labelledby="unlock-heading">
      <div className="mx-auto max-w-2xl text-center">
        <h2
          id="unlock-heading"
          className="font-heading text-cc-heading text-h3 sm:text-h2 font-semibold"
        >
          Unlock more as you grow
        </h2>
        <p className="text-cc-ink-dim mt-5 text-pretty">
          Commit to a minimum monthly spend to unlock more, up to your spend.
        </p>
      </div>

      <ul className="mx-auto mt-12 flex max-w-3xl flex-col gap-3">
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
    <li className="border-cc-card-border bg-cc-card-bg flex items-center gap-4 rounded-2xl border p-5 sm:gap-5 sm:p-6">
      <span className="border-cc-card-border bg-cc-card-bg/60 text-cc-accent flex size-11 shrink-0 items-center justify-center rounded-xl border">
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

function FaqSection() {
  return (
    <section aria-labelledby="faq-heading">
      <Eyebrow>Questions</Eyebrow>
      <h2
        id="faq-heading"
        className="font-heading text-cc-heading text-h3 sm:text-h2 mt-4 font-semibold"
      >
        Frequently asked.
      </h2>
      <p className="text-cc-ink-dim mt-5 max-w-2xl text-pretty">
        Answers to the questions we hear most often. Have another?
        <Link
          href="/services/support/contact"
          className="text-cc-accent hover:text-cc-accent-hover ml-1"
        >
          Ask us directly
        </Link>
        .
      </p>

      <dl className="mt-10 grid gap-4">
        {FAQ.map((faq) => (
          <FaqItem
            key={faq.question}
            question={faq.question}
            answer={faq.answer}
          />
        ))}
      </dl>
    </section>
  );
}

function FaqItem({
  question,
  answer,
}: {
  readonly question: string;
  readonly answer: string;
}) {
  return (
    <div className="border-cc-card-border bg-cc-card-bg/60 hover:border-cc-card-border-hover rounded-2xl border p-6 transition-colors sm:p-7">
      <dt className="font-heading text-cc-heading text-h6 font-semibold">
        {question}
      </dt>
      <dd className="text-cc-ink mt-3 text-sm text-pretty">{answer}</dd>
    </div>
  );
}

function ScaleBand() {
  return (
    <section
      aria-labelledby="scale-heading"
      className="border-cc-card-border bg-cc-card-bg relative overflow-hidden rounded-3xl border p-8 sm:p-12"
    >
      <div
        aria-hidden="true"
        className="pointer-events-none absolute -top-32 -right-32 h-96 w-96 rounded-full opacity-25 blur-3xl"
        style={{
          background:
            "radial-gradient(circle, #f0786a 0%, rgba(240, 120, 106, 0.2) 45%, transparent 75%)",
        }}
      />
      <div className="relative grid items-center gap-10 lg:grid-cols-[3fr_2fr]">
        <div>
          <Eyebrow>At scale</Eyebrow>
          <h2
            id="scale-heading"
            className="font-heading text-cc-heading text-h3 sm:text-h2 mt-4 font-semibold"
          >
            Bigger footprint? Let&rsquo;s design it together.
          </h2>
          <p className="text-cc-ink mt-5 max-w-xl text-pretty">
            BYOC across multiple regions, custom procurement, security review,
            vendor onboarding, training for a hundred engineers, something
            stranger. The engineering team takes these conversations directly,
            no SDR layer in between.
          </p>
        </div>
        <div className="flex flex-col gap-4 lg:items-end">
          <SolidButton href="/services/support/contact">
            Talk to Engineering
          </SolidButton>
          <Link
            href="/platform"
            className="text-cc-nav-label hover:text-cc-ink font-mono text-xs tracking-[0.18em] uppercase"
          >
            Tour the platform &rarr;
          </Link>
        </div>
      </div>
    </section>
  );
}

function ClosingCta() {
  return (
    <section
      aria-labelledby="closing-heading"
      className="py-12 text-center sm:py-16"
    >
      <h2
        id="closing-heading"
        className="font-heading text-cc-heading text-h2 sm:text-hero font-semibold tracking-tight"
      >
        Pick a chapter.
        <br />
        <span
          className="bg-clip-text text-transparent"
          style={{
            backgroundImage:
              "linear-gradient(90deg, #16b9e4 0%, #7c92c6 55%, #f0786a 100%)",
          }}
        >
          Start writing.
        </span>
      </h2>
      <p className="text-cc-ink-dim mx-auto mt-6 max-w-xl text-pretty">
        Free forever on shared cloud. Move to pay as you go when you outgrow the
        caps, to a dedicated instance when isolation, SSO, or scale ask for it.
        Run on your own infrastructure when the world asks for that.
      </p>
      <div className="mt-10 flex flex-wrap justify-center gap-4">
        <SolidButton href="/get-started">Start for Free</SolidButton>
        <OutlineButton href="/services/support/contact">
          Talk to Sales
        </OutlineButton>
      </div>
    </section>
  );
}

/* ---------------------------------------------------------------------- */
/* Primitives                                                              */
/* ---------------------------------------------------------------------- */

interface EyebrowProps {
  readonly children: ReactNode;
}

function Eyebrow({ children }: EyebrowProps) {
  return (
    <p className="text-cc-nav-label font-mono text-xs tracking-[0.22em] uppercase">
      {children}
    </p>
  );
}

// Asserts that the explicit component shapes above match React's expected
// component type, so refactors that change the prop interfaces fail at the
// type level instead of silently at runtime.
type _PlanStoryGuard = ComponentType<PlanStoryProps>;
const _planStoryGuard: _PlanStoryGuard = PlanStory;
void _planStoryGuard;

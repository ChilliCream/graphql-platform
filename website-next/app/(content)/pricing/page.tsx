import type { Metadata } from "next";
import type { ComponentType } from "react";

import { CheckIcon } from "@/src/components/CheckIcon";
import { Offering } from "@/src/components/Offering";
import { OfferingGrid } from "@/src/components/OfferingGrid";
import type {
  Cell,
  PricingFaq,
  Tier,
  TierId,
} from "@/src/components/pricing/pricingData";
import { COMPARISON, FAQ, TIERS } from "@/src/components/pricing/pricingData";
import { OutlineButton, SolidButton } from "@/src/design-system/Button";
import { DripBrewer } from "@/src/icons/DripBrewer";
import { FrenchPress } from "@/src/icons/FrenchPress";
import { PourOver } from "@/src/icons/PourOver";

export const metadata: Metadata = {
  title: "Pricing",
  description:
    "Nitro pricing: start free on the shared cloud, pay as you go at $20/mo, run a dedicated single-tenant instance from $400, or self-host. Compare every plan.",
  keywords: [
    "ChilliCream pricing",
    "Nitro pricing",
    "GraphQL platform pricing",
    "GraphQL plans",
    "dedicated instance",
    "self-hosted",
    "schema registry pricing",
    "GraphQL observability pricing",
  ],
  openGraph: {
    title: "Nitro Pricing",
    description:
      "Start free on the shared cloud, pay as you go at $20/mo, run a dedicated single-tenant instance from $400, or self-host on your own infrastructure.",
  },
};

// Coffee-brew icon per cloud tier, lightest brew to strongest, matching the
// "Brew it your Way" selector on the landing page.
const ICONS: Partial<
  Record<TierId, ComponentType<{ readonly className?: string }>>
> = {
  free: FrenchPress,
  payg: DripBrewer,
  dedicated: PourOver,
};

const CLOUD_TIERS = TIERS.filter((tier) => tier.id !== "self");
const SELF_HOSTED = TIERS.find((tier) => tier.id === "self");

export default function PricingPage() {
  return (
    <>
      <Hero />
      <PlanGrid />
      <CompareTable />
      <Faq />
      <RegulatedBand />
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
      <h1 className="font-heading text-cc-heading sm:text-h2 mt-5 text-4xl font-semibold text-balance">
        Pricing that scales with your platform.
      </h1>
      <p className="text-cc-ink mx-auto mt-6 max-w-2xl text-base text-pretty sm:text-lg">
        Start free on the shared cloud. Pay as you go as traffic grows, run a
        dedicated single-tenant instance when you need your own region and
        isolation, or self-host on your own infrastructure.
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

// --- Plans (same "Brew it your Way" selector as the landing page) -----------

function PlanGrid() {
  return (
    <section aria-labelledby="plans-heading" className="pb-4">
      <h2 id="plans-heading" className="sr-only">
        Plans
      </h2>
      <OfferingGrid columns="md:grid-cols-3">
        {CLOUD_TIERS.map((tier) => (
          <Offering
            key={tier.id}
            Icon={ICONS[tier.id]}
            title={tier.name}
            description={tier.tagline}
            price={tier.price}
            priceNote={tier.priceNote}
            perks={tier.features}
            popular={tier.popular}
            callToAction={{ title: tier.cta, link: tier.ctaHref }}
          />
        ))}
      </OfferingGrid>
      {SELF_HOSTED && <SelfHostedStrip tier={SELF_HOSTED} />}
    </section>
  );
}

function SelfHostedStrip({ tier }: { readonly tier: Tier }) {
  return (
    <div className="border-cc-card-border bg-cc-card-bg/60 mt-6 flex flex-col gap-5 rounded-3xl border p-6 sm:flex-row sm:items-center sm:justify-between sm:p-8">
      <div>
        <h3 className="font-heading text-cc-heading text-h6 font-semibold">
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

// --- Feature comparison (V2 table) -----------------------------------------

function CompareTable() {
  return (
    <section
      aria-labelledby="compare-heading"
      className="mt-24 scroll-mt-24 sm:mt-28"
      id="compare"
    >
      <div className="text-center">
        <p className="text-cc-nav-label font-mono text-xs tracking-[0.18em] uppercase">
          Compare plans
        </p>
        <h2
          id="compare-heading"
          className="font-heading text-cc-heading text-h4 sm:text-h3 mt-3 font-semibold"
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

function CompareCell({ value }: { readonly value: Cell }) {
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

// --- Common questions (V2 FAQ, whole row clickable) ------------------------

function Faq() {
  return (
    <section
      aria-labelledby="faq-heading"
      className="mt-24 scroll-mt-24 sm:mt-28"
      id="faq"
    >
      <div className="text-center">
        <p className="text-cc-nav-label font-mono text-xs tracking-[0.18em] uppercase">
          FAQ
        </p>
        <h2
          id="faq-heading"
          className="font-heading text-cc-heading text-h4 sm:text-h3 mt-3 font-semibold"
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

function FaqItem({ faq }: { readonly faq: PricingFaq }) {
  // The padding lives on the <summary>, so the entire header row (not just the
  // text or the plus) is the click target that toggles the answer.
  return (
    <details className="group border-cc-card-border hover:border-cc-card-border-hover bg-cc-card-bg/60 rounded-2xl border transition-colors">
      <summary className="text-cc-heading font-heading flex cursor-pointer list-none items-start justify-between gap-4 p-5 text-base font-semibold">
        <span>{faq.question}</span>
        <span
          aria-hidden="true"
          className="text-cc-accent mt-1 flex-none font-mono text-sm transition-transform group-open:rotate-45"
        >
          +
        </span>
      </summary>
      <div className="text-cc-ink px-5 pb-5 text-sm leading-relaxed">
        {faq.answer}
      </div>
    </details>
  );
}

// --- Regulated & on-prem (V2 band) -----------------------------------------

function RegulatedBand() {
  return (
    <section
      aria-labelledby="regulated-heading"
      className="border-cc-card-border bg-cc-card-bg/60 mt-24 rounded-3xl border p-8 sm:mt-28 sm:p-12"
    >
      <div className="grid items-center gap-8 md:grid-cols-[1.4fr_1fr]">
        <div>
          <p className="text-cc-nav-label font-mono text-xs tracking-[0.18em] uppercase">
            Regulated &amp; on-prem
          </p>
          <h2
            id="regulated-heading"
            className="font-heading text-cc-heading text-h4 sm:text-h3 mt-3 font-semibold text-balance"
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
          {REGULATED_POINTS.map((item) => (
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

const REGULATED_POINTS: readonly string[] = [
  "Procurement, MSA, and security review",
  "BYOC or fully on-prem deployments",
  "Dedicated onboarding & runbooks",
];

// --- Closing CTA ------------------------------------------------------------

function ClosingCta() {
  return (
    <section className="mt-24 mb-10 sm:mt-28">
      <div className="border-cc-card-border bg-cc-card-bg relative overflow-hidden rounded-3xl border p-10 text-center sm:p-16">
        <div
          aria-hidden="true"
          className="pointer-events-none absolute inset-x-0 top-0 h-px"
          style={{
            background:
              "linear-gradient(90deg, transparent, #16b9e4 30%, #7c92c6 50%, #f0786a 70%, transparent)",
          }}
        />
        <div
          aria-hidden="true"
          className="pointer-events-none absolute -top-32 left-1/2 h-64 w-[40rem] max-w-full -translate-x-1/2 opacity-50 blur-3xl"
          style={{
            background:
              "radial-gradient(50% 50% at 50% 50%, rgba(94,234,212,0.12), transparent 70%)",
          }}
        />
        <h2 className="font-heading text-cc-heading text-h3 sm:text-h2 relative font-semibold text-balance">
          Start free. Scale when you do.
        </h2>
        <p className="text-cc-ink relative mx-auto mt-5 max-w-xl text-base text-pretty sm:text-lg">
          1M operations, 2 GB of ingest, schemas and environments, and the full
          Nitro control plane, free on the shared cloud. Upgrade only when you
          outgrow it.
        </p>
        <div className="relative mt-8 flex flex-wrap items-center justify-center gap-3">
          <SolidButton href="/get-started">Start for free</SolidButton>
          <OutlineButton href="/docs">Read the docs</OutlineButton>
        </div>
        <p className="text-cc-nav-label relative mt-6 font-mono text-xs">
          No credit card. Free forever on the shared cloud.
        </p>
      </div>
    </section>
  );
}

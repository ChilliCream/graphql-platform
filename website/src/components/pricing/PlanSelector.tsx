import { Offering } from "@/src/components/Offering";
import { OfferingGrid } from "@/src/components/OfferingGrid";
import type { Tier } from "@/src/components/pricing/pricingData";
import { TIERS } from "@/src/components/pricing/pricingData";
import { OutlineButton } from "@/src/design-system/Button";

const CLOUD_TIERS = TIERS.filter((tier) => tier.id !== "self");
const SELF_HOSTED = TIERS.find((tier) => tier.id === "self");

/**
 * The pricing plan selector: the three cloud tiers rendered as `Offering` cards
 * (the same component as the landing "Brew it your Way" selector), with the
 * self-hosted option as a strip below. All data comes from the shared module.
 */
export function PlanSelector() {
  return (
    <section aria-labelledby="plans-heading" className="pb-4">
      <h2 id="plans-heading" className="sr-only">
        Plans
      </h2>
      <OfferingGrid columns="md:grid-cols-3">
        {CLOUD_TIERS.map((tier) => (
          <Offering
            key={tier.id}
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

import { Offering } from "@/src/components/Offering";
import { OfferingGrid } from "@/src/components/OfferingGrid";
import { TIERS } from "@/src/components/pricing/pricingData";

/**
 * The pricing plan selector: all four tiers rendered as `Offering` cards (the
 * same component as the landing "Brew it your Way" selector). Self-Hosted is a
 * full card in the row so it reads as a peer of the cloud tiers. All data
 * comes from the shared module.
 */
export function PlanSelector() {
  return (
    <section aria-labelledby="plans-heading" className="pb-4">
      <h2 id="plans-heading" className="sr-only">
        Nitro pricing plans
      </h2>
      <OfferingGrid columns="sm:grid-cols-2 lg:grid-cols-4">
        {TIERS.map((tier) => (
          <Offering
            key={tier.id}
            title={tier.name}
            description={tier.tagline}
            price={tier.price}
            priceNote={tier.priceNote}
            perks={tier.features}
            popular={tier.popular}
            popularLabel={tier.popularLabel}
            callToAction={{ title: tier.cta, link: tier.ctaHref }}
          />
        ))}
      </OfferingGrid>
    </section>
  );
}

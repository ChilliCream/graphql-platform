import type { ComponentType } from "react";

import { Offering } from "@/src/components/Offering";
import { OfferingGrid } from "@/src/components/OfferingGrid";
import { PageSection } from "@/src/components/PageSection";
import type { TierId } from "@/src/components/pricing/pricingData";
import { TIERS } from "@/src/components/pricing/pricingData";
import { DripBrewer } from "@/src/icons/DripBrewer";
import { FrenchPress } from "@/src/icons/FrenchPress";
import { MokaPot } from "@/src/icons/MokaPot";
import { PourOver } from "@/src/icons/PourOver";

// Coffee-brew icon per tier, lightest brew to strongest.
const ICONS: Record<TierId, ComponentType<{ readonly className?: string }>> = {
  free: FrenchPress,
  payg: DripBrewer,
  dedicated: PourOver,
  self: MokaPot,
};

/**
 * Nitro pricing: all four tiers (Free, Pay as you go, Dedicated, Self-Hosted)
 * framed as coffee brews, with Dedicated highlighted as the popular pick.
 * Self-Hosted is a full card in the row so it reads as a peer of the cloud
 * tiers. All data comes from the shared pricing module.
 */
export function NitroPricing() {
  return (
    <PageSection className="py-16 sm:py-24">
      <h2 className="font-heading text-cc-heading text-h4 sm:text-h3 text-center font-semibold">
        Brew it your Way
      </h2>
      <p className="text-cc-ink mx-auto mt-5 max-w-3xl text-center text-base text-pretty sm:text-lg">
        Nitro is the Control Plane and CLI that keeps you in control, whether
        you&rsquo;re deploying a new schema, rolling out a new client, or
        gaining insights into your API environments.
      </p>

      <OfferingGrid columns="mt-14 sm:grid-cols-2 lg:grid-cols-4">
        {TIERS.map((tier) => (
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
    </PageSection>
  );
}

import type { ComponentType } from "react";

import { Offering } from "@/src/components/Offering";
import { OfferingGrid } from "@/src/components/OfferingGrid";
import { PageSection } from "@/src/components/PageSection";
import type { TierId } from "@/src/components/pricing/pricingData";
import { TIERS } from "@/src/components/pricing/pricingData";
import { OutlineButton } from "@/src/design-system/Button";
import { Card } from "@/src/design-system/Card";
import { DripBrewer } from "@/src/illustrations/DripBrewer";
import { FrenchPress } from "@/src/illustrations/FrenchPress";
import { PourOver } from "@/src/illustrations/PourOver";

// Coffee-brew icon per cloud tier, lightest brew to strongest.
const ICONS: Partial<
  Record<TierId, ComponentType<{ readonly className?: string }>>
> = {
  free: FrenchPress,
  payg: DripBrewer,
  dedicated: PourOver,
};

const CLOUD_TIERS = TIERS.filter((tier) => tier.id !== "self");
const SELF_HOSTED = TIERS.find((tier) => tier.id === "self");

/**
 * Nitro pricing: the three cloud tiers (Free, Pay as you go, Dedicated) framed
 * as coffee brews, with Dedicated highlighted as the popular pick, and a
 * self-hosted option below. All data comes from the shared pricing module.
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

      <OfferingGrid columns="mt-14 md:grid-cols-3">
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

      {SELF_HOSTED && (
        <Card
          variant="panel"
          className="mt-6 flex flex-col gap-5 sm:flex-row sm:items-center sm:justify-between"
        >
          <div>
            <h3 className="font-heading text-cc-heading text-h6 font-semibold">
              {SELF_HOSTED.name}
            </h3>
            <p className="text-cc-ink mt-2 text-sm text-pretty">
              {SELF_HOSTED.tagline} Run on your own infrastructure, air-gapped
              or on-prem, with configurable retention and priority support.
            </p>
          </div>
          <OutlineButton
            href={SELF_HOSTED.ctaHref}
            className="shrink-0 sm:w-auto"
          >
            {SELF_HOSTED.cta}
          </OutlineButton>
        </Card>
      )}
    </PageSection>
  );
}

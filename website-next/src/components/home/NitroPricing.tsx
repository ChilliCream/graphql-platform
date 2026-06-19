import type { ComponentType } from "react";

import { Offering } from "@/src/components/Offering";
import { OfferingGrid } from "@/src/components/OfferingGrid";
import { DripBrewer } from "@/src/icons/DripBrewer";
import { FrenchPress } from "@/src/icons/FrenchPress";
import { PourOver } from "@/src/icons/PourOver";

interface Plan {
  readonly Icon: ComponentType<{ readonly className?: string }>;
  readonly name: string;
  readonly description: string;
  readonly price: string;
  readonly priceNote: string;
  readonly features: readonly string[];
  readonly cta: string;
  readonly ctaHref: string;
  readonly popular?: boolean;
}

const PLANS: readonly Plan[] = [
  {
    Icon: FrenchPress,
    name: "Shared Instance",
    description: "Shared resources, fully managed",
    price: "Free",
    priceNote: "pay-as-you-go",
    features: [
      "Multi-tenant cloud region",
      "1 Schema · 3 Environments",
      "Up to 5M ops / month included",
      "Community Slack support",
      "Pay only for what you use after",
    ],
    cta: "Start for Free",
    ctaHref: "/get-started",
  },
  {
    Icon: DripBrewer,
    name: "Dedicated Instance",
    description: "Dedicated resources, fully managed",
    price: "$400",
    priceNote: "per month",
    features: [
      "Single-tenant cloud region",
      "Unlimited schemas",
      "BYOC region · private networking",
      "99.95% SLA · email + private chat",
      "SSO, audit log, role-based access",
    ],
    cta: "Start for Free",
    ctaHref: "/get-started",
    popular: true,
  },
  {
    Icon: PourOver,
    name: "Self-Hosted",
    description: "Self managed",
    price: "Custom",
    priceNote: "talk to us",
    features: [
      "Run on your own infrastructure",
      "Air-gapped & on-prem supported",
      "Priority engineering support",
      "Long-term release channel",
      "Custom training & onboarding",
    ],
    cta: "Talk to Us",
    ctaHref: "/services/support/contact",
  },
];

/**
 * Nitro pricing: three plans framed as coffee brews (French Press, Drip
 * Brewer, Pour-Over). The middle plan is highlighted as the popular pick. Cards
 * stack on small screens and sit side by side from large up.
 */
export function NitroPricing() {
  return (
    <section className="mx-auto max-w-6xl px-5 py-16 sm:px-12 sm:py-24">
      <h2 className="font-heading text-cc-heading text-h4 sm:text-h3 text-center font-semibold">
        Brew it your Way
      </h2>
      <p className="text-cc-ink mx-auto mt-5 max-w-3xl text-center text-base text-pretty sm:text-lg">
        Nitro is the Control Plane and CLI that keeps you in control, whether
        you&rsquo;re deploying a new schema, rolling out a new client, or
        gaining insights into your API environments.
      </p>

      <OfferingGrid columns="mt-14 md:grid-cols-3">
        {PLANS.map((plan) => (
          <Offering
            key={plan.name}
            Icon={plan.Icon}
            title={plan.name}
            description={plan.description}
            price={plan.price}
            priceNote={plan.priceNote}
            perks={plan.features}
            popular={plan.popular}
            callToAction={{ title: plan.cta, link: plan.ctaHref }}
          />
        ))}
      </OfferingGrid>
    </section>
  );
}

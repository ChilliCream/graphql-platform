import { Offering } from "@/src/components/Offering";
import { OfferingGrid } from "@/src/components/OfferingGrid";
import { SectionHeading } from "@/src/components/SectionHeading";

type PlanName = "Community" | "Startup" | "Business" | "Enterprise";

interface Plan {
  readonly name: PlanName;
  readonly price: string;
  readonly priceNote?: string;
  readonly monthlyPrice?: number;
  readonly tagline: string;
  readonly perks: readonly string[];
  readonly cta: { readonly label: string; readonly href: string };
  readonly highlight?: boolean;
  readonly highlightLabel?: string;
}

export const SUPPORT_PLANS: readonly Plan[] = [
  {
    name: "Community",
    price: "Free",
    monthlyPrice: 0,
    tagline: "For hackers and side projects",
    perks: ["Public Slack channel"],
    cta: {
      label: "Join community Slack",
      href: "https://slack.chillicream.com/",
    },
  },
  {
    name: "Startup",
    price: "$450",
    priceNote: "per month",
    monthlyPrice: 450,
    tagline: "Small teams, steady cadence",
    perks: ["Private Slack channel", "2 critical incidents"],
    cta: {
      label: "Discuss Startup",
      href: "/services/support/contact?subject=Pricing%20%26%20Plans&context=Startup%20Support",
    },
  },
  {
    name: "Business",
    price: "$1,300",
    priceNote: "per month",
    monthlyPrice: 1300,
    tagline: "Larger teams, critical work",
    perks: [
      "Private Slack channel",
      "5 critical incidents",
      "Non-critical incident coverage",
      "Email support",
    ],
    cta: {
      label: "Discuss Business",
      href: "/services/support/contact?subject=Pricing%20%26%20Plans&context=Business%20Support",
    },
    highlight: true,
    highlightLabel: "Business Coverage",
  },
  {
    name: "Enterprise",
    price: "Custom",
    tagline: "Whole-org coverage, tailored terms",
    perks: [
      "Private Slack channel",
      "Unlimited critical incidents",
      "10 non-critical incidents",
      "Phone support",
      "Dedicated account manager",
      "Status reviews",
    ],
    cta: {
      label: "Discuss Enterprise",
      href: "/services/support/contact?subject=Pricing%20%26%20Plans&context=Enterprise%20Support",
    },
  },
];

/**
 * The four support plans as a card grid, rendered with the shared `Offering`
 * card (the same component as the pricing tiers), with the Business plan
 * visually highlighted.
 */
export function PlanGrid() {
  return (
    <section id="plans" className="py-16">
      <div className="mb-10">
        <SectionHeading
          align="center"
          eyebrow="Plans"
          title="Four plans. Pick the one that fits."
        />
      </div>
      <OfferingGrid columns="sm:grid-cols-2 lg:grid-cols-4">
        {SUPPORT_PLANS.map((plan) => (
          <Offering
            key={plan.name}
            title={plan.name}
            description={plan.tagline}
            price={plan.price}
            priceNote={plan.priceNote}
            perks={plan.perks}
            popular={plan.highlight}
            popularLabel={plan.highlightLabel}
            callToAction={{ title: plan.cta.label, link: plan.cta.href }}
          />
        ))}
      </OfferingGrid>
    </section>
  );
}

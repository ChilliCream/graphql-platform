import { Offering } from "@/src/components/Offering";
import { OfferingGrid } from "@/src/components/OfferingGrid";
import { SectionHeading } from "@/src/components/SectionHeading";

type PlanName = "Community" | "Startup" | "Business" | "Enterprise";

interface Plan {
  readonly name: PlanName;
  readonly price: string;
  readonly priceNote?: string;
  readonly tagline: string;
  readonly perks: readonly string[];
  readonly cta: { readonly label: string; readonly href: string };
  readonly highlight?: boolean;
}

const PLANS: readonly Plan[] = [
  {
    name: "Community",
    price: "Free",
    tagline: "For hackers and side projects",
    perks: ["Public Slack Channel"],
    cta: { label: "Join Slack", href: "https://slack.chillicream.com/" },
  },
  {
    name: "Startup",
    price: "$450",
    priceNote: "per month",
    tagline: "Small teams, steady cadence",
    perks: ["Private Slack Channel", "2 critical incidents"],
    cta: {
      label: "Contact Us",
      href: "/services/support/contact?plan=Startup",
    },
  },
  {
    name: "Business",
    price: "$1,300",
    priceNote: "per month",
    tagline: "Larger teams, critical work",
    perks: [
      "Private Slack Channel",
      "5 critical incidents",
      "2 non-critical incidents",
      "Email support",
    ],
    cta: {
      label: "Contact Us",
      href: "/services/support/contact?plan=Business",
    },
    highlight: true,
  },
  {
    name: "Enterprise",
    price: "Custom",
    tagline: "Whole-org coverage, tailored terms",
    perks: [
      "Private Slack Channel",
      "Unlimited critical incidents",
      "10 non-critical incidents",
      "Phone support",
      "Dedicated account manager",
      "Status reviews",
    ],
    cta: {
      label: "Contact Us",
      href: "/services/support/contact?plan=Enterprise",
    },
  },
];

/**
 * The four support plans as a card grid, rendered with the shared `Offering`
 * card (the same component as the pricing tiers), with the Business plan
 * highlighted as the recommended pick.
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
        {PLANS.map((plan) => (
          <Offering
            key={plan.name}
            title={plan.name}
            description={plan.tagline}
            price={plan.price}
            priceNote={plan.priceNote}
            perks={plan.perks}
            popular={plan.highlight}
            callToAction={{ title: plan.cta.label, link: plan.cta.href }}
          />
        ))}
      </OfferingGrid>
    </section>
  );
}

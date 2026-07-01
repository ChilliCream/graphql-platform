import { SLACK } from "@/src/components/help/helpLinks";
import { Offering } from "@/src/components/Offering";
import { OfferingGrid } from "@/src/components/OfferingGrid";
import { SectionHeading } from "@/src/components/SectionHeading";

interface Tier {
  readonly title: string;
  readonly description: string;
  readonly price: string;
  readonly priceNote?: string;
  readonly perks: readonly string[];
  readonly callToAction: { readonly title: string; readonly link: string };
  readonly popular?: boolean;
  readonly popularLabel?: string;
}

const TIERS: readonly Tier[] = [
  {
    title: "Community",
    description: "Learn in the open with thousands of practitioners.",
    price: "Free",
    perks: [
      "Public Slack channel",
      "7000+ developers",
      "Open GitHub discussions",
      "Searchable history",
    ],
    callToAction: { title: "Join Slack", link: SLACK },
  },
  {
    title: "Consultancy",
    description: "Bring a problem to an expert and get a clear direction.",
    price: "20h",
    priceNote: "increments",
    perks: [
      "Dedicated expert session",
      "One on one with an engineer",
      "Architecture and review",
      "No long term contract",
    ],
    callToAction: { title: "Explore advisory", link: "/services/advisory" },
  },
  {
    title: "Support",
    description: "Dedicated support for teams running GraphQL in production.",
    price: "Custom",
    perks: [
      "Dedicated account manager",
      "Private Slack channel",
      "Email support",
      "Plan tailored to your team",
    ],
    callToAction: { title: "Check out plans", link: "/services/support" },
    popular: true,
    popularLabel: "Best Value",
  },
];

/**
 * The three help paths as a card grid, rendered with the shared `Offering` card
 * (the same component as the pricing and support tiers), with Support
 * highlighted as the "Best Value" pick.
 */
export function HelpTiers() {
  return (
    <section aria-labelledby="help-tiers-heading" className="py-16">
      <div className="mb-12">
        <SectionHeading
          align="center"
          eyebrow="Three paths"
          title="Choose the help that matches your situation."
          titleId="help-tiers-heading"
          description="Community for open questions, Consultancy for getting unstuck this week, Support for teams that depend on GraphQL in production."
        />
      </div>
      <OfferingGrid columns="md:grid-cols-3">
        {TIERS.map((tier) => (
          <Offering key={tier.title} {...tier} />
        ))}
      </OfferingGrid>
    </section>
  );
}

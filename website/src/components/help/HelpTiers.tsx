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
}

export const HELP_TIERS: readonly Tier[] = [
  {
    title: "Community",
    description: "Ask public questions and learn from other ChilliCream users.",
    price: "Free",
    perks: [
      "Public Slack channel",
      "Open GitHub discussions",
      "Searchable history",
      "Best-effort responses",
    ],
    callToAction: { title: "Join community Slack", link: SLACK },
  },
  {
    title: "Advisory",
    description:
      "Bring a GraphQL problem to an expert and get clear direction.",
    price: "20h",
    priceNote: "increments",
    perks: [
      "Architecture and schema design",
      "Troubleshooting and review",
      "Hot Chocolate and Fusion expertise",
      "Agreed package of hours",
    ],
    callToAction: { title: "Explore advisory", link: "/services/advisory" },
  },
  {
    title: "Support",
    description: "Ongoing coverage for teams running GraphQL in production.",
    price: "From $450",
    priceNote: "per month",
    perks: [
      "Private channel on paid plans",
      "Defined incident allowances",
      "Published response times",
      "Coverage options by plan",
    ],
    callToAction: { title: "Compare support plans", link: "/services/support" },
  },
];

/**
 * The three help paths as a card grid, rendered with the shared `Offering`
 * card used by the pricing and support tiers.
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
          description="Community for open questions, advisory for getting unstuck on a defined problem, support for teams that depend on GraphQL in production."
        />
      </div>
      <OfferingGrid columns="md:grid-cols-3">
        {HELP_TIERS.map((tier) => (
          <Offering key={tier.title} {...tier} />
        ))}
      </OfferingGrid>
    </section>
  );
}

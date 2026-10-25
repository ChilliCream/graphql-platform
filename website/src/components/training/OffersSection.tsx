import { CardGrid } from "@/src/components/CardGrid";
import { PerkCard } from "@/src/components/PerkCard";
import { SectionHeading } from "@/src/components/SectionHeading";
import { TeamIcon } from "@/src/icons/TeamIcon";
import { WorkshopIcon } from "@/src/icons/WorkshopIcon";

export const TRAINING_OFFERS = [
  {
    kind: "Team training",
    tagline: "Flexible curriculum, shaped to your team",
    description:
      "Combine instruction, examples, and exercises across GraphQL, Hot Chocolate, Fusion, Nitro, and client development. We adjust the depth to the team in the room.",
    perks: [
      "Shared GraphQL vocabulary",
      "Topics matched to current skill levels",
      "Examples grounded in .NET",
      "Time for team questions",
    ],
    ctaLabel: "Plan team training",
    ctaHref: "mailto:contact@chillicream.com?subject=Corporate%20Training",
    Icon: TeamIcon,
    highlight: false,
  },
  {
    kind: "Hands-on workshop",
    tagline: "Hands on, with a real project at the end",
    description:
      "Work through an agreed project using ASP.NET Core and Hot Chocolate, with optional Fusion or client topics. The emphasis is on applying the patterns, not copying a finished sample.",
    perks: [
      "Defined workshop problem",
      "Hands-on schema and resolver work",
      "Review and discussion as a group",
      "Production design considerations",
      "Optional work in your codebase",
    ],
    ctaLabel: "Plan a workshop",
    ctaHref: "mailto:contact@chillicream.com?subject=Corporate%20Workshop",
    Icon: WorkshopIcon,
    highlight: true,
  },
] as const;

/**
 * The two real corporate engagements as delivery options: training to align a
 * team, or a workshop to ship a project, with the workshop highlighted.
 */
export function OffersSection() {
  return (
    <section id="offers" className="py-16 sm:py-20">
      <div className="mb-10">
        <SectionHeading
          align="center"
          eyebrow="Two ways to run it"
          title="Training to align, or a workshop to ship."
          description="Both engagements use the same curriculum and trainers. They differ in how much hands-on project work sits at the center of the engagement."
        />
      </div>
      <CardGrid cols={2} breakpoint="md" gap={4}>
        {TRAINING_OFFERS.map((offer) => (
          <PerkCard
            key={offer.kind}
            title={offer.kind}
            subtitle={offer.tagline}
            intro={offer.description}
            listLabel="What is in the box"
            items={offer.perks}
            Icon={offer.Icon}
            cta={{
              label: offer.ctaLabel,
              href: offer.ctaHref,
              solid: offer.highlight,
            }}
            highlight={offer.highlight}
          />
        ))}
      </CardGrid>
    </section>
  );
}

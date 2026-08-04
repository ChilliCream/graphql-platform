import { CardGrid } from "@/src/components/CardGrid";
import { PerkCard } from "@/src/components/PerkCard";
import { SectionHeading } from "@/src/components/SectionHeading";
import { TeamIcon } from "@/src/icons/TeamIcon";
import { WorkshopIcon } from "@/src/icons/WorkshopIcon";

const OFFERS = [
  {
    kind: "Corporate Training",
    tagline: "Flexible curriculum, shaped to your team",
    description:
      "Get your team trained in GraphQL, any of our products, and even React/Relay. Beginner Team? Advanced Team? Or Mixed? Don't panic! Our curriculum is designed to teach in-depth and works really well, but isn't set in stone.",
    perks: [
      "Level up their proficiency",
      "Catered to different skills",
      "Overcome challenges they have been wrestling with",
      "Get everybody on the same technical page",
    ],
    ctaLabel: "Book Corporate Training",
    ctaHref: "mailto:contact@chillicream.com?subject=Corporate%20Training",
    Icon: TeamIcon,
    highlight: false,
  },
  {
    kind: "Corporate Workshop",
    tagline: "Hands on, with a real project at the end",
    description:
      "We will look at how to build a GraphQL server with ASP.NET Core 7 and Hot Chocolate. You will learn how to explore and manage large schemas. Further, we will dive into React and explore how to efficiently build fast and fluent web interfaces using Relay.",
    perks: [
      "Core concepts and advanced",
      "Deepen knowledge of GraphQL API",
      "Work on a real project",
      "Scale and production quirks",
      "Level up your entire team at once",
      "Have Lots of Fun!",
    ],
    ctaLabel: "Book Corporate Workshop",
    ctaHref: "mailto:contact@chillicream.com?subject=Corporate%20Workshop",
    Icon: WorkshopIcon,
    highlight: true,
  },
];

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
          description="Both engagements use the same curriculum and the same trainers. They differ in how much hands-on project work sits at the end of the week."
        />
      </div>
      <CardGrid cols={2} breakpoint="md" gap={4}>
        {OFFERS.map((offer) => (
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

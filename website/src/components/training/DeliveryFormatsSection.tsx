import { CardGrid } from "@/src/components/CardGrid";
import { IconFeatureCard } from "@/src/components/IconFeatureCard";
import { SectionHeading } from "@/src/components/SectionHeading";
import { Icon, IconName } from "@/src/icons/Icon";

const FORMATS: {
  name: string;
  subtitle: string;
  description: string;
  bestFor: string;
  icon: IconName;
}[] = [
  {
    name: "On site",
    subtitle: "We come to you",
    description:
      "A trainer joins your team in a room with a whiteboard and proper coffee. Best when you want the focused energy of being out of inboxes for a week.",
    bestFor: "Best for a single co-located team that can clear the calendar.",
    icon: "house",
  },
  {
    name: "Remote",
    subtitle: "Live, distributed",
    description:
      "Live sessions over your call tool of choice, with shared notebooks, breakout rooms, and homework between days so timezones do not become a wall.",
    bestFor:
      "Best for distributed teams or when travel does not make business sense.",
    icon: "laptop",
  },
  {
    name: "Hybrid",
    subtitle: "Some in the room, some on the call",
    description:
      "Deliberate breakout design and exercises that work for the people in the room and the people on the call, with a good A/V setup so nobody is half-present.",
    bestFor: "Best when part of the team can fly in and part cannot.",
    icon: "house-laptop",
  },
];

/**
 * The delivery formats: on site, remote, or a deliberate hybrid, each as an
 * inline icon-feature card with a "best for" footnote.
 */
export function DeliveryFormatsSection() {
  return (
    <section className="py-16 sm:py-20">
      <div className="mb-10">
        <SectionHeading
          align="center"
          eyebrow="Delivery format"
          title="On site, remote, or a sensible hybrid."
          description="We have run training in all three formats. Pick the one that fits your calendar and your office, not the other way around."
        />
      </div>
      <CardGrid cols={3} gap={4}>
        {FORMATS.map((format) => (
          <IconFeatureCard
            key={format.name}
            layout="inline"
            icon={<Icon icon={format.icon} size="lg" />}
            title={format.name}
            subtitle={format.subtitle}
            copy={format.description}
            footnote={format.bestFor}
          />
        ))}
      </CardGrid>
    </section>
  );
}

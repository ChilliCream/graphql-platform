import { CardGrid } from "@/src/components/CardGrid";
import { LinkCard } from "@/src/components/LinkCard";
import { PageHero } from "@/src/components/PageHero";
import { Section } from "@/src/components/Section";
import { pageMetadata } from "@/src/helpers/pageMetadata";

export const metadata = pageMetadata({
  title: "Services",
  description:
    "Work with ChilliCream's GraphQL experts: advisory and consulting, support plans with SLAs you can rely on, and focused training for your team.",
  path: "/services",
});

const SERVICE_SECTIONS = [
  {
    href: "/services/advisory",
    title: "Advisory",
    description: "Consulting & contracting from GraphQL experts.",
  },
  {
    href: "/services/support",
    title: "Support",
    description: "Get help from experts with SLAs you can rely on.",
  },
  {
    href: "/services/training",
    title: "Training",
    description: "Increase your team's productivity with focused training.",
  },
];

export default function ServicesPage() {
  return (
    <>
      <PageHero
        title="Services"
        teaser="From a one-off conversation to embedded experts: choose the right level of help for your project."
      />
      <Section title="How We Can Help">
        <CardGrid cols={3}>
          {SERVICE_SECTIONS.map((section) => (
            <LinkCard
              key={section.href}
              variant="trailing"
              href={section.href}
              title={section.title}
              description={section.description}
            />
          ))}
        </CardGrid>
      </Section>
    </>
  );
}

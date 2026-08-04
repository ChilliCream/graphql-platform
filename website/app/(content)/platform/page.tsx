import { CardGrid } from "@/src/components/CardGrid";
import { LinkCard } from "@/src/components/LinkCard";
import { PageHero } from "@/src/components/PageHero";
import { Section } from "@/src/components/Section";
import { pageMetadata } from "@/src/helpers/pageMetadata";

export const metadata = pageMetadata({
  title: "Platform",
  description:
    "Discover the ChilliCream GraphQL Platform: one place for analytics, continuous integration, and a trusted ecosystem for every API in your organization.",
  path: "/platform",
});

const PLATFORM_SECTIONS = [
  {
    href: "/platform/analytics",
    title: "Analytics",
    description: "Instant Insights. Enhanced Performance.",
  },
  {
    href: "/platform/continuous-integration",
    title: "Continuous Integration",
    description: "Innovate with Confidence. Deliver with Quality.",
  },
  {
    href: "/platform/ecosystem",
    title: "Ecosystem",
    description: "An Ecosystem You Trust and Love.",
  },
  {
    href: "/platform/agentic-coding",
    title: "Agentic Coding",
    description: "Consistently Good Code, from Any Agent.",
  },
];

export default function PlatformPage() {
  return (
    <>
      <PageHero
        title="The Platform"
        teaser="One platform for every API across your organization — from authoring and composition to operations and telemetry."
      />
      <Section title="Explore the Platform">
        <CardGrid cols={3}>
          {PLATFORM_SECTIONS.map((section) => (
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

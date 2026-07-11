import Link from "next/link";

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
        <div className="grid gap-6 md:grid-cols-3">
          {PLATFORM_SECTIONS.map((section) => (
            <Link
              key={section.href}
              href={section.href}
              className="group border-cc-card-border bg-cc-card-bg hover:border-cc-accent flex flex-col rounded-xl border p-8 no-underline backdrop-blur-sm transition-colors"
            >
              <h2 className="text-cc-heading text-xl font-semibold">
                {section.title}
              </h2>
              <p className="text-cc-ink-dim mt-3 text-sm">
                {section.description}
              </p>
              <span className="text-cc-accent mt-6 text-sm font-medium">
                Learn more →
              </span>
            </Link>
          ))}
        </div>
      </Section>
    </>
  );
}

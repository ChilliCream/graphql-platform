import type { Metadata } from "next";
import Link from "next/link";

import { PageHero } from "@/src/components/PageHero";
import { Section } from "@/src/components/Section";

export const metadata: Metadata = {
  title: "Platform",
  description:
    "Discover the ChilliCream GraphQL Platform: one place for analytics, continuous integration, and a trusted ecosystem for every API in your organization.",
};

const PLATFORM_SECTIONS = [
  {
    href: "/platform/analytics",
    title: "Analytics",
    description: "See what the API is doing.",
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
              <h2 className="text-cc-ink group-hover:text-cc-accent text-xl font-semibold">
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

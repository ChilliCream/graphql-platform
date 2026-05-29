import Link from "next/link";

import { PageHero, Section } from "@/src/components/SectionTitle";

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
              className="group flex flex-col rounded-xl border border-[var(--cc-card-border)] bg-[var(--cc-card-bg)] backdrop-blur-sm p-8 no-underline  transition-colors hover:border-fuchsia-400"
            >
              <h2 className="text-xl font-semibold text-[var(--cc-ink)] group-hover:text-fuchsia-400">
                {section.title}
              </h2>
              <p className="mt-3 text-sm text-[var(--cc-ink-dim)]">
                {section.description}
              </p>
              <span className="mt-6 text-sm font-medium text-fuchsia-400">
                Learn more →
              </span>
            </Link>
          ))}
        </div>
      </Section>
    </>
  );
}

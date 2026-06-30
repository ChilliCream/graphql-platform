import type { Metadata } from "next";
import Link from "next/link";

import { PageHero } from "@/src/components/PageHero";
import { Section } from "@/src/components/Section";

export const metadata: Metadata = {
  title: "Services",
  description:
    "Work with ChilliCream's GraphQL experts: advisory and consulting, support plans with SLAs you can rely on, and focused training for your team.",
};

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
        <div className="grid gap-6 md:grid-cols-3">
          {SERVICE_SECTIONS.map((section) => (
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

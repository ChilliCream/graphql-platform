import Link from "next/link";

import { PageHero } from "@/src/components/PageHero";
import { Section } from "@/src/components/Section";

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
              className="group flex flex-col rounded-xl border border-cc-card-border bg-cc-card-bg backdrop-blur-sm p-8 no-underline  transition-colors hover:border-fuchsia-400"
            >
              <h2 className="text-xl font-semibold text-cc-ink group-hover:text-fuchsia-400">
                {section.title}
              </h2>
              <p className="mt-3 text-sm text-cc-ink-dim">
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

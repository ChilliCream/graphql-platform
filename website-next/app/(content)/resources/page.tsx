import Link from "next/link";

import { PageHero } from "@/src/components/PageHero";
import { Section } from "@/src/components/Section";

export const metadata = {
  title: "Resources",
  description: "ChilliCream brand resources and downloads.",
};

const COMPANY_LINKS = [
  {
    href: "mailto:contact@chillicream.com",
    title: "Contact",
    description: "Get in touch with the team.",
  },
  {
    href: "https://store.chillicream.com",
    title: "Shop",
    description: "ChilliCream merch and goodies.",
    external: true,
  },
  {
    href: "/legal/acceptable-use-policy",
    title: "Acceptable Use Policy",
    description: "Rules for using ChilliCream services.",
  },
  {
    href: "/legal/cookie-policy",
    title: "Cookie Policy",
    description: "How we use cookies.",
  },
  {
    href: "/legal/privacy-policy",
    title: "Privacy Policy",
    description: "How we handle your data.",
  },
  {
    href: "/legal/terms-of-service",
    title: "Terms of Service",
    description: "The agreement between you and us.",
  },
  {
    href: "/licensing/chillicream-license",
    title: "ChilliCream License",
    description: "Commercial license terms.",
  },
];

export default function ResourcesPage() {
  return (
    <>
      <PageHero
        title="Company"
        teaser="Everything you need to know about ChilliCream — contact us, legal terms, and more."
      />
      <Section title="Resources">
        <div className="grid gap-6 sm:grid-cols-2 lg:grid-cols-3">
          {COMPANY_LINKS.map((link) => (
            <Link
              key={link.href}
              href={link.href}
              {...(link.external
                ? { target: "_blank", rel: "noopener noreferrer" }
                : {})}
              className="group border-cc-card-border bg-cc-card-bg hover:border-cc-accent flex flex-col rounded-xl border p-6 no-underline backdrop-blur-sm transition-colors"
            >
              <h2 className="text-cc-heading text-lg font-semibold">
                {link.title}
              </h2>
              <p className="text-cc-ink-dim mt-2 text-sm">{link.description}</p>
            </Link>
          ))}
        </div>
      </Section>
    </>
  );
}

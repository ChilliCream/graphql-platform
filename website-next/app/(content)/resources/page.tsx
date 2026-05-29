import Link from "next/link";

import { PageHero, Section } from "@/src/components/SectionTitle";

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
              className="group flex flex-col rounded-xl border border-[var(--cc-card-border)] bg-[var(--cc-card-bg)] backdrop-blur-sm p-6 no-underline  transition-colors hover:border-fuchsia-400"
            >
              <h2 className="text-lg font-semibold text-[var(--cc-ink)] group-hover:text-fuchsia-400">
                {link.title}
              </h2>
              <p className="mt-2 text-sm text-[var(--cc-ink-dim)]">{link.description}</p>
            </Link>
          ))}
        </div>
      </Section>
    </>
  );
}

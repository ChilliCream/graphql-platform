import { CardGrid } from "@/src/components/CardGrid";
import { LinkCard } from "@/src/components/LinkCard";
import { PageHero } from "@/src/components/PageHero";
import { Section } from "@/src/components/Section";
import { pageMetadata } from "@/src/helpers/pageMetadata";

export const metadata = pageMetadata({
  title: "Resources",
  description: "ChilliCream brand resources and downloads.",
  path: "/resources",
});

const COMPANY_LINKS = [
  {
    href: "/services/support/contact",
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
        <CardGrid cols={3} step="progressive">
          {COMPANY_LINKS.map((link) => (
            <LinkCard
              key={link.href}
              variant="plain"
              href={link.href}
              title={link.title}
              description={link.description}
              external={link.external}
            />
          ))}
        </CardGrid>
      </Section>
    </>
  );
}

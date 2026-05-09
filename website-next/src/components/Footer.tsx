import Link from "next/link";
import type { ReactNode } from "react";
import { BlogIcon } from "@/src/icons/Blog";
import { ChilliCreamText } from "@/src/icons/ChilliCreamText";
import { GitHubIcon } from "@/src/icons/GitHub";
import { LinkedInIcon } from "@/src/icons/LinkedIn";
import { SlackIcon } from "@/src/icons/Slack";
import { XIcon } from "@/src/icons/X";
import { YouTubeIcon } from "@/src/icons/YouTube";

const tools = {
  blog: "/blog",
  github: "https://github.com/ChilliCream/graphql-platform",
  linkedIn: "https://www.linkedin.com/company/chillicream",
  shop: "https://store.chillicream.com",
  slack: "https://slack.chillicream.com/",
  youtube: "https://www.youtube.com/c/ChilliCream",
  x: "https://x.com/Chilli_Cream",
};

const products: { path: string; title: string }[] = [
  { path: "nitro", title: "Nitro" },
  { path: "fusion", title: "Fusion" },
  { path: "hotchocolate", title: "Hot Chocolate" },
  { path: "strawberryshake", title: "Strawberry Shake" },
  { path: "mocha", title: "Mocha" },
];

const basicPages: { path: string; title: string }[] = [
  { path: "legal/acceptable-use-policy", title: "Acceptable Use Policy" },
  { path: "legal/cookie-policy", title: "Cookie Policy" },
  { path: "legal/privacy-policy", title: "Privacy Policy" },
  { path: "legal/terms-of-service", title: "Terms of Service" },
  { path: "licensing/chillicream-license", title: "ChilliCream License" },
];

export default function Footer() {
  return (
    <footer className="mt-10 w-full border-t border-stone-200 bg-white/80 px-4 pt-14 pb-14 text-sm text-stone-600 backdrop-blur-sm lg:pt-36">
      <div className="mx-auto flex max-w-7xl flex-col gap-12 lg:gap-8">
        <Section>
          <div className="flex flex-1 flex-col gap-6">
            <Link
              href="/"
              aria-label="ChilliCream Home"
              className="inline-flex leading-none text-stone-900 transition-colors hover:text-fuchsia-700"
            >
              <ChilliCreamText className="h-[30px] w-auto fill-current" />
            </Link>
            <address className="not-italic">
              1207 Delaware Ave #3567
              <br />
              Wilmington, DE 19806
              <br />
              United States
            </address>
          </div>
          <div className="grid flex-[4] grid-cols-2 gap-8 md:grid-cols-4">
            <LinkColumn title="Platform">
              <NavLink href="/platform/analytics">Analytics</NavLink>
              <NavLink href="/platform/continuous-integration">
                Continuous Integration
              </NavLink>
              <NavLink href="/platform/ecosystem">Ecosystem</NavLink>
              <NavLink href="/products/nitro">Nitro</NavLink>
            </LinkColumn>
            <LinkColumn title="Services">
              <NavLink href="/services/advisory">Advisory</NavLink>
              <NavLink href="/services/support">Support</NavLink>
              <NavLink href="/services/training">Training</NavLink>
            </LinkColumn>
            <LinkColumn title="Documentation">
              {products.map((product) => (
                <NavLink key={product.path} href={`/docs/${product.path}`}>
                  {product.title}
                </NavLink>
              ))}
            </LinkColumn>
            <LinkColumn title="Company">
              <NavLink href="mailto:contact@chillicream.com">Contact</NavLink>
              <NavLink href={tools.shop}>Shop</NavLink>
              {basicPages.map((page) => (
                <NavLink key={page.path} href={`/${page.path}`}>
                  {page.title}
                </NavLink>
              ))}
            </LinkColumn>
          </div>
        </Section>
        <Section>
          <nav className="flex flex-row gap-4 text-stone-500">
            <SocialLink href={tools.blog} label="ChilliCream Blog">
              <BlogIcon className="h-[22px] w-auto fill-current" />
            </SocialLink>
            <SocialLink href={tools.github} label="ChilliCream on GitHub">
              <GitHubIcon className="h-[26px] w-auto fill-current" />
            </SocialLink>
            <SocialLink href={tools.slack} label="ChilliCream Slack Community">
              <SlackIcon className="h-[22px] w-auto fill-current" />
            </SocialLink>
            <SocialLink
              href={tools.youtube}
              label="ChilliCream YouTube Channel"
            >
              <YouTubeIcon className="h-[22px] w-auto fill-current" />
            </SocialLink>
            <SocialLink href={tools.x} label="ChilliCream on X">
              <XIcon className="h-[22px] w-auto fill-current" />
            </SocialLink>
            <SocialLink href={tools.linkedIn} label="ChilliCream on LinkedIn">
              <LinkedInIcon className="h-[22px] w-auto fill-current" />
            </SocialLink>
          </nav>
        </Section>
        <Section>
          <p className="text-stone-500">
            © {new Date().getFullYear()} ChilliCream, Inc. ・ All Rights
            Reserved
          </p>
        </Section>
      </div>
    </footer>
  );
}

function Section({ children }: { children: ReactNode }) {
  return <div className="flex flex-col gap-8 lg:flex-row">{children}</div>;
}

function LinkColumn({
  title,
  children,
}: {
  title: string;
  children: ReactNode;
}) {
  return (
    <div className="flex min-w-[150px] flex-col gap-6">
      <h3 className="flex h-[30px] items-end text-base font-semibold text-stone-900">
        {title}
      </h3>
      <nav className="flex flex-col gap-2.5">{children}</nav>
    </div>
  );
}

function NavLink({ href, children }: { href: string; children: ReactNode }) {
  const className =
    "text-stone-600 no-underline transition-colors hover:text-fuchsia-700";

  if (href.startsWith("/")) {
    return (
      <Link href={href} className={className}>
        {children}
      </Link>
    );
  }

  if (href.startsWith("mailto:") || href.startsWith("#")) {
    return (
      <a href={href} className={className}>
        {children}
      </a>
    );
  }

  return (
    <a
      href={href}
      target="_blank"
      rel="noopener noreferrer"
      className={className}
    >
      {children}
    </a>
  );
}

function SocialLink({
  href,
  label,
  children,
}: {
  href: string;
  label: string;
  children: ReactNode;
}) {
  const className =
    "inline-flex items-center justify-center transition-colors hover:text-fuchsia-700";

  if (href.startsWith("/")) {
    return (
      <Link href={href} aria-label={label} className={className}>
        {children}
        <span className="sr-only">{label}</span>
      </Link>
    );
  }

  return (
    <a
      href={href}
      target="_blank"
      rel="noopener noreferrer"
      aria-label={label}
      className={className}
    >
      {children}
      <span className="sr-only">{label}</span>
    </a>
  );
}

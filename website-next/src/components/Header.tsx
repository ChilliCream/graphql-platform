import Link from "next/link";
import type { ComponentType, ReactNode, SVGProps } from "react";
import { BlogIcon } from "@/src/icons/Blog";
import { ChevronDownIcon } from "@/src/icons/ChevronDown";
import { ChilliCream } from "@/src/icons/ChilliCream";
import { GitHubIcon } from "@/src/icons/GitHub";
import { LinkedInIcon } from "@/src/icons/LinkedIn";
import { SearchIcon } from "@/src/icons/Search";
import { SlackIcon } from "@/src/icons/Slack";
import { XIcon } from "@/src/icons/X";
import { YouTubeIcon } from "@/src/icons/YouTube";
import { MobileNav } from "./MobileNav";

const TOOLS = {
  blog: "/blogs",
  github: "https://github.com/ChilliCream/graphql-platform",
  linkedIn: "https://www.linkedin.com/company/chillicream",
  nitro: "https://nitro.chillicream.com",
  shop: "https://store.chillicream.com",
  slack: "https://slack.chillicream.com/",
  youtube: "https://www.youtube.com/c/ChilliCream",
  x: "https://x.com/Chilli_Cream",
};

const DEMO_HREF = "mailto:contact@chillicream.com?subject=Demo";

type Icon = ComponentType<SVGProps<SVGSVGElement>>;

interface SubLink {
  href: string;
  label: string;
  description?: string;
  icon?: Icon;
}

interface SubGroup {
  title: string;
  links: SubLink[];
}

interface NavItem {
  href: string;
  label: string;
  groups?: SubGroup[];
  panelWidth?: string;
}

const NAV_ITEMS: NavItem[] = [
  {
    href: "/platform",
    label: "Platform",
    panelWidth: "w-[640px]",
    groups: [
      {
        title: "Platform",
        links: [
          {
            href: "/platform/analytics",
            label: "Analytics",
            description: "Instant Insights. Enhanced Performance.",
          },
          {
            href: "/platform/continuous-integration",
            label: "Continuous Integration",
            description: "Innovate with Confidence. Deliver with Quality.",
          },
          {
            href: "/platform/ecosystem",
            label: "Ecosystem",
            description: "An Ecosystem You Trust and Love.",
          },
        ],
      },
      {
        title: "Products",
        links: [
          {
            href: "/products/nitro",
            label: "Nitro",
            description: "GraphQL IDE / API Cockpit",
          },
        ],
      },
    ],
  },
  {
    href: "/services",
    label: "Services",
    panelWidth: "w-[420px]",
    groups: [
      {
        title: "Services",
        links: [
          {
            href: "/services/advisory",
            label: "Advisory",
            description: "Consulting / Contracting",
          },
          {
            href: "/services/support",
            label: "Support",
            description: "Get Help from Experts",
          },
          {
            href: "/services/training",
            label: "Training",
            description: "Increase Your Team's Productivity",
          },
        ],
      },
    ],
  },
  {
    href: "/docs",
    label: "Developers",
    panelWidth: "w-[640px]",
    groups: [
      {
        title: "Documentation",
        links: [
          { href: "/docs/hotchocolate", label: "Hot Chocolate" },
          { href: "/docs/strawberryshake", label: "Strawberry Shake" },
          { href: "/docs/mocha", label: "Mocha" },
          { href: "/docs/fusion", label: "Fusion" },
          { href: "/docs/nitro", label: "Nitro" },
        ],
      },
      {
        title: "Additional Resources",
        links: [
          { href: TOOLS.blog, label: "Blog", icon: BlogIcon },
          { href: TOOLS.github, label: "GitHub", icon: GitHubIcon },
          { href: TOOLS.slack, label: "Slack / Community", icon: SlackIcon },
          { href: TOOLS.youtube, label: "YouTube", icon: YouTubeIcon },
          { href: TOOLS.x, label: "X", icon: XIcon },
          { href: TOOLS.linkedIn, label: "LinkedIn", icon: LinkedInIcon },
        ],
      },
    ],
  },
  {
    href: "/resources",
    label: "Company",
    panelWidth: "w-[320px]",
    groups: [
      {
        title: "Company",
        links: [
          { href: "mailto:contact@chillicream.com", label: "Contact" },
          { href: TOOLS.shop, label: "Shop" },
          {
            href: "/legal/acceptable-use-policy",
            label: "Acceptable Use Policy",
          },
          { href: "/legal/cookie-policy", label: "Cookie Policy" },
          { href: "/legal/privacy-policy", label: "Privacy Policy" },
          { href: "/legal/terms-of-service", label: "Terms of Service" },
          {
            href: "/licensing/chillicream-license",
            label: "ChilliCream License",
          },
        ],
      },
    ],
  },
  { href: "/pricing", label: "Pricing" },
  { href: "/help", label: "Help" },
];

const MOBILE_ITEMS = NAV_ITEMS.map((i) => ({ href: i.href, label: i.label }));

export default function Header() {
  return (
    <header className="sticky top-0 z-30 flex h-[72px] w-full justify-center border-b border-stone-200 bg-white/80 backdrop-blur-md">
      <div className="relative flex h-full w-full max-w-7xl items-center justify-between px-4 lg:gap-8">
        <Link
          href="/"
          aria-label="ChilliCream Home"
          className="flex h-full flex-none items-center text-stone-900 transition-colors hover:text-fuchsia-700"
        >
          <ChilliCream className="h-8 w-8 fill-current" />
        </Link>

        <nav className="relative hidden h-full flex-1 lg:block">
          <ol className="m-0 flex h-full list-none items-stretch p-0">
            {NAV_ITEMS.map((item) =>
              item.groups ? (
                <NavWithSubmenu key={item.href} item={item} />
              ) : (
                <NavSimple key={item.href} item={item} />
              ),
            )}
          </ol>
        </nav>

        <div className="hidden flex-none items-center gap-6 lg:flex">
          <a
            href={DEMO_HREF}
            className="text-sm font-medium text-stone-700 no-underline transition-colors hover:text-fuchsia-700"
          >
            Request a Demo
          </a>
          <a
            href={TOOLS.nitro}
            target="_blank"
            rel="noopener noreferrer"
            className="inline-flex h-[38px] items-center rounded-md border-2 border-fuchsia-700 bg-fuchsia-700 px-7 text-sm font-medium text-white no-underline transition-colors hover:bg-fuchsia-800"
          >
            Launch
          </a>
          <button
            type="button"
            aria-label="Search"
            className="flex h-full items-center text-stone-700 transition-colors hover:text-fuchsia-700"
          >
            <SearchIcon className="h-5 w-5 fill-current" />
          </button>
        </div>

        <MobileNav
          items={MOBILE_ITEMS}
          demoHref={DEMO_HREF}
          nitroHref={TOOLS.nitro}
        />
      </div>
    </header>
  );
}

function NavSimple({ item }: { item: NavItem }) {
  return (
    <li className="flex items-stretch">
      <Link
        href={item.href}
        className="flex items-center px-4 text-sm font-medium text-stone-700 no-underline transition-colors hover:text-fuchsia-700"
      >
        {item.label}
      </Link>
    </li>
  );
}

function NavWithSubmenu({ item }: { item: NavItem }) {
  return (
    <li className="group/nav flex items-stretch">
      <Link
        href={item.href}
        className="flex items-center gap-1.5 px-4 text-sm font-medium text-stone-700 no-underline transition-colors hover:text-fuchsia-700 group-hover/nav:text-fuchsia-700 group-focus-within/nav:text-fuchsia-700"
      >
        {item.label}
        <ChevronDownIcon className="h-3 w-3 fill-current transition-transform duration-200 group-hover/nav:rotate-180 group-focus-within/nav:rotate-180" />
      </Link>

      <SubmenuPanel item={item} />
    </li>
  );
}

function SubmenuPanel({ item }: { item: NavItem }) {
  return (
    <div
      className={[
        "pointer-events-none invisible absolute left-1/2 top-full -translate-x-1/2 pt-2 opacity-0 transition-[opacity,visibility] duration-200",
        "group-hover/nav:pointer-events-auto group-hover/nav:visible group-hover/nav:opacity-100",
        "group-focus-within/nav:pointer-events-auto group-focus-within/nav:visible group-focus-within/nav:opacity-100",
      ].join(" ")}
    >
      <div
        className={[
          "rounded-lg border border-stone-200 bg-white/95 p-6 shadow-lg backdrop-blur-md",
          item.panelWidth ?? "w-[420px]",
        ].join(" ")}
      >
        <div
          className={
            (item.groups?.length ?? 0) > 1
              ? "grid grid-cols-2 gap-x-8 gap-y-6"
              : "grid grid-cols-1 gap-y-6"
          }
        >
          {item.groups!.map((group) => (
            <SubGroupBlock key={group.title} group={group} />
          ))}
        </div>
      </div>
    </div>
  );
}

function SubGroupBlock({ group }: { group: SubGroup }) {
  return (
    <div>
      <div
        role="heading"
        aria-level={2}
        className="mb-3 text-xs font-semibold uppercase tracking-wide text-stone-400"
      >
        {group.title}
      </div>
      <ul className="m-0 flex list-none flex-col gap-1 p-0">
        {group.links.map((link) => (
          <li key={link.href} className="m-0">
            <SubLinkRow link={link} />
          </li>
        ))}
      </ul>
    </div>
  );
}

function SubLinkRow({ link }: { link: SubLink }): ReactNode {
  const isExternal = link.href.startsWith("http");
  const linkProps = isExternal
    ? { target: "_blank" as const, rel: "noopener noreferrer" as const }
    : {};
  const Icon = link.icon;

  return (
    <Link
      href={link.href}
      {...linkProps}
      className="flex items-start gap-3 rounded-md px-2 py-2 text-stone-700 no-underline transition-colors hover:bg-stone-50 hover:text-fuchsia-700"
    >
      {Icon && (
        <Icon className="mt-0.5 h-4 w-4 flex-none fill-current text-stone-500" />
      )}
      <div>
        <div className="text-sm font-medium">{link.label}</div>
        {link.description && (
          <div className="text-xs font-normal text-stone-500">
            {link.description}
          </div>
        )}
      </div>
    </Link>
  );
}

import Link from "next/link";
import Image from "next/image";
import type { ComponentType, ReactNode, SVGProps } from "react";

import {
  getLatestBlogPost,
  type BlogPostSummary,
} from "@/src/helpers/blogPosts";
import { formatDate } from "@/src/helpers/formatDate";
import { BlogIcon } from "@/src/icons/Blog";
import { ChevronDownIcon } from "@/src/icons/ChevronDown";
import { ChilliCream } from "@/src/icons/ChilliCream";
import { GitHubIcon } from "@/src/icons/GitHub";
import { LinkedInIcon } from "@/src/icons/LinkedIn";
import { SlackIcon } from "@/src/icons/Slack";
import { XIcon } from "@/src/icons/X";
import { YouTubeIcon } from "@/src/icons/YouTube";
import {
  BuildingIcon,
  CloudIcon,
  HandshakeAngleIcon,
  LollipopIcon,
  NewspaperIcon,
  ServerIcon,
  SparklesIcon,
  WavePulseIcon,
} from "@/src/icons/NavIcons";

import { MobileNav } from "./MobileNav";
import { Search } from "./Search";

const TOOLS = {
  blog: "/blog",
  github: "https://github.com/ChilliCream/graphql-platform",
  linkedIn: "https://www.linkedin.com/company/chillicream",
  nitro: "https://nitro.chillicream.com",
  shop: "https://store.chillicream.com",
  slack: "https://slack.chillicream.com/",
  youtube: "https://www.youtube.com/c/ChilliCream",
  x: "https://x.com/Chilli_Cream",
};

const CONTACT_HREF = "/services/support/contact";
const GITHUB_REPO_URL = "https://github.com/ChilliCream/graphql-platform";

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
  aside?: "blog" | "get-in-touch";
}

const NAV_ITEMS: NavItem[] = [
  {
    href: "/platform",
    label: "Platform",
    panelWidth: "w-[820px]",
    aside: "blog",
    groups: [
      {
        title: "Platform",
        links: [
          {
            href: "/platform/analytics",
            label: "Analytics",
            description: "Instant Insights. Enhanced Performance.",
            icon: WavePulseIcon,
          },
          {
            href: "/platform/continuous-integration",
            label: "Continuous Integration",
            description: "Innovate with Confidence. Deliver with Quality.",
            icon: SparklesIcon,
          },
          {
            href: "/platform/ecosystem",
            label: "Ecosystem",
            description: "An Ecosystem You Trust and Love.",
            icon: CloudIcon,
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
            icon: LollipopIcon,
          },
        ],
      },
    ],
  },
  {
    href: "/services",
    label: "Services",
    panelWidth: "w-[780px]",
    aside: "get-in-touch",
    groups: [
      {
        title: "Services",
        links: [
          {
            href: "/services/advisory",
            label: "Advisory",
            description: "Consulting / Contracting",
            icon: HandshakeAngleIcon,
          },
          {
            href: "/services/support",
            label: "Support",
            description: "Get Help from Experts",
            icon: ServerIcon,
          },
          {
            href: "/services/training",
            label: "Training",
            description: "Increase Your Team's Productivity",
            icon: BuildingIcon,
          },
        ],
      },
    ],
  },
  {
    href: "/docs",
    label: "Developers",
    panelWidth: "w-[840px]",
    aside: "blog",
    groups: [
      {
        title: "Documentation",
        links: [
          {
            href: "/docs/hotchocolate",
            label: "Hot Chocolate",
            icon: LollipopIcon,
          },
          {
            href: "/docs/strawberryshake",
            label: "Strawberry Shake",
            icon: LollipopIcon,
          },
          { href: "/docs/mocha", label: "Mocha", icon: LollipopIcon },
          { href: "/docs/fusion", label: "Fusion", icon: SparklesIcon },
          { href: "/docs/nitro", label: "Nitro", icon: LollipopIcon },
        ],
      },
      {
        title: "Additional Resources",
        links: [
          { href: TOOLS.blog, label: "Blog", icon: BlogIcon },
          { href: TOOLS.github, label: "GitHub", icon: GitHubIcon },
          { href: TOOLS.slack, label: "Slack / Community", icon: SlackIcon },
          { href: TOOLS.youtube, label: "YouTube Channel", icon: YouTubeIcon },
          { href: TOOLS.x, label: "X (Formerly Twitter)", icon: XIcon },
          { href: TOOLS.linkedIn, label: "LinkedIn", icon: LinkedInIcon },
        ],
      },
    ],
  },
  {
    href: "/resources",
    label: "Company",
    panelWidth: "w-[760px]",
    aside: "get-in-touch",
    groups: [
      {
        title: "Company",
        links: [
          {
            href: "mailto:contact@chillicream.com",
            label: "Contact",
            icon: NewspaperIcon,
          },
          { href: TOOLS.shop, label: "Shop", icon: NewspaperIcon },
          {
            href: "/legal/acceptable-use-policy",
            label: "Acceptable Use Policy",
            icon: NewspaperIcon,
          },
          {
            href: "/legal/cookie-policy",
            label: "Cookie Policy",
            icon: NewspaperIcon,
          },
          {
            href: "/legal/privacy-policy",
            label: "Privacy Policy",
            icon: NewspaperIcon,
          },
          {
            href: "/legal/terms-of-service",
            label: "Terms of Service",
            icon: NewspaperIcon,
          },
          {
            href: "/licensing/chillicream-license",
            label: "ChilliCream License",
            icon: NewspaperIcon,
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
  const latestBlog = getLatestBlogPost();
  return (
    <header className="sticky top-0 z-30 flex h-18 w-full justify-center border-b border-cc-white/10 bg-cc-card-bg shadow-[inset_0_1px_0_var(--cc-highlight)] backdrop-blur-[18px] backdrop-saturate-150">
      <div className="relative flex h-full w-full max-w-7xl items-center justify-between px-4 lg:gap-8">
        <Link
          href="/"
          aria-label="ChilliCream Home"
          className="flex h-full flex-none items-center text-cc-ink transition-colors hover:text-cc-accent"
        >
          <ChilliCream className="h-8 w-8 fill-current" />
        </Link>

        <nav className="relative hidden h-full flex-1 min-[1060px]:block">
          <ol className="m-0 flex h-full list-none items-stretch p-0">
            {NAV_ITEMS.map((item) =>
              item.groups ? (
                <NavWithSubmenu
                  key={item.href}
                  item={item}
                  latestBlog={latestBlog}
                />
              ) : (
                <NavSimple key={item.href} item={item} />
              ),
            )}
          </ol>
        </nav>

        <div className="hidden flex-none items-center gap-5 min-[1060px]:flex">
          <a
            href={GITHUB_REPO_URL}
            target="_blank"
            rel="noopener noreferrer"
            className="inline-flex items-center gap-2 rounded-md border border-cc-card-border bg-cc-hover px-2.5 py-1 text-xs font-medium text-cc-ink no-underline transition-colors hover:border-cc-card-border-hover"
            aria-label="Star ChilliCream on GitHub"
          >
            <GitHubIcon className="h-3.5 w-3.5 fill-current" />
            Star
          </a>
          <Link
            href={CONTACT_HREF}
            className="text-sm font-medium text-cc-ink-dim no-underline transition-colors hover:text-cc-ink"
          >
            Contact Us
          </Link>
          <a
            href={TOOLS.nitro}
            target="_blank"
            rel="noopener noreferrer"
            className="inline-flex h-9.5 items-center rounded-full bg-cc-ink px-7 text-sm font-medium text-cc-surface no-underline transition-colors hover:bg-cc-white"
          >
            Launch
          </a>
          <Search
            ariaLabel="Search"
            className="flex h-full cursor-pointer items-center text-cc-ink-dim transition-colors hover:text-cc-ink"
          />
        </div>

        <MobileNav
          items={MOBILE_ITEMS}
          demoHref={CONTACT_HREF}
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
        className="flex items-center px-4 text-sm font-medium text-cc-ink-dim no-underline transition-colors hover:text-cc-ink"
      >
        {item.label}
      </Link>
    </li>
  );
}

function NavWithSubmenu({
  item,
  latestBlog,
}: {
  item: NavItem;
  latestBlog: BlogPostSummary | null;
}) {
  return (
    <li className="group/nav flex items-stretch">
      <Link
        href={item.href}
        className="flex items-center gap-1.5 px-4 text-sm font-medium text-cc-ink-dim no-underline transition-colors hover:text-cc-ink group-hover/nav:text-cc-ink"
      >
        {item.label}
        <ChevronDownIcon className="h-3 w-3 fill-current" />
      </Link>

      <SubmenuPanel item={item} latestBlog={latestBlog} />
    </li>
  );
}

function SubmenuPanel({
  item,
  latestBlog,
}: {
  item: NavItem;
  latestBlog: BlogPostSummary | null;
}) {
  const showBlog = item.aside === "blog" && latestBlog;
  const showGetInTouch = item.aside === "get-in-touch";
  const showAside = showBlog || showGetInTouch;
  return (
    <div
      className={[
        "pointer-events-none invisible absolute left-1/2 top-full -translate-x-1/2 pt-2 opacity-0 transition-[opacity,visibility] duration-200",
        "group-hover/nav:pointer-events-auto group-hover/nav:visible group-hover/nav:opacity-100",
      ].join(" ")}
    >
      <div
        className={[
          "grid gap-8 rounded-lg border border-cc-white/10 bg-cc-surface/95 p-6 shadow-2xl backdrop-blur-md",
          showAside ? "grid-cols-[1fr_280px]" : "grid-cols-1",
          item.panelWidth ?? "w-120",
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
        {showBlog && <LatestBlogPanel post={latestBlog} />}
        {showGetInTouch && <GetInTouchPanel />}
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
        className="mb-3 text-xs font-semibold uppercase tracking-[0.18em] text-cc-ink-dim"
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
      className="group/link flex items-start gap-3 rounded-md px-2 py-2 text-cc-ink-dim no-underline transition-colors hover:bg-cc-hover"
    >
      {Icon && (
        <span className="mt-0.5 flex h-5 w-5 flex-none items-center justify-center text-cc-ink-dim transition-colors group-hover/link:text-cc-ink">
          <Icon className="h-4 w-4 fill-current" />
        </span>
      )}
      <div>
        <div className="text-sm font-medium text-cc-ink">{link.label}</div>
        {link.description && (
          <div className="text-xs font-normal text-cc-ink-dim">
            {link.description}
          </div>
        )}
      </div>
    </Link>
  );
}

function LatestBlogPanel({ post }: { post: BlogPostSummary }) {
  return (
    <div className="flex flex-col gap-3">
      <div
        role="heading"
        aria-level={2}
        className="text-xs font-semibold uppercase tracking-[0.18em] text-cc-ink-dim"
      >
        Latest Blog Post
      </div>
      <Link
        href={post.href}
        className="group/blog flex flex-col gap-2 rounded-md text-cc-ink no-underline"
      >
        {post.featuredImage && (
          <div className="overflow-hidden rounded-md border border-cc-white/10">
            <Image
              src={post.featuredImage}
              alt={post.title}
              width={320}
              height={180}
              className="block h-auto w-full"
            />
          </div>
        )}
        <div className="text-xs text-cc-ink-dim">{formatDate(post.date)}</div>
        <div className="text-sm font-medium leading-snug text-cc-ink group-hover/blog:text-cc-accent">
          {post.title}
        </div>
      </Link>
    </div>
  );
}

function GetInTouchPanel() {
  return (
    <div className="flex flex-col gap-3">
      <div
        role="heading"
        aria-level={2}
        className="text-xs font-semibold uppercase tracking-[0.18em] text-cc-ink-dim"
      >
        Get in touch
      </div>
      <div className="flex h-45 items-center justify-center rounded-md border border-cc-white/10 bg-[image:var(--cc-promo-gradient)]">
        <div className="text-center text-sm font-medium leading-snug text-cc-ink">
          Your technology journey.
          <br />
          Our expertise.
        </div>
      </div>
      <p className="text-xs leading-relaxed text-cc-ink-dim">
        <span className="font-semibold text-cc-ink">ChilliCream</span> helps you
        unlock your full potential, delivering on its promise to transform your
        business.
      </p>
    </div>
  );
}

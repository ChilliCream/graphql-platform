import type { ComponentType, SVGProps } from "react";
import { BlogIcon } from "@/src/icons/Blog";
import { GitHubIcon } from "@/src/icons/GitHub";
import { LinkedInIcon } from "@/src/icons/LinkedIn";
import { SlackIcon } from "@/src/icons/Slack";
import { XIcon } from "@/src/icons/X";
import { YouTubeIcon } from "@/src/icons/YouTube";
import {
  BuildingIcon,
  CloudIcon,
  HandshakeAngleIcon,
  NewspaperIcon,
  RocketIcon,
  ServerIcon,
  SparklesIcon,
  WavePulseIcon,
} from "@/src/icons/NavIcons";

export const TOOLS = {
  blog: "/blog",
  github: "https://github.com/ChilliCream/graphql-platform",
  linkedIn: "https://www.linkedin.com/company/chillicream",
  nitro: "https://nitro.chillicream.com",
  shop: "https://store.chillicream.com",
  slack: "https://slack.chillicream.com/",
  youtube: "https://www.youtube.com/c/ChilliCream",
  x: "https://x.com/Chilli_Cream",
};

export const CONTACT_HREF = "/services/support/contact";
export const GITHUB_REPO_URL =
  "https://github.com/ChilliCream/graphql-platform";
export const GITHUB_STARGAZERS_URL = `${GITHUB_REPO_URL}/stargazers`;

type Icon = ComponentType<SVGProps<SVGSVGElement>>;

export interface SubLink {
  href: string;
  label: string;
  description?: string;
  icon?: Icon;
}

export interface SubGroup {
  title: string;
  links: SubLink[];
}

export interface NavItem {
  href: string;
  label: string;
  groups?: SubGroup[];
  panelWidth?: string;
  aside?: "blog" | "get-in-touch";
}

export const NAV_ITEMS: NavItem[] = [
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
            icon: RocketIcon,
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
            icon: RocketIcon,
          },
          {
            href: "/docs/strawberryshake",
            label: "Strawberry Shake",
            icon: RocketIcon,
          },
          { href: "/docs/mocha", label: "Mocha", icon: RocketIcon },
          { href: "/docs/fusion", label: "Fusion", icon: RocketIcon },
          { href: "/docs/nitro", label: "Nitro", icon: RocketIcon },
          { href: "/docs/skillz", label: "Skillz", icon: RocketIcon },
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

export const MOBILE_ITEMS = NAV_ITEMS.map((i) => ({
  href: i.href,
  label: i.label,
}));

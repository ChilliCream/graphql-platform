import { GITHUB_REPO_URL } from "@/src/helpers/github";
import { BlogIcon } from "@/src/icons/Blog";
import { FUSION_ARTWORK, Fusion } from "@/src/icons/Fusion";
import { GitHubIcon } from "@/src/icons/GitHub";
import { HOT_CHOCOLATE_ARTWORK, HotChocolate } from "@/src/icons/HotChocolate";
import { LinkedInIcon } from "@/src/icons/LinkedIn";
import { MOCHA_ARTWORK, Mocha } from "@/src/icons/Mocha";
import {
  BuildingIcon,
  CloudIcon,
  ColumnsCompareIcon,
  HandshakeAngleIcon,
  NetworkIcon,
  NewspaperIcon,
  ServerIcon,
  SparklesIcon,
  WavePulseIcon,
} from "@/src/icons/NavIcons";
import { NITRO_ARTWORK, Nitro } from "@/src/icons/Nitro";
import type { ProductArtworkSize } from "@/src/icons/productArtwork";
import { RobotIcon } from "@/src/icons/RobotIcon";
import { SKILLS_ARTWORK, Skills } from "@/src/icons/Skills";
import { SlackIcon } from "@/src/icons/Slack";
import {
  STRAWBERRY_SHAKE_ARTWORK,
  StrawberryShake,
} from "@/src/icons/StrawberryShake";
import { XIcon } from "@/src/icons/X";
import { YouTubeIcon } from "@/src/icons/YouTube";
import type { ComponentType, SVGProps } from "react";

export const TOOLS = {
  blog: "/blog",
  comparison: "/comparison",
  github: GITHUB_REPO_URL,
  linkedIn: "https://www.linkedin.com/company/chillicream",
  nitro: "https://nitro.chillicream.com",
  shop: "https://store.chillicream.com",
  slack: "https://slack.chillicream.com/",
  youtube: "https://www.youtube.com/c/ChilliCream",
  x: "https://x.com/Chilli_Cream",
};

export const CONTACT_HREF = "/services/support/contact";

type Icon = ComponentType<SVGProps<SVGSVGElement>>;

export interface SubLink {
  href: string;
  label: string;
  description?: string;
  icon?: Icon;
  /**
   * Intrinsic size of the icon artwork in sheet units (e.g. `NITRO_ARTWORK`).
   * Set it on product icons: the menu then sizes them all with one scale
   * factor, bottom-aligned, instead of forcing them into a square box.
   */
  iconSize?: ProductArtworkSize;
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
            href: "/platform/release-safety",
            label: "Release Safety",
            description: "Innovate with Confidence. Deliver with Quality.",
            icon: SparklesIcon,
          },
          {
            href: "/platform/agentic-coding",
            label: "Agentic Development",
            description: "Consistently Good Code, from Any Agent.",
            icon: RobotIcon,
          },
          {
            href: "/platform/graphql-federation",
            label: "GraphQL Federation",
            description: "Many Services. One Graph.",
            icon: NetworkIcon,
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
            description: "Observability, governance, and delivery.",
            icon: Nitro,
            iconSize: NITRO_ARTWORK,
          },
          {
            href: "/products/mocha",
            label: "Mocha",
            description: "Messaging for .NET.",
            icon: Mocha,
            iconSize: MOCHA_ARTWORK,
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
            icon: HotChocolate,
            iconSize: HOT_CHOCOLATE_ARTWORK,
          },
          {
            href: "/docs/strawberryshake",
            label: "Strawberry Shake",
            icon: StrawberryShake,
            iconSize: STRAWBERRY_SHAKE_ARTWORK,
          },
          {
            href: "/docs/mocha",
            label: "Mocha",
            icon: Mocha,
            iconSize: MOCHA_ARTWORK,
          },
          {
            href: "/docs/fusion",
            label: "Fusion",
            icon: Fusion,
            iconSize: FUSION_ARTWORK,
          },
          {
            href: "/docs/nitro",
            label: "Nitro",
            icon: Nitro,
            iconSize: NITRO_ARTWORK,
          },
          {
            href: "/docs/skills",
            label: "Skills",
            icon: Skills,
            iconSize: SKILLS_ARTWORK,
          },
        ],
      },
      {
        title: "Additional Resources",
        links: [
          { href: TOOLS.blog, label: "Blog", icon: BlogIcon },
          {
            href: TOOLS.comparison,
            label: "Comparison",
            icon: ColumnsCompareIcon,
          },
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
            href: "/services/support/contact",
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

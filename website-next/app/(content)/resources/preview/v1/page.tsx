import type { Metadata } from "next";
import Link from "next/link";
import type { ReactNode } from "react";

import { OutlineButton, SolidButton } from "@/src/design-system/Button";

export const metadata: Metadata = {
  title: "Builder Library | ChilliCream Resources",
  description:
    "The ChilliCream builder library: docs, blog, videos, Slack, GitHub, contact, shop, and legal. One concrete link per item, nothing invented, nothing fluffy.",
  keywords: [
    "ChilliCream resources",
    "GraphQL docs",
    "Hot Chocolate",
    "Nitro",
    "ChilliCream community",
    "ChilliCream legal",
  ],
  openGraph: {
    title: "Builder Library | ChilliCream Resources",
    description:
      "Docs, blog, videos, Slack, GitHub, contact, shop, and legal. The builder library for everyone shipping on ChilliCream.",
  },
  robots: { index: false, follow: false },
};

interface Entry {
  readonly href: string;
  readonly title: string;
  readonly description: string;
  readonly meta: string;
  readonly external?: boolean;
  readonly icon: ReactNode;
}

interface Group {
  readonly id: string;
  readonly label: string;
  readonly heading: string;
  readonly intro: string;
  readonly entries: readonly Entry[];
}

function DocsGlyph() {
  return (
    <svg viewBox="0 0 24 24" width={20} height={20} aria-hidden="true">
      <path
        d="M5 4h9l5 5v11a1 1 0 0 1-1 1H5a1 1 0 0 1-1-1V5a1 1 0 0 1 1-1Z"
        fill="none"
        stroke="currentColor"
        strokeWidth="1.5"
        strokeLinejoin="round"
      />
      <path
        d="M14 4v5h5"
        fill="none"
        stroke="currentColor"
        strokeWidth="1.5"
        strokeLinejoin="round"
      />
      <path
        d="M7.5 13h7M7.5 16.5h7M7.5 9.5h3"
        stroke="currentColor"
        strokeWidth="1.5"
        strokeLinecap="round"
      />
    </svg>
  );
}

function BlogGlyph() {
  return (
    <svg viewBox="0 0 24 24" width={20} height={20} aria-hidden="true">
      <path
        d="M4 5h14a2 2 0 0 1 2 2v11a2 2 0 0 1-2 2H4V5Z"
        fill="none"
        stroke="currentColor"
        strokeWidth="1.5"
      />
      <path
        d="M4 5v15H3a1 1 0 0 1-1-1V6a1 1 0 0 1 1-1h1Z"
        fill="none"
        stroke="currentColor"
        strokeWidth="1.5"
      />
      <path
        d="M7 9.5h8M7 13h8M7 16.5h5"
        stroke="currentColor"
        strokeWidth="1.5"
        strokeLinecap="round"
      />
    </svg>
  );
}

function VideoGlyph() {
  return (
    <svg viewBox="0 0 24 24" width={20} height={20} aria-hidden="true">
      <rect
        x="3"
        y="6"
        width="14"
        height="12"
        rx="2"
        fill="none"
        stroke="currentColor"
        strokeWidth="1.5"
      />
      <path d="M17 10.5 21 8v8l-4-2.5v-3Z" fill="currentColor" />
    </svg>
  );
}

function SlackGlyph() {
  return (
    <svg viewBox="0 0 24 24" width={20} height={20} aria-hidden="true">
      <rect
        x="4"
        y="10"
        width="10"
        height="3"
        rx="1.5"
        fill="none"
        stroke="currentColor"
        strokeWidth="1.5"
      />
      <rect
        x="11"
        y="4"
        width="3"
        height="10"
        rx="1.5"
        fill="none"
        stroke="currentColor"
        strokeWidth="1.5"
      />
      <rect
        x="10"
        y="11"
        width="10"
        height="3"
        rx="1.5"
        fill="none"
        stroke="currentColor"
        strokeWidth="1.5"
      />
      <rect
        x="10"
        y="10"
        width="3"
        height="10"
        rx="1.5"
        fill="none"
        stroke="currentColor"
        strokeWidth="1.5"
      />
    </svg>
  );
}

function GitHubGlyph() {
  return (
    <svg viewBox="0 0 24 24" width={20} height={20} aria-hidden="true">
      <path
        d="M12 3a9 9 0 0 0-2.85 17.54c.45.08.61-.2.61-.43v-1.7c-2.5.55-3.03-1.06-3.03-1.06-.41-1.04-1-1.32-1-1.32-.82-.56.06-.55.06-.55.9.06 1.37.93 1.37.93.8 1.37 2.11.98 2.62.75.08-.58.31-.98.57-1.2-2-.23-4.1-1-4.1-4.45 0-.98.35-1.78.93-2.41-.09-.23-.4-1.15.09-2.4 0 0 .75-.24 2.47.92a8.5 8.5 0 0 1 4.49 0c1.72-1.16 2.47-.92 2.47-.92.49 1.25.18 2.17.09 2.4.58.63.93 1.43.93 2.41 0 3.46-2.1 4.22-4.11 4.44.32.28.6.83.6 1.68v2.49c0 .24.16.52.62.43A9 9 0 0 0 12 3Z"
        fill="currentColor"
      />
    </svg>
  );
}

function XGlyph() {
  return (
    <svg viewBox="0 0 24 24" width={20} height={20} aria-hidden="true">
      <path
        d="M4 4h3.6l4.5 6.3L17 4h3l-6.3 8.4L20.5 20H17l-4.8-6.7L7 20H4l6.6-8.8L4 4Z"
        fill="currentColor"
      />
    </svg>
  );
}

function LinkedInGlyph() {
  return (
    <svg viewBox="0 0 24 24" width={20} height={20} aria-hidden="true">
      <rect
        x="3"
        y="3"
        width="18"
        height="18"
        rx="3"
        fill="none"
        stroke="currentColor"
        strokeWidth="1.5"
      />
      <circle cx="7.5" cy="8" r="1.2" fill="currentColor" />
      <path
        d="M6.5 11h2v6h-2zM10.5 11h2v1.1c.5-.8 1.4-1.3 2.5-1.3 1.7 0 2.5 1.1 2.5 3v3.2h-2v-2.8c0-1-.4-1.6-1.3-1.6-.9 0-1.5.6-1.5 1.6V17h-2v-6Z"
        fill="currentColor"
      />
    </svg>
  );
}

function MailGlyph() {
  return (
    <svg viewBox="0 0 24 24" width={20} height={20} aria-hidden="true">
      <rect
        x="3"
        y="5"
        width="18"
        height="14"
        rx="2"
        fill="none"
        stroke="currentColor"
        strokeWidth="1.5"
      />
      <path
        d="m4 7 8 6 8-6"
        fill="none"
        stroke="currentColor"
        strokeWidth="1.5"
        strokeLinecap="round"
        strokeLinejoin="round"
      />
    </svg>
  );
}

function ShopGlyph() {
  return (
    <svg viewBox="0 0 24 24" width={20} height={20} aria-hidden="true">
      <path
        d="M4 7h16l-1.2 11.2A2 2 0 0 1 16.8 20H7.2a2 2 0 0 1-2-1.8L4 7Z"
        fill="none"
        stroke="currentColor"
        strokeWidth="1.5"
        strokeLinejoin="round"
      />
      <path
        d="M8 9V6a4 4 0 0 1 8 0v3"
        fill="none"
        stroke="currentColor"
        strokeWidth="1.5"
        strokeLinecap="round"
      />
    </svg>
  );
}

function GavelGlyph() {
  return (
    <svg viewBox="0 0 24 24" width={20} height={20} aria-hidden="true">
      <path
        d="m3 20 6-6M9.5 13.5 13 17M6 17l1 1M14 4l6 6M11 7l6 6M9.5 5.5l9 9"
        fill="none"
        stroke="currentColor"
        strokeWidth="1.5"
        strokeLinecap="round"
        strokeLinejoin="round"
      />
    </svg>
  );
}

function LicenseGlyph() {
  return (
    <svg viewBox="0 0 24 24" width={20} height={20} aria-hidden="true">
      <rect
        x="3"
        y="5"
        width="18"
        height="14"
        rx="2"
        fill="none"
        stroke="currentColor"
        strokeWidth="1.5"
      />
      <circle
        cx="9"
        cy="12"
        r="2.5"
        fill="none"
        stroke="currentColor"
        strokeWidth="1.5"
      />
      <path
        d="M13.5 10.5h5M13.5 14h3.5"
        stroke="currentColor"
        strokeWidth="1.5"
        strokeLinecap="round"
      />
    </svg>
  );
}

function CookieGlyph() {
  return (
    <svg viewBox="0 0 24 24" width={20} height={20} aria-hidden="true">
      <path
        d="M12 3a9 9 0 1 0 9 9 4 4 0 0 1-4-4 4 4 0 0 1-5-5Z"
        fill="none"
        stroke="currentColor"
        strokeWidth="1.5"
        strokeLinejoin="round"
      />
      <circle cx="9" cy="11" r="0.9" fill="currentColor" />
      <circle cx="14" cy="14" r="0.9" fill="currentColor" />
      <circle cx="10" cy="15.5" r="0.9" fill="currentColor" />
    </svg>
  );
}

function ShieldGlyph() {
  return (
    <svg viewBox="0 0 24 24" width={20} height={20} aria-hidden="true">
      <path
        d="M12 3 4 6v6c0 4.5 3.2 7.8 8 9 4.8-1.2 8-4.5 8-9V6l-8-3Z"
        fill="none"
        stroke="currentColor"
        strokeWidth="1.5"
        strokeLinejoin="round"
      />
      <path
        d="m8.5 12 2.5 2.5L15.5 10"
        fill="none"
        stroke="currentColor"
        strokeWidth="1.5"
        strokeLinecap="round"
        strokeLinejoin="round"
      />
    </svg>
  );
}

function ScrollGlyph() {
  return (
    <svg viewBox="0 0 24 24" width={20} height={20} aria-hidden="true">
      <path
        d="M6 4h11a2 2 0 0 1 2 2v11a3 3 0 0 1-3 3H7"
        fill="none"
        stroke="currentColor"
        strokeWidth="1.5"
        strokeLinecap="round"
        strokeLinejoin="round"
      />
      <path
        d="M6 4a2 2 0 0 0-2 2v10a2 2 0 0 0 2 2h1V6a2 2 0 0 0-1-2Z"
        fill="none"
        stroke="currentColor"
        strokeWidth="1.5"
        strokeLinejoin="round"
      />
      <path
        d="M10 9h6M10 12.5h6M10 16h4"
        stroke="currentColor"
        strokeWidth="1.5"
        strokeLinecap="round"
      />
    </svg>
  );
}

const GROUPS: readonly Group[] = [
  {
    id: "learn",
    label: "// 01 / learn",
    heading: "Learn",
    intro:
      "Long-form material maintained by the team. Start here when you need depth, not a tweet.",
    entries: [
      {
        href: "/docs",
        title: "Documentation",
        description:
          "Hot Chocolate, Fusion, Nitro, and the rest of the stack. Versioned, searchable, the source of truth.",
        meta: "/docs",
        icon: <DocsGlyph />,
      },
      {
        href: "/blog",
        title: "Blog",
        description:
          "Release notes, deep dives, and design rationale from the engineers building the platform.",
        meta: "/blog",
        icon: <BlogGlyph />,
      },
      {
        href: "https://www.youtube.com/c/ChilliCream",
        title: "YouTube",
        description:
          "Talks, walkthroughs, and recorded sessions on GraphQL, federation, and Nitro.",
        meta: "youtube.com/c/ChilliCream",
        external: true,
        icon: <VideoGlyph />,
      },
    ],
  },
  {
    id: "community",
    label: "// 02 / community",
    heading: "Community",
    intro:
      "Where users, contributors, and the core team actually talk. Pick the channel that matches the conversation.",
    entries: [
      {
        href: "https://slack.chillicream.com/",
        title: "Slack",
        description:
          "The fastest path to a human. Ask questions, share what you are building, watch issues land.",
        meta: "slack.chillicream.com",
        external: true,
        icon: <SlackGlyph />,
      },
      {
        href: "https://github.com/ChilliCream/graphql-platform",
        title: "GitHub",
        description:
          "Source, issues, discussions, and releases for the open source GraphQL platform.",
        meta: "github.com/ChilliCream",
        external: true,
        icon: <GitHubGlyph />,
      },
      {
        href: "https://x.com/Chilli_Cream",
        title: "X",
        description:
          "Release pings, conference notes, and short announcements from the team.",
        meta: "x.com/Chilli_Cream",
        external: true,
        icon: <XGlyph />,
      },
      {
        href: "https://www.linkedin.com/company/chillicream",
        title: "LinkedIn",
        description:
          "Company updates, roles, and longer announcements aimed at engineering leaders.",
        meta: "linkedin.com/company/chillicream",
        external: true,
        icon: <LinkedInGlyph />,
      },
    ],
  },
  {
    id: "company",
    label: "// 03 / company",
    heading: "Company",
    intro:
      "Reach the people behind ChilliCream, or pick up something with the chili on it.",
    entries: [
      {
        href: "mailto:contact@chillicream.com",
        title: "Contact",
        description:
          "Email the team about commercial questions, partnerships, or anything that does not fit a public channel.",
        meta: "contact@chillicream.com",
        icon: <MailGlyph />,
      },
      {
        href: "https://store.chillicream.com",
        title: "Shop",
        description:
          "Merch from the ChilliCream store. T-shirts, mugs, stickers, and the rest of the goodies.",
        meta: "store.chillicream.com",
        external: true,
        icon: <ShopGlyph />,
      },
    ],
  },
  {
    id: "legal",
    label: "// 04 / legal",
    heading: "Legal",
    intro:
      "The agreements that govern using ChilliCream services and software. Every link below is the live, current document.",
    entries: [
      {
        href: "/legal/terms-of-service",
        title: "Terms of Service",
        description:
          "The agreement between you and ChilliCream when using our services and websites.",
        meta: "/legal/terms-of-service",
        icon: <ScrollGlyph />,
      },
      {
        href: "/legal/privacy-policy",
        title: "Privacy Policy",
        description:
          "What personal data we collect, why we collect it, and how we handle it.",
        meta: "/legal/privacy-policy",
        icon: <ShieldGlyph />,
      },
      {
        href: "/legal/cookie-policy",
        title: "Cookie Policy",
        description:
          "Which cookies our sites set, what they do, and how to control them.",
        meta: "/legal/cookie-policy",
        icon: <CookieGlyph />,
      },
      {
        href: "/legal/acceptable-use-policy",
        title: "Acceptable Use Policy",
        description: "Rules for using ChilliCream services responsibly.",
        meta: "/legal/acceptable-use-policy",
        icon: <GavelGlyph />,
      },
      {
        href: "/licensing/chillicream-license",
        title: "ChilliCream License",
        description:
          "Commercial license terms for ChilliCream products beyond the open source MIT pieces.",
        meta: "/licensing/chillicream-license",
        icon: <LicenseGlyph />,
      },
    ],
  },
];

function isExternal(href: string) {
  return /^https?:/.test(href);
}

interface LinkCardProps {
  readonly entry: Entry;
  readonly index: number;
}

function LinkCard({ entry, index }: LinkCardProps) {
  const idx = String(index + 1).padStart(2, "0");
  const cls =
    "group border-cc-card-border bg-cc-card-bg hover:border-cc-accent relative flex h-full flex-col rounded-xl border p-5 no-underline backdrop-blur-sm transition-colors";

  const body = (
    <>
      <div className="text-cc-nav-label flex items-center justify-between font-mono text-[11px] tracking-widest uppercase">
        <span>{idx}</span>
        <span className="text-cc-ink-faint">
          {entry.external ? "ext" : "→"}
        </span>
      </div>
      <div className="text-cc-ink-dim group-hover:text-cc-accent mt-4 transition-colors">
        {entry.icon}
      </div>
      <h3 className="text-cc-heading group-hover:text-cc-accent mt-3 text-lg font-semibold tracking-tight transition-colors">
        {entry.title}
      </h3>
      <p className="text-cc-ink-dim mt-2 text-sm leading-relaxed">
        {entry.description}
      </p>
      <div className="border-cc-card-border/60 text-cc-nav-label mt-4 truncate border-t pt-3 font-mono text-[11px] tracking-wide">
        {entry.meta}
      </div>
    </>
  );

  if (entry.href.startsWith("/")) {
    return (
      <Link href={entry.href} className={cls}>
        {body}
      </Link>
    );
  }

  if (isExternal(entry.href)) {
    return (
      <a
        href={entry.href}
        target="_blank"
        rel="noopener noreferrer"
        className={cls}
      >
        {body}
      </a>
    );
  }

  return (
    <a href={entry.href} className={cls}>
      {body}
    </a>
  );
}

interface GroupBlockProps {
  readonly group: Group;
}

function GroupBlock({ group }: GroupBlockProps) {
  const cols =
    group.entries.length >= 4
      ? "sm:grid-cols-2 lg:grid-cols-4"
      : group.entries.length === 3
        ? "sm:grid-cols-2 lg:grid-cols-3"
        : "sm:grid-cols-2";
  return (
    <section
      id={group.id}
      aria-labelledby={`${group.id}-heading`}
      className="border-cc-card-border/60 border-t py-14"
    >
      <div className="mb-8 grid gap-2 lg:grid-cols-[16rem_1fr] lg:items-end lg:gap-12">
        <div>
          <div className="text-cc-nav-label font-mono text-xs tracking-widest uppercase">
            {group.label}
          </div>
          <h2
            id={`${group.id}-heading`}
            className="text-cc-heading mt-2 text-3xl font-semibold tracking-tight sm:text-4xl"
          >
            {group.heading}
          </h2>
        </div>
        <p className="text-cc-ink-dim max-w-2xl text-base">{group.intro}</p>
      </div>
      <div className={`grid gap-4 ${cols}`}>
        {group.entries.map((entry, index) => (
          <LinkCard key={entry.href} entry={entry} index={index} />
        ))}
      </div>
    </section>
  );
}

function ResourcesHero() {
  return (
    <section className="relative py-16 sm:py-24">
      <div className="border-cc-card-border bg-cc-card-bg/40 relative overflow-hidden rounded-2xl border backdrop-blur-sm">
        <div
          aria-hidden="true"
          className="absolute inset-0 opacity-[0.35]"
          style={{
            backgroundImage:
              "linear-gradient(rgba(245,241,234,0.05) 1px, transparent 1px), linear-gradient(90deg, rgba(245,241,234,0.05) 1px, transparent 1px)",
            backgroundSize: "32px 32px",
          }}
        />
        <div
          aria-hidden="true"
          className="absolute -top-32 -right-24 h-72 w-72 rounded-full blur-3xl"
          style={{
            background:
              "radial-gradient(circle, rgba(94,234,212,0.18), transparent 70%)",
          }}
        />

        <div className="relative grid gap-10 p-8 sm:p-12 lg:grid-cols-[1.4fr_1fr] lg:gap-16 lg:p-16">
          <div>
            <div className="text-cc-nav-label font-mono text-xs tracking-widest uppercase">
              {"// resources / builder library"}
            </div>
            <h1 className="text-cc-heading mt-4 text-5xl leading-tight font-semibold tracking-tight sm:text-6xl lg:text-7xl">
              The builder
              <br />
              library.
            </h1>
            <p className="text-cc-ink-dim mt-6 max-w-xl text-lg">
              Every link an engineer working on ChilliCream actually opens.
              Docs, blog, videos, Slack, GitHub, contact, shop, legal. No
              brochures, no fluff, no press kits we do not have.
            </p>
            <div className="mt-8 flex flex-wrap gap-3">
              <SolidButton href="/docs">Open the docs</SolidButton>
              <OutlineButton href="https://slack.chillicream.com/">
                Join Slack
              </OutlineButton>
            </div>
          </div>

          <div
            aria-hidden="true"
            className="border-cc-card-border bg-cc-surface/70 relative self-stretch overflow-hidden rounded-xl border font-mono text-[13px] leading-relaxed shadow-inner"
          >
            <div className="border-cc-card-border bg-cc-bg/60 flex items-center gap-1.5 border-b px-4 py-2">
              <span className="bg-cc-danger/70 inline-block h-2.5 w-2.5 rounded-full" />
              <span className="bg-cc-warning/70 inline-block h-2.5 w-2.5 rounded-full" />
              <span className="bg-cc-success/70 inline-block h-2.5 w-2.5 rounded-full" />
              <span className="text-cc-nav-label ml-3 text-[11px] tracking-widest uppercase">
                ~/chillicream/resources
              </span>
            </div>
            <pre className="text-cc-ink-dim m-0 px-5 py-5 whitespace-pre-wrap">
              <span className="text-cc-accent">$</span> ls -1 ./
              {"\n"}
              <span className="text-cc-heading">learn/</span>
              {"     "}
              <span className="text-cc-nav-label">3 entries</span>
              {"\n"}
              <span className="text-cc-heading">community/</span>{" "}
              <span className="text-cc-nav-label">4 entries</span>
              {"\n"}
              <span className="text-cc-heading">company/</span>
              {"   "}
              <span className="text-cc-nav-label">2 entries</span>
              {"\n"}
              <span className="text-cc-heading">legal/</span>
              {"     "}
              <span className="text-cc-nav-label">5 entries</span>
              {"\n\n"}
              <span className="text-cc-accent">$</span> cat README
              {"\n"}
              <span>One hub. Real links. Maintained by humans.</span>
            </pre>
          </div>
        </div>
      </div>
    </section>
  );
}

function JumpNav() {
  return (
    <nav
      aria-label="Sections"
      className="border-cc-card-border/60 flex flex-wrap gap-2 border-y py-4"
    >
      <span className="text-cc-nav-label mr-2 font-mono text-xs tracking-widest uppercase">
        {"// jump to"}
      </span>
      {GROUPS.map((g) => (
        <a
          key={g.id}
          href={`#${g.id}`}
          className="text-cc-ink-dim hover:text-cc-accent hover:border-cc-accent border-cc-card-border rounded-full border px-3 py-1 font-mono text-xs tracking-wide no-underline transition-colors"
        >
          {g.heading.toLowerCase()}
        </a>
      ))}
    </nav>
  );
}

function ContactBand() {
  return (
    <section
      aria-labelledby="contact-heading"
      className="border-cc-card-border/60 border-t py-14"
    >
      <div className="border-cc-card-border bg-cc-card-bg/60 overflow-hidden rounded-2xl border backdrop-blur-sm">
        <div className="grid gap-8 p-8 sm:p-10 lg:grid-cols-[1fr_auto] lg:items-center lg:gap-12">
          <div>
            <div className="text-cc-nav-label font-mono text-xs tracking-widest uppercase">
              {"// contact"}
            </div>
            <h2
              id="contact-heading"
              className="text-cc-heading mt-2 text-3xl font-semibold tracking-tight sm:text-4xl"
            >
              Need a human?
            </h2>
            <p className="text-cc-ink-dim mt-3 max-w-2xl text-base">
              Slack is fastest for everything technical and community-facing.
              For commercial questions, partnerships, security reports, or
              anything that does not belong in a public channel, send us mail.
            </p>
            <div className="mt-5 inline-flex items-center gap-2 font-mono text-sm">
              <span className="text-cc-nav-label">$</span>
              <span className="text-cc-accent">mail</span>
              <span className="text-cc-ink">contact@chillicream.com</span>
            </div>
          </div>
          <div className="flex flex-wrap gap-3 lg:flex-col">
            <SolidButton href="mailto:contact@chillicream.com">
              Email the team
            </SolidButton>
            <OutlineButton href="https://slack.chillicream.com/">
              Open Slack
            </OutlineButton>
          </div>
        </div>
      </div>
    </section>
  );
}

export default function ResourcesBuilderLibraryPage() {
  return (
    <>
      <ResourcesHero />
      <JumpNav />
      {GROUPS.map((group) => (
        <GroupBlock key={group.id} group={group} />
      ))}
      <ContactBand />
    </>
  );
}

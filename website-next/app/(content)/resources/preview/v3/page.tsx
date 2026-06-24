import type { Metadata } from "next";
import Link from "next/link";
import type { ReactNode } from "react";

import { OutlineButton, SolidButton } from "@/src/design-system/Button";

export const metadata: Metadata = {
  title: "Resources Directory",
  description:
    "A dense directory of every ChilliCream resource: product, docs, community, company, and legal links. One row per link for people who know what they want.",
  keywords: [
    "ChilliCream resources",
    "GraphQL platform directory",
    "Hot Chocolate links",
    "Nitro",
    "developer community",
  ],
  openGraph: {
    title: "Resources Directory",
    description:
      "A dense directory of every ChilliCream resource: product, docs, community, company, and legal links. One row per link for people who know what they want.",
  },
  robots: { index: false, follow: false },
};

/* -------------------------------------------------------------------------- */
/*  Directory data                                                            */
/*  One flat list, grouped by `group`. Order inside a group is the order      */
/*  rendered. Keep entries terse: one mono name, one short line.              */
/* -------------------------------------------------------------------------- */

type Group = "Product" | "Docs" | "Community" | "Company" | "Legal";

interface Entry {
  readonly group: Group;
  readonly name: string;
  readonly href: string;
  readonly description: string;
  readonly kind: "internal" | "external" | "mailto";
}

const ENTRIES: readonly Entry[] = [
  // Product
  {
    group: "Product",
    name: "platform",
    href: "/platform",
    description: "The Fusion platform overview, from gateway to insights.",
    kind: "internal",
  },
  {
    group: "Product",
    name: "products/hotchocolate",
    href: "/products/hotchocolate",
    description: "Hot Chocolate, the .NET GraphQL server.",
    kind: "internal",
  },
  {
    group: "Product",
    name: "products/nitro",
    href: "/products/nitro",
    description: "Nitro, the operations and observability cockpit.",
    kind: "internal",
  },
  {
    group: "Product",
    name: "products/strawberryshake",
    href: "/products/strawberryshake",
    description: "Strawberry Shake, the typed .NET GraphQL client.",
    kind: "internal",
  },
  {
    group: "Product",
    name: "nitro.chillicream.com",
    href: "https://nitro.chillicream.com",
    description: "Sign in to Nitro.",
    kind: "external",
  },
  {
    group: "Product",
    name: "pricing",
    href: "/pricing",
    description: "Plans and what is included.",
    kind: "internal",
  },
  {
    group: "Product",
    name: "platform/ecosystem",
    href: "/platform/ecosystem",
    description: "How the platform fits with the wider GraphQL ecosystem.",
    kind: "internal",
  },

  // Docs
  {
    group: "Docs",
    name: "docs",
    href: "/docs",
    description: "All product docs, guides, and references.",
    kind: "internal",
  },
  {
    group: "Docs",
    name: "blog",
    href: "/blog",
    description: "Engineering posts, releases, and deep dives.",
    kind: "internal",
  },
  {
    group: "Docs",
    name: "github.com/ChilliCream/graphql-platform",
    href: "https://github.com/ChilliCream/graphql-platform",
    description: "The monorepo: source, issues, releases.",
    kind: "external",
  },

  // Community
  {
    group: "Community",
    name: "slack.chillicream.com",
    href: "https://slack.chillicream.com/",
    description: "Community Slack: ask, share, lurk.",
    kind: "external",
  },
  {
    group: "Community",
    name: "youtube.com/c/ChilliCream",
    href: "https://www.youtube.com/c/ChilliCream",
    description: "Talks, walkthroughs, release recordings.",
    kind: "external",
  },
  {
    group: "Community",
    name: "x.com/Chilli_Cream",
    href: "https://x.com/Chilli_Cream",
    description: "Short-form updates on X.",
    kind: "external",
  },
  {
    group: "Community",
    name: "linkedin.com/company/chillicream",
    href: "https://www.linkedin.com/company/chillicream",
    description: "Company news on LinkedIn.",
    kind: "external",
  },

  // Company
  {
    group: "Company",
    name: "services",
    href: "/services",
    description: "Advisory, support, and training engagements.",
    kind: "internal",
  },
  {
    group: "Company",
    name: "help",
    href: "/help",
    description: "How to get help, by channel and severity.",
    kind: "internal",
  },
  {
    group: "Company",
    name: "store.chillicream.com",
    href: "https://store.chillicream.com",
    description: "ChilliCream merch and goodies.",
    kind: "external",
  },
  {
    group: "Company",
    name: "contact@chillicream.com",
    href: "mailto:contact@chillicream.com",
    description: "Email the team.",
    kind: "mailto",
  },

  // Legal
  {
    group: "Legal",
    name: "legal/acceptable-use-policy",
    href: "/legal/acceptable-use-policy",
    description: "Rules for using ChilliCream services.",
    kind: "internal",
  },
  {
    group: "Legal",
    name: "legal/cookie-policy",
    href: "/legal/cookie-policy",
    description: "How we use cookies.",
    kind: "internal",
  },
  {
    group: "Legal",
    name: "legal/privacy-policy",
    href: "/legal/privacy-policy",
    description: "How we handle your data.",
    kind: "internal",
  },
  {
    group: "Legal",
    name: "legal/terms-of-service",
    href: "/legal/terms-of-service",
    description: "The agreement between you and us.",
    kind: "internal",
  },
  {
    group: "Legal",
    name: "licensing/chillicream-license",
    href: "/licensing/chillicream-license",
    description: "Commercial license terms.",
    kind: "internal",
  },
];

const GROUP_ORDER: readonly Group[] = [
  "Product",
  "Docs",
  "Community",
  "Company",
  "Legal",
];

function entriesByGroup(group: Group): readonly Entry[] {
  return ENTRIES.filter((entry) => entry.group === group);
}

/* -------------------------------------------------------------------------- */
/*  Brand spectrum: a single gradient event, used only on the hero rule.      */
/* -------------------------------------------------------------------------- */

const SPECTRUM = "linear-gradient(90deg, #16b9e4, #7c92c6, #f0786a)";

/* -------------------------------------------------------------------------- */
/*  Inline chrome                                                             */
/* -------------------------------------------------------------------------- */

interface EyebrowProps {
  readonly children: ReactNode;
}

function Eyebrow({ children }: EyebrowProps) {
  return (
    <p className="text-cc-nav-label font-mono text-[0.65rem] font-semibold tracking-[0.22em] uppercase">
      {children}
    </p>
  );
}

interface KindGlyphProps {
  readonly kind: Entry["kind"];
}

function KindGlyph({ kind }: KindGlyphProps) {
  if (kind === "external") {
    return (
      <svg
        viewBox="0 0 12 12"
        width={11}
        height={11}
        className="text-cc-ink-dim shrink-0"
        aria-hidden="true"
      >
        <path
          d="M4 2 H10 V8"
          fill="none"
          stroke="currentColor"
          strokeWidth="1.4"
          strokeLinecap="round"
          strokeLinejoin="round"
        />
        <path
          d="M10 2 L4.5 7.5"
          fill="none"
          stroke="currentColor"
          strokeWidth="1.4"
          strokeLinecap="round"
        />
        <path
          d="M2 5 V10 H8"
          fill="none"
          stroke="currentColor"
          strokeWidth="1.4"
          strokeLinecap="round"
          strokeLinejoin="round"
        />
      </svg>
    );
  }

  if (kind === "mailto") {
    return (
      <svg
        viewBox="0 0 12 12"
        width={11}
        height={11}
        className="text-cc-ink-dim shrink-0"
        aria-hidden="true"
      >
        <rect
          x="1.5"
          y="3"
          width="9"
          height="6.5"
          rx="1"
          fill="none"
          stroke="currentColor"
          strokeWidth="1.2"
        />
        <path
          d="M1.8 3.5 L6 7 L10.2 3.5"
          fill="none"
          stroke="currentColor"
          strokeWidth="1.2"
          strokeLinecap="round"
          strokeLinejoin="round"
        />
      </svg>
    );
  }

  return (
    <svg
      viewBox="0 0 12 12"
      width={11}
      height={11}
      className="text-cc-ink-dim shrink-0"
      aria-hidden="true"
    >
      <path
        d="M3 6 H9"
        fill="none"
        stroke="currentColor"
        strokeWidth="1.4"
        strokeLinecap="round"
      />
      <path
        d="M6.5 3.5 L9 6 L6.5 8.5"
        fill="none"
        stroke="currentColor"
        strokeWidth="1.4"
        strokeLinecap="round"
        strokeLinejoin="round"
      />
    </svg>
  );
}

/* -------------------------------------------------------------------------- */
/*  Row + Group                                                               */
/*  No card chrome. Whole row is clickable. Hairline divider at the bottom.   */
/* -------------------------------------------------------------------------- */

interface RowProps {
  readonly entry: Entry;
  readonly index: string;
}

function Row({ entry, index }: RowProps) {
  const isInternal = entry.kind === "internal";
  const isMailto = entry.kind === "mailto";

  const className =
    "group border-cc-card-border hover:bg-cc-card-bg grid grid-cols-[2.25rem_minmax(0,1fr)] items-baseline gap-x-4 gap-y-1 border-b px-2 py-3 no-underline transition-colors sm:grid-cols-[2.25rem_minmax(0,22rem)_minmax(0,1fr)] sm:gap-y-0 sm:py-2.5";

  const body = (
    <>
      <span className="text-cc-ink-dim font-mono text-[0.7rem] tracking-wider tabular-nums">
        {index}
      </span>
      <span className="flex min-w-0 items-center gap-2 font-mono text-sm">
        <KindGlyph kind={entry.kind} />
        <span className="text-cc-ink group-hover:text-cc-accent truncate">
          {entry.name}
        </span>
      </span>
      <span className="text-cc-ink-dim col-start-2 text-sm sm:col-start-3">
        {entry.description}
      </span>
    </>
  );

  if (isInternal) {
    return (
      <Link href={entry.href} className={className}>
        {body}
      </Link>
    );
  }

  if (isMailto) {
    return (
      <a href={entry.href} className={className}>
        {body}
      </a>
    );
  }

  return (
    <a
      href={entry.href}
      target="_blank"
      rel="noopener noreferrer"
      className={className}
    >
      {body}
    </a>
  );
}

interface GroupBlockProps {
  readonly group: Group;
  readonly startIndex: number;
  readonly count: number;
}

function GroupBlock({ group, startIndex, count }: GroupBlockProps) {
  const rows = entriesByGroup(group);

  return (
    <section className="pt-10">
      <header className="border-cc-card-border mb-2 flex items-baseline justify-between border-b px-2 pb-2">
        <Eyebrow>{group}</Eyebrow>
        <span className="text-cc-ink-dim font-mono text-[0.65rem] tracking-widest tabular-nums">
          {String(count).padStart(2, "0")} entries
        </span>
      </header>
      <div role="list">
        {rows.map((entry, i) => (
          <Row
            key={entry.href}
            entry={entry}
            index={String(startIndex + i).padStart(2, "0")}
          />
        ))}
      </div>
    </section>
  );
}

/* -------------------------------------------------------------------------- */
/*  Page                                                                      */
/* -------------------------------------------------------------------------- */

export default function ResourcesDirectoryPage() {
  const total = ENTRIES.length;
  const counts = GROUP_ORDER.map((g) => entriesByGroup(g).length);
  const offsets = counts.reduce<number[]>((acc, _c, i) => {
    if (i === 0) {
      acc.push(1);
    } else {
      acc.push(acc[i - 1] + counts[i - 1]);
    }
    return acc;
  }, []);

  return (
    <>
      {/* Hero */}
      <section className="pt-16 pb-10 sm:pt-24 sm:pb-14">
        <Eyebrow>Resources / Directory</Eyebrow>
        <h1 className="text-cc-heading font-heading mt-3 text-5xl leading-tight font-semibold tracking-tight sm:text-6xl">
          Everything,
          <br />
          one row each.
        </h1>
        <p className="text-cc-ink-dim lead mt-6 max-w-2xl">
          A flat index of every ChilliCream link worth bookmarking. Product,
          docs, community, company, legal. No cards, no carousels. Skim the left
          column, click the line you want.
        </p>

        <div
          className="mt-8 h-px w-full"
          style={{ backgroundImage: SPECTRUM }}
          aria-hidden="true"
        />

        <div className="mt-8 flex flex-wrap items-center gap-3">
          <SolidButton href="#product">Jump to product</SolidButton>
          <OutlineButton href="https://github.com/ChilliCream/graphql-platform">
            Open the repo
          </OutlineButton>
          <span className="text-cc-ink-dim ml-1 font-mono text-xs tracking-widest tabular-nums">
            {String(total).padStart(2, "0")} entries / {GROUP_ORDER.length}{" "}
            sections
          </span>
        </div>
      </section>

      {/* Sticky-ish quick jump strip */}
      <nav
        aria-label="Sections"
        className="border-cc-card-border bg-cc-surface/60 sticky top-0 z-10 -mx-5 flex flex-wrap gap-x-5 gap-y-1 border-y px-5 py-3 backdrop-blur sm:-mx-12 sm:px-12"
      >
        {GROUP_ORDER.map((g, i) => (
          <a
            key={g}
            href={`#${g.toLowerCase()}`}
            className="text-cc-ink-dim hover:text-cc-accent font-mono text-xs tracking-widest uppercase no-underline"
          >
            <span className="text-cc-ink-dim/70 mr-1.5 tabular-nums">
              {String(i + 1).padStart(2, "0")}
            </span>
            {g}
          </a>
        ))}
      </nav>

      {/* Directory */}
      <div className="pb-16">
        {GROUP_ORDER.map((g, i) => (
          <div key={g} id={g.toLowerCase()}>
            <GroupBlock group={g} startIndex={offsets[i]} count={counts[i]} />
          </div>
        ))}
      </div>

      {/* Contact band */}
      <section
        aria-labelledby="contact-heading"
        className="border-cc-card-border mb-16 grid gap-6 border-y py-10 sm:grid-cols-[minmax(0,1fr)_auto] sm:items-center"
      >
        <div>
          <Eyebrow>Contact</Eyebrow>
          <h2
            id="contact-heading"
            className="text-cc-heading font-heading mt-2 text-3xl font-semibold tracking-tight sm:text-4xl"
          >
            Missing a link? Tell us.
          </h2>
          <p className="text-cc-ink-dim mt-3 max-w-xl text-sm sm:text-base">
            This directory is the index, not the help desk. For product
            questions head to Slack or the docs. For everything else, email the
            team.
          </p>
        </div>
        <div className="flex flex-wrap gap-3 sm:justify-end">
          <SolidButton href="mailto:contact@chillicream.com">
            contact@chillicream.com
          </SolidButton>
          <OutlineButton href="https://slack.chillicream.com/">
            Open Slack
          </OutlineButton>
        </div>
      </section>

      {/* Footer note */}
      <section className="border-cc-card-border mb-8 border-t pt-6">
        <p className="text-cc-ink-dim font-mono text-[0.7rem] tracking-widest uppercase">
          End of directory.{" "}
          <Link
            href="/resources"
            className="text-cc-ink hover:text-cc-accent no-underline"
          >
            Back to /resources
          </Link>
        </p>
      </section>
    </>
  );
}

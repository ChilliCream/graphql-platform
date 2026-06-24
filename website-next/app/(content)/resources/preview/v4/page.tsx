import type { Metadata } from "next";
import Link from "next/link";
import type { ReactNode } from "react";

export const metadata: Metadata = {
  title: "The Builder Almanac | ChilliCream Resources",
  description:
    "ChilliCream resources read as one long-form essay: docs, blog, videos, Slack, GitHub, contact, shop, and legal, set in chapters with hairline rules and generous body type.",
  keywords: [
    "ChilliCream resources",
    "GraphQL docs",
    "Hot Chocolate",
    "Nitro",
    "ChilliCream community",
    "ChilliCream legal",
  ],
  openGraph: {
    title: "The Builder Almanac | ChilliCream Resources",
    description:
      "Docs, blog, videos, Slack, GitHub, contact, shop, and legal. The ChilliCream builder library, set in long form.",
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

interface Chapter {
  readonly id: string;
  readonly number: string;
  readonly label: string;
  readonly heading: string;
  readonly intro: string;
  readonly entries: readonly Entry[];
}

function DocsGlyph() {
  return (
    <svg viewBox="0 0 24 24" width={16} height={16} aria-hidden="true">
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
    <svg viewBox="0 0 24 24" width={16} height={16} aria-hidden="true">
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
    <svg viewBox="0 0 24 24" width={16} height={16} aria-hidden="true">
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
    <svg viewBox="0 0 24 24" width={16} height={16} aria-hidden="true">
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
    <svg viewBox="0 0 24 24" width={16} height={16} aria-hidden="true">
      <path
        d="M12 3a9 9 0 0 0-2.85 17.54c.45.08.61-.2.61-.43v-1.7c-2.5.55-3.03-1.06-3.03-1.06-.41-1.04-1-1.32-1-1.32-.82-.56.06-.55.06-.55.9.06 1.37.93 1.37.93.8 1.37 2.11.98 2.62.75.08-.58.31-.98.57-1.2-2-.23-4.1-1-4.1-4.45 0-.98.35-1.78.93-2.41-.09-.23-.4-1.15.09-2.4 0 0 .75-.24 2.47.92a8.5 8.5 0 0 1 4.49 0c1.72-1.16 2.47-.92 2.47-.92.49 1.25.18 2.17.09 2.4.58.63.93 1.43.93 2.41 0 3.46-2.1 4.22-4.11 4.44.32.28.6.83.6 1.68v2.49c0 .24.16.52.62.43A9 9 0 0 0 12 3Z"
        fill="currentColor"
      />
    </svg>
  );
}

function XGlyph() {
  return (
    <svg viewBox="0 0 24 24" width={16} height={16} aria-hidden="true">
      <path
        d="M4 4h3.6l4.5 6.3L17 4h3l-6.3 8.4L20.5 20H17l-4.8-6.7L7 20H4l6.6-8.8L4 4Z"
        fill="currentColor"
      />
    </svg>
  );
}

function LinkedInGlyph() {
  return (
    <svg viewBox="0 0 24 24" width={16} height={16} aria-hidden="true">
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
    <svg viewBox="0 0 24 24" width={16} height={16} aria-hidden="true">
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
    <svg viewBox="0 0 24 24" width={16} height={16} aria-hidden="true">
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
    <svg viewBox="0 0 24 24" width={16} height={16} aria-hidden="true">
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
    <svg viewBox="0 0 24 24" width={16} height={16} aria-hidden="true">
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
    <svg viewBox="0 0 24 24" width={16} height={16} aria-hidden="true">
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
    <svg viewBox="0 0 24 24" width={16} height={16} aria-hidden="true">
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
    <svg viewBox="0 0 24 24" width={16} height={16} aria-hidden="true">
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

const CHAPTERS: readonly Chapter[] = [
  {
    id: "learn",
    number: "No. 01",
    label: "// chapter one",
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
    number: "No. 02",
    label: "// chapter two",
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
    number: "No. 03",
    label: "// chapter three",
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
    number: "No. 04",
    label: "// chapter four",
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

function isExternalHref(href: string) {
  return /^https?:/.test(href);
}

interface EntryRowProps {
  readonly entry: Entry;
}

function EntryRow({ entry }: EntryRowProps) {
  const inner = (
    <>
      <span
        aria-hidden="true"
        className="text-cc-ink-faint group-hover:text-cc-accent mt-1 inline-flex w-4 shrink-0 items-start justify-start transition-colors"
      >
        {entry.icon}
      </span>
      <span className="min-w-0 flex-1">
        <span className="flex items-baseline gap-2">
          <span className="text-cc-heading group-hover:text-cc-accent text-h5 font-heading font-semibold tracking-tight transition-colors">
            {entry.title}
          </span>
          <span
            aria-hidden="true"
            className="text-cc-accent inline-block translate-x-[-4px] opacity-0 transition-all duration-200 group-hover:translate-x-0 group-hover:opacity-100"
          >
            &rarr;
          </span>
        </span>
        <span className="text-cc-ink-dim text-body mt-2 block leading-relaxed">
          {entry.description}
        </span>
        <span className="text-cc-nav-label mt-3 block font-mono text-[11px] tracking-widest uppercase">
          {entry.meta}
        </span>
      </span>
    </>
  );

  const rowClass =
    "group border-cc-card-border/40 flex items-start gap-4 border-t py-6 no-underline";

  if (entry.href.startsWith("/")) {
    return (
      <Link href={entry.href} className={rowClass}>
        {inner}
      </Link>
    );
  }

  if (isExternalHref(entry.href)) {
    return (
      <a
        href={entry.href}
        target="_blank"
        rel="noopener noreferrer"
        className={rowClass}
      >
        {inner}
      </a>
    );
  }

  return (
    <a href={entry.href} className={rowClass}>
      {inner}
    </a>
  );
}

interface ChapterBlockProps {
  readonly chapter: Chapter;
  readonly showDropCap?: boolean;
  readonly pullQuote?: { readonly text: string };
}

function ChapterBlock({ chapter, showDropCap, pullQuote }: ChapterBlockProps) {
  return (
    <section
      id={chapter.id}
      aria-labelledby={`${chapter.id}-heading`}
      className="relative mt-20 sm:mt-24"
    >
      <div className="border-cc-card-border/60 border-t pt-6">
        <div className="text-cc-nav-label font-mono text-[11px] tracking-widest uppercase">
          <span>{chapter.number}</span>
          <span className="text-cc-ink-faint"> / </span>
          <span>{chapter.heading.toLowerCase()}</span>
        </div>
        <h2
          id={`${chapter.id}-heading`}
          className="text-cc-heading text-h2 font-heading mt-4 font-semibold tracking-tight"
        >
          {chapter.heading}
        </h2>
      </div>

      <p className="text-cc-ink text-lead mt-8 font-sans font-normal">
        {showDropCap ? (
          <>
            <span
              aria-hidden="true"
              className="text-cc-accent text-h2 font-heading float-left mt-1 mr-3 leading-none font-semibold"
            >
              {chapter.intro.charAt(0)}
            </span>
            {chapter.intro.slice(1)}
          </>
        ) : (
          chapter.intro
        )}
      </p>

      <div className="mt-10">
        {chapter.entries.map((entry) => (
          <EntryRow key={entry.href} entry={entry} />
        ))}
        <div className="border-cc-card-border/40 border-t" />
      </div>

      {pullQuote ? (
        <blockquote className="border-cc-accent text-cc-heading font-heading mt-12 border-l-2 pl-6 text-[1.5rem] leading-snug font-semibold italic sm:text-[1.75rem]">
          {pullQuote.text}
        </blockquote>
      ) : null}
    </section>
  );
}

interface RunningHeadProps {
  readonly chapters: readonly Chapter[];
}

function RunningHead({ chapters }: RunningHeadProps) {
  return (
    <aside
      aria-label="Running head"
      className="text-cc-nav-label mt-12 lg:sticky lg:top-32 lg:mt-0 lg:self-start"
    >
      <div className="border-cc-card-border/60 border-t pt-4">
        <div className="font-mono text-[11px] tracking-widest uppercase">
          {"// running head"}
        </div>
        <ol className="mt-4 space-y-3 font-mono text-[11px] tracking-widest uppercase">
          {chapters.map((c) => (
            <li key={c.id}>
              <a
                href={`#${c.id}`}
                className="hover:text-cc-accent block no-underline transition-colors"
              >
                <span className="text-cc-ink-faint">{c.number}</span>
                <span className="text-cc-ink-dim ml-2">{c.heading}</span>
              </a>
            </li>
          ))}
        </ol>
        <p className="text-cc-ink-faint mt-6 font-mono text-[10px] leading-relaxed tracking-widest uppercase">
          {"// a long-form"}
          <br />
          builder almanac
        </p>
      </div>
    </aside>
  );
}

function Masthead() {
  return (
    <div className="pt-16 sm:pt-24">
      <div className="flex flex-wrap items-baseline justify-between gap-3 font-mono text-[11px] tracking-widest uppercase">
        <span className="text-cc-nav-label">
          {"// resources / the builder almanac"}
        </span>
        <span className="text-cc-ink-faint">
          Vol. 01 / ChilliCream Editorial
        </span>
      </div>
    </div>
  );
}

function Hero() {
  return (
    <section aria-labelledby="page-heading" className="pt-20 pb-10 sm:pt-32">
      <h1
        id="page-heading"
        className="text-cc-heading text-hero font-heading font-semibold tracking-tight text-balance"
      >
        The builder library, set in long form.
      </h1>
      <p className="text-cc-ink-dim text-lead mt-12 max-w-[36rem] font-sans font-normal">
        An unhurried index of every link an engineer working on ChilliCream
        actually opens. Docs, blog, videos, Slack, GitHub, contact, shop, and
        the legal documents that govern the work. No brochure, no fluff, just
        the almanac.
      </p>
    </section>
  );
}

function OpeningColophon() {
  return (
    <section
      aria-labelledby="prologue-heading"
      className="border-cc-card-border/60 mt-12 border-t pt-10"
    >
      <h2
        id="prologue-heading"
        className="text-cc-nav-label font-mono text-[11px] tracking-widest uppercase"
      >
        {"// prologue / about this page"}
      </h2>
      <p className="text-cc-ink text-body mt-6 leading-relaxed">
        <span
          aria-hidden="true"
          className="text-cc-accent text-h2 font-heading float-left mt-1 mr-3 leading-none font-semibold"
        >
          T
        </span>
        his page is the ChilliCream resource hub, kept in essay form so that the
        order of things stays visible. The four chapters ahead cover Learn,
        Community, Company, and Legal. Each entry is one link, one sentence of
        context, and a mono meta line so you know exactly where the click goes.
        Nothing here is invented; everything points to a live, current document.
      </p>
    </section>
  );
}

function ColophonFooter() {
  return (
    <footer
      aria-labelledby="colophon-heading"
      className="border-cc-card-border/60 mt-24 border-t pt-10 pb-24"
    >
      <h2
        id="colophon-heading"
        className="text-cc-nav-label font-mono text-[11px] tracking-widest uppercase"
      >
        {"// colophon"}
      </h2>
      <p className="text-cc-ink text-body mt-6 leading-relaxed">
        Maintained by the ChilliCream team. Last revised on publication. The
        almanac is a single page, a single column, and a single source of truth.
        If a link here breaks, or a chapter feels thin, write to us and we will
        fix it in the next revision.
      </p>
      <p className="text-cc-ink-dim text-body mt-6 leading-relaxed">
        Reach the team at{" "}
        <a
          href="mailto:contact@chillicream.com"
          className="text-cc-accent hover:text-cc-accent-hover underline-offset-4 hover:underline"
        >
          contact@chillicream.com
        </a>
        , or open the room at{" "}
        <a
          href="https://slack.chillicream.com/"
          target="_blank"
          rel="noopener noreferrer"
          className="text-cc-accent hover:text-cc-accent-hover underline-offset-4 hover:underline"
        >
          slack.chillicream.com
        </a>
        .
      </p>
      <p className="text-cc-nav-label mt-10 font-mono text-[11px] tracking-widest uppercase">
        {"// end of file"}
      </p>
    </footer>
  );
}

export default function ResourcesBuilderAlmanacPage() {
  const [learn, community, company, legal] = CHAPTERS;
  return (
    <div className="lg:grid lg:grid-cols-[minmax(0,42rem)_minmax(0,14rem)] lg:gap-16 xl:gap-24">
      <article className="mx-auto w-full max-w-[42rem]">
        <Masthead />
        <Hero />
        <OpeningColophon />
        <ChapterBlock chapter={learn} showDropCap />
        <ChapterBlock
          chapter={community}
          pullQuote={{ text: "Slack is the fastest path to a human." }}
        />
        <ChapterBlock chapter={company} />
        <ChapterBlock chapter={legal} />
        <ColophonFooter />
      </article>
      <RunningHead chapters={CHAPTERS} />
    </div>
  );
}

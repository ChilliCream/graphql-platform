"use client";

import Link from "next/link";
import type { ReactNode } from "react";

import { motion, useReducedMotion } from "motion/react";

import { OutlineButton, SolidButton } from "@/src/design-system/Button";

// ---------------------------------------------------------------------------
// Resource Ticker
// The resources hub reframed as a live broadcast strip. Two opposing marquees
// run the channel names up top, a slow marquee acts as a table of contents,
// and each "story" the ticker references is anchored by a structured card grid
// of real ChilliCream links below. All motion is time-driven and infinite,
// never coupled to scroll position, and collapses to static when the visitor
// prefers reduced motion.
// ---------------------------------------------------------------------------

const CYAN = "#16b9e4";

interface Entry {
  readonly href: string;
  readonly title: string;
  readonly description: string;
  readonly meta: string;
  readonly external?: boolean;
  readonly icon: ReactNode;
}

interface Channel {
  readonly id: string;
  readonly number: string;
  readonly heading: string;
  readonly intro: string;
  readonly entries: readonly Entry[];
}

// --- Icons (inline SVG, inherit currentColor) ------------------------------

function DocsGlyph() {
  return (
    <svg viewBox="0 0 24 24" width={22} height={22} aria-hidden="true">
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
    <svg viewBox="0 0 24 24" width={22} height={22} aria-hidden="true">
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
    <svg viewBox="0 0 24 24" width={22} height={22} aria-hidden="true">
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
    <svg viewBox="0 0 24 24" width={22} height={22} aria-hidden="true">
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
    <svg viewBox="0 0 24 24" width={22} height={22} aria-hidden="true">
      <path
        d="M12 3a9 9 0 0 0-2.85 17.54c.45.08.61-.2.61-.43v-1.7c-2.5.55-3.03-1.06-3.03-1.06-.41-1.04-1-1.32-1-1.32-.82-.56.06-.55.06-.55.9.06 1.37.93 1.37.93.8 1.37 2.11.98 2.62.75.08-.58.31-.98.57-1.2-2-.23-4.1-1-4.1-4.45 0-.98.35-1.78.93-2.41-.09-.23-.4-1.15.09-2.4 0 0 .75-.24 2.47.92a8.5 8.5 0 0 1 4.49 0c1.72-1.16 2.47-.92 2.47-.92.49 1.25.18 2.17.09 2.4.58.63.93 1.43.93 2.41 0 3.46-2.1 4.22-4.11 4.44.32.28.6.83.6 1.68v2.49c0 .24.16.52.62.43A9 9 0 0 0 12 3Z"
        fill="currentColor"
      />
    </svg>
  );
}

function XGlyph() {
  return (
    <svg viewBox="0 0 24 24" width={22} height={22} aria-hidden="true">
      <path
        d="M4 4h3.6l4.5 6.3L17 4h3l-6.3 8.4L20.5 20H17l-4.8-6.7L7 20H4l6.6-8.8L4 4Z"
        fill="currentColor"
      />
    </svg>
  );
}

function LinkedInGlyph() {
  return (
    <svg viewBox="0 0 24 24" width={22} height={22} aria-hidden="true">
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
    <svg viewBox="0 0 24 24" width={22} height={22} aria-hidden="true">
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
    <svg viewBox="0 0 24 24" width={22} height={22} aria-hidden="true">
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
    <svg viewBox="0 0 24 24" width={22} height={22} aria-hidden="true">
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
    <svg viewBox="0 0 24 24" width={22} height={22} aria-hidden="true">
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
    <svg viewBox="0 0 24 24" width={22} height={22} aria-hidden="true">
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
    <svg viewBox="0 0 24 24" width={22} height={22} aria-hidden="true">
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
    <svg viewBox="0 0 24 24" width={22} height={22} aria-hidden="true">
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

// --- Data (real ChilliCream links, reused verbatim from v1) ----------------

const CHANNELS: readonly Channel[] = [
  {
    id: "learn",
    number: "CH 01",
    heading: "Learn",
    intro:
      "Long-form material maintained by the team. Tune in here when you need depth, not a tweet.",
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
    number: "CH 02",
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
    number: "CH 03",
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
    number: "CH 04",
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

// --- Marquee primitive (time-driven, not scroll-coupled) -------------------

interface MarqueeProps {
  readonly items: readonly string[];
  readonly duration: number;
  readonly reverse?: boolean;
  readonly reduced: boolean;
  readonly className?: string;
  readonly bulletClassName?: string;
}

function Marquee({
  items,
  duration,
  reverse,
  reduced,
  className,
  bulletClassName,
}: MarqueeProps) {
  // The track is rendered twice back to back so a -50% shift lands exactly on
  // the seam of the duplicate, producing a seamless loop.
  const track = (
    <div className="flex shrink-0 items-center" aria-hidden="true">
      {items.map((item, i) => (
        <span key={i} className="flex items-center">
          <span className={className}>{item}</span>
          <span className={`mx-5 ${bulletClassName ?? "text-cc-ink-faint"}`}>
            &#9670;
          </span>
        </span>
      ))}
    </div>
  );

  const from = reverse ? "-50%" : "0%";
  const to = reverse ? "0%" : "-50%";

  return (
    <div className="relative flex w-full overflow-hidden">
      <motion.div
        className="flex w-max"
        animate={reduced ? undefined : { x: [from, to] }}
        transition={
          reduced
            ? undefined
            : {
                duration,
                ease: "linear",
                repeat: Infinity,
                repeatType: "loop",
              }
        }
      >
        {track}
        {track}
      </motion.div>
    </div>
  );
}

// --- Sections --------------------------------------------------------------

const RAIL_A = [
  "// DOCS",
  "// SLACK",
  "// GITHUB",
  "// YOUTUBE",
  "// SHOP",
  "// LEGAL",
  "// CONTACT",
];

const RAIL_B = [
  "MIT-LICENSED OSS",
  "REAL LINKS ONLY",
  "VERSIONED DOCS",
  "OPEN SOURCE PLATFORM",
  "MAINTAINED BY HUMANS",
  "ONE HUB",
];

function BroadcastBar({ reduced }: { readonly reduced: boolean }) {
  return (
    <div className="relative -mx-5 sm:-mx-12">
      <div
        aria-hidden="true"
        className="pointer-events-none absolute inset-x-0 top-1/2 -z-10 h-24 -translate-y-1/2 blur-2xl"
        style={{
          background: `radial-gradient(circle, rgba(22,185,228,0.16), transparent 70%)`,
        }}
      />
      <div className="border-cc-card-border bg-cc-surface/80 border-y backdrop-blur-sm">
        <div className="border-cc-card-border/60 flex items-center border-b py-3">
          <span className="text-cc-bg ml-5 hidden shrink-0 rounded-sm bg-[#16b9e4] px-2 py-0.5 font-mono text-[10px] font-bold tracking-widest uppercase sm:inline-block">
            Live
          </span>
          <div className="min-w-0 flex-1">
            <Marquee
              items={RAIL_A}
              duration={38}
              reduced={reduced}
              className="text-cc-heading font-mono text-xs tracking-[0.25em] whitespace-nowrap uppercase"
              bulletClassName="text-[#16b9e4]"
            />
          </div>
        </div>
        <div className="flex items-center py-3">
          <div className="min-w-0 flex-1">
            <Marquee
              items={RAIL_B}
              duration={46}
              reverse
              reduced={reduced}
              className="text-cc-ink-dim font-mono text-xs tracking-[0.25em] whitespace-nowrap uppercase"
            />
          </div>
          <div className="mr-5 flex shrink-0 items-center gap-2">
            <motion.span
              className="inline-block h-2 w-2 rounded-full bg-[#16b9e4]"
              animate={reduced ? undefined : { opacity: [1, 0.4, 1] }}
              transition={
                reduced
                  ? undefined
                  : { duration: 4, ease: "easeInOut", repeat: Infinity }
              }
            />
            <span className="text-cc-nav-label font-mono text-[10px] tracking-[0.3em] uppercase">
              On Air
            </span>
          </div>
        </div>
      </div>
    </div>
  );
}

function TunerDial() {
  // A stylized frequency dial built from inline SVG ticks, with the active
  // band labelled "/resources". Decorative only.
  const ticks = Array.from({ length: 41 }, (_, i) => i);
  const activeIndex = 27;
  return (
    <div
      aria-hidden="true"
      className="border-cc-card-border bg-cc-surface/70 relative overflow-hidden rounded-2xl border p-6 shadow-inner sm:p-8"
    >
      <div
        className="pointer-events-none absolute -top-16 -right-12 h-48 w-48 rounded-full blur-3xl"
        style={{
          background: `radial-gradient(circle, rgba(22,185,228,0.16), transparent 70%)`,
        }}
      />
      <div className="text-cc-nav-label flex items-center justify-between font-mono text-[10px] tracking-[0.3em] uppercase">
        <span>Tuner</span>
        <span className="text-[#16b9e4]">88.0 &ndash; 108.0</span>
      </div>
      <svg
        viewBox="0 0 320 120"
        className="mt-5 w-full"
        role="presentation"
        focusable="false"
      >
        <line
          x1="8"
          y1="74"
          x2="312"
          y2="74"
          stroke="currentColor"
          className="text-cc-card-border"
          strokeWidth="1"
        />
        {ticks.map((i) => {
          const x = 8 + (i * 304) / 40;
          const major = i % 5 === 0;
          const active = i === activeIndex;
          return (
            <line
              key={i}
              x1={x}
              y1={major ? 56 : 64}
              x2={x}
              y2={74}
              stroke={active ? CYAN : "currentColor"}
              className={active ? "" : "text-cc-ink-faint"}
              strokeWidth={active ? 2.5 : major ? 1.5 : 1}
            />
          );
        })}
        <circle cx={8 + (activeIndex * 304) / 40} cy={48} r="3.5" fill={CYAN} />
        <line
          x1={8 + (activeIndex * 304) / 40}
          y1={48}
          x2={8 + (activeIndex * 304) / 40}
          y2={82}
          stroke={CYAN}
          strokeWidth="2.5"
        />
      </svg>
      <div className="mt-5 flex items-center justify-between">
        <span className="text-cc-ink-dim font-mono text-[11px] tracking-widest uppercase">
          Now tuned
        </span>
        <span className="border-cc-card-border bg-cc-bg/60 rounded-md border px-3 py-1 font-mono text-sm tracking-widest text-[#16b9e4]">
          /resources
        </span>
      </div>
    </div>
  );
}

function ResourcesHero() {
  return (
    <section className="relative py-14 sm:py-20">
      <div className="grid items-center gap-10 lg:grid-cols-[1.5fr_1fr] lg:gap-14">
        <div>
          <div className="text-cc-nav-label font-mono text-xs tracking-[0.3em] uppercase">
            {"// resources / on air"}
          </div>
          <h1 className="text-cc-heading text-hero mt-4">
            The ChilliCream resources hub.
          </h1>
          <p className="text-cc-ink-dim text-lead mt-6 max-w-xl">
            Every channel an engineer working on ChilliCream actually opens,
            broadcast across one strip. Docs, blog, videos, Slack, GitHub,
            contact, shop, legal. Read the ticker, then jump to the archive
            below.
          </p>
          <div className="mt-8 flex flex-wrap gap-3">
            <SolidButton href="/docs">Open the docs</SolidButton>
            <OutlineButton href="https://slack.chillicream.com/">
              Join Slack
            </OutlineButton>
          </div>
        </div>
        <TunerDial />
      </div>
    </section>
  );
}

const DIVIDER_ITEMS = [
  "// 01 LEARN",
  "// 02 COMMUNITY",
  "// 03 COMPANY",
  "// 04 LEGAL",
];

function DividerMarquee({ reduced }: { readonly reduced: boolean }) {
  return (
    <div className="border-cc-card-border/60 -mx-5 border-y py-3 sm:-mx-12">
      <Marquee
        items={DIVIDER_ITEMS}
        duration={60}
        reduced={reduced}
        className="text-cc-ink-faint font-mono text-[11px] tracking-[0.4em] whitespace-nowrap uppercase"
      />
    </div>
  );
}

function isExternal(href: string) {
  return /^https?:/.test(href);
}

interface ResourceCardProps {
  readonly entry: Entry;
}

function ResourceCard({ entry }: ResourceCardProps) {
  const cls =
    "group border-cc-card-border bg-cc-card-bg hover:border-cc-card-border-hover relative flex h-full items-start gap-4 rounded-xl border p-5 no-underline transition-colors";

  const body = (
    <>
      <div className="text-cc-ink-dim mt-0.5 shrink-0 transition-colors group-hover:text-[#16b9e4]">
        {entry.icon}
      </div>
      <div className="min-w-0 flex-1">
        <h3 className="text-cc-heading text-h5 font-heading transition-colors group-hover:text-[#16b9e4]">
          {entry.title}
        </h3>
        <p className="text-cc-ink-dim text-body mt-1.5">{entry.description}</p>
        <div className="border-cc-card-border/60 text-cc-nav-label mt-4 flex items-center justify-between gap-3 border-t pt-3 font-mono text-[11px] tracking-wide">
          <span className="truncate">{entry.meta}</span>
          <span className="text-cc-ink-faint shrink-0">
            {entry.external ? "ext" : "→"}
          </span>
        </div>
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

interface ChannelRowProps {
  readonly channel: Channel;
}

function ChannelRow({ channel }: ChannelRowProps) {
  return (
    <section
      id={channel.id}
      aria-labelledby={`${channel.id}-heading`}
      className="border-cc-card-border bg-cc-card-bg/40 mt-6 grid gap-8 rounded-2xl border p-6 sm:p-8 lg:grid-cols-[16rem_1fr] lg:gap-12"
    >
      <div className="lg:sticky lg:top-24 lg:self-start">
        <div className="text-cc-nav-label inline-flex items-center gap-2 font-mono text-[11px] tracking-[0.3em] uppercase">
          <span className="h-1.5 w-1.5 rounded-full bg-[#16b9e4]" />
          {channel.number}
        </div>
        <h2
          id={`${channel.id}-heading`}
          className="text-cc-heading text-h3 font-heading mt-3"
        >
          {channel.heading}
        </h2>
        <p className="text-cc-ink-dim text-body mt-3">{channel.intro}</p>
      </div>
      <div className="grid gap-4 sm:grid-cols-2">
        {channel.entries.map((entry) => (
          <ResourceCard key={entry.href} entry={entry} />
        ))}
      </div>
    </section>
  );
}

const FREQ_HANDLES = [
  "@chillicream on Slack",
  "github.com/ChilliCream",
  "x.com/Chilli_Cream",
  "linkedin.com/company/chillicream",
  "youtube.com/c/ChilliCream",
];

function FrequenciesBand({ reduced }: { readonly reduced: boolean }) {
  return (
    <section
      aria-labelledby="frequencies-heading"
      className="border-cc-card-border bg-cc-surface/60 relative mt-12 overflow-hidden rounded-2xl border p-8 sm:p-10"
    >
      <div className="text-cc-nav-label flex items-center justify-between font-mono text-[11px] tracking-[0.3em] uppercase">
        <span id="frequencies-heading">{"// frequencies"}</span>
        <span className="text-cc-ink-faint">spectrum</span>
      </div>
      {/* Brand spectrum appears exactly once on this decorative band. */}
      <div
        aria-hidden="true"
        className="mt-5 h-1.5 w-full rounded-full"
        style={{
          background: `linear-gradient(90deg, ${CYAN}, #7c92c6, #f0786a)`,
        }}
      />
      <div className="mt-6">
        <Marquee
          items={FREQ_HANDLES}
          duration={50}
          reduced={reduced}
          className="text-cc-ink-dim font-mono text-sm tracking-widest whitespace-nowrap"
        />
      </div>
    </section>
  );
}

function ContactBand() {
  return (
    <section
      aria-labelledby="contact-heading"
      className="border-cc-card-border bg-cc-card-bg/60 relative mt-12 mb-4 overflow-hidden rounded-2xl border p-8 text-center sm:p-12"
    >
      <div className="text-cc-nav-label font-mono text-xs tracking-[0.3em] uppercase">
        {"// contact"}
      </div>
      <div className="border-cc-card-border bg-cc-bg/70 mx-auto mt-5 inline-flex max-w-full items-center gap-2 rounded-lg border px-4 py-2 font-mono text-sm">
        <span className="text-cc-nav-label">$</span>
        <span className="text-[#16b9e4]">mail</span>
        <span className="text-cc-ink truncate">contact@chillicream.com</span>
      </div>
      <p className="text-cc-ink-dim text-body mx-auto mt-6 max-w-2xl">
        Slack is fastest for everything technical and community-facing. For
        commercial questions, partnerships, security reports, or anything that
        does not belong in a public channel, send us mail.
      </p>
      <div className="mt-7 flex flex-wrap justify-center gap-3">
        <SolidButton href="mailto:contact@chillicream.com">
          Email the team
        </SolidButton>
        <OutlineButton href="https://slack.chillicream.com/">
          Open Slack
        </OutlineButton>
      </div>
    </section>
  );
}

export function ClientPage() {
  const reduced = useReducedMotion() ?? false;

  return (
    <div className="relative">
      {/* Faint broadcast-monitor scanline texture behind the whole page. */}
      <div
        aria-hidden="true"
        className="pointer-events-none absolute inset-0 -z-10 opacity-50"
        style={{
          backgroundImage:
            "repeating-linear-gradient(0deg, rgba(22,185,228,0.04) 0px, rgba(22,185,228,0.04) 1px, transparent 1px, transparent 3px)",
        }}
      />

      <BroadcastBar reduced={reduced} />
      <ResourcesHero />
      <DividerMarquee reduced={reduced} />

      {CHANNELS.map((channel) => (
        <ChannelRow key={channel.id} channel={channel} />
      ))}

      <FrequenciesBand reduced={reduced} />
      <ContactBand />
    </div>
  );
}

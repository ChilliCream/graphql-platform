"use client";

import Link from "next/link";
import { useEffect, useMemo, useRef, useState, type ReactNode } from "react";
import {
  animate,
  motion,
  MotionConfig,
  useInView,
  useMotionValue,
  useReducedMotion,
  useTransform,
} from "motion/react";

import { OutlineButton, SolidButton } from "@/src/design-system/Button";

/* -------------------------------------------------------------------------- */
/*  Data (verbatim from v1 ground truth)                                      */
/* -------------------------------------------------------------------------- */

type GroupId = "learn" | "community" | "company" | "legal";

interface Entry {
  readonly href: string;
  readonly title: string;
  readonly description: string;
  readonly meta: string;
  readonly external?: boolean;
  readonly icon: ReactNode;
}

interface Group {
  readonly id: GroupId;
  readonly label: string;
  readonly heading: string;
  readonly intro: string;
  readonly entries: readonly Entry[];
}

/* ---- glyphs (inline SVG, share currentColor) ------------------------------ */

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

/* -------------------------------------------------------------------------- */
/*  Custom event channel: section-enter (rising edge) -> tree branch pulse    */
/* -------------------------------------------------------------------------- */

const SECTION_ENTER_EVENT = "cc-v7-resources-section-enter";

interface ActiveBranchDetail {
  readonly id: GroupId | null;
}

/* -------------------------------------------------------------------------- */
/*  Centerpiece: animated directory tree                                      */
/* -------------------------------------------------------------------------- */

interface BranchSpec {
  readonly id: GroupId;
  readonly label: string;
  readonly leaves: readonly string[];
}

const BRANCHES: readonly BranchSpec[] = [
  {
    id: "learn",
    label: "learn/",
    leaves: ["docs", "blog", "youtube"],
  },
  {
    id: "community",
    label: "community/",
    leaves: ["slack", "github", "x", "linkedin"],
  },
  {
    id: "company",
    label: "company/",
    leaves: ["contact", "shop"],
  },
  {
    id: "legal",
    label: "legal/",
    leaves: ["tos", "privacy", "cookies", "aup", "license"],
  },
];

interface DirectoryTreeProps {
  readonly hoverActive: GroupId | null;
}

function DirectoryTree({ hoverActive }: DirectoryTreeProps) {
  const reduceMotion = useReducedMotion() ?? false;
  const treeRef = useRef<SVGSVGElement>(null);
  const inView = useInView(treeRef, { once: true, amount: 0.4 });
  const [scrollActive, setScrollActive] = useState<GroupId | null>(null);

  // Listen for scroll-spy events emitted by section blocks as they enter view.
  useEffect(() => {
    function onSection(e: Event) {
      const ce = e as CustomEvent<ActiveBranchDetail>;
      setScrollActive(ce.detail.id);
    }
    window.addEventListener(SECTION_ENTER_EVENT, onSection);
    return () => {
      window.removeEventListener(SECTION_ENTER_EVENT, onSection);
    };
  }, []);

  // Hover wins over scroll for the pulse.
  const active: GroupId | null = hoverActive ?? scrollActive;

  // Layout: root at (60, 220). Four branches fan out to x=300 at y=80,160,240,320.
  const root = { x: 60, y: 220 };
  const branchX = 300;
  const branchYs = [60, 160, 260, 360];
  const leafGap = 26;

  return (
    <svg
      ref={treeRef}
      viewBox="0 0 560 440"
      role="img"
      aria-label="Animated directory tree of ChilliCream resources"
      className="h-full w-full"
    >
      <defs>
        <linearGradient id="cc-v7-trunk" x1="0" x2="1" y1="0" y2="0">
          <stop
            offset="0%"
            stopColor="var(--color-cc-accent)"
            stopOpacity="0.75"
          />
          <stop
            offset="100%"
            stopColor="var(--color-cc-accent)"
            stopOpacity="0.35"
          />
        </linearGradient>
      </defs>

      {/* root label */}
      <motion.g
        initial={reduceMotion ? false : { opacity: 0 }}
        animate={inView || reduceMotion ? { opacity: 1 } : { opacity: 0 }}
        transition={{ duration: 0.4 }}
      >
        <rect
          x={root.x - 50}
          y={root.y - 14}
          width={110}
          height={28}
          rx={6}
          fill="var(--color-cc-surface)"
          stroke="var(--color-cc-card-border)"
        />
        <text
          x={root.x}
          y={root.y + 4}
          textAnchor="middle"
          fontFamily="var(--font-mono)"
          fontSize="11"
          fill="var(--color-cc-heading)"
        >
          ~/chillicream/
        </text>
      </motion.g>

      {BRANCHES.map((branch, i) => {
        const by = branchYs[i];
        const stroke =
          active === branch.id
            ? "var(--color-cc-accent)"
            : "var(--color-cc-card-border-hover)";
        const labelFill =
          active === branch.id
            ? "var(--color-cc-accent)"
            : "var(--color-cc-ink-dim)";
        const baseDelay = reduceMotion ? 0 : 0.4 + i * 0.18;
        const leafBaseDelay = baseDelay + 0.35;

        // Branch path: from root, horizontal stub, vertical segment to branch y, horizontal to branchX.
        const stub = root.x + 24;
        const path = `M ${root.x + 56} ${root.y} H ${stub} V ${by} H ${branchX}`;

        return (
          <g key={branch.id}>
            {/* branch line: drawn on, stays lit if active */}
            <motion.path
              d={path}
              fill="none"
              stroke={stroke}
              strokeWidth={1.5}
              strokeLinecap="round"
              strokeLinejoin="round"
              initial={reduceMotion ? false : { pathLength: 0, opacity: 0.7 }}
              animate={
                inView || reduceMotion
                  ? { pathLength: 1, opacity: 1 }
                  : { pathLength: 0, opacity: 0.7 }
              }
              transition={{
                duration: reduceMotion ? 0 : 0.7,
                delay: baseDelay,
                ease: "easeOut",
              }}
            />

            {/* scan-head pulse traveling the branch on activation */}
            {active === branch.id && !reduceMotion && (
              <motion.circle
                r={3}
                fill="var(--color-cc-accent)"
                initial={{ offsetDistance: "0%", opacity: 0.9 }}
                animate={{ offsetDistance: "100%", opacity: 0 }}
                transition={{ duration: 0.9, ease: "easeOut" }}
                style={{ offsetPath: `path("${path}")` }}
              />
            )}

            {/* branch label */}
            <motion.text
              x={branchX + 8}
              y={by + 4}
              fontFamily="var(--font-mono)"
              fontSize="12"
              fill={labelFill}
              initial={reduceMotion ? false : { opacity: 0, x: branchX }}
              animate={
                inView || reduceMotion
                  ? { opacity: 1, x: branchX + 8 }
                  : { opacity: 0, x: branchX }
              }
              transition={{
                duration: reduceMotion ? 0 : 0.4,
                delay: baseDelay + 0.4,
              }}
            >
              {branch.label}
            </motion.text>

            {/* leaves */}
            {branch.leaves.map((leaf, li) => {
              const lx = branchX + 60 + (li % 3) * 80;
              const ly = by + 22 + Math.floor(li / 3) * leafGap;
              const branchPath = `M ${branchX + 40} ${by} H ${branchX + 50} V ${ly} H ${lx - 6}`;
              return (
                <g key={leaf}>
                  <motion.path
                    d={branchPath}
                    fill="none"
                    stroke="var(--color-cc-card-border)"
                    strokeWidth={1}
                    initial={reduceMotion ? false : { pathLength: 0 }}
                    animate={
                      inView || reduceMotion
                        ? { pathLength: 1 }
                        : { pathLength: 0 }
                    }
                    transition={{
                      duration: reduceMotion ? 0 : 0.35,
                      delay: leafBaseDelay + li * 0.06,
                    }}
                  />
                  <motion.text
                    x={lx}
                    y={ly + 4}
                    fontFamily="var(--font-mono)"
                    fontSize="10"
                    fill={
                      active === branch.id
                        ? "var(--color-cc-accent)"
                        : "var(--color-cc-nav-label)"
                    }
                    initial={reduceMotion ? false : { opacity: 0 }}
                    animate={
                      inView || reduceMotion ? { opacity: 1 } : { opacity: 0 }
                    }
                    transition={{
                      duration: reduceMotion ? 0 : 0.3,
                      delay: leafBaseDelay + li * 0.06 + 0.18,
                    }}
                  >
                    {leaf}
                  </motion.text>
                </g>
              );
            })}
          </g>
        );
      })}

      {/* horizontal trunk from root edge into the fan */}
      <motion.line
        x1={root.x + 56}
        y1={root.y}
        x2={root.x + 84}
        y2={root.y}
        stroke="url(#cc-v7-trunk)"
        strokeWidth={1.5}
        initial={reduceMotion ? false : { pathLength: 0 }}
        animate={inView || reduceMotion ? { pathLength: 1 } : { pathLength: 0 }}
        transition={{ duration: reduceMotion ? 0 : 0.3, delay: 0.2 }}
      />
    </svg>
  );
}

/* -------------------------------------------------------------------------- */
/*  Typed `ls -R` caret                                                       */
/* -------------------------------------------------------------------------- */

const PROMPT_TEXT = "ls -R ~/chillicream/";

function TypedPrompt() {
  const reduceMotion = useReducedMotion() ?? false;
  const ref = useRef<HTMLDivElement>(null);
  const inView = useInView(ref, { once: true, amount: 0.6 });
  const [shown, setShown] = useState(reduceMotion ? PROMPT_TEXT.length : 0);

  useEffect(() => {
    if (reduceMotion || !inView) {
      return;
    }
    let i = 0;
    const id = window.setInterval(() => {
      i += 1;
      setShown(i);
      if (i >= PROMPT_TEXT.length) {
        window.clearInterval(id);
      }
    }, 45);
    return () => {
      window.clearInterval(id);
    };
  }, [inView, reduceMotion]);

  return (
    <div
      ref={ref}
      className="text-cc-ink-dim flex items-center gap-2 font-mono text-[12px]"
    >
      <span className="text-cc-accent">$</span>
      <span>{PROMPT_TEXT.slice(0, shown)}</span>
      <span
        aria-hidden="true"
        className="bg-cc-accent inline-block h-3 w-[7px] align-middle"
        style={{ opacity: reduceMotion ? 1 : undefined }}
      />
    </div>
  );
}

/* -------------------------------------------------------------------------- */
/*  Hero                                                                      */
/* -------------------------------------------------------------------------- */

interface HeroProps {
  readonly hoverActive: GroupId | null;
}

function ResourcesHero({ hoverActive }: HeroProps) {
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

        <div className="relative grid gap-10 p-8 sm:p-12 lg:grid-cols-[1.1fr_1fr] lg:gap-16 lg:p-16">
          <div>
            <div className="text-cc-nav-label font-mono text-xs tracking-widest uppercase">
              {"// resources / living index"}
            </div>
            <h1 className="text-cc-heading mt-4 text-5xl leading-tight font-semibold tracking-tight sm:text-6xl lg:text-7xl">
              The builder
              <br />
              library, indexed.
            </h1>
            <p className="text-cc-ink-dim mt-6 max-w-xl text-lg">
              ChilliCream resources as a directory that grows itself. Docs,
              blog, videos, Slack, GitHub, contact, shop, legal. Every leaf is a
              real link, maintained by humans.
            </p>
            <div className="mt-8 flex flex-wrap gap-3">
              <SolidButton href="/docs">Open the docs</SolidButton>
              <OutlineButton href="https://slack.chillicream.com/">
                Join Slack
              </OutlineButton>
            </div>
          </div>

          <div className="border-cc-card-border bg-cc-surface/70 relative self-stretch overflow-hidden rounded-xl border shadow-inner">
            <div className="border-cc-card-border bg-cc-bg/60 flex items-center gap-1.5 border-b px-4 py-2">
              <span className="bg-cc-danger/70 inline-block h-2.5 w-2.5 rounded-full" />
              <span className="bg-cc-warning/70 inline-block h-2.5 w-2.5 rounded-full" />
              <span className="bg-cc-success/70 inline-block h-2.5 w-2.5 rounded-full" />
              <span className="text-cc-nav-label ml-3 text-[11px] tracking-widest uppercase">
                ~/chillicream
              </span>
            </div>
            <div className="px-5 pt-4">
              <TypedPrompt />
            </div>
            <div className="px-2 pt-2 pb-4">
              <DirectoryTree hoverActive={hoverActive} />
            </div>
          </div>
        </div>
      </div>
    </section>
  );
}

/* -------------------------------------------------------------------------- */
/*  JumpNav                                                                   */
/* -------------------------------------------------------------------------- */

interface JumpNavProps {
  readonly onHover: (id: GroupId | null) => void;
}

function JumpNav({ onHover }: JumpNavProps) {
  return (
    <nav
      aria-label="Sections"
      className="border-cc-card-border/60 flex flex-wrap gap-2 border-y py-4"
    >
      <span className="text-cc-nav-label mr-2 font-mono text-xs tracking-widest uppercase">
        {"// jump to"}
      </span>
      {GROUPS.map((g) => (
        <motion.a
          key={g.id}
          href={`#${g.id}`}
          whileHover={{ scale: 1.04 }}
          whileFocus={{ scale: 1.04 }}
          transition={{ type: "spring", stiffness: 360, damping: 24 }}
          onMouseEnter={() => onHover(g.id)}
          onMouseLeave={() => onHover(null)}
          onFocus={() => onHover(g.id)}
          onBlur={() => onHover(null)}
          className="text-cc-ink-dim hover:text-cc-accent hover:border-cc-accent border-cc-card-border inline-block rounded-full border px-3 py-1 font-mono text-xs tracking-wide no-underline transition-colors"
        >
          {g.heading.toLowerCase()}
        </motion.a>
      ))}
    </nav>
  );
}

/* -------------------------------------------------------------------------- */
/*  Section heading with scan-line, optional tick-up counter                  */
/* -------------------------------------------------------------------------- */

interface CounterProps {
  readonly to: number;
  readonly suffix: string;
}

function TickUpCounter({ to, suffix }: CounterProps) {
  const reduceMotion = useReducedMotion() ?? false;
  const ref = useRef<HTMLSpanElement>(null);
  const inView = useInView(ref, { once: true, amount: 0.6 });
  const mv = useMotionValue(reduceMotion ? to : 0);
  const rounded = useTransform(mv, (v) => Math.round(v));
  const [display, setDisplay] = useState(reduceMotion ? to : 0);

  useEffect(() => {
    const unsubscribe = rounded.on("change", (v) => setDisplay(v));
    return () => unsubscribe();
  }, [rounded]);

  useEffect(() => {
    if (reduceMotion || !inView) {
      return;
    }
    const controls = animate(mv, to, { duration: 0.9, ease: "easeOut" });
    return () => controls.stop();
  }, [inView, mv, to, reduceMotion]);

  const padded = String(display).padStart(2, "0");
  return (
    <span
      ref={ref}
      className="text-cc-nav-label font-mono text-xs tracking-wide"
    >
      {`// ${padded} ${suffix}`}
    </span>
  );
}

/* -------------------------------------------------------------------------- */
/*  Link card (whileInView reveal, stagger via delay)                         */
/* -------------------------------------------------------------------------- */

interface LinkCardProps {
  readonly entry: Entry;
  readonly index: number;
}

function isExternal(href: string) {
  return /^https?:/.test(href);
}

function LinkCard({ entry, index }: LinkCardProps) {
  const reduceMotion = useReducedMotion() ?? false;
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

  const inner = entry.href.startsWith("/") ? (
    <Link href={entry.href} className={cls}>
      {body}
    </Link>
  ) : isExternal(entry.href) ? (
    <a
      href={entry.href}
      target="_blank"
      rel="noopener noreferrer"
      className={cls}
    >
      {body}
    </a>
  ) : (
    <a href={entry.href} className={cls}>
      {body}
    </a>
  );

  return (
    <motion.div
      initial={reduceMotion ? false : { opacity: 0, y: 16 }}
      whileInView={{ opacity: 1, y: 0 }}
      viewport={{ once: true, amount: 0.3 }}
      transition={{
        duration: reduceMotion ? 0 : 0.45,
        delay: reduceMotion ? 0 : index * 0.06,
        ease: "easeOut",
      }}
    >
      {inner}
    </motion.div>
  );
}

/* -------------------------------------------------------------------------- */
/*  Group block: stagger reveal + scan-line on heading + section-enter event  */
/* -------------------------------------------------------------------------- */

interface GroupBlockProps {
  readonly group: Group;
}

function ScanLine() {
  const reduceMotion = useReducedMotion() ?? false;
  if (reduceMotion) {
    return null;
  }
  return (
    <motion.span
      aria-hidden="true"
      className="bg-cc-accent absolute top-1/2 left-0 block h-px"
      initial={{ width: 0, opacity: 0 }}
      whileInView={{ width: "100%", opacity: [0, 1, 0] }}
      viewport={{ once: true, amount: 0.6 }}
      transition={{ duration: 0.9, ease: "easeOut" }}
    />
  );
}

function GroupBlock({ group }: GroupBlockProps) {
  const ref = useRef<HTMLElement>(null);
  const inView = useInView(ref, { amount: 0.3 });
  // Only fire the section-enter event on the rising edge (false -> true),
  // so the tree does not get spammed every time a section scrolls back in.
  const wasInView = useRef(false);

  useEffect(() => {
    if (inView && !wasInView.current) {
      wasInView.current = true;
      window.dispatchEvent(
        new CustomEvent<ActiveBranchDetail>(SECTION_ENTER_EVENT, {
          detail: { id: group.id },
        }),
      );
    } else if (!inView && wasInView.current) {
      wasInView.current = false;
    }
  }, [inView, group.id]);

  const cols = useMemo(() => {
    if (group.entries.length >= 4) {
      return "sm:grid-cols-2 lg:grid-cols-4";
    }
    if (group.entries.length === 3) {
      return "sm:grid-cols-2 lg:grid-cols-3";
    }
    return "sm:grid-cols-2";
  }, [group.entries.length]);

  return (
    <section
      id={group.id}
      ref={ref}
      aria-labelledby={`${group.id}-heading`}
      className="border-cc-card-border/60 border-t py-14"
    >
      <div className="mb-8 grid gap-2 lg:grid-cols-[16rem_1fr] lg:items-end lg:gap-12">
        <div>
          <div className="text-cc-nav-label flex items-center gap-3 font-mono text-xs tracking-widest uppercase">
            <span>{group.label}</span>
            {group.id === "community" ? (
              <TickUpCounter to={group.entries.length} suffix="entries" />
            ) : null}
          </div>
          <h2
            id={`${group.id}-heading`}
            className="text-cc-heading relative mt-2 inline-block text-3xl font-semibold tracking-tight sm:text-4xl"
          >
            <ScanLine />
            <span className="relative">{group.heading}</span>
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

/* -------------------------------------------------------------------------- */
/*  Contact band with typed $ mail line                                       */
/* -------------------------------------------------------------------------- */

const MAIL_TEXT = "mail contact@chillicream.com";

function ContactBand() {
  const reduceMotion = useReducedMotion() ?? false;
  const ref = useRef<HTMLDivElement>(null);
  const inView = useInView(ref, { once: true, amount: 0.5 });
  const [shown, setShown] = useState(reduceMotion ? MAIL_TEXT.length : 0);

  useEffect(() => {
    if (reduceMotion || !inView) {
      return;
    }
    let i = 0;
    const id = window.setInterval(() => {
      i += 1;
      setShown(i);
      if (i >= MAIL_TEXT.length) {
        window.clearInterval(id);
      }
    }, 35);
    return () => {
      window.clearInterval(id);
    };
  }, [inView, reduceMotion]);

  const typed = MAIL_TEXT.slice(0, shown);
  // Split typed into the "mail " prefix (accent) and the address (ink) once typed past index 5.
  const cutoff = Math.min(shown, 5);
  const accentPart = MAIL_TEXT.slice(0, cutoff);
  const inkPart = typed.length > 5 ? typed.slice(5) : "";

  return (
    <section
      aria-labelledby="contact-heading"
      className="border-cc-card-border/60 border-t py-14"
    >
      <div
        ref={ref}
        className="border-cc-card-border bg-cc-card-bg/60 overflow-hidden rounded-2xl border backdrop-blur-sm"
      >
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
              <span className="text-cc-accent">{accentPart}</span>
              <span className="text-cc-ink">{inkPart}</span>
              <span
                aria-hidden="true"
                className="bg-cc-accent ml-0.5 inline-block h-3 w-[6px] align-middle"
              />
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

/* -------------------------------------------------------------------------- */
/*  Footer caption strip                                                      */
/* -------------------------------------------------------------------------- */

function FooterCaption() {
  const reduceMotion = useReducedMotion() ?? false;
  return (
    <motion.div
      initial={reduceMotion ? false : { opacity: 0 }}
      whileInView={{ opacity: 1 }}
      viewport={{ once: true, amount: 0.6 }}
      transition={{ duration: reduceMotion ? 0 : 0.6 }}
      className="border-cc-card-border/60 text-cc-nav-label border-t py-8 text-center font-mono text-xs tracking-widest uppercase"
    >
      {"// indexed by humans, growing weekly"}
    </motion.div>
  );
}

/* -------------------------------------------------------------------------- */
/*  Client root                                                               */
/* -------------------------------------------------------------------------- */

export function LivingIndex() {
  const [hoverActive, setHoverActive] = useState<GroupId | null>(null);

  return (
    <MotionConfig reducedMotion="user">
      <ResourcesHero hoverActive={hoverActive} />
      <JumpNav onHover={setHoverActive} />
      {GROUPS.map((group) => (
        <GroupBlock key={group.id} group={group} />
      ))}
      <ContactBand />
      <FooterCaption />
    </MotionConfig>
  );
}

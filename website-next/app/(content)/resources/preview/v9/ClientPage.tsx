"use client";

import Link from "next/link";
import type { CSSProperties, ReactNode } from "react";

import { motion, useReducedMotion } from "motion/react";

import { OutlineButton, SolidButton } from "@/src/design-system/Button";

// ---------------------------------------------------------------------------
// Card Catalog
// Resources staged as a paper card catalog. Every section of the hub is a 3x5
// index card: a typewriter title bar (call number + date stamp), ruled mono
// body lines, and a deterministic dog-eared corner, all rendered as flat
// cc-surface rectangles with cc-accent teal as the only color. The page reads
// top-to-bottom as a small stack of cards laid out on the cc-bg desk.
//
// Motion is time-driven and enter-view only, never coupled to scroll position:
// a blinking cursor in the hero title bar, and each card fades and lifts in
// once via whileInView with viewport once. Both collapse to static under
// prefers-reduced-motion.
// ---------------------------------------------------------------------------

// The single accent for the whole page (cc-accent teal #5eead4) is used via
// cc-* tokens. The cyan -> violet -> coral brand spectrum appears exactly once,
// on the footer hairline below.
const SPECTRUM = "linear-gradient(90deg, #16b9e4, #7c92c6, #f0786a)";

interface CardEntry {
  readonly call: string;
  readonly title: string;
  readonly href: string;
  readonly hrefLabel: string;
  readonly external?: boolean;
  readonly tag?: string;
  readonly primary?: boolean;
}

interface CatalogCard {
  readonly id: string;
  readonly number: string;
  readonly label: string;
  readonly entries: readonly CardEntry[];
}

// --- The filed cards. One concrete link per entry, nothing invented. -------

const CARDS: readonly CatalogCard[] = [
  {
    id: "learn",
    number: "01",
    label: "LEARN",
    entries: [
      {
        call: "// 01.a",
        title: "Documentation",
        href: "/docs",
        hrefLabel: "/docs",
        tag: "ref",
      },
      {
        call: "// 01.b",
        title: "Blog",
        href: "/blog",
        hrefLabel: "/blog",
        tag: "ref",
      },
      {
        call: "// 01.c",
        title: "YouTube",
        href: "https://www.youtube.com/c/ChilliCream",
        hrefLabel: "youtube.com/c/ChilliCream",
        external: true,
        tag: "ext",
      },
    ],
  },
  {
    id: "channels",
    number: "02",
    label: "CHANNELS",
    entries: [
      {
        call: "// 02.a",
        title: "Slack",
        href: "https://slack.chillicream.com/",
        hrefLabel: "slack.chillicream.com",
        external: true,
        tag: "ext",
        primary: true,
      },
      {
        call: "// 02.b",
        title: "GitHub",
        href: "https://github.com/ChilliCream/graphql-platform",
        hrefLabel: "github.com/ChilliCream",
        external: true,
        tag: "ext",
      },
      {
        call: "// 02.c",
        title: "X",
        href: "https://x.com/Chilli_Cream",
        hrefLabel: "x.com/Chilli_Cream",
        external: true,
        tag: "ext",
      },
      {
        call: "// 02.d",
        title: "LinkedIn",
        href: "https://www.linkedin.com/company/chillicream",
        hrefLabel: "linkedin.com/company/chillicream",
        external: true,
        tag: "ext",
      },
    ],
  },
  {
    id: "company",
    number: "03",
    label: "COMPANY",
    entries: [
      {
        call: "// 03.a",
        title: "Contact",
        href: "mailto:contact@chillicream.com",
        hrefLabel: "contact@chillicream.com",
        tag: "mail",
      },
      {
        call: "// 03.b",
        title: "Shop",
        href: "https://store.chillicream.com",
        hrefLabel: "store.chillicream.com",
        external: true,
        tag: "ext",
      },
    ],
  },
];

// Legal is filed on its own taller card, with a slim divider before License.
const LEGAL_ENTRIES: readonly CardEntry[] = [
  {
    call: "// 04.a",
    title: "Terms of Service",
    href: "/legal/terms-of-service",
    hrefLabel: "/legal/terms-of-service",
    tag: "doc",
  },
  {
    call: "// 04.b",
    title: "Privacy Policy",
    href: "/legal/privacy-policy",
    hrefLabel: "/legal/privacy-policy",
    tag: "doc",
  },
  {
    call: "// 04.c",
    title: "Cookie Policy",
    href: "/legal/cookie-policy",
    hrefLabel: "/legal/cookie-policy",
    tag: "doc",
  },
  {
    call: "// 04.d",
    title: "Acceptable Use Policy",
    href: "/legal/acceptable-use-policy",
    hrefLabel: "/legal/acceptable-use-policy",
    tag: "doc",
  },
];

interface DrawerTab {
  readonly id: string;
  readonly label: string;
}

const DRAWER_TABS: readonly DrawerTab[] = [
  { id: "learn", label: "learn" },
  { id: "channels", label: "channels" },
  { id: "company", label: "company" },
  { id: "legal", label: "legal" },
  { id: "license", label: "license" },
];

// --- Ruled-paper backdrop for the body of each card -----------------------

const RULED_PAPER: CSSProperties = {
  backgroundImage:
    "repeating-linear-gradient(to bottom, transparent 0, transparent 27px, rgba(245,241,234,0.06) 27px, rgba(245,241,234,0.06) 28px)",
};

// --- Dog-ear: a flat clip-path triangle plus an inline SVG fold shadow ------

function DogEar() {
  return (
    <span
      aria-hidden="true"
      className="pointer-events-none absolute top-0 right-0"
    >
      <span
        className="bg-cc-bg absolute top-0 right-0 block h-7 w-7"
        style={{ clipPath: "polygon(100% 0, 0 0, 100% 100%)" }}
      />
      <svg
        viewBox="0 0 28 28"
        width={28}
        height={28}
        className="text-cc-accent/40 absolute top-0 right-0 block"
      >
        <path
          d="M28 0 L28 28 L0 0 Z"
          fill="none"
          stroke="currentColor"
          strokeWidth="1"
        />
        <path d="M28 28 L0 0" stroke="currentColor" strokeWidth="1" />
      </svg>
    </span>
  );
}

// --- Title bar: typewriter eyebrow + stamped date --------------------------

interface TitleBarProps {
  readonly callNumber: string;
  readonly children?: ReactNode;
}

function TitleBar({ callNumber, children }: TitleBarProps) {
  return (
    <div className="border-cc-card-border bg-cc-bg/40 flex items-center justify-between gap-3 border-b px-5 py-2.5 font-mono text-[11px] tracking-[0.18em] uppercase">
      <span className="text-cc-nav-label flex items-center gap-2">
        {callNumber}
        {children}
      </span>
      <span className="text-cc-accent border-cc-accent/40 rounded-sm border px-2 py-0.5 text-[10px]">
        JUN 2026
      </span>
    </div>
  );
}

// --- Entry row: tight three-column mono table, hover bracket affordance -----

function EntryRow({ entry }: { readonly entry: CardEntry }) {
  const isInternal = entry.href.startsWith("/");
  const isMail = entry.href.startsWith("mailto:");

  const inner = (
    <>
      <span
        aria-hidden="true"
        className={`absolute top-1.5 bottom-1.5 left-0 w-px transition-colors ${
          entry.primary
            ? "bg-cc-accent"
            : "group-hover/row:bg-cc-accent bg-transparent"
        }`}
      />
      <span
        className={`shrink-0 font-mono text-[12px] tabular-nums transition-colors ${
          entry.primary
            ? "text-cc-accent"
            : "text-cc-nav-label group-hover/row:text-cc-accent"
        }`}
      >
        {entry.call}
      </span>
      <span className="text-cc-heading min-w-0 flex-1 truncate text-[15px] font-medium tracking-tight">
        {entry.title}
      </span>
      <span className="text-cc-ink-dim hidden shrink-0 truncate font-mono text-[12px] sm:inline">
        {entry.hrefLabel}
      </span>
      {entry.tag ? (
        <span className="text-cc-nav-label shrink-0 font-mono text-[10px] tracking-[0.18em] uppercase">
          {entry.tag}
        </span>
      ) : null}
    </>
  );

  const cls =
    "group/row relative flex items-center gap-3 py-3 pr-1 pl-4 no-underline";

  if (isInternal) {
    return (
      <Link href={entry.href} className={cls}>
        {inner}
      </Link>
    );
  }
  if (isMail) {
    return (
      <a href={entry.href} className={cls}>
        {inner}
      </a>
    );
  }
  return (
    <a
      href={entry.href}
      target="_blank"
      rel="noopener noreferrer"
      className={cls}
    >
      {inner}
    </a>
  );
}

// --- The card shell: cc-surface rectangle, nudged, fades/lifts in once ------

interface CardShellProps {
  readonly id?: string;
  readonly index: number;
  readonly children: ReactNode;
  readonly className?: string;
}

function CardShell({ id, index, children, className = "" }: CardShellProps) {
  const reduced = useReducedMotion();
  // Hand-placed feel: nudge cards a couple of pixels at alternating indices,
  // never rotated, so the stack reads without breaking layout.
  const nudge = index % 2 === 0 ? "translate-x-[2px]" : "-translate-x-[3px]";

  return (
    <motion.section
      id={id}
      className={`border-cc-card-border bg-cc-surface relative overflow-hidden rounded-md border ${nudge} ${className}`}
      initial={reduced ? false : { opacity: 0, y: 12 }}
      whileInView={reduced ? undefined : { opacity: 1, y: 0 }}
      viewport={{ once: true, amount: 0.3 }}
      transition={{
        duration: 0.28,
        ease: "easeOut",
        delay: (index % 3) * 0.06,
      }}
    >
      <DogEar />
      {children}
    </motion.section>
  );
}

// --- Filed catalog card (LEARN / CHANNELS / COMPANY) ------------------------

function FiledCard({
  card,
  index,
}: {
  readonly card: CatalogCard;
  readonly index: number;
}) {
  return (
    <CardShell id={card.id} index={index}>
      <TitleBar callNumber={`CC-RES / ${card.number} . ${card.label}`} />
      {card.id === "company" ? (
        <span
          aria-hidden="true"
          className="text-cc-accent/50 border-cc-accent/30 absolute top-14 right-4 rotate-[-8deg] rounded-sm border px-2 py-1 font-mono text-[10px] tracking-[0.2em] uppercase"
        >
          cc stamp
        </span>
      ) : null}
      <div
        className="divide-cc-card-border/40 divide-y px-5 py-2"
        style={RULED_PAPER}
      >
        {card.entries.map((entry) => (
          <EntryRow key={entry.href} entry={entry} />
        ))}
      </div>
    </CardShell>
  );
}

export function ClientPage() {
  const reduced = useReducedMotion();

  return (
    <div className="relative py-10">
      {/* Desk grain: a fixed, very subtle feTurbulence noise overlay. */}
      <div
        aria-hidden="true"
        className="pointer-events-none fixed inset-0 -z-10 opacity-[0.03]"
        style={{
          backgroundImage:
            "url(\"data:image/svg+xml,%3Csvg xmlns='http://www.w3.org/2000/svg' width='160' height='160'%3E%3Cfilter id='n'%3E%3CfeTurbulence type='fractalNoise' baseFrequency='0.9' numOctaves='2'/%3E%3C/filter%3E%3Crect width='100%25' height='100%25' filter='url(%23n)'/%3E%3C/svg%3E\")",
        }}
      />
      {/* Desk blotter rails: faint hairlines top and bottom of the column. */}
      <div aria-hidden="true" className="bg-cc-card-border mb-10 h-px w-full" />

      <div className="mx-auto flex max-w-4xl flex-col gap-10">
        {/* --- Catalog header card --- */}
        <CardShell index={0}>
          <TitleBar callNumber="CARD CATALOG / RESOURCES">
            {reduced ? null : (
              <motion.span
                aria-hidden="true"
                className="bg-cc-accent inline-block h-3 w-[7px] align-middle"
                animate={{ opacity: [1, 1, 0, 0] }}
                transition={{
                  duration: 1.1,
                  repeat: Infinity,
                  ease: "linear",
                  times: [0, 0.5, 0.5, 1],
                }}
              />
            )}
          </TitleBar>
          <div className="px-6 py-9 sm:px-10 sm:py-12" style={RULED_PAPER}>
            <h1 className="font-heading text-cc-heading text-h1 font-semibold tracking-tight">
              ChilliCream resources
            </h1>
            <p className="text-cc-ink-dim mt-5 max-w-2xl font-mono text-[15px] leading-relaxed">
              Open the drawer, thumb through the cards, pull the one you need.
              Every link an engineer working on ChilliCream actually opens,
              filed one entry per card. Docs, channels, contact, shop, and
              legal. No brochures, no fluff.
            </p>
            <div className="mt-8 flex flex-wrap gap-3">
              <SolidButton href="/docs">Open the docs</SolidButton>
              <OutlineButton href="https://slack.chillicream.com/">
                Join Slack
              </OutlineButton>
            </div>
          </div>
        </CardShell>

        {/* --- Drawer index strip --- */}
        <nav
          aria-label="Catalog drawers"
          className="flex flex-wrap items-center gap-2 px-1"
        >
          <span className="text-cc-nav-label mr-1 font-mono text-[11px] tracking-[0.18em] uppercase">
            {"// drawer"}
          </span>
          {DRAWER_TABS.map((tab) => (
            <a
              key={tab.id}
              href={`#${tab.id}`}
              className="text-cc-ink-dim hover:text-cc-accent hover:border-cc-card-border-hover border-cc-card-border bg-cc-card-bg rounded-t-md border border-b-0 px-3 py-1.5 font-mono text-[12px] tracking-wide no-underline transition-colors"
            >
              {tab.label}
            </a>
          ))}
        </nav>

        {/* --- Filed cards 01..03 --- */}
        {CARDS.map((card, i) => (
          <FiledCard key={card.id} card={card} index={i + 1} />
        ))}

        {/* --- Card 04 LEGAL (taller, divider before license link) --- */}
        <CardShell id="legal" index={4}>
          <TitleBar callNumber="CC-RES / 04 . LEGAL" />
          <div className="px-5 py-2" style={RULED_PAPER}>
            <div className="divide-cc-card-border/40 divide-y">
              {LEGAL_ENTRIES.map((entry) => (
                <EntryRow key={entry.href} entry={entry} />
              ))}
            </div>
            <div className="border-cc-card-border/60 mt-1 border-t border-dashed pt-1">
              <EntryRow
                entry={{
                  call: "// 04.e",
                  title: "ChilliCream License",
                  href: "/licensing/chillicream-license",
                  hrefLabel: "/licensing/chillicream-license",
                  tag: "lic",
                }}
              />
            </div>
          </div>
        </CardShell>

        {/* --- Card 05 LICENSE --- */}
        <CardShell id="license" index={5}>
          <TitleBar callNumber="CC-RES / 05 . LICENSE" />
          <div className="px-6 py-8 sm:px-8" style={RULED_PAPER}>
            <h2 className="font-heading text-cc-heading text-h4 font-semibold tracking-tight">
              ChilliCream License
            </h2>
            <p className="text-cc-ink-dim mt-4 max-w-2xl font-mono text-[14px] leading-relaxed">
              The commercial license terms for ChilliCream products beyond the
              open source MIT pieces. Plain English on what you can use, what is
              licensed, and where the open source line sits.
            </p>
            <div className="mt-7">
              <OutlineButton href="/licensing/chillicream-license">
                Read the license
              </OutlineButton>
            </div>
          </div>
        </CardShell>

        {/* --- Need a human card (contact band) --- */}
        <CardShell id="contact" index={6}>
          <TitleBar callNumber="CC-RES / 06 . NEED A HUMAN?" />
          <div
            className="grid gap-8 px-6 py-9 sm:px-10 lg:grid-cols-[1fr_auto] lg:items-center"
            style={RULED_PAPER}
          >
            <div>
              <p className="text-cc-ink-dim max-w-2xl font-mono text-[14px] leading-relaxed">
                Slack is fastest for everything technical and community-facing.
                For commercial questions, partnerships, security reports, or
                anything that does not belong in a public channel, send us mail.
              </p>
              <div className="mt-5 inline-flex items-center gap-2 font-mono text-[14px]">
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
        </CardShell>

        {/* --- Catalog footer card (slim colophon) --- */}
        <CardShell index={7}>
          <div className="flex items-center justify-between gap-4 px-5 py-4">
            <span className="text-cc-nav-label font-mono text-[12px] tracking-wide">
              {"// end of stack, maintained by humans"}
            </span>
            <span className="text-cc-nav-label font-mono text-[11px] tracking-[0.18em] uppercase">
              CC-RES
            </span>
          </div>
          {/* The single full-spectrum appearance on the page. */}
          <div
            aria-hidden="true"
            className="h-px w-full"
            style={{ background: SPECTRUM }}
          />
        </CardShell>
      </div>

      <div aria-hidden="true" className="bg-cc-card-border mt-10 h-px w-full" />
    </div>
  );
}

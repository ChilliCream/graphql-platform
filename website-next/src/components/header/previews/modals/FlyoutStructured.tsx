"use client";

import { motion } from "motion/react";
import Link from "next/link";
import type { ReactNode } from "react";

import {
  type NavItem,
  type SubGroup,
  type SubLink,
  TOOLS,
} from "@/src/components/header/navData";

const SPECTRUM = "linear-gradient(120deg, #16b9e4, #7c92c6, #f0786a)";
const FEATURE_TINT =
  "linear-gradient(150deg, rgba(22,185,228,0.14), rgba(124,146,198,0.06) 55%, rgba(240,120,106,0.14))";
const EASE: [number, number, number, number] = [0.16, 1, 0.3, 1];

interface FlyoutStructuredProps {
  readonly item: NavItem;
  readonly className?: string;
}

/**
 * A structured, professional mega-menu panel (Stripe / Linear / Vercel
 * register): labeled columns of links and a single crafted featured card, with
 * generous whitespace, a monochrome base, the teal accent reserved for hover,
 * and a tasteful staggered entrance. Renders any nav section.
 */
export function FlyoutStructured({ item, className }: FlyoutStructuredProps) {
  const groups = item.groups ?? [];

  return (
    <motion.div
      initial={{ opacity: 0, y: 8 }}
      animate={{ opacity: 1, y: 0 }}
      transition={{ duration: 0.2, ease: EASE }}
      style={{ boxShadow: "0 28px 80px rgba(0, 0, 0, 0.5)" }}
      className={[
        "border-cc-white/10 bg-cc-surface w-fit max-w-[980px] rounded-2xl border p-8",
        className ?? "",
      ]
        .filter(Boolean)
        .join(" ")}
    >
      <div className="flex items-start gap-16">
        <div className="flex gap-14">
          {groups.length > 1 ? (
            groups.map((group) => (
              <GroupColumn key={group.title} group={group} />
            ))
          ) : groups[0] ? (
            <SingleGroup group={groups[0]} />
          ) : null}
        </div>
        {item.aside ? <Feature aside={item.aside} /> : null}
      </div>
    </motion.div>
  );
}

interface GroupColumnProps {
  readonly group: SubGroup;
}

/** A labeled column: section eyebrow over a staggered vertical list of links. */
function GroupColumn({ group }: GroupColumnProps) {
  const rich = group.links.some((link) => link.description);
  return (
    <div className="w-[224px] flex-none">
      <GroupLabel>{group.title}</GroupLabel>
      <ul className="m-0 mt-5 flex list-none flex-col gap-1 p-0">
        {group.links.map((link, index) => (
          <Row key={link.href} index={index}>
            <LinkRow link={link} rich={rich} />
          </Row>
        ))}
      </ul>
    </div>
  );
}

interface SingleGroupProps {
  readonly group: SubGroup;
}

/** A lone group: keeps one label but flows a long link list into two columns. */
function SingleGroup({ group }: SingleGroupProps) {
  const rich = group.links.some((link) => link.description);
  const twoColumn = group.links.length > 5;

  return (
    <div className={twoColumn ? "w-[496px] flex-none" : "w-[248px] flex-none"}>
      <GroupLabel>{group.title}</GroupLabel>
      <ul
        className={[
          "m-0 mt-5 list-none p-0",
          twoColumn
            ? "grid grid-cols-2 gap-x-12 gap-y-1"
            : "flex flex-col gap-1",
        ].join(" ")}
      >
        {group.links.map((link, index) => (
          <Row key={link.href} index={index}>
            <LinkRow link={link} rich={rich} />
          </Row>
        ))}
      </ul>
    </div>
  );
}

interface RowProps {
  readonly index: number;
  readonly children: ReactNode;
}

/** A list row that fades and lifts in, staggered by its position. */
function Row({ index, children }: RowProps) {
  return (
    <motion.li
      className="m-0"
      initial={{ opacity: 0, y: 6 }}
      animate={{ opacity: 1, y: 0 }}
      transition={{ duration: 0.25, ease: EASE, delay: 0.05 + index * 0.03 }}
    >
      {children}
    </motion.li>
  );
}

function GroupLabel({ children }: { readonly children: ReactNode }) {
  return (
    <div className="text-cc-nav-label font-mono text-[11px] font-semibold tracking-[0.18em] uppercase">
      {children}
    </div>
  );
}

interface LinkRowProps {
  readonly link: SubLink;
  readonly rich: boolean;
}

function LinkRow({ link, rich }: LinkRowProps) {
  const Icon = link.icon;

  if (!rich) {
    return (
      <SmartLink
        href={link.href}
        className="group/row text-cc-ink hover:bg-cc-hover hover:text-cc-heading flex items-center gap-3 rounded-md px-2.5 py-2 text-sm no-underline transition-colors"
      >
        {Icon ? (
          <Icon className="text-cc-ink-dim group-hover/row:text-cc-accent h-4 w-4 flex-none fill-current transition-colors" />
        ) : null}
        <span className="truncate">{link.label}</span>
      </SmartLink>
    );
  }

  return (
    <SmartLink
      href={link.href}
      className="group/row hover:bg-cc-hover flex items-start gap-3.5 rounded-lg px-3 py-2.5 no-underline transition-colors"
    >
      {Icon ? (
        <span className="border-cc-card-border bg-cc-hover text-cc-ink-dim group-hover/row:border-cc-card-border-hover group-hover/row:text-cc-accent mt-0.5 flex h-8 w-8 flex-none items-center justify-center rounded-md border transition-colors">
          <Icon className="h-4 w-4 fill-current" />
        </span>
      ) : null}
      <span className="min-w-0">
        <span className="text-cc-heading block text-sm font-medium">
          {link.label}
        </span>
        {link.description ? (
          <span className="text-cc-ink-dim mt-1 block text-xs leading-relaxed">
            {link.description}
          </span>
        ) : null}
      </span>
    </SmartLink>
  );
}

interface FeatureProps {
  readonly aside: "blog" | "get-in-touch";
}

/** The crafted featured card on the right, entering a beat after the columns. */
function Feature({ aside }: FeatureProps) {
  return (
    <motion.div
      className="flex-none"
      initial={{ opacity: 0, y: 10 }}
      animate={{ opacity: 1, y: 0 }}
      transition={{ duration: 0.3, ease: EASE, delay: 0.14 }}
    >
      {aside === "blog" ? <BlogCard /> : <GetInTouchCard />}
    </motion.div>
  );
}

/** Blog feature: a small framed article preview rather than a flat gradient. */
function BlogCard() {
  return (
    <Link
      href={TOOLS.blog}
      style={{ backgroundImage: FEATURE_TINT }}
      className="group/feat border-cc-white/10 flex w-[288px] flex-col rounded-xl border p-6 no-underline"
    >
      <GroupLabel>From the blog</GroupLabel>
      <span
        aria-hidden="true"
        className="border-cc-white/10 bg-cc-bg/40 mt-4 block overflow-hidden rounded-lg border"
      >
        <span className="border-cc-white/10 flex items-center gap-1.5 border-b px-3 py-2">
          <span className="bg-cc-status-firing/60 h-1.5 w-1.5 rounded-full" />
          <span className="bg-cc-status-investigating/60 h-1.5 w-1.5 rounded-full" />
          <span className="bg-cc-status-healthy/60 h-1.5 w-1.5 rounded-full" />
        </span>
        <span className="block px-3.5 py-3.5">
          <span
            style={{ backgroundImage: SPECTRUM }}
            className="block h-2 w-3/4 rounded-full opacity-90"
          />
          <span className="bg-cc-ink-faint mt-2.5 block h-1.5 w-full rounded-full" />
          <span className="bg-cc-ink-faint mt-1.5 block h-1.5 w-5/6 rounded-full" />
          <span className="bg-cc-hover text-cc-ink-dim mt-3 inline-block rounded-full px-2 py-0.5 font-mono text-[9px] tracking-wide uppercase">
            Federation
          </span>
        </span>
      </span>
      <span className="text-cc-ink-dim mt-4 font-mono text-[11px]">
        Jun 24, 2026
      </span>
      <span className="text-cc-heading group-hover/feat:text-cc-accent mt-1.5 block text-sm leading-snug font-medium transition-colors">
        Agents, Federation, and a Community
      </span>
      <span className="text-cc-accent mt-auto pt-5 text-xs font-medium">
        Read more &rarr;
      </span>
    </Link>
  );
}

/** Get-in-touch feature: warm copy over a faint, on-brand coffee motif. */
function GetInTouchCard() {
  return (
    <Link
      href="/services/support/contact"
      style={{ backgroundImage: FEATURE_TINT }}
      className="group/feat border-cc-white/10 relative flex w-[288px] flex-col overflow-hidden rounded-xl border p-6 no-underline"
    >
      <CoffeeMotif className="text-cc-accent pointer-events-none absolute -right-3 -bottom-4 h-32 w-32 opacity-[0.08]" />
      <GroupLabel>Get in touch</GroupLabel>
      <span className="font-heading text-cc-heading mt-4 block text-lg leading-snug font-semibold">
        Your technology journey. Our expertise.
      </span>
      <p className="text-cc-ink-dim mt-2.5 text-xs leading-relaxed">
        Talk to our team about support, training, and advisory.
      </p>
      <span className="bg-cc-heading text-cc-surface mt-6 inline-flex w-fit items-center gap-1 rounded-full px-4 py-1.5 text-xs font-semibold transition-transform group-hover/feat:translate-x-0.5">
        Talk to us &rarr;
      </span>
    </Link>
  );
}

function CoffeeMotif({ className }: { readonly className?: string }) {
  return (
    <svg
      viewBox="0 0 64 64"
      aria-hidden="true"
      fill="none"
      stroke="currentColor"
      strokeWidth="2.5"
      strokeLinecap="round"
      strokeLinejoin="round"
      className={className}
    >
      <path d="M13 28 h31 v11 a13 13 0 0 1 -13 13 h-5 a13 13 0 0 1 -13 -13 z" />
      <path d="M44 31 h6 a7 7 0 0 1 0 14 h-5" />
      <path d="M11 58 h41" />
      <path d="M24 10 c 4 4 -4 7 0 12" />
      <path d="M34 8 c 4 4 -4 7 0 12" />
    </svg>
  );
}

interface SmartLinkProps {
  readonly href: string;
  readonly className?: string;
  readonly children: ReactNode;
}

function SmartLink({ href, className, children }: SmartLinkProps) {
  if (href.startsWith("/")) {
    return (
      <Link href={href} className={className}>
        {children}
      </Link>
    );
  }
  if (href.startsWith("http")) {
    return (
      <a
        href={href}
        target="_blank"
        rel="noopener noreferrer"
        className={className}
      >
        {children}
      </a>
    );
  }
  return (
    <a href={href} className={className}>
      {children}
    </a>
  );
}

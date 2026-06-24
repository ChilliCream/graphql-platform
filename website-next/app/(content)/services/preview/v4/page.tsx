import type { Metadata } from "next";
import type { ReactNode } from "react";

import { CheckIcon } from "@/src/components/CheckIcon";
import { OutlineButton, SolidButton } from "@/src/design-system/Button";

export const metadata: Metadata = {
  title: "ChilliCream GraphQL Services: Advisory, Support, Training",
  description:
    "ChilliCream GraphQL services compared side by side: Advisory consulting, Support plans from $450 per month, and Corporate Training, from the Hot Chocolate team.",
  keywords: [
    "ChilliCream GraphQL services",
    "GraphQL advisory",
    "GraphQL support plans",
    "GraphQL training",
    "Hot Chocolate consulting",
    "Fusion support",
    "Corporate GraphQL workshop",
  ],
  openGraph: {
    title: "ChilliCream GraphQL Services: Advisory, Support, Training",
    description:
      "Hand-rolled vs ChilliCream Services, line by line: Advisory, Support from $450 per month, and Corporate Training.",
  },
  robots: { index: false, follow: false },
};

const BOOKING_URL = "https://calendly.com/chillicream/60min";
const CONTACT_MAILTO = "mailto:contact@chillicream.com";
const ENTERPRISE_MAILTO =
  "mailto:contact@chillicream.com?subject=Enterprise%20Services";
const SUPPORT_CONTACT = "/services/support/contact";

const ACCENT = "#16b9e4";

interface ServiceTrack {
  readonly id: "advisory" | "support" | "training";
  readonly eyebrow: string;
  readonly name: string;
  readonly tagline: string;
  readonly priceLine: string;
  readonly priceNote: string;
  readonly bullets: readonly string[];
  readonly learnMoreHref: string;
  readonly learnMoreLabel: string;
}

const TRACKS: Readonly<Record<ServiceTrack["id"], ServiceTrack>> = {
  advisory: {
    id: "advisory",
    eyebrow: "Consulting & Contracting",
    name: "Advisory",
    tagline:
      "Hourly consulting or scoped contracting from senior engineers. Bring a question, a design, or a deadline.",
    priceLine: "Hourly",
    priceNote: "or scoped contracting",
    bullets: [
      "Architecture, schema review, troubleshooting",
      "Proof of concept and full implementation",
      "Direct line to the core engineering team",
      "Start with a single 60-minute call",
    ],
    learnMoreHref: "/services/advisory",
    learnMoreLabel: "Explore Advisory",
  },
  support: {
    id: "support",
    eyebrow: "Community, Startup, Business, Enterprise",
    name: "Support",
    tagline:
      "Tiered support plans with private channels, defined response times, and an escalation path you can rely on.",
    priceLine: "From $450 / month",
    priceNote: "tiered plans",
    bullets: [
      "Free Community plan, paid tiers from $450",
      "Business at $1,300 with email and incident handling",
      "Enterprise with phone support and a dedicated account manager",
      "Same engineers who ship Hot Chocolate, Fusion, and Nitro",
    ],
    learnMoreHref: "/services/support",
    learnMoreLabel: "Compare Support",
  },
  training: {
    id: "training",
    eyebrow: "Corporate Training & Workshop",
    name: "Training",
    tagline:
      "Hands-on training and workshops for your team. Levels and pacing tuned to where your engineers are today.",
    priceLine: "Custom",
    priceNote: "tailored to team size",
    bullets: [
      "Corporate Training tuned to beginner, advanced, or mixed teams",
      "Corporate Workshop covering Hot Chocolate, ASP.NET Core, React, Relay",
      "Real-project exercises and production quirks",
      "Designed to lift the whole team at once",
    ],
    learnMoreHref: "/services/training",
    learnMoreLabel: "Explore Training",
  },
};

interface SplitRow {
  readonly id: string;
  readonly eyebrow: string;
  readonly without: string;
  readonly withCc: string;
  readonly linkLabel: string;
  readonly linkHref: string;
}

const SPLIT_ROWS: readonly SplitRow[] = [
  {
    id: "architecture",
    eyebrow: "Architecture",
    without:
      "Schema decisions made in a hurry, then carried for years by whoever happens to be in the room.",
    withCc:
      "Senior engineers review the design, name the trade-offs, and write them down before code lands.",
    linkLabel: "Advisory",
    linkHref: "/services/advisory",
  },
  {
    id: "incidents",
    eyebrow: "Incidents",
    without:
      "A 2 a.m. page, a stack trace nobody recognizes, and a thread that goes quiet for hours.",
    withCc:
      "A private channel, defined response times, and the same engineers who ship Hot Chocolate, Fusion, and Nitro.",
    linkLabel: "Support",
    linkHref: "/services/support",
  },
  {
    id: "ramp",
    eyebrow: "Team ramp-up",
    without:
      "Engineers piecing GraphQL together from blog posts, with patterns that age out before the next release.",
    withCc:
      "Corporate Training tuned to beginner, advanced, or mixed teams, with real-project exercises.",
    linkLabel: "Training",
    linkHref: "/services/training",
  },
  {
    id: "enterprise",
    eyebrow: "Enterprise rollout",
    without:
      "Per-team contracts, mismatched SLAs, and procurement paperwork that never quite fits the work.",
    withCc:
      "One agreement that bundles Advisory hours, Enterprise Support, and on-site training across business units.",
    linkLabel: "Enterprise",
    linkHref: SUPPORT_CONTACT,
  },
];

interface DecisionRow {
  readonly id: string;
  readonly need: string;
  readonly withoutLine: string;
  readonly route: string;
  readonly destinations: readonly {
    readonly label: string;
    readonly href: string;
  }[];
}

const DECISION_ROWS: readonly DecisionRow[] = [
  {
    id: "right-now",
    need: "Need help right now",
    withoutLine:
      "You wait on a community thread, hope someone has seen this exact error, and lose the morning to it.",
    route: "A single call, or an ongoing Support plan with a defined SLA.",
    destinations: [
      { label: "Advisory consult", href: "/services/advisory" },
      { label: "Support plans", href: "/services/support" },
    ],
  },
  {
    id: "expert-delivery",
    need: "Need expert delivery",
    withoutLine:
      "You hand the project to a generalist contractor and hope GraphQL is one of the things they actually know.",
    route:
      "A scoped statement of work where our engineers ship the result with you.",
    destinations: [
      { label: "Advisory: Contracting", href: "/services/advisory" },
    ],
  },
  {
    id: "team-trained",
    need: "Need your team trained",
    withoutLine:
      "A generic course off the shelf that covers everything except the patterns your codebase actually uses.",
    route:
      "Corporate training or a hands-on workshop tuned to your team and stack.",
    destinations: [{ label: "Training", href: "/services/training" }],
  },
];

interface EnterpriseBullet {
  readonly label: string;
  readonly value: string;
}

const ENTERPRISE_BULLETS: readonly EnterpriseBullet[] = [
  {
    label: "Coverage",
    value: "Phone support and unlimited critical incidents",
  },
  { label: "Account", value: "Dedicated account manager and status reviews" },
  { label: "Delivery", value: "Embedded engineers across teams and units" },
  { label: "Contract", value: "Custom SLAs and procurement-ready paperwork" },
];

interface ClosingItem {
  readonly label: string;
  readonly without: string;
  readonly withCc: string;
}

const CLOSING_ITEMS: readonly ClosingItem[] = [
  {
    label: "Advisory",
    without: "Ad-hoc help when someone has a spare hour",
    withCc: "Hourly or scoped contracting",
  },
  {
    label: "Support",
    without: "Public threads with no SLA",
    withCc: "From $450 per month, with response times",
  },
  {
    label: "Training",
    without: "Generic course, mixed outcomes",
    withCc: "Custom, tailored to your team",
  },
  {
    label: "Enterprise",
    without: "Per-team contracts that never line up",
    withCc: "Custom SLAs and procurement",
  },
];

export default function ServicesPreviewV4Page() {
  return (
    <div className="mx-auto w-full max-w-6xl px-4 sm:px-6">
      <Hero />
      <TheSplit />
      <TracksSideBySide />
      <DecideByNeed />
      <EnterpriseBand />
      <ClosingCta />
    </div>
  );
}

function Hero() {
  return (
    <section className="pt-10 pb-16 text-center sm:pt-16 sm:pb-20">
      <p className="text-cc-nav-label font-mono text-xs tracking-[0.18em] uppercase">
        ChilliCream GraphQL services
      </p>
      <h1 className="font-heading text-cc-heading text-h1 sm:text-hero mx-auto mt-5 max-w-4xl font-semibold tracking-tight">
        GraphQL, with you on the other side of the line.
      </h1>
      <p className="text-cc-ink text-lead mx-auto mt-6 max-w-2xl text-pretty">
        Two ways to run a GraphQL stack: hand-rolled, or with the team behind
        Hot Chocolate, Fusion, and Nitro. Read the page as a ledger. The line
        down the middle is the difference.
      </p>
      <div className="mt-9 flex flex-wrap items-center justify-center gap-3">
        <SolidButton href={BOOKING_URL}>Book a 60-min call</SolidButton>
        <OutlineButton href="#split">See the split</OutlineButton>
      </div>
      <HeroLegend />
    </section>
  );
}

function HeroLegend() {
  return (
    <div
      aria-hidden="true"
      className="mx-auto mt-10 flex max-w-md items-center justify-center gap-4 font-mono text-[0.65rem] tracking-[0.18em] uppercase"
    >
      <span className="text-cc-nav-label inline-flex items-center gap-2">
        <CrossChip />
        On your own
      </span>
      <span aria-hidden="true" className="bg-cc-card-border h-4 w-px" />
      <span className="text-cc-heading inline-flex items-center gap-2">
        <SmallCheckChip />
        With ChilliCream
      </span>
    </div>
  );
}

function TheSplit() {
  return (
    <section
      id="split"
      aria-labelledby="split-heading"
      className="relative py-16 sm:py-24"
    >
      <SectionHead
        id="split-heading"
        eyebrow="The ledger"
        title="Where the line falls."
        intro="Four places GraphQL teams quietly bleed time, with the ChilliCream alternative on the right of the rule."
      />
      <CompareTable rows={SPLIT_ROWS} />
    </section>
  );
}

function CompareTable({ rows }: { readonly rows: readonly SplitRow[] }) {
  return (
    <div className="relative mt-12">
      <Centerline />
      <ol className="relative flex flex-col">
        {rows.map((row, index) => (
          <CompareRow
            key={row.id}
            row={row}
            isLast={index === rows.length - 1}
          />
        ))}
      </ol>
    </div>
  );
}

function CompareRow({
  row,
  isLast,
}: {
  readonly row: SplitRow;
  readonly isLast: boolean;
}) {
  return (
    <li
      className={`relative grid grid-cols-1 gap-x-12 gap-y-6 py-8 md:grid-cols-2 ${
        isLast ? "" : "border-cc-ink-faint border-b border-dashed"
      }`}
    >
      <RowDiamond />
      <div className="md:pr-10">
        <p className="text-cc-nav-label font-mono text-[0.65rem] tracking-[0.18em] uppercase">
          {row.eyebrow}
        </p>
        <div className="mt-3 flex items-start gap-3">
          <CrossChip />
          <p className="text-cc-ink-dim text-body">{row.without}</p>
        </div>
      </div>
      <div className="md:pl-10">
        <p className="text-cc-nav-label font-mono text-[0.65rem] tracking-[0.18em] uppercase opacity-0 md:opacity-100">
          {row.eyebrow}
        </p>
        <div className="mt-3 flex items-start gap-3">
          <CheckChip />
          <div>
            <p className="text-cc-ink text-lg font-semibold">{row.withCc}</p>
            <a
              href={row.linkHref}
              className="font-mono text-[0.65rem] tracking-[0.18em] uppercase"
              style={{ color: ACCENT }}
            >
              <span aria-hidden="true">{"-> "}</span>
              {row.linkLabel}
            </a>
          </div>
        </div>
      </div>
    </li>
  );
}

function TracksSideBySide() {
  return (
    <section
      aria-labelledby="tracks-heading"
      className="relative py-16 sm:py-24"
    >
      <SectionHead
        id="tracks-heading"
        eyebrow="Three tracks"
        title="Three doors, one team."
        intro="Advisory and Support sit on either side of the line. Training crosses it, because it serves the whole team."
      />

      <div className="relative mt-12">
        <Centerline className="hidden md:block md:[clip-path:inset(0_0_22rem_0)]" />
        <div className="relative grid gap-6 md:grid-cols-2 md:gap-12">
          <TrackCard track={TRACKS.advisory} align="left" />
          <TrackCard track={TRACKS.support} align="right" highlighted />
        </div>
        <TrainingBand track={TRACKS.training} />
      </div>
    </section>
  );
}

function TrackCard({
  track,
  align,
  highlighted = false,
}: {
  readonly track: ServiceTrack;
  readonly align: "left" | "right";
  readonly highlighted?: boolean;
}) {
  const chip = align === "right" ? <CheckChip /> : <CrossChipNeutral />;
  return (
    <div
      className={`bg-cc-card-bg border-cc-card-border hover:border-cc-card-border-hover flex h-full flex-col rounded-2xl border p-7 transition-colors sm:p-9 ${
        align === "right" ? "md:ml-2" : "md:mr-2"
      }`}
      style={
        highlighted
          ? {
              boxShadow: `inset 0 0 0 1px rgba(22, 185, 228, 0.35)`,
            }
          : undefined
      }
    >
      <div className="flex items-center gap-3">
        {chip}
        <p className="text-cc-nav-label font-mono text-[0.65rem] tracking-[0.18em] uppercase">
          {track.eyebrow}
        </p>
      </div>
      <h3 className="font-heading text-cc-heading text-h3 mt-3 font-semibold">
        {track.name}
      </h3>
      <p className="text-cc-ink-dim text-body mt-3">{track.tagline}</p>

      <div className="mt-6 flex items-baseline gap-2">
        <span
          className="font-heading text-h4 font-semibold"
          style={{ color: highlighted ? ACCENT : undefined }}
        >
          {track.priceLine}
        </span>
        <span className="text-cc-nav-label font-mono text-xs">
          {track.priceNote}
        </span>
      </div>

      <div
        aria-hidden="true"
        className="border-cc-ink-faint my-6 border-t border-dashed"
      />

      <ul className="flex flex-1 flex-col gap-3">
        {track.bullets.map((bullet) => (
          <li key={bullet} className="flex items-start gap-3">
            <span className="mt-[5px] flex-none" style={{ color: ACCENT }}>
              <CheckIcon />
            </span>
            <span className="text-cc-ink text-sm">{bullet}</span>
          </li>
        ))}
      </ul>

      <div className="mt-8">
        <SolidButton href={track.learnMoreHref} className="w-full">
          {track.learnMoreLabel}
        </SolidButton>
      </div>
    </div>
  );
}

function TrainingBand({ track }: { readonly track: ServiceTrack }) {
  return (
    <div className="bg-cc-card-bg border-cc-card-border relative mt-10 grid gap-8 rounded-2xl border p-7 sm:p-10 md:grid-cols-[1fr_auto_1fr] md:items-center">
      <div>
        <p className="text-cc-nav-label font-mono text-[0.65rem] tracking-[0.18em] uppercase">
          {track.eyebrow}
        </p>
        <h3 className="font-heading text-cc-heading text-h3 mt-3 font-semibold">
          {track.name}
        </h3>
        <p className="text-cc-ink-dim text-body mt-3">{track.tagline}</p>
        <div className="mt-5 flex items-baseline gap-2">
          <span
            className="font-heading text-h4 font-semibold"
            style={{ color: ACCENT }}
          >
            {track.priceLine}
          </span>
          <span className="text-cc-nav-label font-mono text-xs">
            {track.priceNote}
          </span>
        </div>
        <div className="mt-6">
          <SolidButton href={track.learnMoreHref}>
            {track.learnMoreLabel}
          </SolidButton>
        </div>
      </div>
      <div
        aria-hidden="true"
        className="bg-cc-card-border hidden h-full w-px md:block"
      />
      <ul className="flex flex-col gap-3">
        {track.bullets.map((bullet) => (
          <li key={bullet} className="flex items-start gap-3">
            <span className="mt-[5px] flex-none" style={{ color: ACCENT }}>
              <CheckIcon />
            </span>
            <span className="text-cc-ink text-sm">{bullet}</span>
          </li>
        ))}
      </ul>
    </div>
  );
}

function DecideByNeed() {
  return (
    <section
      id="decide"
      aria-labelledby="decide-heading"
      className="relative py-16 sm:py-24"
    >
      <SectionHead
        id="decide-heading"
        eyebrow="Decide by need"
        title="Which side are you on today?"
        intro="Pick the row that sounds like you. Left is what happens without us, right is where we point you."
      />

      <div className="relative mt-12">
        <Centerline />
        <ol className="relative flex flex-col">
          {DECISION_ROWS.map((row, index) => (
            <DecideRow
              key={row.id}
              row={row}
              isLast={index === DECISION_ROWS.length - 1}
            />
          ))}
        </ol>
      </div>
    </section>
  );
}

function DecideRow({
  row,
  isLast,
}: {
  readonly row: DecisionRow;
  readonly isLast: boolean;
}) {
  return (
    <li
      className={`relative grid grid-cols-1 gap-x-12 gap-y-6 py-8 md:grid-cols-2 ${
        isLast ? "" : "border-cc-ink-faint border-b border-dashed"
      }`}
    >
      <RowDiamond />
      <div className="md:pr-10">
        <div className="flex items-start gap-3">
          <CrossChip />
          <div>
            <p className="font-heading text-cc-heading text-lg font-semibold">
              {row.need}
            </p>
            <p className="text-cc-ink-dim text-body mt-2">{row.withoutLine}</p>
          </div>
        </div>
      </div>
      <div className="md:pl-10">
        <div className="flex items-start gap-3">
          <CheckChip />
          <div className="flex-1">
            <p className="text-cc-ink text-lg font-semibold">{row.route}</p>
            <div className="mt-4 flex flex-wrap gap-2">
              {row.destinations.map((destination) => (
                <OutlineButton key={destination.href} href={destination.href}>
                  {destination.label}
                </OutlineButton>
              ))}
            </div>
          </div>
        </div>
      </div>
    </li>
  );
}

function EnterpriseBand() {
  return (
    <section
      aria-labelledby="enterprise-heading"
      className="relative py-16 sm:py-24"
    >
      <div className="bg-cc-card-bg border-cc-card-border relative grid gap-10 rounded-2xl border p-8 sm:p-12 md:grid-cols-2 md:items-start">
        <div className="md:pr-10">
          <p
            className="font-mono text-xs tracking-[0.18em] uppercase"
            style={{ color: ACCENT }}
          >
            Enterprise
          </p>
          <h2
            id="enterprise-heading"
            className="font-heading text-cc-heading text-h3 mt-3 font-semibold"
          >
            One contract, every team.
          </h2>
          <p className="text-cc-ink text-body mt-4">
            For organizations standardizing on Hot Chocolate, Fusion, and Nitro
            across business units. We bundle Advisory hours, an Enterprise
            Support plan, and on-site training into one agreement that matches
            how procurement actually buys.
          </p>
          <div className="mt-7 flex flex-wrap gap-3">
            <SolidButton href={ENTERPRISE_MAILTO}>Talk to sales</SolidButton>
            <OutlineButton href={SUPPORT_CONTACT}>
              Enterprise Support details
            </OutlineButton>
          </div>
        </div>
        <div className="relative md:pl-10">
          <div
            aria-hidden="true"
            className="bg-cc-card-border absolute top-0 bottom-0 left-0 hidden w-px md:block"
          />
          <ul className="grid gap-4 sm:grid-cols-2">
            {ENTERPRISE_BULLETS.map((bullet) => (
              <EnterpriseBulletCard key={bullet.label} bullet={bullet} />
            ))}
          </ul>
        </div>
      </div>
    </section>
  );
}

function EnterpriseBulletCard({
  bullet,
}: {
  readonly bullet: EnterpriseBullet;
}) {
  return (
    <li className="bg-cc-surface border-cc-card-border flex h-full flex-col rounded-2xl border p-5">
      <span className="text-cc-nav-label font-mono text-[0.65rem] tracking-[0.18em] uppercase">
        {bullet.label}
      </span>
      <span className="text-cc-ink mt-2 text-sm leading-relaxed">
        {bullet.value}
      </span>
    </li>
  );
}

function ClosingCta() {
  return (
    <section
      aria-labelledby="closing-heading"
      className="relative py-16 sm:py-24"
    >
      <SectionHead
        id="closing-heading"
        eyebrow="Still not sure"
        title="One call is usually enough to know."
        intro="Book a 60-minute call with an engineer. You will leave with a clear next step, or a candid no."
      />

      <div className="relative mt-12">
        <Centerline />
        <div className="relative grid grid-cols-1 gap-x-12 gap-y-10 md:grid-cols-2">
          <RowDiamond />
          <ul className="flex flex-col md:pr-10">
            {CLOSING_ITEMS.map((item, index) => (
              <ClosingRow
                key={item.label}
                item={item}
                isLast={index === CLOSING_ITEMS.length - 1}
              />
            ))}
          </ul>
          <div className="flex flex-col gap-3 md:items-start md:pl-10">
            <SolidButton href={BOOKING_URL}>Book a 60-min call</SolidButton>
            <OutlineButton href={CONTACT_MAILTO}>Email us</OutlineButton>
            <p className="text-cc-ink-dim mt-3 text-sm">
              You will leave with a clear next step, or a candid no.
            </p>
          </div>
        </div>
      </div>
    </section>
  );
}

function ClosingRow({
  item,
  isLast,
}: {
  readonly item: ClosingItem;
  readonly isLast: boolean;
}) {
  return (
    <li
      className={`grid grid-cols-[auto_1fr_auto_1fr] items-start gap-3 py-4 ${
        isLast ? "" : "border-cc-ink-faint border-b border-dashed"
      }`}
    >
      <CrossChip />
      <span className="text-cc-ink-dim text-sm">{item.without}</span>
      <CheckChip />
      <span className="text-cc-ink text-sm">
        <span className="text-cc-nav-label block font-mono text-[0.6rem] tracking-[0.18em] uppercase">
          {item.label}
        </span>
        {item.withCc}
      </span>
    </li>
  );
}

function SectionHead({
  id,
  eyebrow,
  title,
  intro,
}: {
  readonly id: string;
  readonly eyebrow: string;
  readonly title: string;
  readonly intro: ReactNode;
}) {
  return (
    <div className="relative z-10">
      <p className="text-cc-nav-label font-mono text-xs tracking-[0.18em] uppercase">
        {eyebrow}
      </p>
      <h2
        id={id}
        className="font-heading text-cc-heading text-h2 mt-3 max-w-3xl font-semibold tracking-tight"
      >
        {title}
      </h2>
      <p className="text-cc-ink text-body mt-4 max-w-2xl">{intro}</p>
    </div>
  );
}

function Centerline({ className = "" }: { readonly className?: string }) {
  return (
    <div
      aria-hidden="true"
      className={`pointer-events-none absolute inset-y-0 left-1/2 hidden w-px -translate-x-1/2 md:block ${className}`}
      style={{
        background:
          "linear-gradient(to bottom, rgba(255,255,255,0.04), rgba(255,255,255,0.10) 12%, rgba(255,255,255,0.10) 88%, rgba(255,255,255,0.04))",
      }}
    />
  );
}

function RowDiamond() {
  return (
    <span
      aria-hidden="true"
      className="pointer-events-none absolute top-10 left-1/2 hidden h-2 w-2 -translate-x-1/2 rotate-45 md:block"
      style={{ backgroundColor: ACCENT, boxShadow: `0 0 0 3px #0b0f1a` }}
    />
  );
}

function CrossChip() {
  return (
    <span
      aria-hidden="true"
      className="border-cc-ink-faint text-cc-ink-faint inline-flex h-5 w-5 flex-none items-center justify-center rounded-full border"
    >
      <svg viewBox="0 0 16 16" width="10" height="10">
        <line
          x1="4"
          y1="12"
          x2="12"
          y2="4"
          stroke="currentColor"
          strokeWidth="1.5"
          strokeLinecap="round"
        />
      </svg>
    </span>
  );
}

function CrossChipNeutral() {
  return <CrossChip />;
}

function CheckChip() {
  return (
    <span
      aria-hidden="true"
      className="inline-flex h-5 w-5 flex-none items-center justify-center rounded-full"
      style={{
        backgroundColor: "rgba(22, 185, 228, 0.12)",
        color: ACCENT,
        boxShadow: "inset 0 0 0 1px rgba(22, 185, 228, 0.45)",
      }}
    >
      <CheckIcon size={10} />
    </span>
  );
}

function SmallCheckChip() {
  return <CheckChip />;
}

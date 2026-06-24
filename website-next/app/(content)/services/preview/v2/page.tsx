import type { Metadata } from "next";
import Link from "next/link";
import type { ComponentType, ReactNode } from "react";

import { CheckIcon } from "@/src/components/CheckIcon";
import { OutlineButton, SolidButton } from "@/src/design-system/Button";

export const metadata: Metadata = {
  title: "ChilliCream Services: Your Engagement Journey",
  description:
    "From a one-off call to embedded experts: route to the right ChilliCream GraphQL service. Advisory, Support with SLAs, and team Training, picked by stage.",
  keywords: [
    "ChilliCream services",
    "GraphQL consulting",
    "GraphQL advisory",
    "GraphQL support",
    "Hot Chocolate training",
    "Fusion federation",
    "engagement journey",
  ],
  openGraph: {
    title: "ChilliCream Services: Your Engagement Journey",
    description:
      "From a one-off call to embedded experts: route to the right ChilliCream GraphQL service. Advisory, Support with SLAs, and team Training, picked by stage.",
  },
  robots: { index: false, follow: false },
};

const BOOKING_URL = "https://calendly.com/chillicream/60min";
const CONTACT_MAILTO = "mailto:contact@chillicream.com";
const ENTERPRISE_MAILTO =
  "mailto:contact@chillicream.com?subject=Enterprise%20engagement";

export default function ServicesEngagementJourneyPage() {
  return (
    <>
      <Hero />
      <Journey />
      <DecisionAid />
      <Credibility />
      <EnterpriseBand />
      <ClosingCta />
    </>
  );
}

function Hero() {
  return (
    <section className="relative pt-16 pb-12 sm:pt-24 sm:pb-16">
      <div className="text-cc-nav-label mb-5 font-mono text-xs font-semibold tracking-[0.2em] uppercase">
        Services / Engagement journey
      </div>
      <h1 className="font-heading text-cc-heading max-w-4xl text-4xl leading-[1.05] font-semibold tracking-tight sm:text-5xl lg:text-6xl">
        From a <SpectrumWord>one-off call</SpectrumWord> to{" "}
        <span className="text-cc-accent">embedded experts</span>.
      </h1>
      <p className="text-cc-prose mt-7 max-w-2xl text-base leading-relaxed sm:text-lg">
        Where you are with your GraphQL platform decides what kind of help you
        need. The journey below is the one most teams travel: a scoped call,
        then contracted work, then support with SLAs, and training once the team
        grows. Step in at whichever stop fits today.
      </p>
      <div className="mt-9 flex flex-wrap items-center gap-3">
        <SolidButton href={BOOKING_URL}>Book a 60 minute call</SolidButton>
        <OutlineButton href="#journey">See the journey</OutlineButton>
      </div>
      <div className="text-cc-ink-dim mt-7 flex flex-wrap items-center gap-x-6 gap-y-2 font-mono text-xs">
        <span>Hourly to retained</span>
        <span className="text-cc-ink-faint">|</span>
        <span>NDA on request</span>
        <span className="text-cc-ink-faint">|</span>
        <span>Remote, async friendly</span>
      </div>
    </section>
  );
}

interface SpectrumWordProps {
  readonly children: ReactNode;
}

function SpectrumWord({ children }: SpectrumWordProps) {
  return (
    <span className="bg-[linear-gradient(90deg,#16b9e4_0%,#7c92c6_55%,#f0786a_100%)] bg-clip-text text-transparent">
      {children}
    </span>
  );
}

interface Stop {
  readonly step: string;
  readonly tag: string;
  readonly title: string;
  readonly subtitle: string;
  readonly signal: string;
  readonly summary: string;
  readonly highlights: readonly string[];
  readonly ctaLabel: string;
  readonly ctaHref: string;
  readonly secondaryLabel?: string;
  readonly secondaryHref?: string;
  readonly Glyph: ComponentType<{ readonly className?: string }>;
  readonly accent: boolean;
}

const STOPS: readonly Stop[] = [
  {
    step: "01",
    tag: "Stop 01 / Advisory",
    title: "Quick consult with an engineer",
    subtitle: "One hour, one decision, no deck.",
    signal:
      "You have a question that needs a senior answer this week, not a sales cycle.",
    summary:
      "Book an hour with an engineer who has shipped this kind of work. We listen, ask the awkward questions, and end with a written summary of what is in scope and how we would start.",
    highlights: [
      "Architecture and schema sounding board",
      "Code review on a thorny PR",
      "Honest go or no go on a planned migration",
    ],
    ctaLabel: "See Advisory",
    ctaHref: "/services/advisory",
    secondaryLabel: "Book a call",
    secondaryHref: BOOKING_URL,
    Glyph: ConsultGlyph,
    accent: false,
  },
  {
    step: "02",
    tag: "Stop 02 / Advisory",
    title: "Scoped contracting engagement",
    subtitle: "Per project, written statement of work.",
    signal:
      "The work is bigger than a call. You want a team that has done this before to come in and build it with you.",
    summary:
      "We turn the scoping call into a written plan and pair you with engineers who own the change end to end. Common shapes are federation rollouts, schema cleanup, performance audits, and .NET native migrations.",
    highlights: [
      "Proof of concept that proves the risky bit first",
      "Implementation in your repo, your tracker, your chat",
      "Written handover so your team owns it after",
    ],
    ctaLabel: "See Advisory",
    ctaHref: "/services/advisory",
    secondaryLabel: "Email us",
    secondaryHref:
      "mailto:contact@chillicream.com?subject=Contracting%20engagement",
    Glyph: ContractGlyph,
    accent: true,
  },
  {
    step: "03",
    tag: "Stop 03 / Support",
    title: "Ongoing support with SLAs",
    subtitle: "Community, Startup, Business, Enterprise.",
    signal:
      "The system is in production. The team needs a number to call when something breaks, not a forum post.",
    summary:
      "Plans start at a free community Slack and scale to retainers with private channels, incident SLAs, phone support, and a dedicated account manager. Pick the tier that matches how much downtime you can afford.",
    highlights: [
      "Community on a public Slack, free",
      "Startup at $450 per month with a private channel",
      "Business at $1,300 per month with email and incident SLAs",
      "Enterprise on a custom contract with phone support",
    ],
    ctaLabel: "See Support plans",
    ctaHref: "/services/support",
    secondaryLabel: "Contact Support",
    secondaryHref: "/services/support/contact",
    Glyph: SupportGlyph,
    accent: false,
  },
  {
    step: "04",
    tag: "Stop 04 / Training",
    title: "Team training when you grow",
    subtitle: "Corporate Training and Corporate Workshop.",
    signal:
      "The platform works. You are hiring. New people need the same mental model as the team that built it.",
    summary:
      "Training is tuned to your group: beginner, advanced, or mixed. Workshops go hands on with ASP.NET Core, Hot Chocolate, and Relay so engineers leave with a real project in their hands, not slides.",
    highlights: [
      "Curriculum tailored to your skill mix",
      "Hands on workshop on a real project",
      "Catches up new hires without slowing the team",
    ],
    ctaLabel: "See Training",
    ctaHref: "/services/training",
    secondaryLabel: "Talk to us",
    secondaryHref:
      "mailto:contact@chillicream.com?subject=Corporate%20Training",
    Glyph: TrainingGlyph,
    accent: false,
  },
];

function Journey() {
  return (
    <section id="journey" className="py-16 sm:py-20">
      <SectionHeader
        eyebrow="The journey"
        title="Four stops, in the order most teams travel"
        intro="You do not have to start at stop 01. Most teams enter where the pain is loudest, then move down the journey as the platform matures."
      />
      <ol className="relative mt-12 space-y-6">
        <span
          aria-hidden="true"
          className="bg-cc-ink-faint absolute top-0 bottom-0 left-7 hidden w-px md:block"
        />
        {STOPS.map((stop, index) => (
          <JourneyRow
            key={stop.step}
            stop={stop}
            isLast={index === STOPS.length - 1}
          />
        ))}
      </ol>
    </section>
  );
}

interface JourneyRowProps {
  readonly stop: Stop;
  readonly isLast: boolean;
}

function JourneyRow({ stop, isLast }: JourneyRowProps) {
  const {
    step,
    tag,
    title,
    subtitle,
    signal,
    summary,
    highlights,
    ctaLabel,
    ctaHref,
    secondaryLabel,
    secondaryHref,
    Glyph,
    accent,
  } = stop;
  return (
    <li className="relative">
      <span
        aria-hidden="true"
        className={`bg-cc-surface ring-cc-card-border absolute top-7 left-7 hidden h-3 w-3 -translate-x-1/2 rounded-full ring-2 md:block ${
          accent ? "bg-cc-accent ring-cc-accent/40" : ""
        }`}
      />
      <div
        className={`border-cc-card-border bg-cc-card-bg/60 hover:border-cc-card-border-hover overflow-hidden rounded-3xl border transition-colors md:ml-16 ${
          accent ? "border-cc-accent/50" : ""
        } ${isLast ? "" : ""}`}
      >
        <div className="grid gap-0 md:grid-cols-[1fr_18rem]">
          <div className="p-7 sm:p-9">
            <div className="text-cc-nav-label flex items-center gap-3 font-mono text-xs tracking-[0.18em] uppercase">
              <span className="font-heading text-cc-ink-faint text-3xl leading-none font-semibold">
                {step}
              </span>
              <span>{tag}</span>
            </div>
            <h3 className="font-heading text-cc-heading mt-4 text-2xl font-semibold sm:text-3xl">
              {title}
            </h3>
            <p className="text-cc-accent mt-2 font-mono text-xs tracking-wide">
              {subtitle}
            </p>
            <p className="text-cc-prose mt-4 text-sm leading-relaxed sm:text-base">
              {signal}
            </p>
            <p className="text-cc-ink-dim mt-3 max-w-2xl text-sm leading-relaxed">
              {summary}
            </p>
            <ul className="mt-6 grid gap-2 sm:grid-cols-2">
              {highlights.map((item) => (
                <li
                  key={item}
                  className="text-cc-ink flex items-start gap-2 text-sm"
                >
                  <span className="text-cc-accent mt-1 flex-none">
                    <CheckIcon />
                  </span>
                  <span>{item}</span>
                </li>
              ))}
            </ul>
            <div className="mt-7 flex flex-wrap gap-3">
              {accent ? (
                <SolidButton href={ctaHref}>{ctaLabel}</SolidButton>
              ) : (
                <OutlineButton href={ctaHref}>{ctaLabel}</OutlineButton>
              )}
              {secondaryLabel && secondaryHref ? (
                <Link
                  href={secondaryHref}
                  className="text-cc-accent hover:text-cc-accent-hover inline-flex items-center text-sm font-medium underline-offset-4 hover:underline"
                >
                  {secondaryLabel} {"->"}
                </Link>
              ) : null}
            </div>
          </div>
          <div className="border-cc-card-border relative flex items-center justify-center border-t bg-[radial-gradient(circle_at_30%_20%,rgba(94,234,212,0.08),transparent_60%)] p-8 md:border-t-0 md:border-l">
            <Glyph className="h-32 w-32 sm:h-40 sm:w-40" />
          </div>
        </div>
      </div>
    </li>
  );
}

interface DecisionRow {
  readonly situation: string;
  readonly destination: string;
  readonly href: string;
  readonly note: string;
}

const DECISIONS: readonly DecisionRow[] = [
  {
    situation:
      "You have one focused question and want a senior answer this week.",
    destination: "Advisory / Consulting",
    href: "/services/advisory",
    note: "Hourly. Easiest place to start.",
  },
  {
    situation:
      "You need a feature, migration, or rollout built with you, not for you.",
    destination: "Advisory / Contracting",
    href: "/services/advisory",
    note: "Per project. Written statement of work.",
  },
  {
    situation:
      "You are in production and need someone on call when things break.",
    destination: "Support / Startup or Business",
    href: "/services/support",
    note: "$450 or $1,300 per month, incident SLAs included.",
  },
  {
    situation:
      "Your organization needs custom SLAs, phone support, and a named account manager.",
    destination: "Support / Enterprise",
    href: "/services/support",
    note: "Custom contract. Talk to us.",
  },
  {
    situation: "You are hiring and want new engineers ramped up the same way.",
    destination: "Training / Corporate Training",
    href: "/services/training",
    note: "Curriculum tuned to your skill mix.",
  },
  {
    situation:
      "Your team needs hands on time with Hot Chocolate, Fusion, and Relay.",
    destination: "Training / Corporate Workshop",
    href: "/services/training",
    note: "Workshop on a real project, with experts.",
  },
];

function DecisionAid() {
  return (
    <section id="which-one" className="py-16 sm:py-20">
      <SectionHeader
        eyebrow="Decision aid"
        title="Which one is right for me?"
        intro="If the journey order does not match where you are today, jump straight to the right page from here."
      />
      <div className="border-cc-card-border bg-cc-card-bg/40 mt-12 overflow-hidden rounded-2xl border">
        <ul className="divide-y divide-[color:var(--color-cc-card-border)]">
          {DECISIONS.map((row) => (
            <li key={row.situation}>
              <Link
                href={row.href}
                className="hover:bg-cc-card-bg/80 grid gap-3 p-6 transition-colors sm:grid-cols-[1.4fr_1fr] sm:items-center sm:gap-6 sm:p-7"
              >
                <div>
                  <p className="text-cc-heading text-sm leading-relaxed sm:text-base">
                    {row.situation}
                  </p>
                  <p className="text-cc-ink-dim mt-1 text-xs sm:text-sm">
                    {row.note}
                  </p>
                </div>
                <div className="flex items-center justify-between gap-3 sm:justify-end">
                  <span className="text-cc-accent font-mono text-xs tracking-[0.14em] uppercase">
                    {row.destination}
                  </span>
                  <span aria-hidden="true" className="text-cc-accent text-base">
                    {"->"}
                  </span>
                </div>
              </Link>
            </li>
          ))}
        </ul>
      </div>
    </section>
  );
}

function Credibility() {
  return (
    <section id="who-you-work-with" className="py-16 sm:py-20">
      <SectionHeader
        eyebrow="Who you work with"
        title="The team behind Hot Chocolate, Fusion, and Nitro"
        intro="You are not handed off to a partner network. The engineers who maintain the products are the same engineers who pick up your engagement."
      />
      <div className="mt-12 grid gap-6 md:grid-cols-3">
        <CredibilityCard
          eyebrow="Hot Chocolate"
          title="The .NET GraphQL server"
          body="Source generated, schema first when you want it, code first when that fits better. We use it on every engagement; we know its sharp edges."
          href="/products/hot-chocolate"
        />
        <CredibilityCard
          eyebrow="Fusion"
          title="Composition at planning time"
          body="A federated gateway you run yourself. We have shipped Fusion rollouts that span half a dozen subgraphs and we will not pretend the migration is trivial."
          href="/products/fusion"
        />
        <CredibilityCard
          eyebrow="Nitro"
          title="Observability for GraphQL"
          body="Traces, schema usage, and operation health, all aware that the request is GraphQL. Configure Nitro telemetry and your support tier reads from the same signals."
          href="/products/nitro"
        />
      </div>
      <p className="text-cc-ink-dim mt-8 max-w-3xl text-sm leading-relaxed">
        We do not invent customer quotes here. If you want to talk to a current
        engagement before you sign anything, ask on the scoping call and we will
        arrange a reference.
      </p>
    </section>
  );
}

interface CredibilityCardProps {
  readonly eyebrow: string;
  readonly title: string;
  readonly body: string;
  readonly href: string;
}

function CredibilityCard({ eyebrow, title, body, href }: CredibilityCardProps) {
  return (
    <Link
      href={href}
      className="border-cc-card-border bg-cc-card-bg/60 hover:border-cc-card-border-hover group flex h-full flex-col rounded-2xl border p-6 transition-colors sm:p-7"
    >
      <div className="text-cc-nav-label font-mono text-xs tracking-[0.18em] uppercase">
        {eyebrow}
      </div>
      <h3 className="font-heading text-cc-heading group-hover:text-cc-accent mt-3 text-xl font-semibold transition-colors sm:text-2xl">
        {title}
      </h3>
      <p className="text-cc-prose mt-3 text-sm leading-relaxed">{body}</p>
      <span className="text-cc-accent mt-5 text-sm font-medium">
        Learn more {"->"}
      </span>
    </Link>
  );
}

function EnterpriseBand() {
  return (
    <section id="enterprise" className="py-16 sm:py-20">
      <div className="border-cc-card-border bg-cc-card-bg/70 relative overflow-hidden rounded-3xl border p-8 sm:p-12">
        <div className="text-cc-nav-label font-mono text-xs tracking-[0.2em] uppercase">
          Enterprise
        </div>
        <div className="mt-3 grid gap-8 lg:grid-cols-[1.5fr_1fr] lg:items-end">
          <div>
            <h2 className="font-heading text-cc-heading max-w-3xl text-3xl font-semibold sm:text-4xl">
              For organizations that need named accountability across teams.
            </h2>
            <p className="text-cc-prose mt-4 max-w-2xl text-sm leading-relaxed sm:text-base">
              Enterprise engagements layer advisory hours, retained support with
              custom SLAs, and training into one contract. You get a dedicated
              account manager, phone support, status reviews, and a single named
              engineering lead across the relationship.
            </p>
            <ul className="mt-6 grid gap-2 sm:grid-cols-2">
              {[
                "Unlimited critical incidents (24 hour SLA)",
                "Phone support and private issue tracking",
                "Dedicated account manager and status reviews",
                "Advisory hours and team training included",
              ].map((perk) => (
                <li
                  key={perk}
                  className="text-cc-ink flex items-start gap-2 text-sm"
                >
                  <span className="text-cc-accent mt-1 flex-none">
                    <CheckIcon />
                  </span>
                  <span>{perk}</span>
                </li>
              ))}
            </ul>
          </div>
          <div className="flex flex-col gap-3">
            <SolidButton href={ENTERPRISE_MAILTO}>
              Email contact@chillicream.com
            </SolidButton>
            <OutlineButton href="/services/support/contact?plan=Enterprise">
              Open the Enterprise contact form
            </OutlineButton>
            <p className="text-cc-ink-dim font-mono text-xs">
              NDA on request. Replies same business day in EU and US hours.
            </p>
          </div>
        </div>
      </div>
    </section>
  );
}

function ClosingCta() {
  return (
    <section className="py-16 sm:py-20">
      <div className="border-cc-accent/40 relative overflow-hidden rounded-3xl border bg-[linear-gradient(135deg,rgba(22,185,228,0.10),rgba(124,146,198,0.08)_55%,rgba(240,120,106,0.08))] p-8 sm:p-12">
        <div className="text-cc-nav-label font-mono text-xs tracking-[0.2em] uppercase">
          Next step
        </div>
        <h2 className="font-heading text-cc-heading mt-3 max-w-3xl text-3xl font-semibold sm:text-4xl">
          Tell us where you are on the journey.
        </h2>
        <p className="text-cc-prose mt-4 max-w-2xl text-sm leading-relaxed sm:text-base">
          One sentence is enough. We read it, decide whether we are the right
          team, and reply with a scoping call slot or a recommendation that does
          not involve us. Either way you walk away with a clearer next step.
        </p>
        <div className="mt-7 flex flex-wrap gap-3">
          <SolidButton href={BOOKING_URL}>Book a 60 minute call</SolidButton>
          <OutlineButton href={CONTACT_MAILTO}>
            Email contact@chillicream.com
          </OutlineButton>
        </div>
      </div>
    </section>
  );
}

interface SectionHeaderProps {
  readonly eyebrow: string;
  readonly title: string;
  readonly intro: string;
}

function SectionHeader({ eyebrow, title, intro }: SectionHeaderProps) {
  return (
    <header className="max-w-3xl">
      <div className="text-cc-nav-label font-mono text-xs font-semibold tracking-[0.2em] uppercase">
        {eyebrow}
      </div>
      <h2 className="font-heading text-cc-heading mt-3 text-3xl font-semibold sm:text-4xl">
        {title}
      </h2>
      <p className="text-cc-prose mt-4 text-sm leading-relaxed sm:text-base">
        {intro}
      </p>
    </header>
  );
}

interface GlyphProps {
  readonly className?: string;
}

function ConsultGlyph({ className }: GlyphProps) {
  return (
    <svg
      viewBox="0 0 160 160"
      className={className}
      role="img"
      aria-label="Quick consult call icon"
    >
      <defs>
        <linearGradient id="svc-consult-grad" x1="0" x2="1" y1="0" y2="1">
          <stop offset="0%" stopColor="#16b9e4" stopOpacity="0.9" />
          <stop offset="100%" stopColor="#5eead4" stopOpacity="0.7" />
        </linearGradient>
      </defs>
      <circle
        cx="80"
        cy="80"
        r="46"
        fill="none"
        stroke="url(#svc-consult-grad)"
        strokeWidth="1.4"
      />
      <circle
        cx="80"
        cy="80"
        r="30"
        fill="none"
        stroke="rgba(245,241,234,0.2)"
        strokeWidth="1"
        strokeDasharray="3 4"
      />
      <path
        d="M62 70 L98 70 M62 82 L92 82 M62 94 L82 94"
        stroke="#5eead4"
        strokeWidth="2"
        strokeLinecap="round"
      />
      <circle cx="118" cy="44" r="6" fill="#5eead4" />
      <circle
        cx="118"
        cy="44"
        r="10"
        fill="none"
        stroke="#5eead4"
        strokeOpacity="0.4"
        strokeWidth="1"
      />
    </svg>
  );
}

function ContractGlyph({ className }: GlyphProps) {
  return (
    <svg
      viewBox="0 0 160 160"
      className={className}
      role="img"
      aria-label="Scoped contracting icon"
    >
      <rect
        x="34"
        y="22"
        width="92"
        height="116"
        rx="10"
        fill="rgba(12,19,34,0.9)"
        stroke="#5eead4"
        strokeOpacity="0.6"
        strokeWidth="1.4"
      />
      {[44, 58, 72, 86, 100, 114].map((y, index) => (
        <rect
          key={y}
          x="46"
          y={y}
          width={[68, 56, 64, 48, 60, 40][index]}
          height="5"
          rx="2.5"
          fill={index === 5 ? "#5eead4" : "rgba(245,241,234,0.32)"}
          opacity={index === 5 ? 1 : 0.7}
        />
      ))}
      <path
        d="M96 124 L108 134 L122 116"
        fill="none"
        stroke="#5eead4"
        strokeWidth="2"
        strokeLinecap="round"
        strokeLinejoin="round"
      />
    </svg>
  );
}

function SupportGlyph({ className }: GlyphProps) {
  return (
    <svg
      viewBox="0 0 160 160"
      className={className}
      role="img"
      aria-label="Ongoing support icon"
    >
      <defs>
        <linearGradient id="svc-support-grad" x1="0" x2="0" y1="0" y2="1">
          <stop offset="0%" stopColor="#5eead4" stopOpacity="0.8" />
          <stop offset="100%" stopColor="#5eead4" stopOpacity="0.2" />
        </linearGradient>
      </defs>
      <path
        d="M80 26 L130 50 L130 92 C130 116 108 132 80 138 C52 132 30 116 30 92 L30 50 Z"
        fill="rgba(12,19,34,0.9)"
        stroke="url(#svc-support-grad)"
        strokeWidth="1.6"
      />
      <path
        d="M58 84 L74 100 L104 66"
        fill="none"
        stroke="#5eead4"
        strokeWidth="2.5"
        strokeLinecap="round"
        strokeLinejoin="round"
      />
      <circle
        cx="80"
        cy="82"
        r="50"
        fill="none"
        stroke="rgba(245,241,234,0.12)"
        strokeWidth="1"
        strokeDasharray="2 4"
      />
    </svg>
  );
}

function TrainingGlyph({ className }: GlyphProps) {
  return (
    <svg
      viewBox="0 0 160 160"
      className={className}
      role="img"
      aria-label="Team training icon"
    >
      <path
        d="M24 70 L80 44 L136 70 L80 96 Z"
        fill="rgba(12,19,34,0.9)"
        stroke="#5eead4"
        strokeWidth="1.6"
        strokeLinejoin="round"
      />
      <path
        d="M52 82 L52 104 C52 114 64 122 80 122 C96 122 108 114 108 104 L108 82"
        fill="none"
        stroke="rgba(245,241,234,0.32)"
        strokeWidth="1.4"
        strokeLinejoin="round"
      />
      <line
        x1="128"
        y1="74"
        x2="128"
        y2="110"
        stroke="#5eead4"
        strokeWidth="1.6"
        strokeLinecap="round"
      />
      <circle cx="128" cy="114" r="3" fill="#5eead4" />
      <circle cx="80" cy="70" r="4" fill="#5eead4" />
    </svg>
  );
}

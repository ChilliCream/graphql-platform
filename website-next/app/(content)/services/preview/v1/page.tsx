import type { Metadata } from "next";
import type { ReactNode } from "react";

import { CheckIcon } from "@/src/components/CheckIcon";
import { OutlineButton, SolidButton } from "@/src/design-system/Button";

export const metadata: Metadata = {
  title: "ChilliCream Services: Advisory, Support, Training",
  description:
    "Route your team to the right ChilliCream service: GraphQL Advisory, Support plans from $450 per month, and Corporate Training, from the Hot Chocolate team.",
  keywords: [
    "ChilliCream services",
    "GraphQL advisory",
    "GraphQL support plans",
    "GraphQL training",
    "Hot Chocolate consulting",
    "Fusion support",
    "Corporate GraphQL workshop",
  ],
  openGraph: {
    title: "ChilliCream Services: Advisory, Support, Training",
    description:
      "Pick the right level of help for your GraphQL project: Advisory, Support, or Training, from the team behind Hot Chocolate, Fusion, and Nitro.",
  },
  robots: { index: false, follow: false },
};

const BOOKING_URL = "https://calendly.com/chillicream/60min";
const CONTACT_MAILTO = "mailto:contact@chillicream.com";
const ENTERPRISE_MAILTO =
  "mailto:contact@chillicream.com?subject=Enterprise%20Services";
const SUPPORT_CONTACT = "/services/support/contact";

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
  readonly highlight?: boolean;
  readonly highlightLabel?: string;
}

const TRACKS: readonly ServiceTrack[] = [
  {
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
  {
    id: "support",
    eyebrow: "Community, Startup, Business, Enterprise",
    name: "Support",
    tagline:
      "Tiered support plans with private channels, defined response times, and an escalation path you can rely on.",
    priceLine: "From $450",
    priceNote: "per month",
    bullets: [
      "Free Community plan, paid tiers from $450",
      "Business at $1,300 with email and incident handling",
      "Enterprise with phone support and a dedicated account manager",
      "Same engineers who ship Hot Chocolate, Fusion, and Nitro",
    ],
    learnMoreHref: "/services/support",
    learnMoreLabel: "Compare Support",
    highlight: true,
    highlightLabel: "Most teams start here",
  },
  {
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
];

interface DecisionRow {
  readonly id: string;
  readonly need: string;
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
    route: "A single call, or an ongoing Support plan with a defined SLA.",
    destinations: [
      { label: "Advisory consult", href: "/services/advisory" },
      { label: "Support plans", href: "/services/support" },
    ],
  },
  {
    id: "expert-delivery",
    need: "Need expert delivery",
    route:
      "A scoped statement of work where our engineers ship the result with you.",
    destinations: [
      { label: "Advisory: Contracting", href: "/services/advisory" },
    ],
  },
  {
    id: "team-trained",
    need: "Need your team trained",
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

export default function ServicesPreviewV1Page() {
  return (
    <>
      <Hero />
      <TrackGrid />
      <DecisionStrip />
      <EnterpriseBand />
      <ClosingCta />
    </>
  );
}

function Hero() {
  return (
    <section className="pt-10 pb-12 text-center sm:pt-16 sm:pb-16">
      <p className="text-cc-nav-label font-mono text-xs tracking-[0.18em] uppercase">
        ChilliCream Services
      </p>
      <h1 className="font-heading text-cc-heading text-h2 sm:text-h1 mt-5 font-semibold tracking-tight">
        Pick the right level of help for your GraphQL stack.
      </h1>
      <p className="text-cc-ink mx-auto mt-6 max-w-2xl text-base text-pretty sm:text-lg">
        Three ways to work with the team behind Hot Chocolate, Fusion, and
        Nitro: hands-on Advisory, ongoing Support plans, or Corporate Training.
        Tell us where you are and we will point you at the right one.
      </p>
      <div className="mt-9 flex flex-wrap items-center justify-center gap-3">
        <SolidButton href={BOOKING_URL}>Book a 60-min call</SolidButton>
        <OutlineButton href="#decide">Help me choose</OutlineButton>
      </div>
    </section>
  );
}

function TrackGrid() {
  return (
    <section aria-labelledby="tracks-heading" className="pt-4 pb-16 sm:pb-20">
      <h2 id="tracks-heading" className="sr-only">
        Service tracks
      </h2>
      <div className="grid gap-6 lg:grid-cols-3 lg:items-stretch">
        {TRACKS.map((track) => (
          <TrackCard key={track.id} track={track} />
        ))}
      </div>
    </section>
  );
}

function TrackCard({ track }: { readonly track: ServiceTrack }) {
  if (track.highlight) {
    return (
      <div
        className="relative isolate rounded-3xl p-[1.5px]"
        style={{
          background:
            "linear-gradient(140deg, #16b9e4 0%, #7c92c6 50%, #f0786a 100%)",
        }}
      >
        <HighlightPill label={track.highlightLabel ?? "Recommended"} />
        <div className="bg-cc-surface flex h-full flex-col rounded-[calc(1.5rem-1.5px)] p-7 sm:p-9">
          <TrackCardBody track={track} />
        </div>
      </div>
    );
  }

  return (
    <div className="bg-cc-card-bg border-cc-card-border hover:border-cc-card-border-hover flex h-full flex-col rounded-3xl border p-7 transition-colors sm:p-9">
      <TrackCardBody track={track} />
    </div>
  );
}

function TrackCardBody({ track }: { readonly track: ServiceTrack }) {
  return (
    <>
      <p className="text-cc-nav-label font-mono text-[0.65rem] tracking-[0.18em] uppercase">
        {track.eyebrow}
      </p>
      <h3 className="font-heading text-cc-heading text-h3 mt-3 font-semibold">
        {track.name}
      </h3>
      <p className="text-cc-ink-dim mt-3 text-sm leading-relaxed sm:text-base">
        {track.tagline}
      </p>

      <div className="mt-6 flex items-baseline gap-2">
        <span className="font-heading text-cc-heading text-h4 font-semibold">
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

      <p className="text-cc-nav-label font-mono text-[0.65rem] tracking-[0.18em] uppercase">
        What you get
      </p>
      <ul className="mt-3 flex flex-1 flex-col gap-3">
        {track.bullets.map((bullet) => (
          <li key={bullet} className="flex items-start gap-3">
            <span className="text-cc-accent mt-[5px] flex-none">
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
    </>
  );
}

function HighlightPill({ label }: { readonly label: string }) {
  return (
    <span className="bg-cc-surface text-cc-accent border-cc-accent absolute top-0 left-9 z-10 -translate-y-1/2 rounded-full border px-4 py-1 font-mono text-[0.65rem] tracking-[0.18em] whitespace-nowrap uppercase">
      {label}
    </span>
  );
}

function DecisionStrip() {
  return (
    <section
      id="decide"
      aria-labelledby="decision-heading"
      className="border-cc-card-border bg-cc-card-bg/40 rounded-3xl border p-6 sm:p-10"
    >
      <div className="text-center">
        <p className="text-cc-nav-label font-mono text-xs tracking-[0.18em] uppercase">
          Help me choose
        </p>
        <h2
          id="decision-heading"
          className="font-heading text-cc-heading text-h4 sm:text-h3 mt-3 font-semibold"
        >
          Which one is right for me?
        </h2>
        <p className="text-cc-ink mx-auto mt-4 max-w-2xl text-base">
          Three common starting points. Pick the row that sounds like you and
          follow the link, or book a call and we will sort it out together.
        </p>
      </div>

      <ol className="mt-10 flex flex-col gap-4">
        {DECISION_ROWS.map((row, index) => (
          <DecisionItem
            key={row.id}
            row={row}
            index={String(index + 1).padStart(2, "0")}
          />
        ))}
      </ol>
    </section>
  );
}

function DecisionItem({
  row,
  index,
}: {
  readonly row: DecisionRow;
  readonly index: string;
}) {
  return (
    <li className="bg-cc-surface border-cc-card-border grid gap-4 rounded-2xl border p-6 md:grid-cols-[auto_1fr] md:items-start lg:grid-cols-[auto_1fr_1.2fr_auto] lg:items-center">
      <span className="text-cc-accent font-mono text-xs tracking-[0.18em] uppercase">
        {index}
      </span>
      <div>
        <p className="text-cc-nav-label font-mono text-[0.65rem] tracking-[0.18em] uppercase">
          You
        </p>
        <p className="font-heading text-cc-heading mt-1 text-lg font-semibold">
          {row.need}
        </p>
      </div>
      <div className="md:col-span-2 lg:col-span-1">
        <p className="text-cc-nav-label font-mono text-[0.65rem] tracking-[0.18em] uppercase">
          We point you at
        </p>
        <p className="text-cc-ink mt-1 text-sm leading-relaxed">{row.route}</p>
      </div>
      <div className="md:col-span-2 lg:col-span-1">
        <DecisionDestinations destinations={row.destinations} />
      </div>
    </li>
  );
}

function DecisionDestinations({
  destinations,
}: {
  readonly destinations: DecisionRow["destinations"];
}) {
  return (
    <div className="flex flex-wrap gap-2 lg:justify-end">
      {destinations.map((destination) => (
        <OutlineButton key={destination.href} href={destination.href}>
          {destination.label}
        </OutlineButton>
      ))}
    </div>
  );
}

function EnterpriseBand() {
  return (
    <section aria-labelledby="enterprise-heading" className="mt-20 sm:mt-28">
      <div className="border-cc-card-border bg-cc-card-bg/60 grid gap-10 rounded-3xl border p-8 sm:p-12 md:grid-cols-[1.2fr_1fr] md:items-start">
        <div>
          <p className="text-cc-nav-label font-mono text-xs tracking-[0.18em] uppercase">
            Enterprise
          </p>
          <h2
            id="enterprise-heading"
            className="font-heading text-cc-heading text-h4 sm:text-h3 mt-3 font-semibold"
          >
            One contract, every team, an SLA you wrote together.
          </h2>
          <p className="text-cc-ink mt-4 text-base leading-relaxed">
            For organizations standardizing on Hot Chocolate, Fusion, and Nitro
            across business units. We will bundle Advisory hours, an Enterprise
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
        <ul className="grid gap-4 sm:grid-cols-2">
          {ENTERPRISE_BULLETS.map((bullet) => (
            <EnterpriseBulletCard key={bullet.label} bullet={bullet} />
          ))}
        </ul>
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
    <section aria-labelledby="closing-heading" className="mt-20 mb-8 sm:mt-28">
      <div className="border-cc-card-border bg-cc-card-bg/70 grid gap-8 rounded-3xl border p-8 sm:p-12 md:grid-cols-[1.4fr_1fr] md:items-center">
        <div>
          <p className="text-cc-nav-label font-mono text-xs tracking-[0.18em] uppercase">
            Still not sure
          </p>
          <h2
            id="closing-heading"
            className="font-heading text-cc-heading text-h4 mt-3 font-semibold"
          >
            One call is usually enough to know.
          </h2>
          <p className="text-cc-ink mt-4 text-base">
            Book a 60-minute call with an engineer. Walk us through the project,
            and you will leave with a clear next step: an Advisory engagement, a
            Support plan, a Training plan, or a candid no when we are not the
            right fit.
          </p>
          <ClosingSpec />
        </div>
        <div className="flex flex-col gap-3 md:items-end">
          <SolidButton href={BOOKING_URL}>Book a 60-min call</SolidButton>
          <OutlineButton href={CONTACT_MAILTO}>Email us</OutlineButton>
        </div>
      </div>
    </section>
  );
}

function ClosingSpec() {
  const items: readonly {
    readonly label: string;
    readonly value: ReactNode;
  }[] = [
    { label: "Advisory", value: "Hourly or scoped contracting" },
    { label: "Support", value: "From $450 per month" },
    { label: "Training", value: "Custom, tailored to your team" },
    { label: "Enterprise", value: "Custom SLAs and procurement" },
  ];
  return (
    <ul className="mt-6 grid gap-3 sm:grid-cols-2">
      {items.map((item) => (
        <li key={item.label} className="flex items-start gap-3">
          <span className="text-cc-accent mt-[5px] flex-none">
            <CheckIcon />
          </span>
          <span className="text-cc-ink text-sm">
            <span className="text-cc-nav-label font-mono text-[0.65rem] tracking-[0.18em] uppercase">
              {item.label}
            </span>
            <br />
            {item.value}
          </span>
        </li>
      ))}
    </ul>
  );
}

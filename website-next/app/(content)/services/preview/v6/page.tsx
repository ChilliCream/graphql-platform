import type { Metadata } from "next";
import type { ReactNode } from "react";

import { CheckIcon } from "@/src/components/CheckIcon";
import { OutlineButton, SolidButton } from "@/src/design-system/Button";
import { CoffeeTray } from "@/src/icons/CoffeeTray";
import { DripBrewer } from "@/src/icons/DripBrewer";
import { PourOver } from "@/src/icons/PourOver";

export const metadata: Metadata = {
  title: "ChilliCream Services: Advisory, Support, Training",
  description:
    "ChilliCream GraphQL services from the Hot Chocolate team: Advisory, Support plans from $450 per month, and Corporate Training. Three pours, one barista.",
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
      "Three pours from the team that brews your GraphQL stack: Advisory, Support, and Training from the Hot Chocolate, Fusion, and Nitro engineers.",
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
  readonly housePour: string;
  readonly bullets: readonly string[];
  readonly learnMoreHref: string;
  readonly learnMoreLabel: string;
  readonly Icon: (props: { readonly className?: string }) => ReactNode;
  readonly highlight?: boolean;
  readonly highlightLabel?: string;
  readonly highlightHelper?: string;
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
    housePour: "House pour, single origin",
    bullets: [
      "Architecture, schema review, troubleshooting",
      "Proof of concept and full implementation",
      "Direct line to the core engineering team",
      "Start with a single 60-minute call",
    ],
    learnMoreHref: "/services/advisory",
    learnMoreLabel: "Explore Advisory",
    Icon: PourOver,
  },
  {
    id: "support",
    eyebrow: "Community, Startup, Business, Enterprise",
    name: "Support",
    tagline:
      "Tiered support plans with private channels, defined response times, and an escalation path you can rely on.",
    priceLine: "From $450",
    priceNote: "per month",
    housePour: "House pour, on a standing order",
    bullets: [
      "Free Community plan, paid tiers from $450",
      "Business at $1,300 with email and incident handling",
      "Enterprise with phone support and a dedicated account manager",
      "Same engineers who ship Hot Chocolate, Fusion, and Nitro",
    ],
    learnMoreHref: "/services/support",
    learnMoreLabel: "Compare Support",
    Icon: DripBrewer,
    highlight: true,
    highlightLabel: "Today's pour",
    highlightHelper: "Most teams start here",
  },
  {
    id: "training",
    eyebrow: "Corporate Training & Workshop",
    name: "Training",
    tagline:
      "Hands-on training and workshops for your team. Levels and pacing tuned to where your engineers are today.",
    priceLine: "Custom",
    priceNote: "tailored to team size",
    housePour: "House pour, brewed for a table",
    bullets: [
      "Corporate Training tuned to beginner, advanced, or mixed teams",
      "Corporate Workshop covering Hot Chocolate, ASP.NET Core, React, Relay",
      "Real-project exercises and production quirks",
      "Designed to lift the whole team at once",
    ],
    learnMoreHref: "/services/training",
    learnMoreLabel: "Explore Training",
    Icon: CoffeeTray,
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

interface QualityStat {
  readonly label: string;
  readonly value: string;
  readonly note: string;
}

const QUALITY_STATS: readonly QualityStat[] = [
  {
    label: "Shipping OSS",
    value: "Years",
    note: "Hot Chocolate, Fusion, and Nitro in the open",
  },
  {
    label: "In production",
    value: "Deployments",
    note: "Across startups and large enterprises",
  },
  {
    label: "Response floor",
    value: "Defined",
    note: "SLAs published on the Support page",
  },
];

interface BarQa {
  readonly q: string;
  readonly a: string;
}

const BAR_QAS: readonly BarQa[] = [
  {
    q: "Can we mix Advisory and Support?",
    a: "Yes. Many teams keep a Support plan for incidents and add Advisory hours for design work or reviews. The Enterprise contract bundles both.",
  },
  {
    q: "Do you do one-off audits?",
    a: "Yes. Schema and architecture reviews are a common Advisory scope. Start with a 60-minute call and we will shape the engagement from there.",
  },
  {
    q: "Is the Community plan really free?",
    a: "Yes. Community is free with public channels. Paid plans start at $450 per month and add private channels, response times, and an escalation path.",
  },
  {
    q: "Can training happen on-site?",
    a: "Yes. Corporate Training and Workshop sessions run remotely or on-site, with content tuned to your team's level and your stack.",
  },
];

export default function ServicesPreviewV6Page() {
  return (
    <>
      <Hero />
      <TrackGrid />
      <DecisionStrip />
      <QualityBand />
      <EnterpriseBand />
      <BarFaq />
      <ClosingCta />
    </>
  );
}

function Hero() {
  return (
    <section className="pt-10 pb-12 sm:pt-16 sm:pb-16">
      <div className="grid items-center gap-12 lg:grid-cols-[1.05fr_1fr] lg:gap-16">
        <div>
          <p className="text-cc-nav-label font-mono text-xs tracking-[0.18em] uppercase">
            ChilliCream Services / On the menu
          </p>
          <h1 className="font-heading text-cc-heading text-h2 sm:text-h1 mt-5 font-semibold tracking-tight text-pretty">
            Three pours from the team that brews your GraphQL stack.
          </h1>
          <p className="text-cc-ink mt-6 max-w-2xl text-base text-pretty sm:text-lg">
            Three ways to work with the team behind Hot Chocolate, Fusion, and
            Nitro: hands-on Advisory, ongoing Support plans, or Corporate
            Training. Tell us where you are and we will point you at the right
            one.
          </p>
          <div className="mt-9 flex flex-wrap items-center gap-3">
            <SolidButton href={BOOKING_URL}>Book a 60-min call</SolidButton>
            <OutlineButton href="#tracks">Help me choose</OutlineButton>
          </div>
        </div>
        <MenuCard />
      </div>
    </section>
  );
}

function MenuCard() {
  const items: readonly {
    readonly name: "Advisory" | "Support" | "Training";
    readonly tagline: string;
    readonly Icon: (props: { readonly className?: string }) => ReactNode;
  }[] = [
    {
      name: "Advisory",
      tagline: "An hour, a design, a deadline.",
      Icon: PourOver,
    },
    {
      name: "Support",
      tagline: "A plan with a response time you can trust.",
      Icon: DripBrewer,
    },
    {
      name: "Training",
      tagline: "Your whole team, lifted at once.",
      Icon: CoffeeTray,
    },
  ];

  return (
    <a
      href="#tracks"
      aria-label="Jump to the three service tracks"
      className="border-cc-card-border hover:border-cc-card-border-hover bg-cc-surface group block rounded-3xl border p-7 transition-colors sm:p-9"
    >
      <div className="flex items-center justify-between">
        <p className="text-cc-nav-label font-mono text-[0.65rem] tracking-[0.18em] uppercase">
          On the menu
        </p>
        <p className="text-cc-nav-label font-mono text-[0.65rem] tracking-[0.18em] uppercase">
          Today
        </p>
      </div>
      <p className="font-heading text-cc-heading mt-3 text-lg font-semibold sm:text-xl">
        Today&apos;s menu
      </p>

      <div
        aria-hidden="true"
        className="border-cc-ink-faint my-5 border-t border-dashed"
      />

      <ul className="flex flex-col gap-5">
        {items.map(({ name, tagline, Icon }) => (
          <li key={name} className="flex items-start gap-4">
            <span
              aria-hidden="true"
              className="text-cc-accent flex-none"
              style={{ width: "2.25rem", height: "2.25rem" }}
            >
              <Icon className="h-full w-full" />
            </span>
            <div className="min-w-0 flex-1">
              <div className="flex items-baseline justify-between gap-3">
                <span className="font-heading text-cc-heading text-base font-semibold sm:text-lg">
                  {name}
                </span>
                <span
                  aria-hidden="true"
                  className="text-cc-ink-faint flex-1 translate-y-[-2px] border-b border-dotted"
                />
                <span className="text-cc-nav-label font-mono text-[0.65rem] tracking-[0.18em] uppercase">
                  See below
                </span>
              </div>
              <p className="text-cc-ink-dim mt-1 text-sm leading-relaxed">
                {tagline}
              </p>
            </div>
          </li>
        ))}
      </ul>

      <div
        aria-hidden="true"
        className="border-cc-ink-faint my-5 border-t border-dashed"
      />

      <p className="text-cc-nav-label font-mono text-[0.65rem] tracking-[0.18em] uppercase">
        Brewed by the Hot Chocolate, Fusion, and Nitro team
      </p>
    </a>
  );
}

function TrackGrid() {
  return (
    <section
      id="tracks"
      aria-labelledby="tracks-heading"
      className="scroll-mt-24 pt-4 pb-16 sm:pb-20"
    >
      <div className="mb-8 text-center sm:mb-10">
        <p className="text-cc-nav-label font-mono text-xs tracking-[0.18em] uppercase">
          Behind the bar
        </p>
        <h2
          id="tracks-heading"
          className="font-heading text-cc-heading text-h4 sm:text-h3 mt-3 font-semibold"
        >
          Three pours, one barista.
        </h2>
      </div>
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
        <HighlightPill label={track.highlightLabel ?? "Today's pour"} />
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
  const { Icon } = track;
  return (
    <>
      <div className="flex items-start justify-between gap-4">
        <p className="text-cc-nav-label font-mono text-[0.65rem] tracking-[0.18em] uppercase">
          {track.eyebrow}
        </p>
        <span
          aria-hidden="true"
          className="text-cc-accent flex-none"
          style={{ width: "1.75rem", height: "1.75rem" }}
        >
          <Icon className="h-full w-full" />
        </span>
      </div>
      <h3 className="font-heading text-cc-heading text-h3 mt-3 font-semibold">
        {track.name}
      </h3>
      {track.highlightHelper ? (
        <p className="text-cc-ink-dim mt-1 text-xs">{track.highlightHelper}</p>
      ) : null}
      <p className="text-cc-ink-dim mt-3 text-sm leading-relaxed sm:text-base">
        {track.tagline}
      </p>

      <p className="text-cc-nav-label mt-6 font-mono text-[0.65rem] tracking-[0.18em] uppercase">
        {track.housePour}
      </p>
      <div className="mt-2 flex items-baseline gap-2">
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
      aria-labelledby="decision-heading"
      className="border-cc-card-border bg-cc-card-bg/40 rounded-3xl border p-6 sm:p-10"
    >
      <div className="text-center">
        <p className="text-cc-nav-label font-mono text-xs tracking-[0.18em] uppercase">
          Order at the counter
        </p>
        <h2
          id="decision-heading"
          className="font-heading text-cc-heading text-h4 sm:text-h3 mt-3 font-semibold"
        >
          Tell us your order.
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
          Your order
        </p>
        <p className="font-heading text-cc-heading mt-1 text-lg font-semibold">
          {row.need}
        </p>
      </div>
      <div className="md:col-span-2 lg:col-span-1">
        <p className="text-cc-nav-label font-mono text-[0.65rem] tracking-[0.18em] uppercase">
          We pour
        </p>
        <p className="text-cc-ink mt-1 text-sm leading-relaxed">{row.route}</p>
      </div>
      <div className="md:col-span-2 lg:col-span-1">
        <div>
          <p className="text-cc-nav-label font-mono text-[0.65rem] tracking-[0.18em] uppercase lg:text-right">
            Pick up at
          </p>
          <div className="mt-2 flex flex-wrap gap-2 lg:justify-end">
            {row.destinations.map((destination) => (
              <OutlineButton key={destination.href} href={destination.href}>
                {destination.label}
              </OutlineButton>
            ))}
          </div>
        </div>
      </div>
    </li>
  );
}

function QualityBand() {
  return (
    <section aria-labelledby="quality-heading" className="mt-20 sm:mt-28">
      <div className="border-cc-card-border bg-cc-card-bg/50 rounded-3xl border p-8 sm:p-12">
        <div className="flex items-center gap-3">
          <span
            aria-hidden="true"
            className="bg-cc-accent inline-block h-2 w-2 rounded-full"
          />
          <p className="text-cc-nav-label font-mono text-xs tracking-[0.18em] uppercase">
            Quality control
          </p>
        </div>
        <h2
          id="quality-heading"
          className="font-heading text-cc-heading text-h4 sm:text-h3 mt-4 max-w-3xl font-semibold"
        >
          Brewed by the same people who roast the beans.
        </h2>
        <p className="text-cc-ink mt-4 max-w-3xl text-base leading-relaxed">
          The engineers on these engagements are the engineers who ship Hot
          Chocolate, Fusion, and Nitro. The advice you get on a call, the fix
          you get under a Support plan, and the patterns you see in a workshop
          all come from the same source as the libraries you are running in
          production.
        </p>

        <ul className="mt-8 grid gap-4 sm:grid-cols-3">
          {QUALITY_STATS.map((stat) => (
            <li
              key={stat.label}
              className="bg-cc-card-bg border-cc-card-border flex h-full flex-col rounded-2xl border p-5"
            >
              <span className="text-cc-nav-label font-mono text-[0.65rem] tracking-[0.18em] uppercase">
                {stat.label}
              </span>
              <span className="font-heading text-cc-heading mt-2 text-lg font-semibold">
                {stat.value}
              </span>
              <span className="text-cc-ink-dim mt-2 text-sm leading-relaxed">
                {stat.note}
              </span>
            </li>
          ))}
        </ul>
      </div>
    </section>
  );
}

function EnterpriseBand() {
  return (
    <section aria-labelledby="enterprise-heading" className="mt-20 sm:mt-28">
      <div className="border-cc-card-border bg-cc-card-bg/60 grid gap-10 rounded-3xl border p-8 sm:p-12 md:grid-cols-[1.2fr_1fr] md:items-start">
        <div>
          <p className="text-cc-nav-label font-mono text-xs tracking-[0.18em] uppercase">
            Enterprise / The standing order
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

function BarFaq() {
  return (
    <section aria-labelledby="bar-faq-heading" className="mt-20 sm:mt-28">
      <div className="border-cc-card-border bg-cc-card-bg/40 rounded-3xl border p-8 sm:p-12">
        <div>
          <p className="text-cc-nav-label font-mono text-xs tracking-[0.18em] uppercase">
            At the bar
          </p>
          <h2
            id="bar-faq-heading"
            className="font-heading text-cc-heading text-h4 sm:text-h3 mt-3 font-semibold"
          >
            A few questions we get over the counter.
          </h2>
        </div>

        <ul className="mt-8 grid gap-x-10 gap-y-0 md:grid-cols-2">
          {BAR_QAS.map((item, index) => (
            <li
              key={item.q}
              className={
                "border-cc-ink-faint border-t border-dashed py-5 first:border-t-0 md:py-6 " +
                (index < 2 ? "md:first:border-t-0" : "")
              }
            >
              <p className="flex items-baseline gap-3">
                <span className="text-cc-accent font-mono text-[0.65rem] tracking-[0.18em] uppercase">
                  Q.
                </span>
                <span className="font-heading text-cc-heading text-base font-semibold sm:text-lg">
                  {item.q}
                </span>
              </p>
              <p className="text-cc-ink mt-2 pl-8 text-sm leading-relaxed">
                {item.a}
              </p>
            </li>
          ))}
        </ul>
      </div>
    </section>
  );
}

function ClosingCta() {
  return (
    <section aria-labelledby="closing-heading" className="mt-20 mb-8 sm:mt-28">
      <div className="border-cc-card-border bg-cc-card-bg/70 grid gap-8 rounded-3xl border p-8 sm:p-12 md:grid-cols-[1.4fr_1fr] md:items-center">
        <div>
          <p className="text-cc-nav-label font-mono text-xs tracking-[0.18em] uppercase">
            Last call
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

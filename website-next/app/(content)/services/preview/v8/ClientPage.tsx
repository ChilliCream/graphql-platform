"use client";

import type { ReactNode } from "react";
import { motion, useReducedMotion } from "motion/react";

import { CheckIcon } from "@/src/components/CheckIcon";
import { OutlineButton, SolidButton } from "@/src/design-system/Button";

/* ------------------------------------------------------------------ */
/* Shared constants and data (sourced verbatim from v1)                */
/* ------------------------------------------------------------------ */

const BOOKING_URL = "https://calendly.com/chillicream/60min";
const CONTACT_MAILTO = "mailto:contact@chillicream.com";
const ENTERPRISE_MAILTO =
  "mailto:contact@chillicream.com?subject=Enterprise%20Services";
const SUPPORT_CONTACT = "/services/support/contact";

type TrackId = "advisory" | "support" | "training";

interface ServiceTrack {
  readonly id: TrackId;
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

/* ------------------------------------------------------------------ */
/* Page                                                                */
/* ------------------------------------------------------------------ */

export function ClientPage() {
  return (
    <>
      <Hero />
      <TracksDock />
      <HelpMeChoose />
      <EnterpriseBand />
      <TrustStrip />
      <ClosingCta />
    </>
  );
}

/* ------------------------------------------------------------------ */
/* Hero: floating glyph between two static waterlines                  */
/* ------------------------------------------------------------------ */

function Hero() {
  const reduced = useReducedMotion();

  return (
    <section className="relative isolate pt-12 pb-14 text-center sm:pt-20 sm:pb-20">
      {/* Waterline pair: two static dashed hairlines the glyph drifts between. */}
      <div
        aria-hidden="true"
        className="pointer-events-none absolute inset-0 -z-10"
      >
        <span className="border-cc-card-border absolute top-[40%] right-0 left-0 border-t border-dashed" />
        <span className="border-cc-card-border absolute top-[60%] right-0 left-0 border-t border-dashed" />
      </div>

      <p className="text-cc-nav-label font-mono text-xs tracking-[0.18em] uppercase">
        ChilliCream GraphQL services
      </p>

      <div className="mt-8 flex justify-center">
        <motion.div
          animate={reduced ? { y: 0 } : { y: [0, -6, 0] }}
          transition={
            reduced
              ? undefined
              : { duration: 4.5, repeat: Infinity, ease: "easeInOut" }
          }
        >
          <BuoyGlyph />
        </motion.div>
      </div>

      <h1 className="font-heading text-cc-heading text-h2 sm:text-h1 mt-7 font-semibold tracking-tight text-balance">
        Three doors. Pick the one you can float into.
      </h1>
      <p className="text-cc-ink mx-auto mt-6 max-w-2xl text-base text-pretty sm:text-lg">
        Three ways to work with the team behind Hot Chocolate, Fusion, and
        Nitro: hands-on Advisory, ongoing Support plans, or Corporate Training.
        Settle into the one that fits and we will route you from there.
      </p>
      <div className="mt-9 flex flex-wrap items-center justify-center gap-3">
        <SolidButton href={BOOKING_URL}>Book a 60-min call</SolidButton>
        <OutlineButton href="#decide">Help me choose</OutlineButton>
      </div>
    </section>
  );
}

function BuoyGlyph() {
  return (
    <svg
      width="48"
      height="48"
      viewBox="0 0 48 48"
      fill="none"
      aria-hidden="true"
      className="text-cc-accent"
    >
      <circle
        cx="24"
        cy="20"
        r="11"
        stroke="currentColor"
        strokeWidth="2"
        opacity="0.55"
      />
      <circle cx="24" cy="20" r="3.5" fill="currentColor" />
      <path
        d="M24 31 L24 42"
        stroke="currentColor"
        strokeWidth="2"
        strokeLinecap="round"
        opacity="0.55"
      />
      <path
        d="M24 6 L24 9 M38 20 L35 20 M24 34 L24 31 M10 20 L13 20"
        stroke="currentColor"
        strokeWidth="2"
        strokeLinecap="round"
      />
    </svg>
  );
}

/* ------------------------------------------------------------------ */
/* Tracks dock: vertical list, alternating lanes on desktop           */
/* ------------------------------------------------------------------ */

function TracksDock() {
  return (
    <section aria-labelledby="tracks-heading" className="pt-2 pb-16 sm:pb-20">
      <h2 id="tracks-heading" className="sr-only">
        Service tracks
      </h2>
      <ol className="flex flex-col gap-6">
        {TRACKS.map((track, index) => (
          <TrackRow
            key={track.id}
            track={track}
            lane={index % 2 === 0 ? "left" : "right"}
          />
        ))}
      </ol>
    </section>
  );
}

function TrackRow({
  track,
  lane,
}: {
  readonly track: ServiceTrack;
  readonly lane: "left" | "right";
}) {
  const laneClass = lane === "right" ? "lg:ml-auto" : "lg:mr-auto";

  return (
    <li className={`w-full lg:w-[88%] ${laneClass}`}>
      <div className="bg-cc-card-bg border-cc-card-border hover:border-cc-card-border-hover relative overflow-hidden rounded-3xl border p-7 transition-colors sm:p-9">
        {track.highlight ? <SupportMarker /> : null}
        <div className="grid gap-7 md:grid-cols-[1fr_1fr] md:items-start">
          <div>
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
            {track.highlight && track.highlightLabel ? (
              <span className="text-cc-accent border-cc-accent mt-5 inline-block rounded-full border px-4 py-1 font-mono text-[0.65rem] tracking-[0.18em] uppercase">
                {track.highlightLabel}
              </span>
            ) : null}
          </div>

          <div className="flex h-full flex-col">
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
              <SolidButton
                href={track.learnMoreHref}
                className="w-full sm:w-auto"
              >
                {track.learnMoreLabel}
              </SolidButton>
            </div>
          </div>
        </div>
      </div>
    </li>
  );
}

// Single-use brand-spectrum hairline strip marking the row most teams start on.
function SupportMarker() {
  return (
    <span
      aria-hidden="true"
      className="absolute inset-x-0 top-0 h-[2px]"
      style={{
        background:
          "linear-gradient(90deg, #16b9e4 0%, #7c92c6 50%, #f0786a 100%)",
      }}
    />
  );
}

/* ------------------------------------------------------------------ */
/* Help me choose                                                      */
/* ------------------------------------------------------------------ */

function HelpMeChoose() {
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
          Which door fits?
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
      <div className="flex flex-wrap gap-2 md:col-span-2 lg:col-span-1 lg:justify-end">
        {row.destinations.map((destination) => (
          <OutlineButton key={destination.href} href={destination.href}>
            {destination.label}
          </OutlineButton>
        ))}
      </div>
    </li>
  );
}

/* ------------------------------------------------------------------ */
/* Enterprise band                                                     */
/* ------------------------------------------------------------------ */

function EnterpriseBand() {
  return (
    <section aria-labelledby="enterprise-heading" className="mt-20 sm:mt-28">
      <div className="border-cc-card-border bg-cc-card-bg grid gap-10 rounded-3xl border p-8 sm:p-12 md:grid-cols-[1.2fr_1fr] md:items-start">
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
            <li
              key={bullet.label}
              className="bg-cc-surface border-cc-card-border flex h-full flex-col rounded-2xl border p-5"
            >
              <span className="text-cc-nav-label font-mono text-[0.65rem] tracking-[0.18em] uppercase">
                {bullet.label}
              </span>
              <span className="text-cc-ink mt-2 text-sm leading-relaxed">
                {bullet.value}
              </span>
            </li>
          ))}
        </ul>
      </div>
    </section>
  );
}

/* ------------------------------------------------------------------ */
/* Trust strip                                                         */
/* ------------------------------------------------------------------ */

function TrustStrip() {
  const products: readonly string[] = ["Hot Chocolate", "Fusion", "Nitro"];
  return (
    <section className="mt-16 sm:mt-20">
      <p className="text-cc-nav-label flex flex-wrap items-center justify-center gap-x-3 gap-y-2 text-center font-mono text-[0.7rem] tracking-[0.16em] uppercase">
        <span>Same engineers shipping</span>
        {products.map((product, index) => (
          <span key={product} className="flex items-center gap-x-3">
            <span className="text-cc-ink">{product}</span>
            {index < products.length - 1 ? (
              <span aria-hidden="true" className="text-cc-ink-faint">
                /
              </span>
            ) : null}
          </span>
        ))}
      </p>
    </section>
  );
}

/* ------------------------------------------------------------------ */
/* Closing CTA                                                         */
/* ------------------------------------------------------------------ */

function ClosingCta() {
  return (
    <section aria-labelledby="closing-heading" className="mt-16 mb-8 sm:mt-20">
      <div className="border-cc-card-border bg-cc-card-bg/70 mx-auto max-w-2xl rounded-3xl border p-8 text-center sm:p-12">
        <p className="text-cc-nav-label font-mono text-xs tracking-[0.18em] uppercase">
          Still floating
        </p>
        <h3 className="font-heading text-cc-heading text-h4 mt-3 font-semibold">
          One call is usually enough to know.
        </h3>
        <p className="text-cc-ink mx-auto mt-4 max-w-xl text-base">
          Book a 60-minute call with an engineer. Walk us through the project,
          and you will leave with a clear next step: an Advisory engagement, a
          Support plan, a Training plan, or a candid no when we are not the
          right fit.
        </p>
        <ClosingSpec />
        <div className="mt-8 flex flex-wrap items-center justify-center gap-3">
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
    <ul className="mx-auto mt-6 grid max-w-md gap-3 text-left sm:grid-cols-2">
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

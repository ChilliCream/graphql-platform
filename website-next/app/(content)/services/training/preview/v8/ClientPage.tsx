"use client";

import { motion, useReducedMotion } from "motion/react";
import type { ReactNode } from "react";

import { CheckIcon } from "@/src/components/CheckIcon";
import { OutlineButton, SolidButton } from "@/src/design-system/Button";

// Canonical facts (plans, copy, perks, URLs) are reused verbatim from v1.

interface CurriculumTrack {
  readonly code: string;
  readonly title: string;
  readonly level: "Foundations" | "Core" | "Advanced" | "Production";
  readonly summary: string;
  readonly topics: readonly string[];
}

const CURRICULUM: readonly CurriculumTrack[] = [
  {
    code: "GQL-101",
    title: "GraphQL Fundamentals",
    level: "Foundations",
    summary:
      "The mental model: types, fields, resolvers, and how a query maps onto your data.",
    topics: [
      "Schema, types, and the request lifecycle",
      "Queries, mutations, subscriptions",
      "Fragments, variables, directives",
      "Error shapes, nullability, and pagination patterns",
    ],
  },
  {
    code: "HC-201",
    title: "Hot Chocolate Server",
    level: "Core",
    summary:
      "Build a production GraphQL server on ASP.NET Core with Hot Chocolate, end to end.",
    topics: [
      "Code-first types, resolvers, and DI",
      "Data loaders, projections, and EF Core integration",
      "Authorization, filtering, sorting, paging",
      "Testing, instrumentation, and configuration",
    ],
  },
  {
    code: "FUS-301",
    title: "Fusion Federation",
    level: "Advanced",
    summary:
      "Compose multiple Hot Chocolate services into one Fusion graph without leaking the seams.",
    topics: [
      "Subgraph design and ownership boundaries",
      "Composition, source schemas, and lookup keys",
      "Cross-subgraph entities and shared types",
      "Gateway configuration and rollout strategy",
    ],
  },
  {
    code: "NIT-401",
    title: "Production Observability with Nitro",
    level: "Production",
    summary:
      "Wire Hot Chocolate and Fusion into Nitro to see what production is actually doing.",
    topics: [
      "Schema registry and published client tracking",
      "Operation telemetry and slow-resolver triage",
      "Persisted operations and safe schema evolution",
      "Reading traces when an incident is live",
    ],
  },
  {
    code: "REL-301",
    title: "React + Relay Client",
    level: "Advanced",
    summary:
      "Drive a React UI with Relay so data flows match component boundaries by default.",
    topics: [
      "Fragments, connections, and colocation",
      "Mutations, optimistic updates, and store consistency",
      "Suspense, streaming, and refetch patterns",
      "Working with persisted queries from Nitro",
    ],
  },
  {
    code: "DSN-301",
    title: "Schema Design & Evolution",
    level: "Advanced",
    summary:
      "Design a schema your team can change safely once real clients depend on it.",
    topics: [
      "Naming, nullability, and shaping for change",
      "Errors as data, mutations, and result unions",
      "Versionless evolution and deprecation",
      "Reviewing diffs against published clients",
    ],
  },
];

interface DeliveryFormat {
  readonly name: "On-site" | "Remote" | "Hybrid";
  readonly tagline: string;
  readonly notes: readonly string[];
}

const FORMATS: readonly DeliveryFormat[] = [
  {
    name: "On-site",
    tagline: "We come to you.",
    notes: [
      "Instructor on location with your team",
      "Best for hands-on labs and whiteboard design",
      "Travel quoted with the engagement",
    ],
  },
  {
    name: "Remote",
    tagline: "Live, distributed cohorts.",
    notes: [
      "Live sessions across time zones",
      "Shared repo, recordings, and Q&A channel",
      "Easiest to schedule across multiple offices",
    ],
  },
  {
    name: "Hybrid",
    tagline: "Half in the room, half on Zoom.",
    notes: [
      "Anchor cohort in one location, others dial in",
      "Workshops can splice on-site labs with remote review",
      "Useful when seniors are co-located and juniors are not",
    ],
  },
];

interface OfferPerk {
  readonly text: string;
}

interface Offer {
  readonly name: "Corporate Training" | "Corporate Workshop";
  readonly tagline: string;
  readonly description: string;
  readonly perks: readonly OfferPerk[];
  readonly highlight?: boolean;
}

const OFFERS: readonly Offer[] = [
  {
    name: "Corporate Training",
    tagline: "Flexible curriculum, shaped to your team.",
    description:
      "Get your team trained in GraphQL, any of our products, and even React/Relay. Beginner Team? Advanced Team? Or Mixed? Don't panic! Our curriculum is designed to teach in-depth and works really well, but isn't set in stone.",
    perks: [
      { text: "Level up their proficiency" },
      { text: "Catered to different skills" },
      { text: "Overcome challenges they have been wrestling with" },
      { text: "Get everybody on the same technical page" },
    ],
  },
  {
    name: "Corporate Workshop",
    tagline: "Focused, hands-on, project-shaped.",
    description:
      "We will look at how to build a GraphQL server with ASP.NET Core 7 and Hot Chocolate. You will learn how to explore and manage large schemas. Further, we will dive into React and explore how to efficiently build fast and fluent web interfaces using Relay.",
    perks: [
      { text: "Core concepts and advanced" },
      { text: "Deepen knowledge of GraphQL API" },
      { text: "Work on a real project" },
      { text: "Scale and production quirks" },
      { text: "Level up your entire team at once" },
      { text: "Have Lots of Fun!" },
    ],
    highlight: true,
  },
];

interface FaqItem {
  readonly q: string;
  readonly a: string;
}

const FAQ: readonly FaqItem[] = [
  {
    q: "How long is a typical engagement?",
    a: "Most workshops run two to five days. A Corporate Training that spans several tracks is usually split into multiple weeks so people keep shipping in between. We size duration to the topics you pick and the seniority of the room, then put it in writing before we start.",
  },
  {
    q: "How many people can attend?",
    a: "We have run sessions from a single team of five up to a few dozen engineers across offices. For workshops with live labs we keep cohorts small enough that everyone gets feedback. For larger groups we either split into cohorts or lean on the lecture-and-clinic format.",
  },
  {
    q: "What do attendees need to know beforehand?",
    a: "For the Hot Chocolate and Fusion tracks, comfort with C# and ASP.NET Core. For the Relay track, comfort with React and TypeScript. No prior GraphQL is required for the foundations track. We ask a few questions before we start and adjust the depth of each module to the room.",
  },
  {
    q: "How is pricing handled?",
    a: "Pricing is on request because the right number depends on tracks, duration, headcount, format, and travel. Tell us what you want covered and we send back a written quote.",
  },
  {
    q: "How soon can we book?",
    a: "Lead time is typically a few weeks so we can tailor the curriculum and line up the instructor. Urgent engagements are possible when we have a slot open. Get in touch early if your delivery date is fixed.",
  },
  {
    q: "Do you sign NDAs and work on our code?",
    a: "Yes. We routinely sign NDAs and tailor workshop projects against your own schema or service. If you would rather keep the workshop on a neutral codebase we bring one.",
  },
];

const TRAINING_MAILTO = "mailto:contact@chillicream.com?subject=Training";

interface KmStop {
  readonly km: string;
  readonly anchor: string;
  readonly label: string;
}

// Tick map for the rail. Each numbered KM is a section anchor; the finish line
// gets the one-time brand spectrum.
const KM_STOPS: readonly KmStop[] = [
  { km: "0", anchor: "km-0", label: "Day Zero" },
  { km: "1", anchor: "km-1", label: "Route brief" },
  { km: "2", anchor: "km-2", label: "Curriculum" },
  { km: "3", anchor: "km-3", label: "Format" },
  { km: "4", anchor: "km-4", label: "Two ways" },
  { km: "5", anchor: "km-5", label: "Route guides" },
  { km: "6", anchor: "km-6", label: "Questions" },
  { km: "Fin", anchor: "km-finish", label: "Finish" },
];

export function ClientPage() {
  return (
    <div className="relative">
      <div className="grid gap-y-0 lg:grid-cols-[112px_minmax(0,1fr)] lg:gap-x-10">
        <KilometreRail />
        <div className="relative mx-auto w-full max-w-[720px]">
          {/* Faint vertical guide that pairs with the rail behind the column. */}
          <span
            aria-hidden
            className="border-cc-card-border pointer-events-none absolute top-0 -left-5 hidden h-full border-l border-dashed opacity-50 lg:block"
          />
          <KmHero />
          <SubTick />
          <RouteBrief />
          <SubTick />
          <CurriculumLeg />
          <SubTick />
          <FormatLeg />
          <SubTick />
          <OffersLeg />
          <SubTick />
          <GuidesLeg />
          <SubTick />
          <FaqLeg />
          <SubTick />
          <FinishLeg />
        </div>
      </div>
    </div>
  );
}

// The sticky kilometre rail. CSS sticky is layout only, not scroll-coupled
// motion. The active dot pulses on a time-driven loop.
function KilometreRail() {
  return (
    <aside
      aria-label="Route distance markers"
      className="relative hidden lg:block"
    >
      <nav className="sticky top-24 self-start">
        <div className="text-cc-nav-label mb-6 font-mono text-[10px] font-semibold tracking-widest uppercase">
          Route
        </div>
        <ol className="relative pl-[1px]">
          {/* The rail rule. */}
          <span
            aria-hidden
            className="border-cc-card-border absolute top-1 bottom-1 left-0 border-l"
          />
          {KM_STOPS.map((stop, index) => (
            <RailTick
              key={stop.anchor}
              stop={stop}
              active={index === 0}
              finish={stop.anchor === "km-finish"}
            />
          ))}
        </ol>
      </nav>
    </aside>
  );
}

function RailTick({
  stop,
  active,
  finish,
}: {
  readonly stop: KmStop;
  readonly active: boolean;
  readonly finish: boolean;
}) {
  const reduce = useReducedMotion();

  return (
    <li className="relative mb-9 pl-5 last:mb-0">
      {/* Horizontal tick into the rail. Numbered KMs get a full tick. */}
      <span
        aria-hidden
        className={`absolute top-[7px] left-0 h-px ${
          finish ? "bg-cc-card-border-hover w-4" : "bg-cc-card-border w-3.5"
        }`}
      />
      {/* The cap. Active section gets the accent filled square + pulse. */}
      {active ? (
        <motion.span
          aria-hidden
          className="bg-cc-accent absolute top-[5px] -left-[3px] block size-1.5"
          animate={reduce ? undefined : { opacity: [1, 0.4, 1] }}
          transition={
            reduce
              ? undefined
              : { duration: 2, repeat: Infinity, ease: "easeInOut" }
          }
        />
      ) : (
        <span
          aria-hidden
          className={`absolute top-[5px] -left-[2px] block size-1 ${
            finish ? "bg-cc-heading" : "bg-cc-ink-dim"
          }`}
        />
      )}
      <a href={`#${stop.anchor}`} className="group block no-underline">
        <div
          className={`font-mono text-xs font-semibold tracking-widest uppercase ${
            active ? "text-cc-accent" : "text-cc-heading"
          }`}
        >
          {stop.km} {stop.km === "Fin" ? "" : "KM"}
        </div>
        <div className="text-cc-ink-dim group-hover:text-cc-ink mt-0.5 font-mono text-[10px] tracking-wider uppercase transition-colors">
          {stop.label}
        </div>
      </a>
    </li>
  );
}

// Dashed minor sub-tick between numbered KM bands inside the reading column.
function SubTick() {
  return (
    <div aria-hidden className="relative py-2">
      <span className="border-cc-card-border block w-10 border-t border-dashed opacity-70" />
    </div>
  );
}

interface KmBandProps {
  readonly anchor: string;
  readonly km: string;
  readonly eyebrow: string;
  readonly children: ReactNode;
}

// Each section is a KM band: enter-view-once fade-and-rise, mono distance
// eyebrow, generous vertical breathing room.
function KmBand({ anchor, km, eyebrow, children }: KmBandProps) {
  const reduce = useReducedMotion();

  return (
    <motion.section
      id={anchor}
      className="scroll-mt-24 py-12 sm:py-14"
      initial={reduce ? false : { opacity: 0, y: 8 }}
      whileInView={reduce ? undefined : { opacity: 1, y: 0 }}
      viewport={{ once: true, amount: 0.2 }}
      transition={{ duration: 0.5, ease: "easeOut" }}
    >
      <div className="text-cc-nav-label mb-5 font-mono text-xs font-semibold tracking-widest uppercase">
        KM {km} / {eyebrow}
      </div>
      {children}
    </motion.section>
  );
}

function KmHero() {
  return (
    <KmBand anchor="km-0" km="0" eyebrow="Day Zero">
      <h1 className="font-heading text-cc-heading text-h1 sm:text-hero leading-tight font-semibold tracking-tight">
        Training is a route, not a brochure.
      </h1>
      <p className="text-cc-prose mt-6 text-base sm:text-lg">
        GraphQL training workshops built around Hot Chocolate, Fusion, Nitro,
        and Relay. We start at Day Zero, pick the tracks your team needs, and
        pace the route to the room, then ship together at the finish line.
      </p>
      <div className="mt-8 flex flex-col gap-3 sm:flex-row sm:gap-4">
        <SolidButton href="#km-4">Plan the route</SolidButton>
        <OutlineButton href={TRAINING_MAILTO}>Talk to us</OutlineButton>
      </div>
      <dl className="border-cc-card-border mt-10 grid grid-cols-2 gap-x-6 gap-y-5 border-t pt-8 sm:grid-cols-4">
        <Signpost label="Tracks" value="6" />
        <Signpost label="Levels" value="Foundations to Production" />
        <Signpost label="Formats" value="On-site, Remote, Hybrid" />
        <Signpost label="Cadence" value="Workshop or multi-week" />
      </dl>
    </KmBand>
  );
}

function Signpost({
  label,
  value,
}: {
  readonly label: string;
  readonly value: string;
}) {
  return (
    <div>
      <dt className="text-cc-ink-dim font-mono text-[10px] tracking-widest uppercase">
        {label}
      </dt>
      <dd className="text-cc-heading mt-1.5 text-sm font-medium">{value}</dd>
    </div>
  );
}

function RouteBrief() {
  return (
    <KmBand anchor="km-1" km="1" eyebrow="The Route Brief">
      <h2 className="font-heading text-cc-heading text-h3 sm:text-h2 font-semibold tracking-tight">
        How an engagement is shaped.
      </h2>
      <p className="text-cc-prose mt-4 text-base leading-relaxed sm:text-lg">
        Before anyone boards, we agree on the route. You choose the tracks, we
        tune the depth to the room, and we run the labs on a real codebase or a
        neutral one we bring. The curriculum is in-depth and works really well,
        but it is not set in stone.
      </p>
      <ul className="text-cc-prose mt-7 space-y-3 text-sm sm:text-base">
        {[
          "Tracks chosen with you, not handed down as a fixed syllabus",
          "Depth tuned to the seniority of the room before we start",
          "Run on your own schema, or a neutral codebase we provide",
        ].map((line) => (
          <li key={line} className="flex items-start gap-3">
            <span
              className="text-cc-accent mt-[3px] inline-flex shrink-0"
              aria-hidden
            >
              <CheckIcon />
            </span>
            <span>{line}</span>
          </li>
        ))}
      </ul>
    </KmBand>
  );
}

function CurriculumLeg() {
  return (
    <KmBand anchor="km-2" km="2" eyebrow="Curriculum">
      <h2 className="font-heading text-cc-heading text-h3 sm:text-h2 font-semibold tracking-tight">
        Six checkpoints along the way.
      </h2>
      <p className="text-cc-prose mt-4 text-base sm:text-lg">
        Each track is modular. A Corporate Training stitches several together. A
        Corporate Workshop usually goes deep on one or two.
      </p>
      <ol className="border-cc-card-border mt-8 border-t">
        {CURRICULUM.map((track, index) => (
          <TrackRow key={track.code} track={track} number={index + 1} />
        ))}
      </ol>
    </KmBand>
  );
}

function TrackRow({
  track,
  number,
}: {
  readonly track: CurriculumTrack;
  readonly number: number;
}) {
  return (
    <li className="border-cc-card-border hover:border-cc-card-border-hover border-b py-6 transition-colors">
      <div className="flex items-baseline gap-4">
        <span className="text-cc-ink-dim font-mono text-xs tabular-nums">
          {String(number).padStart(2, "0")}
        </span>
        <span className="text-cc-accent font-mono text-xs font-semibold tracking-widest uppercase">
          {track.code}
        </span>
        <LevelChip level={track.level} />
        <span className="text-cc-ink-dim ml-auto font-mono text-[10px] tracking-wider uppercase">
          {track.topics.length} topics
        </span>
      </div>
      <h3 className="font-heading text-cc-heading mt-3 text-xl font-semibold tracking-tight">
        {track.title}
      </h3>
      <p className="text-cc-prose mt-2 text-sm leading-relaxed">
        {track.summary}
      </p>
    </li>
  );
}

function LevelChip({ level }: { readonly level: CurriculumTrack["level"] }) {
  return (
    <span className="border-cc-card-border text-cc-ink-dim rounded-full border px-2.5 py-0.5 font-mono text-[10px] font-semibold tracking-widest uppercase">
      {level}
    </span>
  );
}

function FormatLeg() {
  return (
    <KmBand anchor="km-3" km="3" eyebrow="Format">
      <h2 className="font-heading text-cc-heading text-h3 sm:text-h2 font-semibold tracking-tight">
        How the route is run on the ground.
      </h2>
      <p className="text-cc-prose mt-4 text-base sm:text-lg">
        The curriculum is the same. The room is up to you.
      </p>
      <div className="mt-8 flex flex-col gap-px">
        {FORMATS.map((format) => (
          <FormatCell key={format.name} format={format} />
        ))}
      </div>
    </KmBand>
  );
}

function FormatCell({ format }: { readonly format: DeliveryFormat }) {
  return (
    <article className="border-cc-card-border bg-cc-card-bg border p-6">
      <div className="flex flex-col gap-1 sm:flex-row sm:items-baseline sm:justify-between sm:gap-4">
        <h3 className="font-heading text-cc-heading text-xl font-semibold tracking-tight">
          {format.name}
        </h3>
        <p className="text-cc-ink-dim text-sm">{format.tagline}</p>
      </div>
      <ul className="text-cc-prose mt-4 grid gap-2 text-sm sm:grid-cols-3">
        {format.notes.map((note) => (
          <li key={note} className="flex items-start gap-2.5">
            <span
              className="text-cc-accent mt-[3px] inline-flex shrink-0"
              aria-hidden
            >
              <CheckIcon size={12} />
            </span>
            <span>{note}</span>
          </li>
        ))}
      </ul>
    </article>
  );
}

function OffersLeg() {
  return (
    <KmBand anchor="km-4" km="4" eyebrow="Two Ways To Run It">
      <h2 className="font-heading text-cc-heading text-h3 sm:text-h2 font-semibold tracking-tight">
        Two ways to run the route.
      </h2>
      <p className="text-cc-prose mt-4 text-base sm:text-lg">
        Pick the shape that fits how your team learns. The curriculum carries
        across both.
      </p>
      <div className="mt-8 grid gap-5 lg:grid-cols-2">
        {OFFERS.map((offer) => (
          <OfferCell key={offer.name} offer={offer} />
        ))}
      </div>
      <div className="mt-10 flex flex-col gap-3 sm:flex-row sm:gap-4">
        <SolidButton href={TRAINING_MAILTO}>Plan the route</SolidButton>
        <OutlineButton href={TRAINING_MAILTO}>Talk to us</OutlineButton>
      </div>
    </KmBand>
  );
}

function OfferCell({ offer }: { readonly offer: Offer }) {
  const cellSkin = offer.highlight
    ? "border-cc-accent/60 border-l-2 border-l-cc-accent"
    : "border-cc-card-border";

  return (
    <article
      className={`bg-cc-card-bg relative flex h-full flex-col border p-6 ${cellSkin}`}
    >
      <header>
        <div className="flex items-baseline justify-between gap-3">
          <h3 className="font-heading text-cc-heading text-xl font-semibold tracking-tight">
            {offer.name}
          </h3>
          {offer.highlight && (
            <span className="text-cc-accent font-mono text-[10px] font-semibold tracking-widest uppercase">
              Deep dive
            </span>
          )}
        </div>
        <p className="text-cc-ink-dim mt-1 text-sm">{offer.tagline}</p>
      </header>
      <p className="text-cc-prose mt-4 text-sm leading-relaxed">
        {offer.description}
      </p>
      <ul className="mt-5 flex-1 space-y-2.5">
        {offer.perks.map((perk) => (
          <li key={perk.text} className="flex items-start gap-3 text-sm">
            <span
              className="text-cc-accent mt-[3px] inline-flex shrink-0"
              aria-hidden
            >
              <CheckIcon />
            </span>
            <span className="text-cc-prose">{perk.text}</span>
          </li>
        ))}
      </ul>
    </article>
  );
}

function GuidesLeg() {
  return (
    <KmBand anchor="km-5" km="5" eyebrow="Route Guides">
      <h2 className="font-heading text-cc-heading text-h3 sm:text-h2 font-semibold tracking-tight">
        Guides who wrote the map.
      </h2>
      <p className="text-cc-prose mt-4 max-w-xl text-base leading-relaxed sm:text-lg">
        Every session is led by ChilliCream engineers who write and maintain Hot
        Chocolate, Fusion, and Nitro. When a question slides past the slide
        deck, you get an answer from the people who decided how it works.
      </p>
      <ul className="mt-8 grid gap-px sm:grid-cols-2">
        <GuideFact title="Product maintainers">
          Trainers ship on Hot Chocolate, Fusion, and Nitro.
        </GuideFact>
        <GuideFact title="Real engagements">
          Years of paid work shaping production GraphQL on .NET.
        </GuideFact>
        <GuideFact title="Public speakers">
          Regulars at GraphQL Conf and .NET community events.
        </GuideFact>
        <GuideFact title="Honest answers">
          We will tell you what the product does and what it does not.
        </GuideFact>
      </ul>
    </KmBand>
  );
}

function GuideFact({
  title,
  children,
}: {
  readonly title: string;
  readonly children: ReactNode;
}) {
  return (
    <li className="border-cc-card-border bg-cc-card-bg border p-5">
      <div className="flex items-start gap-3">
        <span
          className="text-cc-accent mt-[3px] inline-flex shrink-0"
          aria-hidden
        >
          <CheckIcon size={16} />
        </span>
        <div>
          <div className="text-cc-heading font-medium">{title}</div>
          <p className="text-cc-ink-dim mt-1 text-sm leading-relaxed">
            {children}
          </p>
        </div>
      </div>
    </li>
  );
}

function FaqLeg() {
  return (
    <KmBand anchor="km-6" km="6" eyebrow="Questions Before You Leave">
      <h2 className="font-heading text-cc-heading text-h3 sm:text-h2 font-semibold tracking-tight">
        The questions managers ask first.
      </h2>
      <p className="text-cc-prose mt-4 text-base sm:text-lg">
        Straight answers, no hedging.
      </p>
      <div className="border-cc-card-border divide-cc-card-border mt-8 divide-y border-t border-b">
        {FAQ.map((item, index) => (
          <details
            key={item.q}
            className="group py-5"
            name="training-faq-v8"
            open={index === 0}
          >
            <summary className="flex cursor-pointer list-none items-start justify-between gap-6">
              <span className="text-cc-heading text-base font-medium sm:text-lg">
                {item.q}
              </span>
              <span
                className="text-cc-ink-dim mt-1 inline-flex shrink-0 transition-transform group-open:rotate-45"
                aria-hidden
              >
                <PlusGlyph />
              </span>
            </summary>
            <p className="text-cc-prose mt-3 pr-10 text-sm leading-relaxed sm:text-base">
              {item.a}
            </p>
          </details>
        ))}
      </div>
    </KmBand>
  );
}

function PlusGlyph() {
  return (
    <svg viewBox="0 0 16 16" width={16} height={16} aria-hidden>
      <path
        d="M8 3v10M3 8h10"
        stroke="currentColor"
        strokeWidth="1.5"
        strokeLinecap="round"
      />
    </svg>
  );
}

function FinishLeg() {
  return (
    <KmBand anchor="km-finish" km="Fin" eyebrow="Finish, Ship Together">
      <div className="border-cc-card-border bg-cc-surface relative overflow-hidden rounded-2xl border p-8 sm:p-12">
        <FinishStripe />
        <h2 className="font-heading text-cc-heading text-h3 sm:text-h2 max-w-2xl font-semibold tracking-tight">
          Tell us which tracks, we will quote the rest.
        </h2>
        <p className="text-cc-prose mt-4 max-w-xl text-base sm:text-lg">
          Send a short note with the tracks you are interested in, headcount,
          and your preferred format. We come back with a written proposal and a
          date.
        </p>
        <div className="mt-8 flex flex-col gap-3 sm:flex-row sm:gap-4">
          <SolidButton href={TRAINING_MAILTO}>Email the team</SolidButton>
          <OutlineButton href="/services/advisory">
            Pair with Advisory
          </OutlineButton>
        </div>
      </div>
    </KmBand>
  );
}

// The one-time brand spectrum on the page: a 1px stripe that draws once on
// enter-view, replacing the rail rule at the finish line. cyan to violet to
// coral.
function FinishStripe() {
  const reduce = useReducedMotion();

  return (
    <svg
      className="pointer-events-none absolute top-0 left-0 h-full w-px"
      viewBox="0 0 1 100"
      preserveAspectRatio="none"
      aria-hidden
    >
      <defs>
        <linearGradient id="cc-training-v8-finish" x1="0" y1="0" x2="0" y2="1">
          <stop offset="0%" stopColor="#16b9e4" />
          <stop offset="50%" stopColor="#7c92c6" />
          <stop offset="100%" stopColor="#f0786a" />
        </linearGradient>
      </defs>
      <motion.line
        x1="0.5"
        y1="0"
        x2="0.5"
        y2="100"
        stroke="url(#cc-training-v8-finish)"
        strokeWidth="1"
        pathLength={1}
        strokeDasharray={1}
        initial={reduce ? { strokeDashoffset: 0 } : { strokeDashoffset: 1 }}
        whileInView={{ strokeDashoffset: 0 }}
        viewport={{ once: true, amount: 0.4 }}
        transition={{ duration: 1.1, ease: "easeInOut" }}
      />
    </svg>
  );
}

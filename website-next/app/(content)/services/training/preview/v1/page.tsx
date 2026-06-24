import type { Metadata } from "next";
import type { ReactNode } from "react";

import { CheckIcon } from "@/src/components/CheckIcon";
import { OutlineButton, SolidButton } from "@/src/design-system/Button";

export const metadata: Metadata = {
  title: "GraphQL Training Curriculum & Workshops | ChilliCream",
  description:
    "Catalogue of ChilliCream's GraphQL training: Hot Chocolate, Fusion, Nitro, Relay, schema design. Delivered as corporate training or focused workshops.",
  keywords: [
    "GraphQL training",
    "Hot Chocolate training",
    "Fusion federation training",
    "Relay workshop",
    "GraphQL workshop",
    "ChilliCream training",
    "corporate GraphQL training",
  ],
  openGraph: {
    title: "GraphQL Training Curriculum & Workshops | ChilliCream",
    description:
      "Catalogue of ChilliCream's GraphQL training: Hot Chocolate, Fusion, Nitro, Relay, schema design. Delivered as corporate training or focused workshops.",
  },
  robots: { index: false, follow: false },
};

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

export default function TrainingPreviewV1Page() {
  return (
    <>
      <CatalogHero />
      <CurriculumSection />
      <DeliveryFormats />
      <OffersSection />
      <InstructorsBand />
      <FaqSection />
      <ContactBand />
    </>
  );
}

function CatalogHero() {
  return (
    <section className="py-20 sm:py-24">
      <div className="grid gap-10 lg:grid-cols-[1.3fr_1fr] lg:items-end">
        <div>
          <div className="text-cc-nav-label mb-5 font-mono text-xs font-semibold tracking-widest uppercase">
            Training catalogue
          </div>
          <h1 className="font-heading text-cc-heading text-5xl leading-tight font-semibold tracking-tight sm:text-6xl lg:text-7xl">
            Six tracks. One team that wrote the code.
          </h1>
          <p className="text-cc-prose mt-6 max-w-2xl text-base sm:text-lg">
            A catalogue of GraphQL training built around Hot Chocolate, Fusion,
            Nitro, and Relay. Pick the tracks your team needs, then choose
            whether to run them as a tailored Corporate Training or as a focused
            Corporate Workshop on a real project.
          </p>
          <div className="mt-8 flex flex-col gap-3 sm:flex-row sm:gap-4">
            <SolidButton href="#curriculum">Browse the catalogue</SolidButton>
            <OutlineButton href={TRAINING_MAILTO}>Talk to us</OutlineButton>
          </div>
        </div>
        <CatalogTicker />
      </div>
    </section>
  );
}

function CatalogTicker() {
  return (
    <aside
      aria-label="At a glance"
      className="border-cc-card-border bg-cc-card-bg rounded-2xl border p-6 sm:p-7"
    >
      <div className="text-cc-nav-label mb-4 font-mono text-[11px] font-semibold tracking-widest uppercase">
        At a glance
      </div>
      <dl className="divide-cc-card-border divide-y">
        <TickerRow label="Tracks" value="6" />
        <TickerRow label="Levels" value="Foundations to Production" />
        <TickerRow label="Formats" value="On-site, Remote, Hybrid" />
        <TickerRow label="Cadence" value="Workshop or multi-week" />
        <TickerRow label="Pricing" value="On request" />
      </dl>
    </aside>
  );
}

function TickerRow({
  label,
  value,
}: {
  readonly label: string;
  readonly value: string;
}) {
  return (
    <div className="flex items-baseline justify-between gap-4 py-3 first:pt-0 last:pb-0">
      <dt className="text-cc-ink-dim font-mono text-xs tracking-wider uppercase">
        {label}
      </dt>
      <dd className="text-cc-heading text-right text-sm font-medium">
        {value}
      </dd>
    </div>
  );
}

function CurriculumSection() {
  return (
    <section id="curriculum" className="py-16">
      <div className="mb-10 flex flex-col gap-4 sm:flex-row sm:items-end sm:justify-between">
        <div className="max-w-2xl">
          <div className="text-cc-nav-label mb-3 font-mono text-xs font-semibold tracking-widest uppercase">
            What we teach
          </div>
          <h2 className="font-heading text-cc-heading text-3xl font-semibold tracking-tight sm:text-4xl">
            Six tracks, mix and match.
          </h2>
          <p className="text-cc-prose mt-3 text-base sm:text-lg">
            Each track is modular. A Corporate Training stitches several tracks
            together. A Corporate Workshop usually goes deep on one or two.
          </p>
        </div>
        <div className="text-cc-ink-dim font-mono text-xs tracking-widest uppercase">
          Catalogue v1
        </div>
      </div>

      <ol className="grid gap-5 md:grid-cols-2 xl:grid-cols-3">
        {CURRICULUM.map((track) => (
          <TrackCard key={track.code} track={track} />
        ))}
      </ol>
    </section>
  );
}

function TrackCard({ track }: { readonly track: CurriculumTrack }) {
  return (
    <li className="border-cc-card-border bg-cc-card-bg hover:border-cc-card-border-hover relative flex h-full flex-col rounded-2xl border p-6 transition-colors">
      <header className="border-cc-card-border mb-5 flex items-baseline justify-between gap-3 border-b pb-4">
        <span className="text-cc-accent font-mono text-xs font-semibold tracking-widest uppercase">
          {track.code}
        </span>
        <LevelChip level={track.level} />
      </header>
      <h3 className="font-heading text-cc-heading text-xl font-semibold tracking-tight">
        {track.title}
      </h3>
      <p className="text-cc-prose mt-3 text-sm leading-relaxed">
        {track.summary}
      </p>
      <ul className="text-cc-prose mt-5 space-y-2 text-sm">
        {track.topics.map((topic) => (
          <li key={topic} className="flex items-start gap-3">
            <span
              className="text-cc-accent mt-[3px] inline-flex shrink-0"
              aria-hidden
            >
              <CheckIcon />
            </span>
            <span>{topic}</span>
          </li>
        ))}
      </ul>
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

function DeliveryFormats() {
  return (
    <section className="py-16">
      <div className="mx-auto mb-10 max-w-2xl text-center">
        <div className="text-cc-nav-label mb-3 font-mono text-xs font-semibold tracking-widest uppercase">
          Delivery format
        </div>
        <h2 className="font-heading text-cc-heading text-3xl font-semibold tracking-tight sm:text-4xl">
          On-site, remote, or hybrid.
        </h2>
        <p className="text-cc-prose mt-3 text-base sm:text-lg">
          The catalogue is the same. The room is up to you.
        </p>
      </div>
      <div className="grid gap-5 md:grid-cols-3">
        {FORMATS.map((format) => (
          <FormatCard key={format.name} format={format} />
        ))}
      </div>
    </section>
  );
}

function FormatCard({ format }: { readonly format: DeliveryFormat }) {
  return (
    <article className="border-cc-card-border bg-cc-card-bg flex h-full flex-col rounded-2xl border p-6">
      <h3 className="font-heading text-cc-heading text-xl font-semibold tracking-tight">
        {format.name}
      </h3>
      <p className="text-cc-ink-dim mt-1 text-sm">{format.tagline}</p>
      <ul className="text-cc-prose mt-5 space-y-3 text-sm">
        {format.notes.map((note) => (
          <li key={note} className="flex items-start gap-3">
            <span
              className="text-cc-accent mt-[3px] inline-flex shrink-0"
              aria-hidden
            >
              <CheckIcon />
            </span>
            <span>{note}</span>
          </li>
        ))}
      </ul>
    </article>
  );
}

function OffersSection() {
  return (
    <section className="py-16">
      <div className="mx-auto mb-10 max-w-2xl text-center">
        <div className="text-cc-nav-label mb-3 font-mono text-xs font-semibold tracking-widest uppercase">
          How it is delivered
        </div>
        <h2 className="font-heading text-cc-heading text-3xl font-semibold tracking-tight sm:text-4xl">
          Two ways to run the catalogue.
        </h2>
        <p className="text-cc-prose mt-3 text-base sm:text-lg">
          Pick the shape that fits how your team learns. The curriculum carries
          across both.
        </p>
      </div>
      <div className="grid gap-6 lg:grid-cols-2">
        {OFFERS.map((offer) => (
          <OfferCard key={offer.name} offer={offer} />
        ))}
      </div>
    </section>
  );
}

function OfferCard({ offer }: { readonly offer: Offer }) {
  const cardSkin = offer.highlight
    ? "border-cc-accent/60 shadow-[0_0_0_1px_rgba(94,234,212,0.25)]"
    : "border-cc-card-border";
  const CtaButton = offer.highlight ? SolidButton : OutlineButton;

  return (
    <article
      className={`bg-cc-card-bg relative flex h-full flex-col rounded-2xl border p-7 ${cardSkin}`}
    >
      {offer.highlight && (
        <span className="bg-cc-accent text-cc-surface absolute -top-3 left-6 rounded-full px-3 py-1 font-mono text-[10px] font-semibold tracking-widest uppercase">
          Deep dive
        </span>
      )}
      <header>
        <h3 className="font-heading text-cc-heading text-2xl font-semibold tracking-tight">
          {offer.name}
        </h3>
        <p className="text-cc-ink-dim mt-1 text-sm">{offer.tagline}</p>
      </header>
      <p className="text-cc-prose mt-5 text-sm leading-relaxed sm:text-base">
        {offer.description}
      </p>
      <ul className="mt-6 flex-1 space-y-3">
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
      <div className="mt-7">
        <CtaButton
          href={`mailto:contact@chillicream.com?subject=${offer.name}`}
          className="w-full"
        >
          Talk to us about {offer.name}
        </CtaButton>
      </div>
    </article>
  );
}

function InstructorsBand() {
  return (
    <section className="py-16">
      <div className="border-cc-card-border bg-cc-card-bg relative overflow-hidden rounded-2xl border p-8 sm:p-12">
        <InstructorsGlow />
        <div className="relative grid gap-8 lg:grid-cols-[1.2fr_1fr] lg:items-center">
          <div>
            <div className="text-cc-accent mb-3 font-mono text-xs font-semibold tracking-widest uppercase">
              Who teaches
            </div>
            <h2 className="font-heading text-cc-heading text-3xl font-semibold tracking-tight sm:text-4xl">
              The team behind Hot Chocolate, Fusion, and Nitro.
            </h2>
            <p className="text-cc-prose mt-4 max-w-xl text-base leading-relaxed sm:text-lg">
              Every session is led by ChilliCream engineers who write and
              maintain the products you are training on. When a question slides
              past the slide deck, you get an answer from the people who decided
              how it works.
            </p>
          </div>
          <ul className="grid gap-4 sm:grid-cols-2">
            <InstructorFact title="Product maintainers">
              Trainers ship on Hot Chocolate, Fusion, and Nitro.
            </InstructorFact>
            <InstructorFact title="Real engagements">
              Years of paid work shaping production GraphQL on .NET.
            </InstructorFact>
            <InstructorFact title="Public speakers">
              Regulars at GraphQL Conf and .NET community events.
            </InstructorFact>
            <InstructorFact title="Honest answers">
              We will tell you what the product does and what it does not.
            </InstructorFact>
          </ul>
        </div>
      </div>
    </section>
  );
}

function InstructorsGlow() {
  return (
    <svg
      className="pointer-events-none absolute -top-24 -right-24 h-[420px] w-[420px] opacity-60"
      viewBox="0 0 400 400"
      aria-hidden
    >
      <defs>
        <radialGradient id="cc-training-v1-glow" cx="50%" cy="50%" r="50%">
          <stop offset="0%" stopColor="#5eead4" stopOpacity="0.35" />
          <stop offset="60%" stopColor="#5eead4" stopOpacity="0.05" />
          <stop offset="100%" stopColor="#5eead4" stopOpacity="0" />
        </radialGradient>
      </defs>
      <circle cx="200" cy="200" r="200" fill="url(#cc-training-v1-glow)" />
    </svg>
  );
}

function InstructorFact({
  title,
  children,
}: {
  readonly title: string;
  readonly children: ReactNode;
}) {
  return (
    <li className="border-cc-card-border rounded-xl border p-4">
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

function FaqSection() {
  return (
    <section className="py-16">
      <div className="mx-auto mb-10 max-w-2xl text-center">
        <div className="text-cc-nav-label mb-3 font-mono text-xs font-semibold tracking-widest uppercase">
          FAQ
        </div>
        <h2 className="font-heading text-cc-heading text-3xl font-semibold tracking-tight sm:text-4xl">
          The questions managers ask first.
        </h2>
        <p className="text-cc-prose mt-3 text-base sm:text-lg">
          Straight answers, no hedging.
        </p>
      </div>
      <div className="border-cc-card-border bg-cc-card-bg divide-cc-card-border divide-y rounded-2xl border">
        {FAQ.map((item) => (
          <details
            key={item.q}
            className="group px-5 py-5 sm:px-6"
            name="training-faq"
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
    </section>
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

function ContactBand() {
  return (
    <section className="py-20">
      <div className="border-cc-card-border bg-cc-card-bg rounded-2xl border p-8 text-center sm:p-12">
        <div className="text-cc-nav-label mb-3 font-mono text-xs font-semibold tracking-widest uppercase">
          Get in touch
        </div>
        <h2 className="font-heading text-cc-heading mx-auto max-w-2xl text-3xl font-semibold tracking-tight sm:text-4xl">
          Tell us which tracks, we will quote the rest.
        </h2>
        <p className="text-cc-prose mx-auto mt-4 max-w-xl text-base sm:text-lg">
          Send a short note with the tracks you are interested in, headcount,
          and your preferred format. We come back with a written proposal and a
          date.
        </p>
        <div className="mt-8 flex flex-col items-center justify-center gap-3 sm:flex-row sm:gap-4">
          <SolidButton href={TRAINING_MAILTO}>Email the team</SolidButton>
          <OutlineButton href="/services/advisory">
            Pair with Advisory
          </OutlineButton>
        </div>
      </div>
    </section>
  );
}

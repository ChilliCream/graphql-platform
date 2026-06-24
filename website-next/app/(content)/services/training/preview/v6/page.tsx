import type { Metadata } from "next";
import type { ReactNode } from "react";

import { CheckIcon } from "@/src/components/CheckIcon";
import { OutlineButton, SolidButton } from "@/src/design-system/Button";
import { DripBrewer } from "@/src/icons/DripBrewer";
import { FrenchPress } from "@/src/icons/FrenchPress";
import { PourOver } from "@/src/icons/PourOver";

export const metadata: Metadata = {
  title: "GraphQL Training Workshops, House Blend | ChilliCream",
  description:
    "GraphQL training workshops from ChilliCream covering Hot Chocolate, Fusion, Nitro, Relay, schema design. On-site, remote, or hybrid, brewed by our engineers.",
  keywords: [
    "GraphQL training workshops",
    "GraphQL training",
    "Hot Chocolate training",
    "Fusion federation training",
    "Relay workshop",
    "ChilliCream training",
    "corporate GraphQL training",
  ],
  openGraph: {
    title: "GraphQL Training Workshops, House Blend | ChilliCream",
    description:
      "GraphQL training workshops from ChilliCream: Hot Chocolate, Fusion, Nitro, Relay, schema design. Six tracks, brewed by the engineers behind the products.",
  },
  robots: { index: false, follow: false },
};

type TrackLevel = "Foundations" | "Core" | "Advanced" | "Production";
type SizeLabel = "Short" | "Tall" | "Grande" | "Reserve";

interface CurriculumTrack {
  readonly code: string;
  readonly title: string;
  readonly level: TrackLevel;
  readonly size: SizeLabel;
  readonly brewStyle: string;
  readonly brewIcon: "drip" | "french" | "pour" | "espresso" | "tray" | "cup";
  readonly summary: string;
  readonly topics: readonly string[];
}

const CURRICULUM: readonly CurriculumTrack[] = [
  {
    code: "GQL-101",
    title: "GraphQL Fundamentals",
    level: "Foundations",
    size: "Short",
    brewStyle: "Short pour",
    brewIcon: "cup",
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
    size: "Tall",
    brewStyle: "House drip",
    brewIcon: "drip",
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
    size: "Grande",
    brewStyle: "Pour-over",
    brewIcon: "pour",
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
    size: "Reserve",
    brewStyle: "Cold brew",
    brewIcon: "espresso",
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
    size: "Grande",
    brewStyle: "French press",
    brewIcon: "french",
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
    size: "Grande",
    brewStyle: "Tasting flight",
    brewIcon: "tray",
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
  readonly kicker: string;
  readonly tagline: string;
  readonly notes: readonly string[];
}

const FORMATS: readonly DeliveryFormat[] = [
  {
    name: "On-site",
    kicker: "We come to your bar",
    tagline: "Instructor in the room with your team.",
    notes: [
      "Instructor on location with your team",
      "Best for hands-on labs and whiteboard design",
      "Travel quoted with the engagement",
    ],
  },
  {
    name: "Remote",
    kicker: "Delivered to your door",
    tagline: "Live, distributed cohorts across time zones.",
    notes: [
      "Live sessions across time zones",
      "Shared repo, recordings, and Q&A channel",
      "Easiest to schedule across multiple offices",
    ],
  },
  {
    name: "Hybrid",
    kicker: "Some in, some takeaway",
    tagline: "Half in the room, half on the call.",
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
  readonly kicker: string;
  readonly tagline: string;
  readonly description: string;
  readonly cta: string;
  readonly perks: readonly OfferPerk[];
  readonly highlight?: boolean;
}

const OFFERS: readonly Offer[] = [
  {
    name: "Corporate Training",
    kicker: "The tasting flight",
    tagline: "Flexible curriculum, shaped to your team.",
    description:
      "Get your team trained in GraphQL, any of our products, and even React/Relay. Beginner Team? Advanced Team? Or Mixed? Don't panic! Our curriculum is designed to teach in-depth and works really well, but isn't set in stone.",
    cta: "Order the flight",
    perks: [
      { text: "Level up their proficiency" },
      { text: "Catered to different skills" },
      { text: "Overcome challenges they have been wrestling with" },
      { text: "Get everybody on the same technical page" },
    ],
  },
  {
    name: "Corporate Workshop",
    kicker: "The reserve pour",
    tagline: "Focused, hands-on, project-shaped.",
    description:
      "We will look at how to build a GraphQL server with ASP.NET Core 7 and Hot Chocolate. You will learn how to explore and manage large schemas. Further, we will dive into React and explore how to efficiently build fast and fluent web interfaces using Relay.",
    cta: "Reserve a pour",
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

export default function TrainingPreviewV6Page() {
  return (
    <>
      <CatalogHero />
      <MenuBoard />
      <ServiceStyle />
      <OrderTheRound />
      <BehindTheBar />
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
          <div className="text-cc-nav-label mb-5 inline-flex items-center gap-3 font-mono text-xs font-semibold tracking-widest uppercase">
            <SteamingCup className="text-cc-accent h-5 w-5" />
            <span>Today&apos;s pour</span>
          </div>
          <h1 className="font-heading text-cc-heading text-5xl leading-tight font-semibold tracking-tight sm:text-6xl lg:text-7xl">
            Six tracks, brewed by the team who roasted the beans.
          </h1>
          <p className="text-cc-prose mt-6 max-w-2xl text-base sm:text-lg">
            GraphQL training workshops built around Hot Chocolate, Fusion,
            Nitro, and Relay. Pick the tracks your team needs, then choose
            whether to run them as a tailored Corporate Training or as a focused
            Corporate Workshop on a real project.
          </p>
          <div className="mt-8 flex flex-col gap-3 sm:flex-row sm:gap-4">
            <SolidButton href="#menu">Browse the menu</SolidButton>
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
      aria-label="At the bar"
      className="border-cc-card-border bg-cc-card-bg rounded-2xl border p-6 sm:p-7"
    >
      <div className="text-cc-nav-label mb-4 flex items-center justify-between font-mono text-[11px] font-semibold tracking-widest uppercase">
        <span>At the bar</span>
        <SteamingCup className="text-cc-accent h-4 w-4" />
      </div>
      <dl className="divide-cc-card-border divide-y">
        <TickerRow label="Tracks" value="6" />
        <TickerRow label="Levels" value="Short to Reserve" />
        <TickerRow label="Service" value="On-site, Remote, Hybrid" />
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

function MenuBoard() {
  return (
    <section id="menu" className="py-16">
      <div className="mb-10 flex flex-col gap-4 sm:flex-row sm:items-end sm:justify-between">
        <div className="max-w-2xl">
          <div className="text-cc-nav-label mb-3 font-mono text-xs font-semibold tracking-widest uppercase">
            On the menu
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
          House menu, v6
        </div>
      </div>

      <div className="border-cc-card-border bg-cc-card-bg overflow-hidden rounded-2xl border">
        <div className="bg-cc-accent/70 h-px w-full" />
        <ol className="divide-cc-card-border divide-y">
          {CURRICULUM.map((track, index) => (
            <MenuRow key={track.code} track={track} index={index} />
          ))}
        </ol>
        <div className="bg-cc-accent/70 h-px w-full" />
      </div>
    </section>
  );
}

function MenuRow({
  track,
  index,
}: {
  readonly track: CurriculumTrack;
  readonly index: number;
}) {
  return (
    <li className="px-5 py-6 sm:px-7 sm:py-7">
      <div className="grid gap-5 md:grid-cols-[auto_1fr_auto] md:items-start md:gap-7">
        <div className="flex items-start gap-4 md:w-56">
          <span className="text-cc-ink-dim font-mono text-sm tabular-nums">
            {String(index + 1).padStart(2, "0")}
          </span>
          <div>
            <div className="text-cc-accent font-mono text-xs font-semibold tracking-widest uppercase">
              {track.code}
            </div>
            <div className="text-cc-ink-dim mt-2 flex items-center gap-2 font-mono text-[11px] tracking-wider uppercase">
              <BrewGlyph kind={track.brewIcon} />
              <span>{track.brewStyle}</span>
            </div>
          </div>
        </div>

        <div>
          <h3 className="font-heading text-cc-heading text-xl font-semibold tracking-tight">
            {track.title}
          </h3>
          <p className="text-cc-prose mt-2 text-sm leading-relaxed sm:text-base">
            {track.summary}
          </p>
          <ul className="text-cc-prose mt-4 grid gap-2 text-sm md:grid-cols-2 md:gap-x-6">
            {track.topics.map((topic) => (
              <li key={topic} className="flex items-start gap-2.5">
                <span
                  className="text-cc-accent mt-[3px] inline-flex shrink-0"
                  aria-hidden
                >
                  <CheckIcon size={14} />
                </span>
                <span>{topic}</span>
              </li>
            ))}
          </ul>
        </div>

        <SizeChip size={track.size} level={track.level} />
      </div>
    </li>
  );
}

function SizeChip({
  size,
  level,
}: {
  readonly size: SizeLabel;
  readonly level: TrackLevel;
}) {
  return (
    <div className="flex shrink-0 items-start md:flex-col md:items-end md:gap-2">
      <span className="border-cc-card-border text-cc-heading inline-flex items-center gap-1.5 rounded-full border px-3 py-1 font-mono text-[11px] font-semibold tracking-widest uppercase">
        <span
          className="bg-cc-accent inline-block h-1.5 w-1.5 rounded-full"
          aria-hidden
        />
        {size}
      </span>
      <span className="text-cc-ink-dim ml-3 font-mono text-[10px] tracking-widest uppercase md:ml-0">
        {level}
      </span>
    </div>
  );
}

function BrewGlyph({ kind }: { readonly kind: CurriculumTrack["brewIcon"] }) {
  const className = "text-cc-accent h-5 w-5 shrink-0";
  switch (kind) {
    case "drip":
      return <DripBrewer className={className} />;
    case "french":
      return <FrenchPress className={className} />;
    case "pour":
      return <PourOver className={className} />;
    case "espresso":
      return <ColdBrewGlyph className={className} />;
    case "tray":
      return <TastingFlightGlyph className={className} />;
    case "cup":
    default:
      return <ShortPourGlyph className={className} />;
  }
}

function ShortPourGlyph({ className }: { readonly className?: string }) {
  return (
    <svg
      viewBox="0 0 24 24"
      fill="none"
      stroke="currentColor"
      strokeWidth={1.6}
      strokeLinecap="round"
      strokeLinejoin="round"
      aria-hidden="true"
      className={className}
    >
      <path d="M5 10h11v5a3 3 0 0 1-3 3H8a3 3 0 0 1-3-3z" />
      <path d="M16 11h1.5a2.5 2.5 0 0 1 0 5H16" />
      <path d="M8 6v2M11 5v3M14 6v2" opacity={0.6} />
    </svg>
  );
}

function ColdBrewGlyph({ className }: { readonly className?: string }) {
  return (
    <svg
      viewBox="0 0 24 24"
      fill="none"
      stroke="currentColor"
      strokeWidth={1.6}
      strokeLinecap="round"
      strokeLinejoin="round"
      aria-hidden="true"
      className={className}
    >
      <rect x="7" y="4" width="10" height="16" rx="1.5" />
      <line x1="7" y1="9" x2="17" y2="9" />
      <line x1="10" y1="13" x2="14" y2="13" opacity={0.6} />
      <line x1="10" y1="16" x2="14" y2="16" opacity={0.4} />
      <path d="M9 2v2M15 2v2" />
    </svg>
  );
}

function TastingFlightGlyph({ className }: { readonly className?: string }) {
  return (
    <svg
      viewBox="0 0 24 24"
      fill="none"
      stroke="currentColor"
      strokeWidth={1.6}
      strokeLinecap="round"
      strokeLinejoin="round"
      aria-hidden="true"
      className={className}
    >
      <path d="M3 18h18" />
      <path d="M5 10h3v5a1.5 1.5 0 0 1-1.5 1.5h0A1.5 1.5 0 0 1 5 15z" />
      <path d="M10.5 10h3v5a1.5 1.5 0 0 1-1.5 1.5h0a1.5 1.5 0 0 1-1.5-1.5z" />
      <path d="M16 10h3v5a1.5 1.5 0 0 1-1.5 1.5h0A1.5 1.5 0 0 1 16 15z" />
      <path d="M6.5 6v2M12 5v3M17.5 6v2" opacity={0.55} />
    </svg>
  );
}

function SteamingCup({ className }: { readonly className?: string }) {
  return (
    <svg
      viewBox="0 0 24 24"
      fill="none"
      stroke="currentColor"
      strokeWidth={1.6}
      strokeLinecap="round"
      strokeLinejoin="round"
      aria-hidden="true"
      className={className}
    >
      <path d="M4 11h12v5a4 4 0 0 1-4 4H8a4 4 0 0 1-4-4z" />
      <path d="M16 12h2a2.5 2.5 0 0 1 0 5h-2" />
      <path d="M8 7c0-1 1-1.5 1-2.5S8 3 8 2" opacity={0.7} />
      <path d="M12 7c0-1 1-1.5 1-2.5S12 3 12 2" opacity={0.7} />
    </svg>
  );
}

function ServiceStyle() {
  return (
    <section className="py-16">
      <div className="mx-auto mb-10 max-w-2xl text-center">
        <div className="text-cc-nav-label mb-3 font-mono text-xs font-semibold tracking-widest uppercase">
          How we serve
        </div>
        <h2 className="font-heading text-cc-heading text-3xl font-semibold tracking-tight sm:text-4xl">
          On-site, remote, or hybrid.
        </h2>
        <p className="text-cc-prose mt-3 text-base sm:text-lg">
          The menu is the same. The room is up to you.
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
      <div className="text-cc-accent font-mono text-[11px] font-semibold tracking-widest uppercase">
        {format.kicker}
      </div>
      <h3 className="font-heading text-cc-heading mt-2 text-xl font-semibold tracking-tight">
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

function OrderTheRound() {
  return (
    <section className="py-16">
      <div className="mx-auto mb-10 max-w-2xl text-center">
        <div className="text-cc-nav-label mb-3 font-mono text-xs font-semibold tracking-widest uppercase">
          Two ways to order
        </div>
        <h2 className="font-heading text-cc-heading text-3xl font-semibold tracking-tight sm:text-4xl">
          Two ways to run the menu.
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
          Reserve pour
        </span>
      )}
      <header>
        <div className="text-cc-accent font-mono text-[11px] font-semibold tracking-widest uppercase">
          {offer.kicker}
        </div>
        <h3 className="font-heading text-cc-heading mt-2 text-2xl font-semibold tracking-tight">
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
          {offer.cta}
        </CtaButton>
      </div>
    </article>
  );
}

function BehindTheBar() {
  return (
    <section className="py-16">
      <div className="border-cc-card-border bg-cc-card-bg relative overflow-hidden rounded-2xl border p-8 sm:p-12">
        <BehindTheBarGlow />
        <div className="relative grid gap-8 lg:grid-cols-[1.2fr_1fr] lg:items-center">
          <div>
            <div className="text-cc-accent mb-3 font-mono text-xs font-semibold tracking-widest uppercase">
              Behind the bar
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
            <InstructorFact title="Roasters">
              We wrote the beans. Trainers ship on Hot Chocolate, Fusion, and
              Nitro.
            </InstructorFact>
            <InstructorFact title="Years pulling shots">
              Years of paid engagements shaping production GraphQL on .NET.
            </InstructorFact>
            <InstructorFact title="Open mic">
              Regulars at GraphQL Conf and .NET community events.
            </InstructorFact>
            <InstructorFact title="No watered-down answers">
              We will tell you what the product does and what it does not.
            </InstructorFact>
          </ul>
        </div>
      </div>
    </section>
  );
}

function BehindTheBarGlow() {
  return (
    <svg
      className="pointer-events-none absolute -top-24 -right-24 h-[420px] w-[420px] opacity-60"
      viewBox="0 0 400 400"
      aria-hidden
    >
      <defs>
        <radialGradient id="cc-training-v6-glow" cx="50%" cy="50%" r="50%">
          <stop offset="0%" stopColor="#5eead4" stopOpacity="0.35" />
          <stop offset="60%" stopColor="#5eead4" stopOpacity="0.05" />
          <stop offset="100%" stopColor="#5eead4" stopOpacity="0" />
        </radialGradient>
      </defs>
      <circle cx="200" cy="200" r="200" fill="url(#cc-training-v6-glow)" />
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
          House rules
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
            name="training-faq-v6"
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
      <div className="border-cc-card-border bg-cc-card-bg relative overflow-hidden rounded-2xl border p-8 text-center sm:p-12">
        <SteamingCup
          className="text-cc-accent pointer-events-none absolute -right-6 -bottom-6 h-44 w-44 opacity-10"
          aria-hidden
        />
        <div className="relative">
          <div className="text-cc-nav-label mb-3 font-mono text-xs font-semibold tracking-widest uppercase">
            Place an order
          </div>
          <h2 className="font-heading text-cc-heading mx-auto max-w-2xl text-3xl font-semibold tracking-tight sm:text-4xl">
            Tell us your order, we will write the ticket.
          </h2>
          <p className="text-cc-prose mx-auto mt-4 max-w-xl text-base sm:text-lg">
            Send a short note with the tracks you are interested in, headcount,
            and your preferred format. We come back with a written proposal and
            a date.
          </p>
          <div className="mt-8 flex flex-col items-center justify-center gap-3 sm:flex-row sm:gap-4">
            <SolidButton href={TRAINING_MAILTO}>Email the team</SolidButton>
            <OutlineButton href="/services/advisory">
              Pair with Advisory
            </OutlineButton>
          </div>
        </div>
      </div>
    </section>
  );
}

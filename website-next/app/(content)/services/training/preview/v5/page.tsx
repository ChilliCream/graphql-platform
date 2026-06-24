import type { Metadata } from "next";
import type { ReactNode } from "react";

import { CheckIcon } from "@/src/components/CheckIcon";
import { OutlineButton, SolidButton } from "@/src/design-system/Button";

export const metadata: Metadata = {
  title: "GraphQL Training Workshops | ChilliCream",
  description:
    "Catalogue of ChilliCream GraphQL training on Hot Chocolate, Fusion, Nitro, and Relay. Corporate training or focused workshops, on-site, remote, or hybrid.",
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
    title: "GraphQL Training Workshops | ChilliCream",
    description:
      "Catalogue of ChilliCream GraphQL training on Hot Chocolate, Fusion, Nitro, and Relay. Corporate training or focused workshops, on-site, remote, or hybrid.",
  },
  robots: { index: false, follow: false },
};

const TRAINING_MAILTO = "mailto:contact@chillicream.com?subject=Training";

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

interface TocEntry {
  readonly num: string;
  readonly id: string;
  readonly label: string;
}

const TOC: readonly TocEntry[] = [
  { num: "01", id: "overview", label: "Overview" },
  { num: "02", id: "curriculum", label: "Curriculum" },
  { num: "03", id: "formats", label: "Delivery formats" },
  { num: "04", id: "offers", label: "How it is delivered" },
  { num: "05", id: "instructors", label: "Instructors" },
  { num: "06", id: "faq", label: "FAQ" },
  { num: "07", id: "contact", label: "Get in touch" },
];

export default function TrainingPreviewV5Page() {
  return (
    <div className="py-12 sm:py-16">
      <MobileAnchorBar />
      <div className="lg:grid lg:grid-cols-[240px_minmax(0,1fr)] lg:gap-12">
        <Sidebar />
        <main className="max-w-[760px] min-w-0">
          <OverviewSection />
          <SectionDivider />
          <CurriculumSection />
          <SectionDivider />
          <FormatsSection />
          <SectionDivider />
          <OffersSection />
          <SectionDivider />
          <InstructorsSection />
          <SectionDivider />
          <FaqSection />
          <SectionDivider />
          <ContactSection />
        </main>
      </div>
    </div>
  );
}

function Sidebar() {
  return (
    <aside
      aria-label="Section index"
      className="sticky top-24 hidden self-start lg:block"
    >
      <div className="text-cc-nav-label mb-5 font-mono text-[11px] font-semibold tracking-widest uppercase">
        Syllabus
      </div>
      <nav>
        <ol className="space-y-1">
          {TOC.map((entry, index) => (
            <li key={entry.id}>
              <a
                href={`#${entry.id}`}
                className="group border-cc-card-border hover:border-cc-accent flex items-baseline gap-3 border-l-2 py-1.5 pl-3 transition-colors"
              >
                <span className="text-cc-ink-dim group-hover:text-cc-accent font-mono text-[11px] tracking-widest tabular-nums">
                  {entry.num}
                </span>
                <span
                  className={`font-mono text-xs tracking-widest uppercase ${
                    index === 0
                      ? "text-cc-accent"
                      : "text-cc-ink-dim group-hover:text-cc-heading"
                  }`}
                >
                  {entry.label}
                </span>
              </a>
            </li>
          ))}
        </ol>
      </nav>
      <div className="border-cc-card-border mt-6 border-t pt-5">
        <div className="text-cc-ink-dim font-mono text-[10px] tracking-widest uppercase">
          Catalogue v1
        </div>
        <div className="text-cc-ink-dim mt-2 font-mono text-[10px] tracking-widest uppercase">
          Six tracks
        </div>
      </div>
    </aside>
  );
}

function MobileAnchorBar() {
  return (
    <nav
      aria-label="Section anchors"
      className="border-cc-card-border bg-cc-bg/90 sticky top-16 z-20 -mx-4 mb-8 overflow-x-auto border-b px-4 py-3 backdrop-blur sm:-mx-6 sm:px-6 lg:hidden"
    >
      <ul className="flex min-w-max gap-2">
        {TOC.map((entry) => (
          <li key={entry.id}>
            <a
              href={`#${entry.id}`}
              className="border-cc-card-border text-cc-ink-dim hover:border-cc-accent hover:text-cc-accent inline-flex items-center gap-2 rounded-full border px-3 py-1 font-mono text-[10px] tracking-widest uppercase transition-colors"
            >
              <span className="tabular-nums">{entry.num}</span>
              <span>{entry.label}</span>
            </a>
          </li>
        ))}
      </ul>
    </nav>
  );
}

function SectionDivider() {
  return <hr className="border-cc-card-border my-12 border-t sm:my-16" />;
}

interface SectionTagProps {
  readonly num: string;
  readonly slug: string;
}

function SectionTag({ num, slug }: SectionTagProps) {
  return (
    <div className="text-cc-nav-label border-cc-card-border mb-5 inline-flex items-center gap-2 rounded-sm border px-2 py-1 font-mono text-[10px] tracking-widest uppercase">
      <span className="tabular-nums">{`§ ${num}`}</span>
      <span className="text-cc-ink-dim">/ {slug}</span>
    </div>
  );
}

function OverviewSection() {
  return (
    <section id="overview" className="scroll-mt-28">
      <SectionTag num="01" slug="overview" />
      <div className="grid grid-cols-1 gap-10 lg:grid-cols-[minmax(0,1fr)_220px]">
        <div>
          <div className="text-cc-nav-label mb-4 font-mono text-xs font-semibold tracking-widest uppercase">
            Training catalogue
          </div>
          <h1 className="font-heading text-cc-heading text-hero tracking-tight">
            GraphQL training workshops, taught by the team that ships the code.
          </h1>
          <p className="text-cc-prose text-lead mt-6">
            A reference catalogue of GraphQL training built around Hot
            Chocolate, Fusion, Nitro, and Relay. Pick the tracks your team
            needs, then choose whether to run them as a tailored Corporate
            Training or as a focused Corporate Workshop on a real project.
          </p>
          <div className="mt-8 flex flex-col gap-3 sm:flex-row sm:gap-4">
            <SolidButton href="#curriculum">Browse the catalogue</SolidButton>
            <OutlineButton href={TRAINING_MAILTO}>Talk to us</OutlineButton>
          </div>
        </div>
        <dl className="border-cc-card-border divide-cc-card-border divide-y border-y lg:mt-2">
          <SpecRow label="Tracks" value="6" />
          <SpecRow label="Levels" value="Foundations to Production" />
          <SpecRow label="Formats" value="On-site, Remote, Hybrid" />
          <SpecRow label="Cadence" value="Workshop or multi-week" />
          <SpecRow label="Pricing" value="On request" />
        </dl>
      </div>
    </section>
  );
}

interface SpecRowProps {
  readonly label: string;
  readonly value: string;
}

function SpecRow({ label, value }: SpecRowProps) {
  return (
    <div className="flex items-baseline justify-between gap-4 py-3">
      <dt className="text-cc-ink-dim font-mono text-xs tracking-widest uppercase">
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
    <section id="curriculum" className="scroll-mt-28">
      <SectionTag num="02" slug="curriculum" />
      <h2 className="font-heading text-cc-heading text-h2 font-semibold tracking-tight">
        Curriculum.
      </h2>
      <p className="text-cc-prose text-body mt-3 max-w-2xl">
        Six modular tracks. A Corporate Training stitches several tracks
        together. A Corporate Workshop usually goes deep on one or two.
      </p>
      <ol className="divide-cc-card-border mt-8 divide-y">
        {CURRICULUM.map((track, index) => (
          <TrackEntry key={track.code} track={track} index={index + 1} />
        ))}
      </ol>
    </section>
  );
}

interface TrackEntryProps {
  readonly track: CurriculumTrack;
  readonly index: number;
}

function TrackEntry({ track, index }: TrackEntryProps) {
  const numLabel = index.toString().padStart(2, "0");
  return (
    <li className="py-7 first:pt-0 last:pb-0">
      <div className="flex items-baseline gap-4">
        <span className="text-cc-ink-dim font-mono text-xs tracking-widest tabular-nums">
          {numLabel}
        </span>
        <div className="flex flex-wrap items-baseline gap-3">
          <span className="text-cc-accent font-mono text-xs font-semibold tracking-widest uppercase">
            {track.code}
          </span>
          <span className="border-cc-card-border text-cc-ink-dim rounded-full border px-2 py-0.5 font-mono text-[10px] font-semibold tracking-widest uppercase">
            {track.level}
          </span>
        </div>
      </div>
      <h3 className="font-heading text-cc-heading text-h4 mt-3 font-semibold tracking-tight">
        {track.title}
      </h3>
      <p className="text-cc-prose text-body mt-2">{track.summary}</p>
      <ul className="text-cc-prose mt-5 grid gap-x-6 gap-y-2 text-sm sm:grid-cols-2">
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
    </li>
  );
}

function FormatsSection() {
  return (
    <section id="formats" className="scroll-mt-28">
      <SectionTag num="03" slug="delivery-formats" />
      <h2 className="font-heading text-cc-heading text-h2 font-semibold tracking-tight">
        Delivery formats.
      </h2>
      <p className="text-cc-prose text-body mt-3 max-w-2xl">
        The catalogue is the same. The room is up to you.
      </p>
      <dl className="divide-cc-card-border mt-8 divide-y">
        {FORMATS.map((format) => (
          <FormatEntry key={format.name} format={format} />
        ))}
      </dl>
    </section>
  );
}

function FormatEntry({ format }: { readonly format: DeliveryFormat }) {
  return (
    <div className="grid grid-cols-1 gap-3 py-6 first:pt-0 last:pb-0 sm:grid-cols-[160px_minmax(0,1fr)] sm:gap-6">
      <div>
        <dt className="text-cc-accent font-mono text-xs font-semibold tracking-widest uppercase">
          {format.name}
        </dt>
        <p className="text-cc-ink-dim mt-1 text-sm">{format.tagline}</p>
      </div>
      <dd>
        <ul className="text-cc-prose space-y-2 text-sm">
          {format.notes.map((note) => (
            <li key={note} className="flex items-start gap-2.5">
              <span
                className="text-cc-accent mt-[3px] inline-flex shrink-0"
                aria-hidden
              >
                <CheckIcon size={14} />
              </span>
              <span>{note}</span>
            </li>
          ))}
        </ul>
      </dd>
    </div>
  );
}

function OffersSection() {
  return (
    <section id="offers" className="scroll-mt-28">
      <SectionTag num="04" slug="how-it-is-delivered" />
      <h2 className="font-heading text-cc-heading text-h2 font-semibold tracking-tight">
        How it is delivered.
      </h2>
      <p className="text-cc-prose text-body mt-3 max-w-2xl">
        Pick the shape that fits how your team learns. The curriculum carries
        across both.
      </p>
      <div className="divide-cc-card-border mt-8 grid gap-8 lg:grid-cols-2 lg:gap-0 lg:divide-x">
        {OFFERS.map((offer) => (
          <OfferEntry key={offer.name} offer={offer} />
        ))}
      </div>
    </section>
  );
}

function OfferEntry({ offer }: { readonly offer: Offer }) {
  const containerClass = offer.highlight
    ? "border-cc-accent border-l-2 pl-6 lg:pl-8"
    : "lg:pl-8";
  return (
    <article className={containerClass}>
      <div className="flex flex-wrap items-baseline gap-3">
        <h3 className="font-heading text-cc-heading text-h4 font-semibold tracking-tight">
          {offer.name}
        </h3>
        {offer.highlight && (
          <span className="text-cc-accent border-cc-accent/40 rounded-sm border px-2 py-0.5 font-mono text-[10px] font-semibold tracking-widest uppercase">
            Deep dive
          </span>
        )}
      </div>
      <p className="text-cc-ink-dim mt-1 text-sm">{offer.tagline}</p>
      <p className="text-cc-prose text-body mt-4">{offer.description}</p>
      <ul className="mt-5 space-y-2.5 text-sm">
        {offer.perks.map((perk) => (
          <li key={perk.text} className="flex items-start gap-2.5">
            <span
              className="text-cc-accent mt-[3px] inline-flex shrink-0"
              aria-hidden
            >
              <CheckIcon size={14} />
            </span>
            <span className="text-cc-prose">{perk.text}</span>
          </li>
        ))}
      </ul>
      <div className="mt-6">
        <OutlineButton
          href={`mailto:contact@chillicream.com?subject=${offer.name}`}
        >
          Talk to us about {offer.name}
        </OutlineButton>
      </div>
    </article>
  );
}

function InstructorsSection() {
  return (
    <section id="instructors" className="scroll-mt-28">
      <SectionTag num="05" slug="instructors" />
      <h2 className="font-heading text-cc-heading text-h2 font-semibold tracking-tight">
        Instructors.
      </h2>
      <p className="text-cc-prose text-lead mt-4 max-w-2xl">
        Every session is led by ChilliCream engineers who write and maintain the
        products you are training on. When a question slides past the slide
        deck, you get an answer from the people who decided how it works.
      </p>
      <dl className="border-cc-card-border mt-8 grid border-t sm:grid-cols-2">
        <InstructorFact label="Product maintainers">
          Trainers ship on Hot Chocolate, Fusion, and Nitro.
        </InstructorFact>
        <InstructorFact label="Real engagements">
          Years of paid work shaping production GraphQL on .NET.
        </InstructorFact>
        <InstructorFact label="Public speakers">
          Regulars at GraphQL Conf and .NET community events.
        </InstructorFact>
        <InstructorFact label="Honest answers">
          We will tell you what the product does and what it does not.
        </InstructorFact>
      </dl>
    </section>
  );
}

interface InstructorFactProps {
  readonly label: string;
  readonly children: ReactNode;
}

function InstructorFact({ label, children }: InstructorFactProps) {
  return (
    <div className="border-cc-card-border border-b px-0 py-5 sm:px-6 sm:odd:pl-0 sm:even:pr-0">
      <dt className="text-cc-accent font-mono text-xs font-semibold tracking-widest uppercase">
        {label}
      </dt>
      <dd className="text-cc-prose mt-2 text-sm leading-relaxed">{children}</dd>
    </div>
  );
}

function FaqSection() {
  return (
    <section id="faq" className="scroll-mt-28">
      <SectionTag num="06" slug="faq" />
      <h2 className="font-heading text-cc-heading text-h2 font-semibold tracking-tight">
        FAQ.
      </h2>
      <p className="text-cc-prose text-body mt-3 max-w-2xl">
        The questions managers ask first. Straight answers, no hedging.
      </p>
      <div className="divide-cc-card-border border-cc-card-border mt-8 divide-y border-y">
        {FAQ.map((item, index) => (
          <FaqRow
            key={item.q}
            item={item}
            num={(index + 1).toString().padStart(2, "0")}
          />
        ))}
      </div>
    </section>
  );
}

interface FaqRowProps {
  readonly item: FaqItem;
  readonly num: string;
}

function FaqRow({ item, num }: FaqRowProps) {
  return (
    <details className="group py-5" name="training-faq-v5">
      <summary className="grid cursor-pointer list-none grid-cols-[2.5rem_minmax(0,1fr)_auto] items-baseline gap-4">
        <span className="text-cc-ink-dim font-mono text-xs tracking-widest tabular-nums">
          {num}
        </span>
        <span className="text-cc-heading text-base font-medium sm:text-lg">
          {item.q}
        </span>
        <span
          className="text-cc-ink-dim inline-flex shrink-0 transition-transform group-open:rotate-45"
          aria-hidden
        >
          <PlusGlyph />
        </span>
      </summary>
      <p className="text-cc-prose text-body mt-3 pr-10 pl-[3.5rem]">{item.a}</p>
    </details>
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

function ContactSection() {
  return (
    <section id="contact" className="scroll-mt-28">
      <SectionTag num="07" slug="get-in-touch" />
      <h2 className="font-heading text-cc-heading text-h2 font-semibold tracking-tight">
        Get in touch.
      </h2>
      <p className="text-cc-prose text-lead mt-4 max-w-2xl">
        Send a short note with the tracks you are interested in, headcount, and
        your preferred format. We come back with a written proposal and a date.
      </p>
      <div className="mt-8 flex flex-col gap-3 sm:flex-row sm:gap-4">
        <SolidButton href={TRAINING_MAILTO}>Email the team</SolidButton>
        <OutlineButton href="/services/advisory">
          Pair with Advisory
        </OutlineButton>
      </div>
      <p className="text-cc-ink-dim mt-10 font-mono text-[10px] tracking-widest uppercase">
        End of catalogue. {TOC.length.toString().padStart(2, "0")} sections.
      </p>
    </section>
  );
}

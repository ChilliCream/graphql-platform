import type { Metadata } from "next";
import type { ReactNode } from "react";

import { CheckIcon } from "@/src/components/CheckIcon";
import { OutlineButton, SolidButton } from "@/src/design-system/Button";

export const metadata: Metadata = {
  title: "GraphQL Training Workshops | ChilliCream",
  description:
    "GraphQL training workshops led by the team that ships Hot Chocolate, Fusion, Nitro, and Relay. Six tracks, run as corporate training or focused workshop.",
  keywords: [
    "GraphQL training workshops",
    "Hot Chocolate training",
    "Fusion federation training",
    "Relay workshop",
    "ChilliCream training",
    "corporate GraphQL training",
  ],
  openGraph: {
    title: "GraphQL Training Workshops | ChilliCream",
    description:
      "GraphQL training workshops led by the team that ships Hot Chocolate, Fusion, Nitro, and Relay. Six tracks, run as corporate training or focused workshop.",
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
const TOTAL_CHAPTERS = 7;

export default function TrainingPreviewV4Page() {
  return (
    <div className="relative mx-auto max-w-3xl px-4 sm:px-6">
      <RailBackdrop />
      <HeroChapter />
      <PrologueChapter />
      <CurriculumChapter />
      <PullQuoteDivider />
      <DeliveryChapter />
      <OffersChapter />
      <FaqChapter />
      <ClosingChapter />
    </div>
  );
}

function RailBackdrop() {
  return (
    <div
      aria-hidden
      className="bg-cc-card-border pointer-events-none absolute top-0 bottom-0 left-3 hidden w-px sm:left-4 lg:block"
    />
  );
}

interface ChapterAnchorProps {
  readonly index: number;
}

function ChapterAnchor({ index }: ChapterAnchorProps) {
  const label = `${String(index).padStart(2, "0")} / ${String(TOTAL_CHAPTERS).padStart(2, "0")}`;
  return (
    <div
      aria-hidden
      className="text-cc-ink-dim text-caption relative mb-6 flex items-center gap-3 font-mono tracking-widest uppercase"
    >
      <span className="bg-cc-accent inline-block size-2 rounded-full" />
      <span>{label}</span>
    </div>
  );
}

function HeroChapter() {
  return (
    <section className="relative py-24 sm:py-28 lg:py-32">
      <div className="mx-auto max-w-2xl">
        <ChapterAnchor index={1} />
        <div className="text-cc-nav-label text-caption mb-6 font-mono tracking-widest uppercase">
          Training / 2026 catalogue
        </div>
        <h1 className="font-heading text-cc-heading text-hero font-semibold tracking-tight">
          Six tracks. One team that wrote the code.
        </h1>
        <p className="text-cc-prose text-lead mt-8 max-w-2xl">
          A long read on how we run GraphQL training workshops at ChilliCream.
          Pick the tracks your team needs, then decide whether to run them as a
          tailored Corporate Training or as a focused Corporate Workshop on a
          real project.
        </p>
        <div className="mt-10 flex flex-col gap-3 sm:flex-row sm:items-center sm:gap-4">
          <SolidButton href="#curriculum">Browse the catalogue</SolidButton>
          <OutlineButton href={TRAINING_MAILTO}>Talk to us</OutlineButton>
        </div>
      </div>
    </section>
  );
}

function PrologueChapter() {
  return (
    <section className="relative py-24 sm:py-28">
      <div className="mx-auto max-w-2xl">
        <ChapterAnchor index={2} />
        <h2 className="font-heading text-cc-heading text-h2 font-semibold tracking-tight">
          Why training from the maintainers reads differently.
        </h2>
        <p className="text-cc-prose text-body mt-8 leading-relaxed">
          Most GraphQL courses are written about the products. Ours are written
          by the people who decided how those products behave. When an attendee
          asks why Hot Chocolate resolves a field a particular way, or how
          Fusion plans a query across subgraphs, the answer is not a guess from
          the docs. It is the rationale from the room where the decision was
          made.
        </p>
        <p className="text-cc-prose text-body mt-8 leading-relaxed">
          That changes the shape of a session. We can spend less time defending
          the framework and more time on the part that matters to your team: the
          schema you are actually shipping, the resolvers that are actually
          slow, and the rollout you are actually nervous about.
        </p>
        <p className="text-cc-accent font-heading text-lead mt-12 font-semibold tracking-tight">
          Six tracks. Four levels. Two ways to run it. One team behind all of
          it.
        </p>
      </div>
    </section>
  );
}

function CurriculumChapter() {
  return (
    <section id="curriculum" className="relative py-24 sm:py-28">
      <div className="mx-auto max-w-2xl">
        <ChapterAnchor index={3} />
        <h2 className="font-heading text-cc-heading text-h2 font-semibold tracking-tight">
          The curriculum, told as six chapters.
        </h2>
        <p className="text-cc-prose text-lead mt-8">
          Each track is modular. A Corporate Training stitches several tracks
          together across multiple weeks. A Corporate Workshop usually goes deep
          on one or two.
        </p>
      </div>
      <ol className="mx-auto mt-16 max-w-2xl">
        {CURRICULUM.map((track, index) => (
          <TrackChapter key={track.code} track={track} index={index} />
        ))}
      </ol>
    </section>
  );
}

interface TrackChapterProps {
  readonly track: CurriculumTrack;
  readonly index: number;
}

function TrackChapter({ track, index }: TrackChapterProps) {
  const number = String(index + 1).padStart(2, "0");
  return (
    <li className="border-cc-card-border hover:border-cc-card-border-hover relative border-l py-10 pl-6 transition-colors sm:pl-8">
      <div className="text-cc-ink-dim text-caption mb-3 flex flex-wrap items-center gap-3 font-mono tracking-widest uppercase">
        <span className="text-cc-accent">{number}</span>
        <span aria-hidden className="bg-cc-card-border h-px w-6" />
        <span>{track.code}</span>
        <LevelChip level={track.level} />
      </div>
      <h3 className="font-heading text-cc-heading text-h4 font-semibold tracking-tight">
        {track.title}
      </h3>
      <p className="text-cc-prose text-body mt-4 leading-relaxed">
        {track.summary}
      </p>
      <ul className="text-cc-prose mt-6 space-y-3">
        {track.topics.map((topic) => (
          <li key={topic} className="text-body flex items-start gap-3">
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

function PullQuoteDivider() {
  return (
    <section className="relative py-24 sm:py-28">
      <div className="mx-auto max-w-2xl">
        <div className="text-cc-accent text-caption mb-6 font-mono tracking-widest uppercase">
          Aside
        </div>
        <p className="font-heading text-cc-heading text-h3 leading-tight font-semibold tracking-tight">
          Every session is led by the people who decide how it works.
        </p>
      </div>
    </section>
  );
}

function DeliveryChapter() {
  return (
    <section className="relative py-24 sm:py-28">
      <div className="mx-auto max-w-2xl">
        <ChapterAnchor index={4} />
        <h2 className="font-heading text-cc-heading text-h2 font-semibold tracking-tight">
          On-site, remote, or hybrid.
        </h2>
        <p className="text-cc-prose text-lead mt-8">
          The catalogue is the same. The room is up to you. Three sub-chapters
          on how a session can land with your team.
        </p>
        <div className="mt-16 space-y-16">
          {FORMATS.map((format) => (
            <FormatSubChapter key={format.name} format={format} />
          ))}
        </div>
      </div>
    </section>
  );
}

function FormatSubChapter({ format }: { readonly format: DeliveryFormat }) {
  return (
    <article>
      <div className="text-cc-accent text-caption mb-2 font-mono tracking-widest uppercase">
        Format
      </div>
      <h3 className="font-heading text-cc-heading text-h4 font-semibold tracking-tight">
        {format.name}
      </h3>
      <p className="text-cc-prose text-body mt-3 leading-relaxed">
        {format.tagline}
      </p>
      <ul className="text-cc-prose mt-5 space-y-3">
        {format.notes.map((note) => (
          <li key={note} className="text-body flex items-start gap-3">
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

function OffersChapter() {
  return (
    <section className="relative py-24 sm:py-28">
      <div className="mx-auto max-w-2xl">
        <ChapterAnchor index={5} />
        <h2 className="font-heading text-cc-heading text-h2 font-semibold tracking-tight">
          Two ways to run it.
        </h2>
        <p className="text-cc-prose text-lead mt-8">
          Pick the shape that fits how your team learns. The curriculum carries
          across both.
        </p>
        <div className="mt-16 space-y-20">
          {OFFERS.map((offer) => (
            <OfferSubChapter key={offer.name} offer={offer} />
          ))}
        </div>
      </div>
    </section>
  );
}

function OfferSubChapter({ offer }: { readonly offer: Offer }) {
  return (
    <article>
      <div className="mb-3 flex flex-wrap items-center gap-3">
        <span className="text-cc-nav-label text-caption font-mono tracking-widest uppercase">
          Offer
        </span>
        {offer.highlight && (
          <span className="border-cc-accent text-cc-accent rounded-full border px-2.5 py-0.5 font-mono text-[10px] font-semibold tracking-widest uppercase">
            Deep dive
          </span>
        )}
      </div>
      <h3 className="font-heading text-cc-heading text-h4 font-semibold tracking-tight">
        {offer.name}
      </h3>
      <p className="text-cc-ink-dim text-body mt-2">{offer.tagline}</p>
      <p className="text-cc-prose text-body mt-5 leading-relaxed">
        {offer.description}
      </p>
      <ul className="mt-6 space-y-3">
        {offer.perks.map((perk) => (
          <li
            key={perk.text}
            className="text-cc-prose text-body flex items-start gap-3"
          >
            <span
              className="text-cc-accent mt-[3px] inline-flex shrink-0"
              aria-hidden
            >
              <CheckIcon />
            </span>
            <span>{perk.text}</span>
          </li>
        ))}
      </ul>
      <div className="mt-8">
        <InlineCtaLink
          href={`mailto:contact@chillicream.com?subject=${offer.name}`}
        >
          Talk to us about {offer.name}
        </InlineCtaLink>
      </div>
    </article>
  );
}

interface InlineCtaLinkProps {
  readonly href: string;
  readonly children: ReactNode;
}

function InlineCtaLink({ href, children }: InlineCtaLinkProps) {
  return (
    <a
      href={href}
      className="text-cc-accent hover:text-cc-heading text-caption inline-flex items-center gap-2 font-mono font-semibold tracking-widest uppercase transition-colors"
    >
      <span>{children}</span>
      <span aria-hidden>{"->"}</span>
    </a>
  );
}

function FaqChapter() {
  return (
    <section className="relative py-24 sm:py-28">
      <div className="mx-auto max-w-2xl">
        <ChapterAnchor index={6} />
        <h2 className="font-heading text-cc-heading text-h2 font-semibold tracking-tight">
          The questions managers ask first.
        </h2>
        <p className="text-cc-prose text-lead mt-8">
          Straight answers, no hedging.
        </p>
        <div className="divide-cc-card-border border-cc-card-border mt-12 divide-y border-y">
          {FAQ.map((item) => (
            <details key={item.q} className="group py-6" name="training-faq-v4">
              <summary className="flex cursor-pointer list-none items-start justify-between gap-6">
                <span className="font-heading text-cc-heading text-h5 font-semibold tracking-tight">
                  {item.q}
                </span>
                <span
                  className="text-cc-ink-dim mt-1 inline-flex shrink-0 transition-transform group-open:rotate-45"
                  aria-hidden
                >
                  <PlusGlyph />
                </span>
              </summary>
              <p className="text-cc-prose text-body mt-4 pr-10 leading-relaxed">
                {item.a}
              </p>
            </details>
          ))}
        </div>
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

function ClosingChapter() {
  return (
    <section className="relative py-24 sm:py-32">
      <div className="mx-auto max-w-2xl text-center">
        <ChapterAnchor index={7} />
        <div className="text-cc-nav-label text-caption mb-6 font-mono tracking-widest uppercase">
          End of read
        </div>
        <h2 className="font-heading text-cc-heading text-h3 font-semibold tracking-tight">
          Tell us which tracks, we will quote the rest.
        </h2>
        <p className="text-cc-prose text-body mt-6 leading-relaxed">
          Send a short note with the tracks you are interested in, headcount,
          and your preferred format. We come back with a written proposal and a
          date.
        </p>
        <div className="mt-10 flex justify-center">
          <SolidButton href={TRAINING_MAILTO}>Email the team</SolidButton>
        </div>
        <p className="text-cc-ink-dim text-caption mt-8">
          Running an enduring engagement?{" "}
          <a
            href="/services/advisory"
            className="text-cc-accent hover:text-cc-heading transition-colors"
          >
            Pair with Advisory
          </a>
          .
        </p>
      </div>
    </section>
  );
}

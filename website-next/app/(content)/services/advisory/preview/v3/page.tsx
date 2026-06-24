import type { Metadata } from "next";

import { CheckIcon } from "@/src/components/CheckIcon";
import { OutlineButton, SolidButton } from "@/src/design-system/Button";

const CALENDLY_URL = "https://calendly.com/chillicream/60min";
const CONTACT_EMAIL = "contact@chillicream.com";

export const metadata: Metadata = {
  title: "Book a GraphQL Consult with a Core Engineer",
  description:
    "Book a 60-minute GraphQL advisory consult with a Hot Chocolate, Fusion, or Nitro core engineer. Hourly consulting at $300/hour, with a reply within 24 hours.",
  keywords: [
    "GraphQL consulting",
    "GraphQL advisory",
    "Hot Chocolate",
    "Fusion",
    "Nitro",
    "ChilliCream",
    "book a consult",
  ],
  robots: { index: false, follow: false },
  openGraph: {
    title: "Book a GraphQL Consult with a Core Engineer",
    description:
      "Book a 60-minute advisory consult with a Hot Chocolate, Fusion, or Nitro core engineer. Hourly consulting at $300/hour.",
  },
};

interface CapabilityProps {
  readonly label: string;
  readonly description: string;
}

const CAPABILITIES: readonly CapabilityProps[] = [
  {
    label: "Mentoring and guidance",
    description:
      "Pair with a core engineer on the design choices in front of your team this sprint.",
  },
  {
    label: "Architecture",
    description:
      "Pressure-test a schema, gateway topology, or migration plan against real ChilliCream usage.",
  },
  {
    label: "Troubleshooting",
    description:
      "Bring the failing query, the stack trace, or the latency graph. We dig in live.",
  },
  {
    label: "Code review",
    description:
      "Targeted review of resolvers, DataLoaders, subgraphs, or operation pipelines.",
  },
  {
    label: "Best practices",
    description:
      "Pagination, errors, authorization, caching, performance: the patterns we recommend, and why.",
  },
  {
    label: "Proof of concept",
    description:
      "Scoped contracting to prove out a thin slice end to end before you commit the team.",
  },
];

interface BringItemProps {
  readonly title: string;
  readonly detail: string;
}

const BRING_TO_THE_CALL: readonly BringItemProps[] = [
  {
    title: "The actual problem in one paragraph",
    detail:
      "What you are trying to ship, what is blocking, and the deadline you are working against.",
  },
  {
    title: "A repo, schema, or query you can share",
    detail:
      "A link, gist, or screen share beats a description. NDA available on request before the call.",
  },
  {
    title: "The decision you need to make",
    detail:
      "Picking a gateway? Choosing a pagination shape? Naming the call clarifies the hour.",
  },
  {
    title: "Who else should be on the call",
    detail:
      "Up to three teammates is comfortable. Bigger groups, we recommend a follow-up workshop.",
  },
];

interface FaqProps {
  readonly question: string;
  readonly answer: string;
}

const FAQS: readonly FaqProps[] = [
  {
    question: "What does it cost?",
    answer:
      "Consulting is billed at $300 per hour. The first 60-minute consult is booked directly on Calendly, invoiced after the call. Contracting engagements are scoped and quoted separately.",
  },
  {
    question: "How fast can we talk?",
    answer:
      "Most consults are scheduled within 24 hours of booking. If you need same-day, drop a note when you book and we will try to move things around.",
  },
  {
    question: "Can you sign an NDA before we share code?",
    answer:
      "Yes. Send your standard NDA when you book and we will return it signed before the call, or we can countersign ours. Either works.",
  },
  {
    question: "Who actually shows up on the call?",
    answer:
      "A ChilliCream core engineer working on Hot Chocolate, Fusion, or Nitro. We match by topic, not by sales rotation. You will know who is on the call before it starts.",
  },
  {
    question: "What does a good outcome look like?",
    answer:
      "You leave the hour with a clear next step on your specific problem: a design decision made, a bug identified, a review delivered, or a written follow-up if we need a deeper look.",
  },
  {
    question: "What if one hour is not enough?",
    answer:
      "Most teams continue as ongoing hourly consulting. When the scope grows past advisory and into delivery, we move it into a contracting engagement with a fixed scope and timeline.",
  },
];

export default function AdvisoryBookPreviewPage() {
  return (
    <>
      <BookingHero />
      <CapabilityList />
      <TierStrip />
      <BringChecklist />
      <FaqSection />
      <ContactBand />
    </>
  );
}

function BookingHero() {
  return (
    <section className="border-cc-card-border bg-cc-card-bg relative overflow-hidden rounded-3xl border px-6 py-16 sm:px-12 sm:py-20">
      <HeroBackdrop />
      <div className="relative mx-auto max-w-3xl text-center">
        <div className="border-cc-card-border bg-cc-surface/60 text-cc-nav-label mb-5 inline-flex items-center gap-2 rounded-full border px-3 py-1 font-mono text-xs tracking-widest uppercase">
          <PulseDot />
          Advisory, booking open
        </div>
        <h1 className="font-heading text-cc-heading text-4xl font-bold tracking-tight sm:text-5xl lg:text-6xl">
          Book a 60-minute consult
        </h1>
        <p className="text-cc-prose mx-auto mt-6 max-w-2xl text-lg sm:text-xl">
          Talk to a Hot Chocolate, Fusion, or Nitro core engineer about your
          GraphQL problem within 24 hours.
        </p>
        <div className="mt-9 flex flex-col items-center justify-center gap-3 sm:flex-row">
          <SolidButton href={CALENDLY_URL}>
            <span className="flex items-center gap-2">
              <CalendarGlyph />
              Book on Calendly
            </span>
          </SolidButton>
          <OutlineButton href={`mailto:${CONTACT_EMAIL}?subject=Advisory`}>
            Or email us instead
          </OutlineButton>
        </div>
        <dl className="border-cc-card-border mx-auto mt-10 grid max-w-xl grid-cols-3 gap-4 border-t pt-6 text-left sm:gap-8">
          <HeroStat label="Rate" value="$300/hr" />
          <HeroStat label="Reply" value="< 24h" />
          <HeroStat label="Format" value="60 min" />
        </dl>
      </div>
    </section>
  );
}

function HeroBackdrop() {
  return (
    <div className="pointer-events-none absolute inset-0" aria-hidden="true">
      <svg
        className="absolute inset-0 h-full w-full"
        viewBox="0 0 800 400"
        preserveAspectRatio="none"
      >
        <defs>
          <radialGradient id="hero-v3-glow" cx="50%" cy="0%" r="60%">
            <stop offset="0%" stopColor="#5eead4" stopOpacity="0.18" />
            <stop offset="60%" stopColor="#7c92c6" stopOpacity="0.05" />
            <stop offset="100%" stopColor="#0c1322" stopOpacity="0" />
          </radialGradient>
          <linearGradient id="hero-v3-line" x1="0%" y1="0%" x2="100%" y2="0%">
            <stop offset="0%" stopColor="#16b9e4" stopOpacity="0" />
            <stop offset="50%" stopColor="#7c92c6" stopOpacity="0.45" />
            <stop offset="100%" stopColor="#f0786a" stopOpacity="0" />
          </linearGradient>
        </defs>
        <rect width="800" height="400" fill="url(#hero-v3-glow)" />
        <path
          d="M0 320 Q 200 280 400 310 T 800 290"
          fill="none"
          stroke="url(#hero-v3-line)"
          strokeWidth="1"
        />
      </svg>
    </div>
  );
}

function PulseDot() {
  return (
    <span className="relative inline-flex h-2 w-2" aria-hidden="true">
      <span className="bg-cc-accent absolute inline-flex h-full w-full animate-ping rounded-full opacity-60" />
      <span className="bg-cc-accent relative inline-flex h-2 w-2 rounded-full" />
    </span>
  );
}

function CalendarGlyph() {
  return (
    <svg
      width="14"
      height="14"
      viewBox="0 0 16 16"
      fill="none"
      aria-hidden="true"
    >
      <rect
        x="2"
        y="3"
        width="12"
        height="11"
        rx="2"
        stroke="currentColor"
        strokeWidth="1.5"
      />
      <path
        d="M2 6 H14"
        stroke="currentColor"
        strokeWidth="1.5"
        strokeLinecap="round"
      />
      <path
        d="M6 2 V4 M10 2 V4"
        stroke="currentColor"
        strokeWidth="1.5"
        strokeLinecap="round"
      />
    </svg>
  );
}

interface HeroStatProps {
  readonly label: string;
  readonly value: string;
}

function HeroStat({ label, value }: HeroStatProps) {
  return (
    <div>
      <dt className="text-cc-nav-label font-mono text-[0.65rem] tracking-widest uppercase">
        {label}
      </dt>
      <dd className="font-heading text-cc-heading mt-1 text-xl font-semibold sm:text-2xl">
        {value}
      </dd>
    </div>
  );
}

function SectionEyebrow({ children }: { children: React.ReactNode }) {
  return (
    <div className="text-cc-nav-label mb-4 font-mono text-xs tracking-widest uppercase">
      {children}
    </div>
  );
}

function CapabilityList() {
  return (
    <section className="mt-20">
      <div className="mx-auto max-w-3xl text-center">
        <SectionEyebrow>What we cover</SectionEyebrow>
        <h2 className="font-heading text-cc-heading text-3xl font-semibold tracking-tight sm:text-4xl">
          Six things one hour can move forward
        </h2>
        <p className="text-cc-prose mt-4">
          A consult is the cheapest way to find out whether your idea is sound,
          whether your bug is in your code or ours, and what the right next step
          is.
        </p>
      </div>
      <ol className="border-cc-card-border bg-cc-card-border mt-10 grid gap-px overflow-hidden rounded-2xl border sm:grid-cols-2 lg:grid-cols-3">
        {CAPABILITIES.map((capability, index) => (
          <CapabilityCell
            key={capability.label}
            index={index + 1}
            label={capability.label}
            description={capability.description}
          />
        ))}
      </ol>
    </section>
  );
}

interface CapabilityCellProps {
  readonly index: number;
  readonly label: string;
  readonly description: string;
}

function CapabilityCell({ index, label, description }: CapabilityCellProps) {
  return (
    <li className="group bg-cc-surface hover:bg-cc-surface/80 relative p-6 transition-colors">
      <div className="flex items-baseline gap-3">
        <span className="text-cc-accent font-mono text-xs tracking-widest">
          {String(index).padStart(2, "0")}
        </span>
        <h3 className="font-heading text-cc-heading text-lg font-semibold">
          {label}
        </h3>
      </div>
      <p className="text-cc-prose mt-3 text-sm">{description}</p>
    </li>
  );
}

function TierStrip() {
  return (
    <section className="mt-20">
      <div className="mx-auto max-w-3xl text-center">
        <SectionEyebrow>Engagement shape</SectionEyebrow>
        <h2 className="font-heading text-cc-heading text-3xl font-semibold tracking-tight sm:text-4xl">
          Two ways to work with us
        </h2>
        <p className="text-cc-prose mt-4">
          Most teams start on the consulting side. When the work outgrows
          advisory, we move it into a contracting engagement.
        </p>
      </div>
      <div className="mt-10 grid gap-6 md:grid-cols-2">
        <TierCard
          title="Consulting"
          tag="Hourly, advisory"
          description="Hourly consulting services to get the help you need at any stage of your project. This is the best way to get started."
          perks={[
            "Mentoring and guidance",
            "Architecture",
            "Troubleshooting",
            "Code Review",
            "Best practices education",
          ]}
          ctaText="Book a consult"
          ctaHref={CALENDLY_URL}
          secondaryText="Email about consulting"
          secondaryHref={`mailto:${CONTACT_EMAIL}?subject=Consulting`}
          accent="accent"
        />
        <TierCard
          title="Contracting"
          tag="Scoped delivery"
          description="Options for teams who do not have the time, bandwidth, and/or expertise to implement their own GraphQL solutions."
          perks={["Proof of concept", "Implementation"]}
          ctaText="Talk about contracting"
          ctaHref={`mailto:${CONTACT_EMAIL}?subject=Contracting`}
          secondaryText="Or start with a consult"
          secondaryHref={CALENDLY_URL}
          accent="warm"
        />
      </div>
    </section>
  );
}

interface TierCardProps {
  readonly title: string;
  readonly tag: string;
  readonly description: string;
  readonly perks: readonly string[];
  readonly ctaText: string;
  readonly ctaHref: string;
  readonly secondaryText: string;
  readonly secondaryHref: string;
  readonly accent: "accent" | "warm";
}

function TierCard({
  title,
  tag,
  description,
  perks,
  ctaText,
  ctaHref,
  secondaryText,
  secondaryHref,
  accent,
}: TierCardProps) {
  const accentBorder =
    accent === "accent"
      ? "before:bg-cc-accent"
      : "before:bg-[oklch(70.4%_0.191_22.216)]";
  return (
    <article
      className={`border-cc-card-border bg-cc-card-bg relative overflow-hidden rounded-2xl border p-7 before:absolute before:inset-x-0 before:top-0 before:h-px ${accentBorder}`}
    >
      <div className="flex items-center justify-between">
        <h3 className="font-heading text-cc-heading text-2xl font-semibold">
          {title}
        </h3>
        <span className="text-cc-nav-label font-mono text-[0.65rem] tracking-widest uppercase">
          {tag}
        </span>
      </div>
      <p className="text-cc-prose mt-3 text-sm">{description}</p>
      <ul className="text-cc-ink mt-5 space-y-2 text-sm">
        {perks.map((perk) => (
          <li key={perk} className="flex items-start gap-3">
            <span className="text-cc-accent mt-1">
              <CheckIcon />
            </span>
            <span>{perk}</span>
          </li>
        ))}
      </ul>
      <div className="mt-7 flex flex-col gap-2 sm:flex-row">
        <SolidButton href={ctaHref}>{ctaText}</SolidButton>
        <OutlineButton href={secondaryHref}>{secondaryText}</OutlineButton>
      </div>
    </article>
  );
}

function BringChecklist() {
  return (
    <section className="border-cc-card-border bg-cc-surface/60 mt-20 overflow-hidden rounded-2xl border">
      <div className="grid gap-0 md:grid-cols-[1fr_1.4fr]">
        <div className="border-cc-card-border p-8 md:border-r md:p-10">
          <SectionEyebrow>What you bring</SectionEyebrow>
          <h2 className="font-heading text-cc-heading text-3xl font-semibold tracking-tight sm:text-4xl">
            Make the hour count
          </h2>
          <p className="text-cc-prose mt-4">
            A consult is more useful when we walk in knowing the shape of the
            problem. None of this is required, but each item shortens the loop.
          </p>
          <div className="mt-6">
            <SolidButton href={CALENDLY_URL}>Pick a time</SolidButton>
          </div>
        </div>
        <ul className="divide-cc-card-border divide-y">
          {BRING_TO_THE_CALL.map((item) => (
            <li key={item.title} className="flex items-start gap-4 p-6 sm:p-8">
              <span className="text-cc-accent mt-1" aria-hidden="true">
                <CheckIcon size={16} />
              </span>
              <div>
                <h3 className="font-heading text-cc-heading text-base font-semibold">
                  {item.title}
                </h3>
                <p className="text-cc-prose mt-1 text-sm">{item.detail}</p>
              </div>
            </li>
          ))}
        </ul>
      </div>
    </section>
  );
}

function FaqSection() {
  return (
    <section className="mt-20">
      <div className="mx-auto max-w-3xl text-center">
        <SectionEyebrow>FAQ</SectionEyebrow>
        <h2 className="font-heading text-cc-heading text-3xl font-semibold tracking-tight sm:text-4xl">
          Straight answers before you book
        </h2>
      </div>
      <dl className="mt-10 grid gap-4 md:grid-cols-2">
        {FAQS.map((faq) => (
          <div
            key={faq.question}
            className="border-cc-card-border bg-cc-card-bg rounded-xl border p-6"
          >
            <dt className="font-heading text-cc-heading text-base font-semibold">
              {faq.question}
            </dt>
            <dd className="text-cc-prose mt-2 text-sm">{faq.answer}</dd>
          </div>
        ))}
      </dl>
    </section>
  );
}

function ContactBand() {
  return (
    <section className="border-cc-card-border bg-cc-card-bg mt-20 mb-8 overflow-hidden rounded-3xl border">
      <div className="grid items-center gap-6 px-8 py-10 md:grid-cols-[1.4fr_1fr] md:px-12 md:py-12">
        <div>
          <SectionEyebrow>Ready when you are</SectionEyebrow>
          <h2 className="font-heading text-cc-heading text-3xl font-semibold tracking-tight sm:text-4xl">
            One call. One clear next step.
          </h2>
          <p className="text-cc-prose mt-4 max-w-xl">
            Sixty minutes with a core engineer is usually enough to unblock the
            decision in front of you. If it is not, we will tell you, and tell
            you what would be.
          </p>
        </div>
        <div className="flex flex-col gap-3 md:items-end">
          <SolidButton href={CALENDLY_URL}>
            <span className="flex items-center gap-2">
              <CalendarGlyph />
              Book a 60-minute consult
            </span>
          </SolidButton>
          <OutlineButton href={`mailto:${CONTACT_EMAIL}?subject=Advisory`}>
            contact@chillicream.com
          </OutlineButton>
        </div>
      </div>
    </section>
  );
}
